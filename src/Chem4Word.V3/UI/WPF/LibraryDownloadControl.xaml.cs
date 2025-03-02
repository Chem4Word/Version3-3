﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI;
using Chem4Word.Core.UI.Forms;
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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Point = System.Windows.Point;
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
        private List<string> _paidFor;

        private string _downloadPath;
        private bool _userIsDirty;

        public LibraryDownloadControl()
        {
            InitializeComponent();
        }

        private void OnLoaded_LibraryDownloadControl(object sender, RoutedEventArgs e)
        {
            UserErrorMessage.Visibility = Visibility.Collapsed;
            EmailErrorMessage.Visibility = Visibility.Collapsed;

            RefreshListOfLibraries();
        }

        private void RefreshListOfLibraries()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Obtaining list of libraries available for download");

            using (new WaitCursor())
            {
                try
                {
                    _settings = new AzureSettings(true);

                    if (_userIsDirty)
                    {
                        if (!string.IsNullOrEmpty(UserName.Text) && StringHelper.IsValidEmail(UserEmail.Text))
                        {
                            CredentialManager.WriteCredential(Chem4WordUser,
                                                              $"{UserName.Text}<{UserEmail.Text.ToLower()}>",
                                                              Globals.Chem4WordV3.Helper.MachineId,
                                                              CredentialPersistence.LocalMachine);
                        }
                        else
                        {
                            var credential = CredentialManager.ReadCredential(Chem4WordUser);
                            if (credential != null)
                            {
                                CredentialManager.DeleteCredential(Chem4WordUser);
                            }
                        }

                        Libraries.UnselectAll();
                        Download.IsEnabled = false;
                        _userIsDirty = false;
                    }
                    else
                    {
                        // Restore UserName and Email from Credential Store
                        var credential = CredentialManager.ReadCredential(Chem4WordUser);
                        if (credential != null && credential.UserName != null)
                        {
                            var temp = credential.UserName.Replace("<", "|").Replace(">", "");
                            var parts = temp.Split('|');
                            if (parts.Length == 2)
                            {
                                UserName.Text = parts[0].Trim();
                                UserEmail.Text = parts[1].ToLower();
                                _userIsDirty = false;
                            }
                        }
                    }

                    var formData = new Dictionary<string, string>
                                   {
                                       { "Version", Globals.Chem4WordV3.Helper.AddInVersion.Replace("Chem4Word V", "") },
                                       { "MachineId", Globals.Chem4WordV3.Helper.MachineId }
                                   };

                    var helper = new ApiHelper(_settings.LibrariesUri, Globals.Chem4WordV3.Telemetry);

#if DEBUG
                    // Dummy call to wake up API in debug mode

                    var dummy = helper.GetCatalogue(formData, 5);
                    if (!dummy.Any())
                    {
                        Thread.Sleep(500);
                    }
#endif
                    _catalogue = helper.GetCatalogue(formData, 15);
                    var data = new List<LibraryDownloadGridSource>();
                    foreach (var entry in _catalogue)
                    {
                        var obj = new LibraryDownloadGridSource
                        {
                            Sku = entry.Id,
                            Name = entry.Name,
                            Description = entry.Description,
                            RequiresPayment = !entry.Driver.Equals(Constants.SQLiteStandardDriver)
                        };
                        data.Add(obj);
                    }
                    Libraries.ItemsSource = data;

                    RefreshPaidFor();
                }
                catch (Exception exception)
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.Message);
                    Globals.Chem4WordV3.Telemetry.Write(module, "Exception", exception.StackTrace);
                }
            }
        }

        private void RefreshPaidFor()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Obtaining list of libraries paid for by this user");

            _paidFor = new List<string>();

            var email = UserEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email))
            {
                var formData = new Dictionary<string, string>
                {
                    { "Version", Globals.Chem4WordV3.Helper.AddInVersion.Replace("Chem4Word V", "") },
                    { "MachineId", Globals.Chem4WordV3.Helper.MachineId },
                    { "Email", email}
                };

                var helper = new ApiHelper(_settings.LibrariesUri, Globals.Chem4WordV3.Telemetry);
                var result = helper.GetPaidFor(formData, 10);
                if (result.Any())
                {
                    _paidFor = result.Select(c => c.Id).ToList();
                }
            }
        }

        private void OnClick_DownloadButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

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

            if (UserEmail.Text.Trim().Length == 0 || !StringHelper.IsValidEmail(UserEmail.Text.Trim()))
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
                                           { "Version", Globals.Chem4WordV3.Helper.AddInVersion.Replace("Chem4Word V", "") },
                                           { "MachineId", Globals.Chem4WordV3.Helper.MachineId },
                                           { "MachineName", Environment.MachineName},
                                           { "Customer", UserName.Text },
                                           { "Email", UserEmail.Text.ToLower() },
                                           { "IpAddress", Globals.Chem4WordV3.Helper.IpAddress.Replace("IpAddress ", "") },
                                           { "Library", library.Id },
                                           { "LibraryName", library.Name },
                                           { "Driver", library.Driver }
                                       };
                        var helper = new ApiHelper(_settings.LibrariesUri, Globals.Chem4WordV3.Telemetry);

                        var response = helper.RequestLibraryDetails(formData, 15);

                        if (response.Allowed)
                        {
                            _downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);

                            DisableControls();
                            DownloadLibrary(formData);
                            if (File.Exists(Path.Combine(_downloadPath, $"{library.Name}.zip")))
                            {
                                InstallLibrary(library, _downloadPath);
                            }

                            // Once library and driver have been installed add/update user details in the Windows credential store
                            // Common values required for license verification by the driver.
                            if (_userIsDirty)
                            {
                                CredentialManager.WriteCredential(Chem4WordUser,
                                                                  $"{UserName.Text}<{UserEmail.Text.ToLower()}>",
                                                                  Globals.Chem4WordV3.Helper.MachineId,
                                                                  CredentialPersistence.LocalMachine);
                                _userIsDirty = false;
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
                    entry.ExtractToFile(Path.Combine(driversPath, "Updates", entry.FullName), overwrite: true);
                }
            }

            archive.Dispose();
            File.Delete(zipFileName);
        }

        private void InstallLibrary(CatalogueEntry library, string downloadPath)
        {
            var librariesPath = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, "Libraries");

            var zipFileName = Path.Combine(downloadPath, $"{library.Name}.zip");
            var zipStream = File.OpenRead(zipFileName);
            var archive = new ZipArchive(zipStream);
            archive.ExtractToDirectory(librariesPath);

            var librarySource = Path.Combine(librariesPath, $"{library.Id}.db");
            var libraryDestination = Path.Combine(librariesPath, $"{library.Name}.db");

            // Delete target if it exists
            if (File.Exists(libraryDestination))
            {
                File.Delete(libraryDestination);
            }
            // Rename database file
            File.Move(librarySource, libraryDestination);

            var licenseSource = Path.Combine(librariesPath, $"{library.Id}.lic");
            var licenseDestination = Path.Combine(librariesPath, $"{library.Name}.lic");

            if (File.Exists(licenseSource))
            {
                // Delete target if it exists
                if (File.Exists(licenseDestination))
                {
                    File.Delete(licenseDestination);
                }

                // Rename license file
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
                    Connection = Path.Combine(librariesPath, $"{library.Name}.db"),
                    ShortFileName = $"{library.Name}.db"
                };
                listOfDetectedLibraries.AvailableDatabases.Add(details);
                new LibraryFileHelper(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.AddInInfo.ProgramDataPath)
                    .SaveSettingsFile(listOfDetectedLibraries);
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            StatusMessage.Text = "Downloading library";
            StartProgressIndicator();

            var worker = new BackgroundWorker();
            worker.DoWork += OnDoWork_DownloadLibrary;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;

            worker.RunWorkerAsync(formData);

            while (worker.IsBusy)
            {
                Thread.Sleep(1);
                DoWpfEvents();
            }

            stopwatch.Stop();
            Debug.WriteLine($"{stopwatch.Elapsed}");

            var downloadedFile = Path.Combine(_downloadPath, $"{formData["LibraryName"]}.zip");
            if (File.Exists(downloadedFile))
            {
                var fileInfo = new FileInfo(downloadedFile);

                Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Downloading of '{fileInfo.Name}' took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)}ms");
            }
        }

        private void OnDoWork_DownloadLibrary(object sender, DoWorkEventArgs e)
        {
            var helper = new ApiHelper(_settings.LibrariesUri, Globals.Chem4WordV3.Telemetry);
            helper.DownloadLibrary((Dictionary<string, string>)e.Argument, _downloadPath, 60);
        }

        private void DownloadDriver(Dictionary<string, string> formData)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            StatusMessage.Text = "Downloading driver";
            StartProgressIndicator();

            var worker = new BackgroundWorker();
            worker.DoWork += OnDoWork_DownloadDriver;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;

            worker.RunWorkerAsync(formData);

            while (worker.IsBusy)
            {
                Thread.Sleep(1);
                DoWpfEvents();
            }

            stopwatch.Stop();
            Debug.WriteLine($"{stopwatch.Elapsed}");

            var downloadedFile = Path.Combine(_downloadPath, $"{formData["Driver"]}.zip");
            if (File.Exists(downloadedFile))
            {
                var fileInfo = new FileInfo(downloadedFile);

                Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Downloading of '{fileInfo.Name}' took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)}ms");
            }
        }

        private void OnDoWork_DownloadDriver(object sender, DoWorkEventArgs e)
        {
            var helper = new ApiHelper(_settings.LibrariesUri, Globals.Chem4WordV3.Telemetry);
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

        private void OnClick_BuyButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                if (Libraries.SelectedItem is LibraryDownloadGridSource data)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"Please purchase the library '{data.Name}' from our shop.");
                    stringBuilder.AppendLine("NB: Some libraries are FREE!");
                    stringBuilder.AppendLine($"Clicking OK will open your default browser with library '{data.Name}' selected.");
                    stringBuilder.AppendLine("After you receive an email confirming your purchase, return here to download it.");
                    var answer = UserInteractions.AskUserOkCancel(stringBuilder.ToString());
                    if (answer == DialogResult.OK)
                    {
                        // With the help of https://stackoverflow.com/questions/64086598/redirect-product-sku-from-url-to-the-related-product-in-woocommerce
                        //  we now have a redirect from SKU to product name
                        // e.g. https://www.chem4word.co.uk/product/d91e2e64-95dd-4652-ac23-5c07a261a1b4 ==> https://www.chem4word.co.uk/product/simple-heterocycles

                        var productPage = $"https://www.chem4word.co.uk/product/{data.Sku}";

                        Globals.Chem4WordV3.Telemetry.Write(module, "Action", $"Opening {productPage}");
                        Process.Start(productPage);
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnClick_FinishedButton(object sender, RoutedEventArgs e)
        {
            var eventArgs = new WpfEventArgs
            {
                Button = "Finished",
                OutputValue = ""
            };

            if (_userIsDirty)
            {
                CredentialManager.WriteCredential(Chem4WordUser,
                                                  $"{UserName.Text}<{UserEmail.Text.ToLower()}>",
                                                  Globals.Chem4WordV3.Helper.MachineId,
                                                  CredentialPersistence.LocalMachine);
                _userIsDirty = false;
            }

            OnButtonClick?.Invoke(this, eventArgs);
        }

        private void OnSelectionChanged_ListOfLibraries(object sender, SelectionChangedEventArgs e)
        {
            if (Libraries.SelectedItems.Count > 0
                && Libraries.SelectedItem is LibraryDownloadGridSource data)
            {
                StatusMessage.Text = string.Empty;
                Download.IsEnabled = false;
                Buy.IsEnabled = false;

                var library = _catalogue.FirstOrDefault(l => l.Name.Equals(data.Name));
                if (library != null)
                {
                    RefreshPaidFor();
                    Download.IsEnabled = true;
                    if (!library.Driver.Equals(Constants.SQLiteStandardDriver))
                    {
                        Buy.IsEnabled = true;
                        Download.IsEnabled = _paidFor.Contains(library.Id);
                    }
                }
            }
        }

        private void OnTextChanged_UserNameOrEmail(object sender, TextChangedEventArgs e)
        {
            _userIsDirty = true;

            UserEmail.Background = UserEmail.Text.Trim().Length > 0 && StringHelper.IsValidEmail(UserEmail.Text.Trim())
                ? SystemColors.WindowBrush
                : Brushes.Salmon;
        }

        private void OnLostFocus_UserNameOrEmail(object sender, RoutedEventArgs e)
        {
            _userIsDirty = true;
            RefreshListOfLibraries();
        }
    }
}