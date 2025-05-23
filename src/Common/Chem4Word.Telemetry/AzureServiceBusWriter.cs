﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Azure.Messaging.ServiceBus;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chem4Word.Telemetry
{
    public class AzureServiceBusWriter
    {
        // The Service Bus client types are safe to cache and use as a singleton for the lifetime
        //  of the application, which is best practice when messages are being published or read regularly.

        // The client that owns the connection and can be used to create senders and receivers
        private readonly ServiceBusClient _client;

        // The sender used to publish messages to the queue
        private readonly ServiceBusSender _sender;

        private AzureSettings _settings;

        private static readonly object QueueLock = Guid.NewGuid();

        private readonly Queue<OutputMessage> _mainBuffer = new Queue<OutputMessage>();
        private readonly Queue<OutputMessage> _secondaryBuffer = new Queue<OutputMessage>();

        private bool _running = false;

        public int BufferCount => _mainBuffer.Count + _secondaryBuffer.Count;

        public AzureServiceBusWriter(AzureSettings settings)
        {
            _settings = settings;

            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            if (!string.IsNullOrEmpty(_settings.ServiceBusQueue))
            {
                try
                {
                    // Set the transport type to AmqpWebSockets so that the ServiceBusClient uses the port 443.
                    // If you use the default AmqpTcp, you will need to make sure that the ports 5671 and 5672 are open.
                    var clientOptions = new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets };

                    _client = new ServiceBusClient($"{_settings.ServiceBusEndPoint};{_settings.ServiceBusToken}", clientOptions);
                    _sender = _client.CreateSender(_settings.ServiceBusQueue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debugger.Break();
                    // Do nothing
                }
            }
        }

        public void QueueMessage(OutputMessage message)
        {
            lock (QueueLock)
            {
                _mainBuffer.Enqueue(message);
                Monitor.PulseAll(QueueLock);
            }

            if (!_running)
            {
                var t = new Thread(WriteOnThread);
                t.SetApartmentState(ApartmentState.STA);
                _running = true;
                t.Start();
            }
        }

        private void WriteOnThread()
        {
            // Small sleep before we start
            Thread.Sleep(25);

            while (_running)
            {
                // Move messages from 1st stage buffer to 2nd stage buffer
                lock (QueueLock)
                {
                    while (_mainBuffer.Count > 0)
                    {
                        _secondaryBuffer.Enqueue(_mainBuffer.Dequeue());
                    }
                    Monitor.PulseAll(QueueLock);
                }

                while (_secondaryBuffer.Count > 0)
                {
                    var task = WriteMessage(_secondaryBuffer.Dequeue());
                    task.Wait();

                    // Small micro sleep between each message
                    Thread.Sleep(5);
                }

                lock (QueueLock)
                {
                    if (_mainBuffer.Count == 0)
                    {
                        _running = false;
                    }
                    Monitor.PulseAll(QueueLock);
                }
            }
        }

        public void SendZipFileMessage(ServiceBusMessage message)
        {
            _sender.SendMessageAsync(message);
        }

        private async Task WriteMessage(OutputMessage message)
        {
            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                if (_sender != null)
                {
                    using (var messageBatch = await _sender.CreateMessageBatchAsync())
                    {
                        var msg = new ServiceBusMessage(message.Message);
                        msg.ApplicationProperties.Add("PartitionKey", message.PartitionKey);
                        msg.ApplicationProperties.Add("RowKey", message.RowKey);
                        msg.ApplicationProperties.Add("Chem4WordVersion", message.AssemblyVersionNumber);
                        msg.ApplicationProperties.Add("MachineId", message.MachineId);
                        msg.ApplicationProperties.Add("Operation", message.Operation);
                        msg.ApplicationProperties.Add("Level", message.Level);
#if DEBUG
                        msg.ApplicationProperties.Add("IsDebug", "True");
#endif

                        if (!messageBatch.TryAddMessage(msg))
                        {
                            Debugger.Break();
                        }

                        await _sender.SendMessagesAsync(messageBatch);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Exception in WriteMessage: {exception.Message}");
                Debugger.Break();

                try
                {
                    var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                $@"Chem4Word.V3\Telemetry\{SafeDate.ToIsoShortDate(DateTime.UtcNow)}.log");
                    using (var streamWriter = File.AppendText(fileName))
                    {
                        await streamWriter.WriteLineAsync($"[{SafeDate.ToShortTime(DateTime.UtcNow)}] Exception in WriteMessage: {exception.Message}");
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }
    }
}