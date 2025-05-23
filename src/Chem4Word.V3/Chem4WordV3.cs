﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Library;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using Chem4Word.Navigator;
using Chem4Word.Shared;
using Chem4Word.Telemetry;
using IChem4Word.Contracts;
using Microsoft.Office.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using OfficeTools = Microsoft.Office.Tools;
using Word = Microsoft.Office.Interop.Word;
using WordExtensions = Microsoft.Office.Tools.Word.Extensions;

namespace Chem4Word
{
    public partial class Chem4WordV3
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public static CustomRibbon Ribbon;

        public int VersionsBehind = 0;
        public DateTime VersionLastChecked = DateTime.MinValue;
        public string VersionAvailable = string.Empty;
        public bool VersionAvailableIsBeta = false;
        public bool IsEnabled;
        public bool IsBeta = true;

        public bool IsEndOfLife;
        public bool WordIsActivated;

        public XDocument AllVersions;
        public XDocument ThisVersion;

        public bool EventsEnabled = true;

        public bool ChemistryAllowed;
        public string ChemistryProhibitedReason = "";
        private string _lastContentControlAdded = "";

        private bool _chemistrySelected;
        private bool _markAsChemistryHandled;

        public bool OptionsReloadRequired = false;

        private ConfigWatcher _configWatcher;
        private ReferenceKeeper _keeper;

        public bool LibraryState;

        private Thread _slowOperationsThread;
        public List<string> StartUpTimings = new List<string>();

        public C4wAddInInfo AddInInfo;
        public SystemHelper Helper;
        public Chem4WordOptions SystemOptions;
        public TelemetryWriter Telemetry;

        private System.Timers.Timer _timer;

        public bool PlugInsHaveBeenLoaded;

        public List<IChem4WordEditor> Editors = new List<IChem4WordEditor>();
        public List<IChem4WordRenderer> Renderers = new List<IChem4WordRenderer>();
        public List<IChem4WordSearcher> Searchers = new List<IChem4WordSearcher>();
        public List<IChem4WordLibraryBase> Drivers = new List<IChem4WordLibraryBase>();

        public Dictionary<string, int> LibraryNames;
        public ListOfLibraries ListOfDetectedLibraries;

        private static readonly string[] ContextMenusTargets = { "Text", "Table Text", "Spelling", "Grammar", "Grammar (2)", "Lists", "Table Lists" };
        private const string ContextMenuTag = "2829AECC-061C-4DC5-8CC0-CAEC821B9127";
        private const string ContextMenuText = "Convert to Chemistry";

        public int WordWidth
        {
            get
            {
                var width = 0;

                try
                {
                    var commandBar1 = Application.CommandBars["Ribbon"];
                    if (commandBar1 != null)
                    {
                        width = Math.Max(width, commandBar1.Width);
                    }
                    var commandBar2 = Application.CommandBars["Status Bar"];
                    if (commandBar2 != null)
                    {
                        width = Math.Max(width, commandBar2.Width);
                    }
                }
                catch
                {
                    //
                }

                try
                {
                    if (width == 0)
                    {
                        width = Screen.PrimaryScreen.Bounds.Width;
                    }
                }
                catch
                {
                    //
                }

                return width;
            }
        }

        public Point WordTopLeft
        {
            get
            {
                var pp = new Point();

                try
                {
                    // Get position of Standard CommandBar (<ALT>+F)
                    var commandBar = Application.CommandBars["Standard"];
                    pp.X = commandBar.Left + commandBar.Height;
                    pp.Y = commandBar.Top + commandBar.Height;
                }
                catch
                {
                    //
                }
                return pp;
            }
        }

        public int WordVersion
        {
            get
            {
                var version = -1;

                try
                {
                    switch (Application.Version)
                    {
                        case "12.0":
                            version = 2007;
                            break;

                        case "14.0":
                            version = 2010;
                            break;

                        case "15.0":
                            version = 2013;
                            break;

                        case "16.0":
                            version = 2016;
                            break;
                    }
                }
                catch
                {
                    //
                }

                return version;
            }
        }

        public static void SetGlobalRibbon(CustomRibbon ribbon)
        {
            Ribbon = ribbon;
        }

        private void C4WAddIn_Startup(object sender, EventArgs e)
        {
            var module = $"{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;

                var cmd = Environment.CommandLine.ToLower();
                if (Ribbon != null && !cmd.Contains("-embedding"))
                {
                    var message = $"{module} started at {SafeDate.ToLongDate(DateTime.UtcNow)}";
                    Debug.WriteLine(message);
                    StartUpTimings.Add(message);

                    var sw = new Stopwatch();
                    sw.Start();

                    _keeper = new ReferenceKeeper();

                    CheckIfWordIsActivated();
                    PerformStartUpActions();

                    sw.Stop();
                    message = $"{module} took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms";
                    StartUpTimings.Add(message);
                    Debug.WriteLine(message);

                    if (!WordIsActivated)
                    {
                        UserInteractions.AlertUser("Microsoft Word is not activated!\nChem4Word uses features of Word which are only available if it is activated.");
                    }
                }
                else
                {
#if DEBUG
                    if (Ribbon == null)
                    {
                        RegistryHelper.StoreMessage(module, "Ribbon is null");
                    }
                    RegistryHelper.StoreMessage(module, $"Command line {cmd}");
#endif
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void CheckIfWordIsActivated()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                bool? flag = Globals.Chem4WordV3.Application.Options.SmartCutPaste;
                WordIsActivated = flag.HasValue;
            }
            catch (Exception exception)
            {
                WordIsActivated = false;
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void C4WAddIn_Shutdown(object sender, EventArgs e)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Debug.WriteLine("Starting Shutdown ...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (Ribbon != null)
                {
                    PerformShutDownActions();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);
            }

            stopwatch.Stop();
            Debug.WriteLine($"Shutdown took {stopwatch.ElapsedMilliseconds:N0}ms");
        }

        private void SlowOperations()
        {
            var module = $"{MethodBase.GetCurrentMethod()?.Name}()";
            var securityProtocol = ServicePointManager.SecurityProtocol;

            try
            {
                var message = $"{module} started at {SafeDate.ToLongDate(DateTime.UtcNow)}";

                Debug.WriteLine(message);
                StartUpTimings.Add(message);

                var sw = new Stopwatch();
                sw.Start();

                LoadPlugins();

                ServicePointManager.DefaultConnectionLimit = 100;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.Expect100Continue = false;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // ToDo: Check if we still need the config watcher
                _configWatcher = new ConfigWatcher(AddInInfo.ProductAppDataPath);

                Telemetry = new TelemetryWriter(true, true, Helper);

                sw.Stop();
                message = $"{module} took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms";
                Debug.WriteLine(message);
                StartUpTimings.Add(message);
            }
            catch // (ThreadAbortException threadAbortException)
            {
                // Do Nothing
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private void PerformStartUpActions()
        {
            var module = $"{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Enabled = true;

                Helper = new SystemHelper(StartUpTimings);

                SetButtonStates(ButtonState.NoDocument);

                LoadOptions();

                AddInInfo = new C4wAddInInfo();

                UpdateHelper.ReadSavedValues();
                UpdateHelper.ReadThisVersion(Assembly.GetExecutingAssembly());
                ShowOrHideUpdateShield();

                // Handle slower startup stuff on thread
                _slowOperationsThread = new Thread(SlowOperations);
                _slowOperationsThread.SetApartmentState(ApartmentState.STA);
                _slowOperationsThread.Start();

                if (VersionsBehind < Constants.MaximumVersionsBehind)
                {
                    var app = Application;

                    // Hook in Global Application level events
                    app.WindowBeforeDoubleClick += OnWindowBeforeDoubleClick;
                    app.WindowSelectionChange += OnWindowSelectionChange;
                    app.WindowActivate += OnWindowActivate;
                    app.WindowBeforeRightClick += OnWindowBeforeRightClick;

                    // Hook in Global Document Level Events
                    app.DocumentOpen += OnDocumentOpen;
                    app.DocumentChange += OnDocumentChange;
                    app.DocumentBeforeClose += OnDocumentBeforeClose;
                    app.DocumentBeforeSave += OnDocumentBeforeSave;
                    ((Word.ApplicationEvents4_Event)Application).NewDocument += OnNewDocument;

                    if (app.Documents.Count > 0)
                    {
                        EnableContentControlEvents();

                        if (app.ActiveDocument.CompatibilityMode >= (int)Word.WdCompatibilityMode.wdWord2010)
                        {
                            SetButtonStates(ButtonState.CanInsert);
                        }
                        else
                        {
                            SetButtonStates(ButtonState.NoDocument);
                        }
                    }

                    if (AddInInfo.DeploymentPath.ToLower().Contains("vso-ci"))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Hey {Environment.UserName}");
                        sb.AppendLine("");
                        sb.AppendLine("You should not be running this build configuration");
                        sb.AppendLine("Please select Debug or Release build!");
                        UserInteractions.StopUser(sb.ToString());
                    }
                }
                else
                {
                    SetButtonStates(ButtonState.Disabled);
                    StartUpTimings.Add(
                        $"{module} chemistry operations disabled because Chem4Word is {VersionsBehind} versions behind!");
                    RegistryHelper.StoreMessage(module, $"Chem4Word is disabled because it is {VersionsBehind} versions behind!");
                }

                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;
            }
            catch (COMException comException)
            {
                RegistryHelper.StoreException(module, comException);
            }
            catch (ThreadAbortException threadAbortException)
            {
                RegistryHelper.StoreException(module, threadAbortException);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{module} {exception.Message}");
                RegistryHelper.StoreException(module, exception);

                using (var form = new ReportError(Telemetry, WordTopLeft, module, exception))
                {
                    form.ShowDialog();
                }
            }
        }

        private bool CanSendTelemetry() =>
            Telemetry != null
            && Helper != null
            && !string.IsNullOrEmpty(Helper.IpAddress)
            && !Helper.IpAddress.Equals("0.0.0.0")
            && !Helper.IpAddress.Equals("8.8.8.8");

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (CanSendTelemetry())
            {
                _timer.Enabled = false;
                _timer.Elapsed -= OnTimerElapsed;

                RegistryHelper.SendMsiActions();
                RegistryHelper.SendSetupActions();
                RegistryHelper.SendUpdateActions();

                CollectAndSendMsiLogs();

                // Early check for updates but with longer that normal period
                UpdateHelper.CheckForUpdates(30);
            }
        }

        private void CollectAndSendMsiLogs()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);
            if (!Directory.Exists(downloadPath))
            {
                downloadPath = Path.GetTempPath();
            }

            var logFiles = Directory.GetFiles(downloadPath, "Chem4Word-Setup.*.log");
            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    var zipFile = Path.Combine(fileInfo.DirectoryName, $"{fileName}.zip");

                    ZipLogFile(logFile, zipFile);

                    if (File.Exists(zipFile))
                    {
                        var fileBytes = File.ReadAllBytes(zipFile);
                        Globals.Chem4WordV3.Telemetry.SendZipFile(fileBytes, $"{fileName}.zip");
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", $"{fileName}.zip uploaded");

                        Thread.Sleep(25);
                        File.Delete(zipFile);
                        Thread.Sleep(25);
                        File.Delete(logFile);
                    }
                }
                catch (Exception exception)
                {
                    if (Telemetry != null)
                    {
                        Telemetry.Write(module, "Exception", exception.Message);
                        Telemetry.Write(module, "Exception", exception.StackTrace);
                    }
                    else
                    {
                        RegistryHelper.StoreException(module, exception);
                    }
                }
            }
        }

        private static void ZipLogFile(string logFilePath, string zipFilePath)
        {
            using (var zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath));
                }
            }
        }

        public void LoadNamesFromLibrary()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";
            try
            {
                var details = GetSelectedDatabaseDetails();
                if (details != null)
                {
                    var driver = (IChem4WordLibraryReader)GetDriverPlugIn(details.Driver);
                    if (driver != null)
                    {
                        driver.FileName = details.Connection;
                        LibraryNames = driver.GetSubstanceNamesWithIds();
                    }
                }
            }
            catch (Exception exception)
            {
                if (Telemetry != null)
                {
                    Telemetry.Write(module, "Exception", exception.Message);
                    Telemetry.Write(module, "Exception", exception.StackTrace);
                }
                else
                {
                    RegistryHelper.StoreException(module, exception);
                }

                LibraryNames = null;
            }
        }

        public void LoadOptions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            try
            {
                // Initialise Telemetry with send permission
                Telemetry = new TelemetryWriter(true, true, Helper);

                if (AddInInfo != null)
                {
                    // Read in options file
                    SystemOptions = new Chem4WordOptions(AddInInfo.ProductAppDataPath);
                    if (SystemOptions.Errors.Any())
                    {
                        RegistryHelper.StoreMessage(module, "Error(s):" + Environment.NewLine + string.Join(Environment.NewLine, SystemOptions.Errors));
                        SystemOptions.Errors = new List<string>();
                    }

                    try
                    {
                        if (ThisVersion != null)
                        {
                            var betaValue = ThisVersion.Root?.Element("IsBeta")?.Value;
                            IsBeta = betaValue != null && bool.Parse(betaValue);
                        }
                    }
                    catch
                    {
                        // Assume isBeta
                    }

                    // Belt and braces ...
                    if (SystemOptions == null)
                    {
                        SystemOptions = new Chem4WordOptions
                        {
                            SettingsPath = AddInInfo.ProductAppDataPath
                        };
                    }

                    // ... as we are seeing some errors here ?
                    // Re-Initialise Telemetry with granted permissions
                    Telemetry = new TelemetryWriter(IsBeta || Debugger.IsAttached || SystemOptions.TelemetryEnabled, IsBeta, Helper);

                    try
                    {
                        var settingsChanged = false;

                        if (string.IsNullOrEmpty(SystemOptions.SelectedEditorPlugIn))
                        {
                            SystemOptions.SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
                            settingsChanged = true;
                        }
                        else
                        {
                            if (Editors.Count > 0)
                            {
                                var editor = GetEditorPlugIn(SystemOptions.SelectedEditorPlugIn);
                                if (editor == null)
                                {
                                    SystemOptions.SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
                                    RegistryHelper.StoreMessage(module, $"Setting editor to {SystemOptions.SelectedEditorPlugIn}");
                                    settingsChanged = true;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(SystemOptions.SelectedRendererPlugIn))
                        {
                            SystemOptions.SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;
                            settingsChanged = true;
                        }
                        else
                        {
                            if (Renderers.Count > 0)
                            {
                                var renderer = GetRendererPlugIn(SystemOptions.SelectedRendererPlugIn);
                                if (renderer == null)
                                {
                                    SystemOptions.SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;
                                    RegistryHelper.StoreMessage(module, $"Setting renderer to {SystemOptions.SelectedRendererPlugIn}");
                                    settingsChanged = true;
                                }
                            }
                        }

                        if (settingsChanged)
                        {
                            RegistryHelper.StoreMessage(module, "Saving revised settings");
                            SystemOptions.Save();
                            if (SystemOptions.Errors.Any())
                            {
                                RegistryHelper.StoreMessage(module, "Error(s):" + Environment.NewLine + string.Join(Environment.NewLine, SystemOptions.Errors));
                                SystemOptions.Errors = new List<string>();
                            }
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }

                    try
                    {
                        if (ListOfDetectedLibraries == null)
                        {
                            ListOfDetectedLibraries
                                = new LibraryFileHelper(Telemetry, AddInInfo.ProgramDataPath)
                                    .GetListOfLibraries(silent: true);
                            LoadNamesFromLibrary();
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
                SystemOptions = null;
            }
        }

        private void PerformShutDownActions()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Debug.WriteLine("Checking if Slow Operations thread is alive");
                if (_slowOperationsThread != null)
                {
                    Debug.WriteLine("Joining Slow Operations thread");
                    _slowOperationsThread.Join();
                    if (_slowOperationsThread.IsAlive)
                    {
                        _slowOperationsThread.Abort();
                    }
                }

                Debug.WriteLine("Shutting down Editor Plug Ins");
                if (Editors != null)
                {
                    for (var i = 0; i < Editors.Count; i++)
                    {
                        Debug.WriteLine($" Shutting down {Editors[i].Name} Plug In");
                        Editors[i].Telemetry = null;
                        Editors[i] = null;
                    }
                }

                Debug.WriteLine("Shutting down Renderer Plug Ins");
                if (Renderers != null)
                {
                    for (var i = 0; i < Renderers.Count; i++)
                    {
                        Debug.WriteLine($" Shutting down {Renderers[i].Name} Plug In");
                        Renderers[i].Telemetry = null;
                        Renderers[i] = null;
                    }
                }

                Debug.WriteLine("Shutting down Searcher Plug Ins");
                if (Searchers != null)
                {
                    for (var i = 0; i < Searchers.Count; i++)
                    {
                        Debug.WriteLine($" Shutting down {Searchers[i].Name} Plug In");
                        Searchers[i].Telemetry = null;
                        Searchers[i] = null;
                    }
                }

                Debug.WriteLine("Shutting down Driver Plug Ins");
                if (Drivers != null)
                {
                    for (var i = 0; i < Drivers.Count; i++)
                    {
                        Debug.WriteLine($" Shutting down {Drivers[i].Name} Plug In");
                        Drivers[i].Telemetry = null;
                        Drivers[i] = null;
                    }
                }

                Debug.WriteLine("Disposing of Config Watcher");
                _configWatcher?.Dispose();
                Debug.WriteLine("Disposing of Reference Keeper");
                _keeper?.Dispose();

#if DEBUG
                // Wait up to 5 seconds for all telemetry to be sent
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                while (Globals.Chem4WordV3.Telemetry.BufferCount() > 0)
                {
                    Debug.WriteLine($"Buffer count is {Globals.Chem4WordV3.Telemetry.BufferCount()}");
                    Thread.Sleep(25);
                    if (stopwatch.ElapsedMilliseconds > 5000)
                    {
                        break;
                    }
                }
#endif

                Debug.WriteLine("Waiting for garbage collection");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            finally
            {
                // Fix reported issue with Windows 8 and WPF See
                // https://social.msdn.microsoft.com/Forums/office/en-US/bb990ddb-ecde-4161-8915-e66e913e3a3b/invalidoperationexception-localdatastoreslot-storage-has-been-freed?forum=exceldev
                // I saw this on Server 2008 R2 which is very closely related to Windows 8
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }

        private void LoadPlugins()
        {
            var module = $"{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                LoadPlugIns(false);

                if (Ribbon != null)
                {
                    if (VersionsBehind >= Constants.MaximumVersionsBehind)
                    {
                        SetButtonStates(ButtonState.Disabled);
                        ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                    }
                    else
                    {
                        try
                        {
                            if (Application.Documents.Count > 0 && Application.Selection != null)
                            {
                                OnWindowSelectionChange(Application.Selection);
                            }
                        }
                        catch
                        {
                            // Do Nothing
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }

        private void LoadPlugIns(bool mustBeSigned)
        {
            var module = $"{MethodBase.GetCurrentMethod().Name}()";
            // http://www.codeproject.com/Articles/453778/Loading-Assemblies-from-Anywhere-into-a-New-AppDom
            var message = $"{module} started at {SafeDate.ToLongDate(DateTime.UtcNow)}";
            StartUpTimings.Add(message);
            Debug.WriteLine(message);

            var sw = new Stopwatch();
            sw.Start();

            var files = new List<string>();

            var plugInPath = Path.Combine(AddInInfo.DeploymentPath, "PlugIns");
            if (Directory.Exists(plugInPath))
            {
                files = Directory.GetFiles(plugInPath, "Chem4Word*.dll").ToList();
            }

            var filesFound = files.Count;

            var plugInsFound = new List<string>();

            #region Find Our PlugIns

            foreach (var file in files)
            {
                using (var manager = new AssemblyReflectionManager())
                {
                    try
                    {
                        var success = manager.LoadAssembly(file, "Chem4WordTempDomain");
                        if (success)
                        {
                            #region Find Our Interfaces

                            var results = manager.Reflect(file, a =>
                            {
                                var names = new List<string>();

                                try
                                {
                                    #region Get Code Signing Certificate details

                                    var signedBy = "";

                                    var mod = a.GetModules().First();
                                    var certificate = mod.GetSignerCertificate();
                                    if (certificate != null)
                                    {
                                        signedBy = certificate.Subject;
                                        // E=developer@chem4word.co.uk, CN="Open Source Developer, Mike Williams", O=Open Source Developer, C=GB
                                        Debug.WriteLine(certificate.Subject);
                                        Debug.WriteLine(certificate.Issuer);
                                    }

                                    #endregion Get Code Signing Certificate details

                                    var types = a.GetTypes();
                                    foreach (var t in types)
                                    {
                                        if (t.IsClass && t.IsPublic && !t.IsAbstract)
                                        {
                                            var ifaces = t.GetInterfaces();
                                            foreach (var iface in ifaces)
                                            {
                                                if (iface.FullName.StartsWith("IChem4Word.Contracts"))
                                                {
                                                    var parts = a.FullName.Split(',');
                                                    var fi = new FileInfo(t.Module.FullyQualifiedName);
                                                    names.Add($"{parts[0]}|{iface.FullName}|{fi.Name}|{signedBy}");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Debug.WriteLine(exception.Message);
                                }

                                return names;
                            });

                            #endregion Find Our Interfaces

                            manager.UnloadAssembly(file);

                            plugInsFound.AddRange(results);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception.Message);
                    }
                }
            }

            #endregion Find Our PlugIns

            var editorType = typeof(IChem4WordEditor);
            var rendererType = typeof(IChem4WordRenderer);
            var searcherType = typeof(IChem4WordSearcher);
            var driverType = typeof(IChem4WordLibraryBase);

            foreach (var plugIn in plugInsFound)
            {
                var parts = plugIn.Split('|');
                Debug.WriteLine($"Loading PlugIn {parts[0]} with Interface {parts[1]} from file {parts[2]} signed by {parts[3]}");

                var allowed = true;
                if (mustBeSigned)
                {
                    // Is it signed by us?
                    allowed = parts[3].Contains("admin@chem4word.co.uk");
                }

                if (allowed)
                {
                    #region Find Source File

                    var sourceFile = "";
                    foreach (var file in files)
                    {
                        if (file.EndsWith(parts[2]))
                        {
                            sourceFile = file;
                            break;
                        }
                    }

                    #endregion Find Source File

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        #region Load Editor(s)

                        if (parts[1].Contains("IChem4WordEditor"))
                        {
                            var asm = Assembly.LoadFile(sourceFile);
                            var types = asm.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.GetInterface(editorType.FullName) != null)
                                {
                                    var plugin = (IChem4WordEditor)Activator.CreateInstance(type);
                                    Editors.Add(plugin);

                                    break;
                                }
                            }
                        }

                        #endregion Load Editor(s)

                        #region Load Renderer(s)

                        if (parts[1].Contains("IChem4WordRenderer"))
                        {
                            var asm = Assembly.LoadFile(sourceFile);
                            var types = asm.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.GetInterface(rendererType.FullName) != null)
                                {
                                    var plugin = (IChem4WordRenderer)Activator.CreateInstance(type);
                                    Renderers.Add(plugin);

                                    break;
                                }
                            }
                        }

                        #endregion Load Renderer(s)

                        #region Load Searcher(s)

                        if (parts[1].Contains("IChem4WordSearcher"))
                        {
                            var asm = Assembly.LoadFile(sourceFile);
                            var types = asm.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.GetInterface(searcherType.FullName) != null)
                                {
                                    var plugin = (IChem4WordSearcher)Activator.CreateInstance(type);
                                    Searchers.Add(plugin);

                                    break;
                                }
                            }
                        }

                        #endregion Load Searcher(s)

                        #region Load Driver(s)

                        if (parts[1].Contains("IChem4WordLibraryBase"))
                        {
                            var asm = Assembly.LoadFile(sourceFile);
                            var types = asm.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.GetInterface(driverType.FullName) != null)
                                {
                                    var plugin = (IChem4WordLibraryBase)Activator.CreateInstance(type);
                                    Drivers.Add(plugin);

                                    break;
                                }
                            }
                        }

                        #endregion Load Driver(s)
                    }
                }
            }

            sw.Stop();

            message = $"{module} examining {filesFound} files took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms";
            Debug.WriteLine(message);
            StartUpTimings.Add(message);

            PlugInsHaveBeenLoaded = true;
        }

        public DatabaseDetails GetSelectedDatabaseDetails()
        {
            if (ListOfDetectedLibraries == null)
            {
                ListOfDetectedLibraries
                    = new LibraryFileHelper(Telemetry, AddInInfo.ProgramDataPath)
                        .GetListOfLibraries(silent: true);
            }

            if (ListOfDetectedLibraries != null)
            {
                return ListOfDetectedLibraries
                       .AvailableDatabases?
                       .FirstOrDefault(l => l.DisplayName.Equals(ListOfDetectedLibraries.SelectedLibrary));
            }

            return null;
        }

        public IChem4WordLibraryBase GetDriverPlugIn(string name)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            IChem4WordLibraryBase plugin = null;

            if (PlugInsHaveBeenLoaded && !string.IsNullOrEmpty(name))
            {
                foreach (var ice in Drivers)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.BackupFolder = Path.Combine(AddInInfo.ProgramDataPath, "Libraries", "Backups");
                        plugin.TopLeft = WordTopLeft;

                        break;
                    }
                }

                if (plugin == null)
                {
                    Telemetry.Write(module, "Warning", $"Could not find driver plug in [{name}]");
                    Debug.WriteLine($"Could not find driver plug in [{name}]");
                    Debugger.Break();
                }
            }

            return plugin;
        }

        public IChem4WordEditor GetEditorPlugIn(string name)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            IChem4WordEditor plugin = null;

            if (PlugInsHaveBeenLoaded && !string.IsNullOrEmpty(name))
            {
                foreach (var ice in Editors)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;

                        break;
                    }
                }

                if (plugin == null)
                {
                    Telemetry.Write(module, "Warning", $"Could not find editor plug in [{name}]");
                    Debug.WriteLine($"Could not find editor plug in [{name}]");
                    Debugger.Break();
                }
            }

            return plugin;
        }

        public IChem4WordRenderer GetRendererPlugIn(string name)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            IChem4WordRenderer plugin = null;

            if (PlugInsHaveBeenLoaded && !string.IsNullOrEmpty(name))
            {
                foreach (var ice in Renderers)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;

                        break;
                    }
                }

                if (plugin == null)
                {
                    Telemetry.Write(module, "Warning", $"Could not find renderer plug in [{name}]");
                    Debug.WriteLine($"Could not find renderer plug in [{name}]");
                    Debugger.Break();
                }
            }

            return plugin;
        }

        public IChem4WordSearcher GetSearcherPlugIn(string name)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            IChem4WordSearcher plugin = null;

            if (PlugInsHaveBeenLoaded && !string.IsNullOrEmpty(name))
            {
                foreach (var ice in Searchers)
                {
                    if (ice.Name.Equals(name))
                    {
                        plugin = ice;
                        plugin.Telemetry = Telemetry;
                        plugin.SettingsPath = AddInInfo.ProductAppDataPath;
                        plugin.TopLeft = WordTopLeft;

                        break;
                    }
                }

                if (plugin == null)
                {
                    Telemetry.Write(module, "Warning", $"Could not find searcher plug in [{name}]");
                    Debug.WriteLine($"Could not find searcher plug in [{name}]");
                    Debugger.Break();
                }
            }

            return plugin;
        }

        [HandleProcessCorruptedStateExceptions]
        public void EnableContentControlEvents()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Application.Documents.Count > 0)
                {
                    // Get reference to active document
                    var vstoObject = Globals.Factory.GetVstoObject(Globals.Chem4WordV3.Application.ActiveDocument);

                    // Hook in Content Control Events
                    // See: https://msdn.microsoft.com/en-us/library/Microsoft.Office.Interop.Word.DocumentEvents2_methods%28v=office.14%29.aspx
                    // See: https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.documentevents2_event_methods%28v=office.14%29.aspx

                    // Remember to add corresponding code in DisableContentControlEvents()

                    // ContentControlOnEnter Event Handler
                    try
                    {
                        vstoObject.ContentControlOnEnter -= OnContentControlOnEnter;
                        vstoObject.ContentControlOnEnter += OnContentControlOnEnter;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlOnExit Event Handler
                    try
                    {
                        vstoObject.ContentControlOnExit -= OnContentControlOnExit;
                        vstoObject.ContentControlOnExit += OnContentControlOnExit;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlBeforeDelete Event Handler
                    try
                    {
                        vstoObject.ContentControlBeforeDelete -= OnContentControlBeforeDelete;
                        vstoObject.ContentControlBeforeDelete += OnContentControlBeforeDelete;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlAfterAdd Event Handler
                    try
                    {
                        vstoObject.ContentControlAfterAdd -= OnContentControlAfterAdd;
                        vstoObject.ContentControlAfterAdd += OnContentControlAfterAdd;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                    EventsEnabled = true;
                }
            }
            catch // (Exception exception)
            {
                // RegistryHelper.StoreException(module, exception)
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void DisableContentControlEvents()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                EventsEnabled = false;

                if (Application.Documents.Count > 0)
                {
                    // Get reference to active document
                    var vstoObject = Globals.Factory.GetVstoObject(Globals.Chem4WordV3.Application.ActiveDocument);

                    // Hook out Content Control Events
                    // See: https://msdn.microsoft.com/en-us/library/Microsoft.Office.Interop.Word.DocumentEvents2_methods%28v=office.14%29.aspx
                    // See: https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.documentevents2_event_methods%28v=office.14%29.aspx

                    // Remember to add corresponding code in EnableContentControlEvents()

                    // ContentControlOnEnter Event Handler
                    try
                    {
                        vstoObject.ContentControlOnEnter -= OnContentControlOnEnter;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlOnExit Event Handler
                    try
                    {
                        vstoObject.ContentControlOnExit -= OnContentControlOnExit;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlBeforeDelete Event Handler
                    try
                    {
                        vstoObject.ContentControlBeforeDelete -= OnContentControlBeforeDelete;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    // ContentControlAfterAdd Event Handler
                    try
                    {
                        vstoObject.ContentControlAfterAdd -= OnContentControlAfterAdd;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            }
            catch // (Exception exception)
            {
                // RegistryHelper.StoreException(module, exception)
            }
        }

        private void SetButtonStates(ButtonState state)
        {
            if (Ribbon != null)
            {
                // Help is always enabled
                Ribbon.HelpMenu.Enabled = true;

                if (PlugInsHaveBeenLoaded)
                {
                    var databaseDetails = GetSelectedDatabaseDetails();
                    var plugInsLoaded = Editors.Count + Renderers.Count + Searchers.Count > 0;

                    // Enabled once any PlugIns are loaded
                    Ribbon.ChangeOptions.Enabled = plugInsLoaded;
                    IsEnabled = true;

                    switch (state)
                    {
                        case ButtonState.Disabled:
                        case ButtonState.NoDocument:
                            Ribbon.EditStructure.Enabled = false;
                            Ribbon.EditStructure.Label = "Draw";
                            Ribbon.EditLabels.Enabled = false;
                            Ribbon.ViewCml.Enabled = false;
                            Ribbon.ImportFromFile.Enabled = false;
                            Ribbon.ExportToFile.Enabled = false;
                            Ribbon.ShowAsMenu.Enabled = false;
                            Ribbon.ShowNavigator.Enabled = false;
                            Ribbon.ShowLibrary.Enabled = false;
                            Ribbon.BuyLibrary.Enabled = false;
                            Ribbon.ManageLibraries.Enabled = false;
                            Ribbon.WebSearchMenu.Enabled = false;
                            Ribbon.SaveToLibrary.Enabled = false;
                            Ribbon.EditLibrary.Enabled = false;
                            Ribbon.ArrangeMolecules.Enabled = false;
                            Ribbon.ButtonsDisabled.Enabled = true;
                            break;

                        case ButtonState.CanEdit:
                            Ribbon.EditStructure.Enabled = plugInsLoaded && Editors.Count > 0;
                            Ribbon.EditStructure.Label = "Edit";
                            Ribbon.EditLabels.Enabled = true;
                            Ribbon.ViewCml.Enabled = true;
                            Ribbon.ImportFromFile.Enabled = false;
                            Ribbon.ExportToFile.Enabled = true;
                            Ribbon.ShowAsMenu.Enabled = true;
                            Ribbon.ShowNavigator.Enabled = true;
                            Ribbon.ShowLibrary.Enabled = true;
                            Ribbon.BuyLibrary.Enabled = true;
                            Ribbon.ManageLibraries.Enabled = true;
                            Ribbon.WebSearchMenu.Enabled = false;
                            var canSaveOrEdit = false;
                            if (databaseDetails != null)
                            {
                                canSaveOrEdit = !databaseDetails.IsLocked();
                            }
                            Ribbon.SaveToLibrary.Enabled = canSaveOrEdit;
                            Ribbon.EditLibrary.Enabled = canSaveOrEdit;
                            Ribbon.ArrangeMolecules.Enabled = true;
                            Ribbon.ButtonsDisabled.Enabled = false;
                            break;

                        case ButtonState.CanInsert:
                            Ribbon.EditStructure.Enabled = plugInsLoaded && Editors.Count > 0;
                            Ribbon.EditStructure.Label = "Draw";
                            Ribbon.EditLabels.Enabled = false;
                            Ribbon.ViewCml.Enabled = false;
                            Ribbon.ImportFromFile.Enabled = plugInsLoaded;
                            Ribbon.ExportToFile.Enabled = false;
                            Ribbon.ShowAsMenu.Enabled = false;
                            Ribbon.ShowNavigator.Enabled = true;
                            Ribbon.ShowLibrary.Enabled = true;
                            Ribbon.BuyLibrary.Enabled = true;
                            Ribbon.ManageLibraries.Enabled = true;
                            Ribbon.SaveToLibrary.Enabled = false;
                            var canEdit = false;
                            if (databaseDetails != null)
                            {
                                canEdit = !databaseDetails.IsLocked();
                            }
                            Ribbon.EditLibrary.Enabled = canEdit;
                            Ribbon.WebSearchMenu.Enabled = plugInsLoaded && Searchers.Count > 0;
                            Ribbon.ArrangeMolecules.Enabled = false;
                            Ribbon.ButtonsDisabled.Enabled = false;
                            break;
                    }

                    var betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
                    var isBeta = betaValue != null && bool.Parse(betaValue);

                    if (IsEndOfLife || isBeta && !VersionAvailableIsBeta)
                    {
                        Ribbon.EditStructure.Enabled = false;
                        Ribbon.EditStructure.Label = "Draw";
                        Ribbon.EditLabels.Enabled = false;
                        Ribbon.ViewCml.Enabled = false;
                        Ribbon.ImportFromFile.Enabled = false;
                        Ribbon.ExportToFile.Enabled = false;
                        Ribbon.ShowAsMenu.Enabled = false;
                        Ribbon.ShowNavigator.Enabled = false;
                        Ribbon.ShowLibrary.Enabled = false;
                        Ribbon.SaveToLibrary.Enabled = false;
                        Ribbon.EditLibrary.Enabled = false;
                        Ribbon.BuyLibrary.Enabled = false;
                        Ribbon.ManageLibraries.Enabled = false;
                        Ribbon.WebSearchMenu.Enabled = false;
                        Ribbon.ArrangeMolecules.Enabled = false;
                        Ribbon.ButtonsDisabled.Enabled = true;

                        IsEnabled = false;
                    }
                }
            }
        }

        public void ShowOrHideUpdateShield()
        {
            if (Ribbon != null)
            {
                switch (VersionsBehind)
                {
                    case 0:
                        Ribbon.Update.Visible = false;
                        Ribbon.Update.Enabled = false;
                        Ribbon.Update.Image = Properties.Resources.Shield_Good;
                        ChemistryProhibitedReason = "";
                        break;

                    case 1:
                    case 2:
                    case 3:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Warning;
                        Ribbon.Update.Label = "Update Advised";
                        Ribbon.Update.ScreenTip = "Please update";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind.";
                        ChemistryProhibitedReason = "";
                        break;

                    case 4:
                    case 5:
                    case 6:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                        Ribbon.Update.Label = "Update Essential";
                        Ribbon.Update.ScreenTip = "Please update";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind.";
                        ChemistryProhibitedReason = "";
                        break;

                    default:
                        Ribbon.Update.Visible = true;
                        Ribbon.Update.Enabled = true;
                        Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                        Ribbon.Update.Label = "Update to use Chem4Word again";
                        Ribbon.Update.ScreenTip = "You must update to continue using Chem4Word";
                        Ribbon.Update.SuperTip = $"You are {VersionsBehind} versions behind therefore Chem4Word has been disabled as it is too many versions old.";
                        SetButtonStates(ButtonState.Disabled);
                        ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                        break;
                }

                var betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
                var isBeta = betaValue != null && bool.Parse(betaValue);

                if (isBeta && VersionsBehind > 0 && !VersionAvailableIsBeta)
                {
                    Ribbon.Update.Visible = true;
                    Ribbon.Update.Enabled = true;
                    Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                    Ribbon.Update.Label = "Update to use Chem4Word again";
                    Ribbon.Update.ScreenTip = "You must update to continue using Chem4Word";
                    Ribbon.Update.SuperTip = $"Your beta version has been disabled, because a production release '{VersionAvailable}' is available.";
                    SetButtonStates(ButtonState.Disabled);
                    ChemistryProhibitedReason = Constants.Chem4WordIsBeta;
                }

                if (IsEndOfLife)
                {
                    Ribbon.Update.Visible = true;
                    Ribbon.Update.Enabled = true;
                    Ribbon.Update.Image = Properties.Resources.Shield_Danger;
                    Ribbon.Update.Label = "End of Life";
                    Ribbon.Update.ScreenTip = "This version of Chem4Word is no longer supported";
                    Ribbon.Update.SuperTip = "Please see our website https://www.chem4word.co.uk for details of where to obtain a new version from.";
                    SetButtonStates(ButtonState.Disabled);
                    ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                }
            }
        }

        public void SelectChemistry(Word.Selection sel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            EventsEnabled = false;

            var chemistrySelected = false;

            try
            {
                if (sel != null)
                {
                    var ccCount = sel.ContentControls.Count;

                    var activeDocument = sel.Application.ActiveDocument;
                    if (activeDocument != null)
                    {
                        var targets = (from Word.ContentControl ccs in activeDocument.ContentControls
                                       orderby ccs.Range.Start
                                       where $"{ccs.Title}" == Constants.ContentControlTitle
                                       select ccs).ToList();

                        foreach (var cc in targets)
                        {
                            // Already Selected
                            if (sel.Range.Start == cc.Range.Start - 1 && sel.Range.End == cc.Range.End + 1)
                            {
                                if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                                {
                                    NavigatorSupport.SelectNavigatorItem(CustomXmlPartHelper.GuidFromTag(cc.Tag));
                                    chemistrySelected = true;
                                }
                                break;
                            }

                            // Inside CC
                            if (sel.Range.Start >= cc.Range.Start && sel.Range.End <= cc.Range.End)
                            {
                                if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                                {
                                    activeDocument.Application.Selection.SetRange(cc.Range.Start - 1, cc.Range.End + 1);
                                    NavigatorSupport.SelectNavigatorItem(CustomXmlPartHelper.GuidFromTag(cc.Tag));
                                    chemistrySelected = true;
                                }
                                break;
                            }
                        }

                        if (VersionsBehind >= Constants.MaximumVersionsBehind)
                        {
                            SetButtonStates(ButtonState.Disabled);
                            ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                        }
                        else
                        {
                            if (chemistrySelected)
                            {
                                Ribbon.ActivateChemistryTab();
                                SetButtonStates(ButtonState.CanEdit);
                            }
                            else
                            {
                                if (ccCount == 0)
                                {
                                    SetButtonStates(ButtonState.CanInsert);
                                }
                                else
                                {
                                    SetButtonStates(ButtonState.NoDocument);
                                    ChemistryProhibitedReason = "more than a single content control selected";
                                }
                            }
                        }

                        _chemistrySelected = chemistrySelected;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            finally
            {
                EventsEnabled = true;
            }
        }

        #region Right Click

        #region Events

        private void OnWindowBeforeRightClick(Word.Selection selection, ref bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (!IsEndOfLife && VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (PlugInsHaveBeenLoaded)
                    {
                        ClearChemistryContextMenus();
                        EvaluateChemistryAllowed(inRightClick: true);
                        if (ChemistryAllowed)
                        {
                            if (selection.Start != selection.End)
                            {
                                HandleRightClick(selection);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        private void OnCommandBarButtonClick(CommandBarButton ctrl, ref bool cancelDefault)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                if (!_markAsChemistryHandled)
                {
                    var targetWord = JsonConvert.DeserializeObject<TargetWord>(ctrl.Tag);

                    var details = GetSelectedDatabaseDetails();
                    if (details != null)
                    {
                        var driver = (IChem4WordLibraryReader)GetDriverPlugIn(details.Driver);
                        if (driver != null)
                        {
                            var dto = driver.GetChemistryById(targetWord.ChemistryId);

                            if (dto == null)
                            {
                                UserInteractions.WarnUser($"No match for '{targetWord.ChemicalName}' was found in your selected library");
                            }
                            else
                            {
                                var converter = new CMLConverter();
                                Model model;
                                if (dto.DataType.Equals("cml"))
                                {
                                    model = converter.Import(Encoding.UTF8.GetString(dto.Chemistry));
                                }
                                else
                                {
                                    var pbc = new ProtocolBufferConverter();
                                    model = pbc.Import(dto.Chemistry);
                                }

                                var document = Application.ActiveDocument;

                                // Generate new CustomXmlPartGuid
                                model.CustomXmlPartGuid = Guid.NewGuid().ToString("N");
                                model.EnsureBondLength(SystemOptions.BondLength,
                                                       SystemOptions.SetBondLengthOnImportFromLibrary);
                                var cml = converter.Export(model);

                                #region Find Id of name

                                var tagPrefix = "";
                                foreach (var mol in model.Molecules.Values)
                                {
                                    foreach (var name in mol.Names)
                                    {
                                        if (targetWord.ChemicalName.ToLower().Equals(name.Value.ToLower()))
                                        {
                                            tagPrefix = name.Id;
                                            break;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(tagPrefix))
                                    {
                                        break;
                                    }
                                }

                                if (string.IsNullOrEmpty(tagPrefix))
                                {
                                    tagPrefix = "c0";
                                }

                                #endregion Find Id of name

                                // Test phrases (ensure benzene and cyclopropane are in your library)
                                // This is benzene, this is not.
                                // This is cyclopropane. This is not.

                                Word.ContentControl contentControl = null;
                                var wordSettings = new WordSettings(Application);

                                try
                                {
                                    Application.ScreenUpdating = false;
                                    DisableContentControlEvents();

                                    var insertionPoint = targetWord.Start;
                                    document.Range(targetWord.Start, targetWord.Start + targetWord.ChemicalName.Length).Delete();

                                    Application.Selection.SetRange(insertionPoint, insertionPoint);

                                    var tag = $"{tagPrefix}:{model.CustomXmlPartGuid}";
                                    contentControl = ChemistryHelper.Insert1DChemistry(document, targetWord.ChemicalName, false, tag);

                                    Telemetry.Write(module, "Information", $"Inserted 1D version of {targetWord.ChemicalName} from library");
                                }
                                catch (Exception e)
                                {
                                    Telemetry.Write(module, "Exception", e.Message);
                                    Telemetry.Write(module, "Exception", e.StackTrace);
                                }
                                finally
                                {
                                    EnableContentControlEvents();
                                    Application.ScreenUpdating = true;
                                    wordSettings.RestoreSettings(Application);
                                }

                                if (contentControl != null)
                                {
                                    document.CustomXMLParts.Add(XmlHelper.AddHeader(cml));
                                    Application.Selection.SetRange(contentControl.Range.Start, contentControl.Range.End);
                                }
                            }

                            ClearChemistryContextMenus();
                            _markAsChemistryHandled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        #endregion Events

        #region Methods

        private void HandleRightClick(Word.Selection selection)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _markAsChemistryHandled = false;

            var selectedWords = new List<TargetWord>();

            try
            {
                if (LibraryNames == null)
                {
                    LoadNamesFromLibrary();
                }

                if (LibraryNames != null && LibraryNames.Any())
                {
                    // Limit to selections which have less than 5 sentences
                    if (selection != null && selection.Sentences != null && selection.Sentences.Count <= 5)
                    {
                        var activeDocument = Application.ActiveDocument;
                        if (activeDocument != null)
                        {
                            var last = activeDocument.Range().End;
                            var sentenceCount = selection.Sentences.Count;
                            // Handling the selected text sentence by sentence should make us immune to return character sizing.
                            for (var i = 1; i <= sentenceCount; i++)
                            {
                                // GitHub: Issue #10 https://github.com/Chem4Word/Version3/issues/10
                                try
                                {
                                    var sentence = selection.Sentences[i];
                                    var start = Math.Max(sentence.Start, selection.Start);
                                    start = Math.Max(0, start);
                                    var end = Math.Min(selection.End, sentence.End);
                                    end = Math.Min(end, last);
                                    if (start < end)
                                    {
                                        var range = activeDocument.Range(start, end);
                                        //Exclude any ranges which contain content controls
                                        if (range.ContentControls.Count == 0)
                                        {
                                            var sentenceText = range.Text;
                                            if (!string.IsNullOrEmpty(sentenceText))
                                            {
                                                foreach (var kvp in LibraryNames)
                                                {
                                                    var idx = sentenceText.IndexOf(kvp.Key, StringComparison.InvariantCultureIgnoreCase);
                                                    if (idx >= 0)
                                                    {
                                                        var tw = new TargetWord
                                                        {
                                                            ChemicalName = kvp.Key,
                                                            Start = start + idx,
                                                            ChemistryId = kvp.Value,
                                                            End = start + idx + kvp.Key.Length
                                                        };
                                                        selectedWords.Add(tw);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Telemetry.Write(module, "Exception", $"Handled; Sentences[{i}] of {sentenceCount} not found");
                                    Telemetry.Write(module, "Exception", e.Message);
                                    Telemetry.Write(module, "Exception", e.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (COMException cex)
            {
                var comCode = HexErrorCode(cex.ErrorCode);
                switch (comCode)
                {
                    case "0x800A1200":
                        ChemistryAllowed = false;
                        ChemistryProhibitedReason = "can't create a selection object";
                        break;

                    default:
                        // Keep exception hidden from end user.
                        Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                        Telemetry.Write(module, "Exception", cex.Message);
                        Telemetry.Write(module, "Exception", cex.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                // Keep exception hidden from end user.
                Telemetry.Write(module, "Exception", ex.Message);
                Telemetry.Write(module, "Exception", ex.ToString());
            }

            ClearChemistryContextMenus();

            if (selectedWords.Count > 0)
            {
                AddChemistryMenuPopup(selectedWords);
            }
        }

        #endregion Methods

        [HandleProcessCorruptedStateExceptions]
        private void AddChemistryMenuPopup(List<TargetWord> selectedWords)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Application.Documents.Count > 0)
                {
                    var vstoObject = WordExtensions.DocumentExtensions.GetVstoObject(Application.ActiveDocument, Globals.Factory);
                    Application.CustomizationContext = vstoObject.AttachedTemplate;

                    foreach (var contextMenuName in ContextMenusTargets)
                    {
                        var contextMenu = Application.CommandBars[contextMenuName];
                        if (contextMenu != null)
                        {
                            var popupControl = (CommandBarPopup)contextMenu.Controls.Add(
                                MsoControlType.msoControlPopup,
                                Type.Missing, Type.Missing, Type.Missing, true);
                            if (popupControl != null)
                            {
                                popupControl.Caption = ContextMenuText;
                                popupControl.Tag = ContextMenuTag;
                                foreach (var word in selectedWords)
                                {
                                    var button = (CommandBarButton)popupControl.Controls.Add(
                                        MsoControlType.msoControlButton,
                                        Type.Missing, Type.Missing, Type.Missing, true);
                                    if (button != null)
                                    {
                                        button.Caption = word.ChemicalName;
                                        button.Tag = JsonConvert.SerializeObject(word);
                                        button.FaceId = 1241;
                                        button.Click += OnCommandBarButtonClick;
                                    }
                                }
                            }
                        }
                    }

                    ((Word.Template)vstoObject.AttachedTemplate).Saved = true;
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void ClearChemistryContextMenus()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (Application.Documents.Count > 0)
                {
                    var vstoObject = WordExtensions.DocumentExtensions.GetVstoObject(Application.ActiveDocument, Globals.Factory);
                    Application.CustomizationContext = vstoObject.AttachedTemplate;

                    foreach (var contextMenuName in ContextMenusTargets)
                    {
                        var contextMenu = Application.CommandBars[contextMenuName];
                        if (contextMenu != null)
                        {
                            var popupControl = (CommandBarPopup)contextMenu.FindControl(
                                MsoControlType.msoControlPopup, Type.Missing,
                                ContextMenuTag, true, true);
                            if (popupControl != null
                                && popupControl.Caption.Equals(ContextMenuText))
                            {
                                popupControl.Delete(true);
                            }
                        }
                    }

                    ((Word.Template)vstoObject.AttachedTemplate).Saved = true;
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        #endregion Right Click

        #region Document Events

        private void OnNewDocument(Word.Document document)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                var autoUpdateFrequency = Constants.DefaultCheckInterval;
                try
                {
                    if (Ribbon != null)
                    {
                        if (SystemOptions == null && Helper != null)
                        {
                            LoadOptions();
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (SystemOptions == null && Helper != null)
                    {
                        LoadOptions();
                    }

                    if (Telemetry == null || Helper == null)
                    {
                        RegistryHelper.StoreException(module, exception);
                    }
                    else
                    {
                        using (var form = new ReportError(Telemetry, WordTopLeft, module, exception))
                        {
                            form.ShowDialog();
                        }
                    }

                    UpdateHelper.ClearSettings();
                    if (SystemOptions != null)
                    {
                        autoUpdateFrequency = SystemOptions.AutoUpdateFrequency;
                    }
                    UpdateHelper.CheckForUpdates(autoUpdateFrequency);
                }
            }
        }

        private void OnDocumentChange()
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (Application.Documents.Count > 0)
            {
                if (VersionsBehind < Constants.MaximumVersionsBehind)
                {
                    try
                    {
                        Word.Document document = null;
                        try
                        {
                            document = Application.ActiveDocument;
                        }
                        catch
                        {
                            // This only happens when document is in protected mode
                            SetButtonStates(ButtonState.NoDocument);
                        }

                        if (document != null)
                        {
                            var docxMode = document.CompatibilityMode >= (int)Word.WdCompatibilityMode.wdWord2010;

                            if (Ribbon != null)
                            {
                                Ribbon.ShowNavigator.Checked = false;
                                Ribbon.ShowLibrary.Checked = LibraryState;
                                Ribbon.ShowLibrary.Label = Ribbon.ShowLibrary.Checked ? "Hide" : "Show";
                            }

                            HandleNavigatorPane(document);
                            HandleLibraryPane(document, docxMode);

                            if (docxMode)
                            {
                                // Call disable first to ensure events not registered multiple times
                                DisableContentControlEvents();
                                EnableContentControlEvents();

                                ClearChemistryContextMenus();

                                SelectChemistry(document.Application.Selection);
                                EvaluateChemistryAllowed();

                                if (!ChemistryAllowed)
                                {
                                    SetButtonStates(ButtonState.NoDocument);
                                }
                            }
                            else
                            {
                                SetButtonStates(ButtonState.NoDocument);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (SystemOptions == null)
                        {
                            LoadOptions();
                        }

                        using (var form = new ReportError(Telemetry, WordTopLeft, module, exception))
                        {
                            form.ShowDialog();
                        }

                        UpdateHelper.ClearSettings();
                        var autoUpdateFrequency = Constants.DefaultCheckInterval;
                        if (SystemOptions != null)
                        {
                            autoUpdateFrequency = SystemOptions.AutoUpdateFrequency;
                        }
                        UpdateHelper.CheckForUpdates(autoUpdateFrequency);
                    }
                }
                else
                {
                    SetButtonStates(ButtonState.Disabled);
                }
            }
        }

        private void HandleLibraryPane(Word.Document document, bool showPane, bool clear = false)
        {
            #region Handle Library Task Panes

            try
            {
                var libraryFound = false;

                foreach (var taskPane in CustomTaskPanes)
                {
                    if (taskPane.Window != null)
                    {
                        var documentName = ((Word.Window)taskPane.Window).Document.Name;
                        if (document.Name.Equals(documentName))
                        {
                            if (taskPane.Title.Equals(Constants.LibraryTaskPaneTitle))
                            {
                                if (Ribbon != null)
                                {
                                    if (clear)
                                    {
                                        (taskPane.Control as LibraryHost)?.Clear();
                                    }
                                    if (!showPane)
                                    {
                                        Ribbon.ShowLibrary.Checked = false;
                                    }

                                    taskPane.Visible = Ribbon.ShowLibrary.Checked;
                                    Ribbon.ShowLibrary.Label = Ribbon.ShowLibrary.Checked ? "Hide" : "Show";
                                }

                                libraryFound = true;
                                break;
                            }
                        }
                    }
                }

                if (!libraryFound)
                {
                    if (Ribbon != null && Ribbon.ShowLibrary.Checked)
                    {
                        if (showPane)
                        {
                            var custTaskPane =
                                CustomTaskPanes.Add(new LibraryHost(),
                                                    Constants.LibraryTaskPaneTitle, Application.ActiveWindow);
                            // Opposite side to Navigator's default placement
                            custTaskPane.DockPosition = MsoCTPDockPosition.msoCTPDockPositionLeft;
                            custTaskPane.Width = WordWidth / 4;
                            custTaskPane.VisibleChanged += Ribbon.OnVisibleChanged_LibraryPane;
                            custTaskPane.Visible = true;
                            (custTaskPane.Control as LibraryHost)?.Refresh();
                        }
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            #endregion Handle Library Task Panes
        }

        private void HandleNavigatorPane(Word.Document document)
        {
            #region Handle Navigator Task Panes

            try
            {
                foreach (var taskPane in CustomTaskPanes)
                {
                    if (taskPane.Window != null)
                    {
                        var documentName = ((Word.Window)taskPane.Window).Document.Name;
                        if (document.Name.Equals(documentName))
                        {
                            if (taskPane.Title.Equals(Constants.NavigatorTaskPaneTitle))
                            {
                                if (Ribbon != null)
                                {
                                    Ribbon.ShowNavigator.Checked = taskPane.Visible;
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            #endregion Handle Navigator Task Panes
        }

        private void OnDocumentOpen(Word.Document document)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
#if DEBUG
                    Telemetry.Write(module, "Information", $"Checking if upgrade of document [{document.DocID}] is required.");
#endif
                    var answer = Upgrader.UpgradeIsRequired(document);
                    switch (answer)
                    {
                        case DialogResult.Yes:
                            if (SystemOptions == null)
                            {
                                LoadOptions();
                            }

                            Upgrader.DoUpgrade(document);
                            break;

                        case DialogResult.No:
                            Telemetry.Write(module, "Information", "User chose not to upgrade");
                            break;

                        case DialogResult.Cancel:
                            // Returns Cancel if nothing to do
                            break;
                    }

                    if (Ribbon != null)
                    {
                        if (SystemOptions == null)
                        {
                            LoadOptions();
                        }
                        Debug.WriteLine(module);
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    var autoUpdateFrequency = Constants.DefaultCheckInterval;
                    if (SystemOptions != null)
                    {
                        autoUpdateFrequency = SystemOptions.AutoUpdateFrequency;
                    }
                    UpdateHelper.CheckForUpdates(autoUpdateFrequency);
                }
            }
        }

        private void OnDocumentBeforeSave(Word.Document document, ref bool saveAsUi, ref bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Debug.WriteLine($"OnDocumentBeforeSave({document.Name})");

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (!document.ReadOnly)
                    {
                        if (Upgrader.LegacyChemistryCount(document) == 0)
                        {
                            // Handle Word 2013+ AutoSave
                            if (WordVersion >= 2013)
                            {
                                if (!document.IsInAutosave)
                                {
                                    CustomXmlPartHelper.RemoveOrphanedXmlParts(document);
                                }
                            }
                            else
                            {
                                CustomXmlPartHelper.RemoveOrphanedXmlParts(document);
                            }
                        }
                    }
                }
                catch (COMException cex)
                {
                    var comCode = HexErrorCode(cex.ErrorCode);
                    switch (comCode)
                    {
                        case "0xE0041804":
                            Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                            Telemetry.Write(module, "Exception", $"Handled {cex.Message}");
                            Telemetry.Write(module, "Exception", cex.ToString());
                            break;

                        default:
                            // Keep exception hidden from end user.
                            Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                            Telemetry.Write(module, "Exception", cex.Message);
                            Telemetry.Write(module, "Exception", cex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    RegistryHelper.StoreException(module, ex);

                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    // Keep exception hidden from end user.
                    //using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    //{
                    //    form.ShowDialog();
                    //}

                    UpdateHelper.ClearSettings();
                    var autoUpdateFrequency = Constants.DefaultCheckInterval;
                    if (SystemOptions != null)
                    {
                        autoUpdateFrequency = SystemOptions.AutoUpdateFrequency;
                    }
                    UpdateHelper.CheckForUpdates(autoUpdateFrequency);
                }
            }
        }

        private void OnDocumentBeforeClose(Word.Document document, ref bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Debug.WriteLine($"OnDocumentBeforeClose({document.Name})");
            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (Ribbon != null)
                    {
                        SetButtonStates(ButtonState.NoDocument);
                    }

                    var app = Application;
                    OfficeTools.CustomTaskPane custTaskPane = null;

                    foreach (var taskPane in CustomTaskPanes)
                    {
                        try
                        {
                            if (app.ActiveWindow == taskPane.Window)
                            {
                                custTaskPane = taskPane;
                            }
                        }
                        catch
                        {
                            // Nothing much we can do here!
                        }
                    }
                    if (custTaskPane != null)
                    {
                        try
                        {
                            CustomTaskPanes.Remove(custTaskPane);
                        }
                        catch
                        {
                            // Nothing much we can do here!
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    var autoUpdateFrequency = Constants.DefaultCheckInterval;
                    if (SystemOptions != null)
                    {
                        autoUpdateFrequency = SystemOptions.AutoUpdateFrequency;
                    }
                    UpdateHelper.CheckForUpdates(autoUpdateFrequency);
                }
            }
        }

        #endregion Document Events

        #region Window Events

        private void OnWindowSelectionChange(Word.Selection selection)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (EventsEnabled)
                    {
                        EventsEnabled = false;

                        SelectChemistry(selection);
                        EvaluateChemistryAllowed();

                        if (!ChemistryAllowed)
                        {
                            SetButtonStates(ButtonState.NoDocument);
                        }

                        EventsEnabled = true;
                    }

                    // Deliberate crash to test Error Reporting
                    //int ii = 2;
                    //int dd = 0;
                    //int bang = ii / dd;
                }
                catch (COMException cex)
                {
                    RegistryHelper.StoreException(module, cex);
                }
                catch (ThreadAbortException tex)
                {
                    RegistryHelper.StoreException(module, tex);
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    if (Telemetry != null)
                    {
                        using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                        {
                            form.ShowDialog();
                        }
                    }
                    else
                    {
                        RegistryHelper.StoreException(module, ex);
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        public void EvaluateChemistryAllowed(bool inRightClick = false)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
            var isBeta = betaValue != null && bool.Parse(betaValue);

            if (IsEndOfLife || VersionsBehind >= Constants.MaximumVersionsBehind
                            || !WordIsActivated
                            || isBeta && !VersionAvailableIsBeta)
            {
                if (isBeta && !VersionAvailableIsBeta)
                {
                    ChemistryProhibitedReason = Constants.Chem4WordIsBeta;
                    ChemistryAllowed = false;
                }

                if (IsEndOfLife || VersionsBehind >= Constants.MaximumVersionsBehind)
                {
                    ChemistryProhibitedReason = Constants.Chem4WordTooOld;
                    ChemistryAllowed = false;
                }

                if (!WordIsActivated)
                {
                    ChemistryProhibitedReason = Constants.WordIsNotActivated;
                    ChemistryAllowed = false;
                }
            }
            else
            {
                var allowed = true;

                try
                {
                    if (Application.Documents != null && Application.Documents.Count > 0)
                    {
                        Word.Document activeDocument = null;
                        try
                        {
                            activeDocument = Application.ActiveDocument;
                        }
                        catch
                        {
                            // This only happens when document is in protected mode
                            allowed = false;
                            activeDocument = null;
                            ChemistryProhibitedReason = "document is readonly.";
                        }

                        if (allowed && activeDocument != null)
                        {
                            try
                            {
                                if (activeDocument.CompatibilityMode < (int)Word.WdCompatibilityMode.wdWord2010)
                                {
                                    allowed = false;
                                    ChemistryProhibitedReason = "document is in compatibility mode.";
                                }
                            }
                            catch
                            {
                                allowed = false;
                                ChemistryProhibitedReason = "can't determine if document is in compatibility mode.";
                            }

                            try
                            {
                                //if (doc.CoAuthoring.Conflicts.Count > 0) // <-- This clears current selection ???
                                if (activeDocument.CoAuthoring.Locks.Count > 0)
                                {
                                    allowed = false;
                                    ChemistryProhibitedReason = "document is in co-authoring mode.";
                                }
                            }
                            catch
                            {
                                // CoAuthoring or Conflicts/Locks may not be initialised!
                            }

                            try
                            {
                                if (allowed && activeDocument.IsSubdocument)
                                {
                                    ChemistryProhibitedReason = "current document is a sub document.";
                                    allowed = false;
                                }
                            }
                            catch
                            {
                                allowed = false;
                                ChemistryProhibitedReason = "can't determine if document is a sub document.";
                            }

                            var sel = Application.Selection;
                            if (allowed && sel != null)
                            {
                                if (!inRightClick)
                                {
                                    if (allowed && sel.Start != sel.End)
                                    {
                                        if (!string.IsNullOrEmpty(sel.Text) && sel.Text.Contains("\r"))
                                        {
                                            ChemistryProhibitedReason = "selection contains Line Ending";
                                            allowed = false;
                                        }
                                    }

                                    if (allowed && sel.Paragraphs.Count > 1)
                                    {
                                        ChemistryProhibitedReason = $"selection contains {sel.Paragraphs.Count} paragraphs.";
                                        allowed = false;
                                    }
                                }

                                if (allowed && sel.OMaths.Count > 0)
                                {
                                    ChemistryProhibitedReason = "selection is in an Equation.";
                                    allowed = false;
                                }

                                if (allowed && sel.Tables.Count > 0)
                                {
                                    try
                                    {
                                        if (sel.Cells.Count > 1)
                                        {
                                            ChemistryProhibitedReason = "selection contains more than one cell of a table.";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // Cells may not be initialised!
                                    }
                                }

                                if (allowed)
                                {
                                    try
                                    {
                                        var story = sel.StoryType;
                                        if (story != Word.WdStoryType.wdMainTextStory)
                                        {
                                            ChemistryProhibitedReason = $"selection is in a '{DecodeStoryType(story)}' story.";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // ComException 0x80004005
                                        ChemistryProhibitedReason = "can't determine which part of the story the selection point is.";
                                        allowed = false;
                                    }
                                }

                                if (allowed)
                                {
                                    var ccCount = sel.ContentControls.Count;
                                    if (ccCount > 1)
                                    {
                                        allowed = false;
                                        ChemistryProhibitedReason = "more than one ContentControl is selected";
                                    }
                                }

                                if (allowed)
                                {
                                    Word.WdContentControlType? contentControlType = null;
                                    var title = "";
                                    foreach (Word.ContentControl ccd in activeDocument.ContentControls)
                                    {
                                        if (ccd.Range.Start <= sel.Range.Start && ccd.Range.End >= sel.Range.End)
                                        {
                                            contentControlType = ccd.Type;
                                            title = ccd.Title;
                                            break;
                                        }
                                    }

                                    if (contentControlType != null)
                                    {
                                        if (!string.IsNullOrEmpty(title) && title.Equals(Constants.ContentControlTitle))
                                        {
                                            // Handle old Word 2007 style
                                            if (contentControlType != Word.WdContentControlType.wdContentControlRichText
                                                && contentControlType != Word.WdContentControlType.wdContentControlPicture)
                                            {
                                                allowed = false;
                                                ChemistryProhibitedReason =
                                                    $"selection is in a '{DecodeContentControlType(contentControlType)}' Content Control.";
                                            }
                                        }
                                        else
                                        {
                                            if (contentControlType != Word.WdContentControlType.wdContentControlRichText)
                                            {
                                                allowed = false;
                                                ChemistryProhibitedReason =
                                                    $"selection is in a '{DecodeContentControlType(contentControlType)}' Content Control";
                                            }

                                            // Test for Shape inside CC which is not ours
                                            if (allowed)
                                            {
                                                try
                                                {
                                                    if (sel.ShapeRange.Count > 0)
                                                    {
                                                        ChemistryProhibitedReason = "selection contains shape(s) inside Content Control.";
                                                        allowed = false;
                                                    }
                                                }
                                                catch
                                                {
                                                    // Shape may not evaluate
                                                }
                                            }
                                        }
                                    }
                                }

                                // Test for Shape in document body
                                if (allowed)
                                {
                                    try
                                    {
                                        if (sel.ShapeRange.Count > 0)
                                        {
                                            ChemistryProhibitedReason = "selection contains shape(s).";
                                            allowed = false;
                                        }
                                    }
                                    catch
                                    {
                                        // Shape may not evaluate
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        allowed = false;
                        ChemistryProhibitedReason = "no document is open.";
                    }
                }
                catch (COMException cex)
                {
                    var comCode = HexErrorCode(cex.ErrorCode);
                    switch (comCode)
                    {
                        case "0x80004005":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "can't determine where the current selection is.";
                            break;

                        case "0x800A11FD":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "changes are not permitted in the current selection.";
                            break;

                        case "0x800A1759":
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = "can't create a selection when a dialogue is active.";
                            break;

                        default:
                            ChemistryAllowed = false;
                            ChemistryProhibitedReason = $"COMException {cex.Message} ErrorCode: {comCode}";
                            if (Telemetry != null)
                            {
                                // Keep exception hidden from end user.
                                Telemetry.Write(module, "Exception", $"ErrorCode: {comCode}");
                                Telemetry.Write(module, "Exception", cex.Message);
                                Telemetry.Write(module, "Exception", cex.ToString());
                            }
                            else
                            {
                                RegistryHelper.StoreException(module, cex);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (Telemetry != null)
                    {
                        // Keep exception hidden from end user.
                        Telemetry.Write(module, "Exception", ex.Message);
                        Telemetry.Write(module, "Exception", ex.ToString());
                    }
                    else
                    {
                        RegistryHelper.StoreException(module, ex);
                    }
                }

                ChemistryAllowed = allowed;
                if (!allowed)
                {
                    Debug.WriteLine($"ChemistryProhibitedReason: {ChemistryProhibitedReason}");
                }
            }
        }

        private string HexErrorCode(int code)
        {
            try
            {
                var x = code & 0xFFFFFFFF;
                return string.Format("0x{0:X}", x);
            }
            catch
            {
                return string.Empty;
            }
        }

        private object DecodeStoryType(Word.WdStoryType storyType)
        {
            // Data from https://msdn.microsoft.com/en-us/vba/word-vba/articles/wdstorytype-enumeration-word
            string result;

            switch (storyType)
            {
                case Word.WdStoryType.wdCommentsStory:
                    result = "Comments";
                    break;

                case Word.WdStoryType.wdEndnoteContinuationNoticeStory:
                    result = "Endnote continuation notice";
                    break;

                case Word.WdStoryType.wdEndnoteContinuationSeparatorStory:
                    result = "Endnote continuation separator";
                    break;

                case Word.WdStoryType.wdEndnoteSeparatorStory:
                    result = "Endnote separator";
                    break;

                case Word.WdStoryType.wdEndnotesStory:
                    result = "Endnotes";
                    break;

                case Word.WdStoryType.wdEvenPagesFooterStory:
                    result = "Even pages footer";
                    break;

                case Word.WdStoryType.wdEvenPagesHeaderStory:
                    result = "Even pages header";
                    break;

                case Word.WdStoryType.wdFirstPageFooterStory:
                    result = "First page footer";
                    break;

                case Word.WdStoryType.wdFirstPageHeaderStory:
                    result = "First page header";
                    break;

                case Word.WdStoryType.wdFootnoteContinuationNoticeStory:
                    result = "Footnote continuation notice";
                    break;

                case Word.WdStoryType.wdFootnoteContinuationSeparatorStory:
                    result = "Footnote continuation separator";
                    break;

                case Word.WdStoryType.wdFootnoteSeparatorStory:
                    result = "Footnote separator";
                    break;

                case Word.WdStoryType.wdFootnotesStory:
                    result = "Footnotes";
                    break;

                case Word.WdStoryType.wdMainTextStory:
                    result = "Main text";
                    break;

                case Word.WdStoryType.wdPrimaryFooterStory:
                    result = "Primary footer";
                    break;

                case Word.WdStoryType.wdPrimaryHeaderStory:
                    result = "Primary header";
                    break;

                case Word.WdStoryType.wdTextFrameStory:
                    result = "Text frame";
                    break;

                default:
                    result = storyType.ToString();
                    break;
            }

            return result;
        }

        private static string DecodeContentControlType(Word.WdContentControlType? contentControlType)
        {
            // Date from https://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.wdcontentcontroltype(v=office.14).aspx
            string result;

            switch (contentControlType)
            {
                case Word.WdContentControlType.wdContentControlRichText:
                    result = "Rich-Text";
                    break;

                case Word.WdContentControlType.wdContentControlText:
                    result = "Text";
                    break;

                case Word.WdContentControlType.wdContentControlBuildingBlockGallery:
                    result = "Picture";
                    break;

                case Word.WdContentControlType.wdContentControlComboBox:
                    result = "ComboBox";
                    break;

                case Word.WdContentControlType.wdContentControlDropdownList:
                    result = "Drop-Down List";
                    break;

                case Word.WdContentControlType.wdContentControlPicture:
                    result = "Building Block Gallery";
                    break;

                case Word.WdContentControlType.wdContentControlDate:
                    result = "Date";
                    break;

                case Word.WdContentControlType.wdContentControlGroup:
                    result = "Group";
                    break;

                case Word.WdContentControlType.wdContentControlCheckBox:
                    result = "CheckBox";
                    break;

                case Word.WdContentControlType.wdContentControlRepeatingSection:
                    result = "Repeating Section";
                    break;

                default:
                    result = contentControlType.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="selection">The current selection.</param>
        /// <param name="cancel">False when the event occurs.
        /// If the event procedure sets this argument to True, the default double-click action does not occur when the procedure is finished.</param>
        private void OnWindowBeforeDoubleClick(Word.Selection selection, ref bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    if (EventsEnabled && _chemistrySelected)
                    {
                        Telemetry.Write(module, "Information", "Double click on Chemistry");
                        EventsEnabled = false;
                        CustomRibbon.PerformEdit();
                        EventsEnabled = true;
                        UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                        cancel = true;
                    }
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <param name="window"></param>
        private void OnWindowActivate(Word.Document document, Word.Window window)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            if (VersionsBehind < Constants.MaximumVersionsBehind)
            {
                try
                {
                    Debug.WriteLine($"{module.Replace("()", $"({document.Name})")}");

                    EvaluateChemistryAllowed();

                    // Deliberate crash to test Error Reporting
                    //int ii = 2;
                    //int dd = 0;
                    //int bang = ii / dd;
                }
                catch (Exception ex)
                {
                    if (SystemOptions == null)
                    {
                        LoadOptions();
                    }

                    using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }

                    UpdateHelper.ClearSettings();
                    UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
                }
            }
        }

        #endregion Window Events

        #region Content Control Events

        private void OnContentControlAfterAdd(Word.ContentControl newContentControl, bool inUndoRedo)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                if (EventsEnabled)
                {
                    var thisDocument = newContentControl.Application.ActiveDocument;
                    var ccId = newContentControl.ID;
                    var ccTag = newContentControl.Tag;
                    if (!inUndoRedo && !string.IsNullOrEmpty(ccTag))
                    {
                        var xmlPartFound = false;

                        if (!ccId.Equals(_lastContentControlAdded))
                        {
                            // Check that tag looks like it might be a C4W Tag
                            var regex = @"^[0-9a-fmn.:]+$";
                            var match = Regex.Match(ccTag, regex, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                var prefix = CustomXmlPartHelper.PrefixFromTag(ccTag);
                                var guid = CustomXmlPartHelper.GuidFromTag(ccTag);

                                var message = $"ContentControl {ccId} added at position {newContentControl.Range.Start}; Looking for structure {ccTag} in document [{thisDocument.DocID}]";
                                Telemetry.Write(module, "Information", message);

                                var cxml = CustomXmlPartHelper.GetCustomXmlPart(thisDocument, guid);
                                if (cxml != null)
                                {
                                    Telemetry.Write(module, "Information", $"Found CustomXmlPart for structure {ccTag} in this document.");
                                    xmlPartFound = true;
                                }
                                else
                                {
                                    if (Globals.Chem4WordV3.Application.Documents.Count > 1)
                                    {
                                        var foundIn = string.Empty;
                                        cxml = CustomXmlPartHelper.FindCustomXmlPartInOtherDocuments(guid, thisDocument.Name, ref foundIn);
                                        if (cxml != null)
                                        {
                                            Telemetry.Write(module, "Information", $"Found CustomXmlPart for structure {ccTag} in other document [{foundIn}], adding it into this.");

                                            // Generate new molecule Guid and apply it
                                            var newGuid = Guid.NewGuid().ToString("N");
                                            if (string.IsNullOrEmpty(prefix))
                                            {
                                                newContentControl.Tag = newGuid;
                                            }
                                            else
                                            {
                                                newContentControl.Tag = $"{prefix}:{newGuid}";
                                            }

                                            var cmlConverter = new CMLConverter();
                                            var model = cmlConverter.Import(cxml.XML);
                                            model.CustomXmlPartGuid = newGuid;

                                            thisDocument.CustomXMLParts.Add(XmlHelper.AddHeader(cmlConverter.Export(model)));
                                            Telemetry.Write(module, "Information", $"Added CustomXmlPart {newGuid} in document [{thisDocument.DocID}]");
                                            xmlPartFound = true;
                                        }
                                    }
                                }
                            }

                            if (xmlPartFound)
                            {
                                _lastContentControlAdded = ccId;
                            }
                            else
                            {
                                Telemetry.Write(module, "Warning", $"CustomXmlPart with tag {ccTag} not found in any open document(s)");
                                newContentControl.Title = $"{Constants.ContentControlTitle}-Missing";
                            }
                        }
                        else
                        {
                            Telemetry.Write(module, "Warning", $"CustomXmlPart with tag {ccTag} not searched for");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        /// Fires before a content control is deleted
        /// </summary>
        /// <param name="contentControl"></param>
        /// <param name="cancel"></param>
        private void OnContentControlBeforeDelete(Word.ContentControl contentControl, bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        /// Fires when a content control is exited
        /// </summary>
        /// <param name="contentControl"></param>
        /// <param name="cancel"></param>
        private void OnContentControlOnExit(Word.ContentControl contentControl, ref bool cancel)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        /// <summary>
        /// Fires when a content control is entered
        /// </summary>
        /// <param name="contentControl"></param>
        private void OnContentControlOnEnter(Word.ContentControl contentControl)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                if (EventsEnabled)
                {
                    EventsEnabled = false;
                    EvaluateChemistryAllowed();
                    EventsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (SystemOptions == null)
                {
                    LoadOptions();
                }

                using (var form = new ReportError(Telemetry, WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                UpdateHelper.ClearSettings();
                UpdateHelper.CheckForUpdates(SystemOptions.AutoUpdateFrequency);
            }
        }

        #endregion Content Control Events

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new EventHandler(C4WAddIn_Startup);
            this.Shutdown += new EventHandler(C4WAddIn_Shutdown);
        }

        #endregion VSTO generated code
    }

    public enum ButtonState
    {
        Disabled,
        NoDocument,
        CanEdit,
        CanInsert
    }
}