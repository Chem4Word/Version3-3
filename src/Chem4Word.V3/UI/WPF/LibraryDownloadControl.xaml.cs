// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
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

        public System.Windows.Point TopLeft { get; set; }

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
                            UserName.Text = parts[0].Trim();
                            UserEmail.Text = parts[1].ToLower();
                            _userIsDirty = false;
                        }
                    }

                    var formData = new Dictionary<string, string>
                               {
                                   { "Version", Globals.Chem4WordV3.Helper.AddInVersion.Replace("Chem4Word V", "") },
                                   { "MachineId", Globals.Chem4WordV3.Helper.MachineId }
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
                                Description = entry.Description,
                                RequiresPayment = !entry.Driver.Equals(Constants.SQLiteStandardDriver)
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

                var helper = new ApiHelper(_settings.LibrariesUri);
                var result = helper.GetPaidFor(formData, 10);
                if (result.Success)
                {
                    _paidFor = result.Catalogue.Select(c => c.Id).ToList();
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
            if (UserEmail.Text.Trim().Length == 0 || !IsValidEmail(UserEmail.Text.Trim()))
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
                        var helper = new ApiHelper(_settings.LibrariesUri);

                        var response = helper.RequestLibraryDetails(formData, 15);
                        if (response.Success)
                        {
                            if (response.Details.Allowed)
                            {
                                _downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);

                                DisableControls();
                                DownloadLibrary(formData);
                                if (File.Exists(Path.Combine(_downloadPath, $"{library.Name}.zip")))
                                {
                                    InstallLibrary(library, _downloadPath);
                                }
                                if (!library.Driver.Equals(Constants.SQLiteStandardDriver))
                                {
                                    DownloadDriver(formData);
                                    if (File.Exists(Path.Combine(_downloadPath, $"{library.Driver}.zip")))
                                    {
                                        InstallDriver(library.Driver, _downloadPath);
                                        UserInteractions.InformUser("Microsoft Word needs to be restarted to activate the downloaded driver.");
                                    }
                                }

                                // Once library and driver have been installed add/replace user details in credential store
                                // Common values required for license verification by the driver.
                                if (_userIsDirty)
                                {
                                    CredentialManager.WriteCredential(Chem4WordUser, $"{UserName.Text}<{UserEmail.Text.ToLower()}>", Globals.Chem4WordV3.Helper.MachineId, CredentialPersistence.LocalMachine);
                                    _userIsDirty = false;
                                }
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
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

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

            var downloadedFile = Path.Combine(_downloadPath, $"{formData["LibraryName"]}.zip");
            if (File.Exists(downloadedFile))
            {
                var fileInfo = new FileInfo(downloadedFile);

                Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Downloading of '{fileInfo.Name}' took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)}ms");
            }
        }

        private void WorkDownloadLibrary(object sender, DoWorkEventArgs e)
        {
            var helper = new ApiHelper(_settings.LibrariesUri);
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

            var downloadedFile = Path.Combine(_downloadPath, $"{formData["Driver"]}.zip");
            if (File.Exists(downloadedFile))
            {
                var fileInfo = new FileInfo(downloadedFile);

                Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Downloading of '{fileInfo.Name}' took {SafeDouble.AsString0(stopwatch.ElapsedMilliseconds)}ms");
            }
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

        private void OnClick_BuyButton(object sender, RoutedEventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Process.Start("https://www.chem4word.co.uk/shop/");
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
                CredentialManager.WriteCredential(Chem4WordUser, $"{UserName.Text}<{UserEmail.Text.ToLower()}>", Globals.Chem4WordV3.Helper.MachineId, CredentialPersistence.LocalMachine);
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
        }
    }
}