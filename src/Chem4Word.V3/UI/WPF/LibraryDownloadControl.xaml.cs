// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Helpers;
using Chem4Word.Models;
using Chem4Word.Shared;
using IChem4Word.Contracts;
using Meziantou.Framework.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for LibraryDownloadControl.xaml
    /// </summary>
    public partial class LibraryDownloadControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private const string Chem4WordUser = "Chem4Word-User";

        public Point TopLeft { get; set; }

        public event EventHandler OnButtonClick;

        private AzureSettings _settings;

        private List<CatalogueEntry> _catalogue;

        private string _downloadPath;

        public LibraryDownloadControl()
        {
            InitializeComponent();
        }

        private void OnLoaded_LibraryDownloadControl(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Obtaining list of libraries available for download");

            using (new WaitCursor())
            {
                try
                {
                    _settings = new AzureSettings(true);

                    // Restore User Name and Email from Credential Store
                    var lastUser = CredentialManager.ReadCredential(Chem4WordUser);
                    if (lastUser != null
                        && lastUser.UserName != null)
                    {
                        var temp = lastUser.UserName.Replace("<", "|").Replace(">", "");
                        var parts = temp.Split('|');
                        if (parts.Length == 2)
                        {
                            UserName.Text = parts[0];
                            UserEmail.Text = parts[1];
                        }
                    }

                    var formData = new Dictionary<string, string>
                               {
                                   { "version", Globals.Chem4WordV3.Helper.AddInVersion },
                                   { "machineid", Globals.Chem4WordV3.Helper.MachineId }
                               };

                    var helper = new ApiHelper(_settings.LibrariesUri);

                    // Dummy call to wake up API
                    var dummy = helper.GetCatalogue(formData, 15);
                    if (!dummy.Success)
                    {
                        Thread.Sleep(500);
                    }

                    var result = helper.GetCatalogue(formData, 15);
                    if (result.Success)
                    {
                        _catalogue = result.Catalogue;

                        var data = new List<LibraryDownloadGridSource>();
                        foreach (var entry in result.Catalogue)
                        {
                            var obj = new LibraryDownloadGridSource
                            {
                                Name = entry.Name,
                                Description = entry.Description
                            };
                            data.Add(obj);
                        }
                        Libraries.ItemsSource = data;
                    }
                    else
                    {
                        var lines = result.Message.Split(Environment.NewLine.ToCharArray());
                        StatusMessage.Text = $"{lines.Last()}";
                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception", result.Message);
                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"HasException {result.HasException}");
                        Globals.Chem4WordV3.Telemetry.Write(module, "Exception", $"HttpStatusCode {result.HttpStatusCode}");
                    }
                }
                catch (Exception exception)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.Message);
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.StackTrace);
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false; // suggested by @TK-421
            }

            if (trimmedEmail.Contains(".."))
            {
                return false;
            }

            try
            {
                var address = new MailAddress(trimmedEmail);
                return address.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        private void OnClick_DownloadButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Clicked");

            var validated = true;

            if (UserName.Text.Trim().Length == 0)
            {
                UserErrorMessage.Visibility = Visibility.Visible;
                UserErrorMessage.Text = "Please enter your name.";
                validated = false;
            }
            else
            {
                UserErrorMessage.Visibility = Visibility.Collapsed;
            }
            if (UserEmail.Text.Trim().Length == 0 || !IsValidEmail(UserEmail.Text))
            {
                EmailErrorMessage.Visibility = Visibility.Visible;
                EmailErrorMessage.Text = "Please enter a valid email address.";
                validated = false;
            }
            else
            {
                EmailErrorMessage.Visibility = Visibility.Collapsed;
            }

            if (validated
                && Libraries.SelectedItem is LibraryDownloadGridSource data)
            {
                var library = _catalogue.FirstOrDefault(l => l.Name.Equals(data.Name));
                if (library != null)
                {
                    var existing = Globals.Chem4WordV3.ListOfDetectedLibraries.AvailableDatabases
                                          .Where(l => l.ShortFileName.Equals(library.OriginalFileName)
                                                      || l.DisplayName.Equals(library.Name)).ToList();
                    if (existing.Count > 0)
                    {
                        var answer = UserInteractions.AskUserYesNo("You already have a library with this name, do you wish to proceed?", MessageBoxDefaultButton.Button2);
                        if (answer == DialogResult.No)
                        {
                            // Abort download
                            return;
                        }
                    }

                    using (new WaitCursor())
                    {
                        var formData = new Dictionary<string, string>
                                       {
                                           { "version", Globals.Chem4WordV3.Helper.AddInVersion },
                                           { "machineid", Globals.Chem4WordV3.Helper.MachineId },
                                           { "machinename", Environment.MachineName},
                                           { "customer", UserName.Text },
                                           { "email", UserEmail.Text },
                                           { "ipaddress", Globals.Chem4WordV3.Helper.IpAddress },
                                           { "library", library.Id },
                                           { "libraryname", library.Name },
                                           { "driver", library.Driver }
                                       };
                        var helper = new ApiHelper(_settings.LibrariesUri);

                        var response = helper.RequestLibraryDetails(formData, 15);
                        if (response.Success)
                        {
                            if (response.Details.Allowed)
                            {
                                _downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);

                                DisableControls();
                                DownloadLibrary(formData);
                                if (File.Exists(Path.Combine(_downloadPath, $"{library.Id}.zip")))
                                {
                                    InstallLibrary(library, _downloadPath);
                                }
                                if (!library.Driver.Equals("SQLite Standard"))
                                {
                                    DownloadDriver(formData);
                                    if (File.Exists(Path.Combine(_downloadPath, $"{library.Driver}.zip")))
                                    {
                                        InstallDriver(library.Driver, _downloadPath);
                                    }
                                }

                                // Once library and driver have been installed add/replace user details in credential store
                                // Common values required for licence verification by the driver.
                                CredentialManager.WriteCredential(Chem4WordUser, $"{UserName.Text}<{UserEmail.Text}>", Globals.Chem4WordV3.Helper.MachineId, CredentialPersistence.LocalMachine);
                            }
                            else
                            {
                                StatusMessage.Text = response.Details.Message;
                            }
                        }
                        else
                        {
                            StatusMessage.Text = response.Message;
                        }
                        EnableControls();
                    }
                }
            }
        }

        private void InstallDriver(string driverName, string downloadPath)
        {
            var driversPath = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, "Plugins");

            var zipFileName = Path.Combine(downloadPath, $"{driverName}.zip");
            var zipStream = File.OpenRead(zipFileName);
            var archive = new ZipArchive(zipStream);

            var entries = archive.Entries;
            foreach (var entry in entries)
            {
                var completeFileName = Path.Combine(driversPath, entry.FullName);
                if (!File.Exists(completeFileName))
                {
                    entry.ExtractToFile(completeFileName);
                }
                else
                {
                    entry.ExtractToFile(Path.Combine(driversPath, "Updates", entry.FullName));
                }
            }

            archive.Dispose();
            File.Delete(zipFileName);
        }

        private void InstallLibrary(CatalogueEntry library, string downloadPath)
        {
            var librariesPath = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, "Libraries");

            var zipFileName = Path.Combine(downloadPath, $"{library.Id}.zip");
            var zipStream = File.OpenRead(zipFileName);
            var archive = new ZipArchive(zipStream);
            archive.ExtractToDirectory(librariesPath);

            var librarySource = Path.Combine(librariesPath, $"{library.Id}.db");
            var libraryDestination = Path.Combine(librariesPath, library.OriginalFileName);

            // Delete target if it exists
            if (File.Exists(libraryDestination))
            {
                File.Delete(libraryDestination);
            }
            // Rename database file
            File.Move(librarySource, libraryDestination);

            var licenseSource = Path.Combine(librariesPath, $"{library.Id}.lic");
            var licenseDestination = Path.Combine(librariesPath, library.OriginalFileName).Replace(".db", ".lic");

            if (File.Exists(licenseSource))
            {
                // Delete target if it exists
                if (File.Exists(licenseDestination))
                {
                    File.Delete(licenseDestination);
                }

                // Rename licence file
                File.Move(licenseSource, licenseDestination);
            }

            archive.Dispose();
            File.Delete(zipFileName);

            var listOfDetectedLibraries = Globals.Chem4WordV3.ListOfDetectedLibraries;
            var details = listOfDetectedLibraries.AvailableDatabases.FirstOrDefault(l => l.DisplayName.Equals(library.Name));
            if (details == null)
            {
                details = new DatabaseDetails
                {
                    Driver = library.Driver,
                    DisplayName = library.Name,
                    Connection = Path.Combine(librariesPath, library.OriginalFileName),
                    ShortFileName = library.OriginalFileName
                };
                listOfDetectedLibraries.AvailableDatabases.Add(details);
                new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                    .SaveFile(listOfDetectedLibraries);
            }
        }

        private void EnableControls()
        {
            Download.IsEnabled = Libraries.SelectedItems.Count > 0;
            Finished.IsEnabled = true;
            Libraries.IsEnabled = true;
            UserName.IsEnabled = true;
            UserEmail.IsEnabled = true;
        }

        private void DisableControls()
        {
            Download.IsEnabled = false;
            Finished.IsEnabled = false;
            Libraries.IsEnabled = false;
            UserName.IsEnabled = false;
            UserEmail.IsEnabled = false;
        }

        private void StopProgressIndicator()
        {
            ProgressBar.BeginAnimation(RangeBase.ValueProperty, null);
        }

        private void StartProgressIndicator()
        {
            // Give the user something to look at while the download occurs

            var duration = new Duration(TimeSpan.FromSeconds(5));
            var animation = new DoubleAnimation
            {
                Duration = duration,
                From = 0,
                To = 100
            };
            Storyboard.SetTarget(animation, ProgressBar);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RangeBase.ValueProperty));
            var storyboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StopProgressIndicator();
            StatusMessage.Text = "";
        }

        private void DownloadLibrary(Dictionary<string, string> formData)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            StatusMessage.Text = "Downloading library";
            StartProgressIndicator();

            var worker = new BackgroundWorker();
            worker.DoWork += WorkDownloadLibrary;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;

            worker.RunWorkerAsync(formData);

            while (worker.IsBusy)
            {
                Thread.Sleep(1);
                DoWpfEvents();
            }

            stopwatch.Stop();
            Debug.WriteLine($"{stopwatch.Elapsed}");
        }

        private void WorkDownloadLibrary(object sender, DoWorkEventArgs e)
        {
            var helper = new ApiHelper(_settings.LibrariesUri);
            helper.DownloadLibrary((Dictionary<string, string>)e.Argument, _downloadPath, 60);
        }

        private void DownloadDriver(Dictionary<string, string> formData)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            StatusMessage.Text = "Downloading driver";
            StartProgressIndicator();

            var worker = new BackgroundWorker();
            worker.DoWork += WorkDownloadDriver;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;

            worker.RunWorkerAsync(formData);

            while (worker.IsBusy)
            {
                Thread.Sleep(1);
                DoWpfEvents();
            }

            stopwatch.Stop();
            Debug.WriteLine($"{stopwatch.Elapsed}");
        }

        private void WorkDownloadDriver(object sender, DoWorkEventArgs e)
        {
            var helper = new ApiHelper(_settings.LibrariesUri);
            helper.DownloadDriver((Dictionary<string, string>)e.Argument, _downloadPath, 15);
        }

        private static void DoWpfEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                                                     new DispatcherOperationCallback(
                                                         delegate (object f)
                                                         {
                                                             ((DispatcherFrame)f).Continue = false;
                                                             return null;
                                                         }), frame);
            Dispatcher.PushFrame(frame);
        }

        private void OnClick_FinishedButton(object sender, RoutedEventArgs e)
        {
            var args = new WpfEventArgs
            {
                Button = "Finished",
                OutputValue = ""
            };

            OnButtonClick?.Invoke(this, args);
        }

        private void OnSelectionChanged_ListOfLibraries(object sender, SelectionChangedEventArgs e)
        {
            Download.IsEnabled = Libraries.SelectedItems.Count > 0;
        }
    }
}