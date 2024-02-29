// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Chem4Word.Helpers
{
    public class C4wAddInInfo
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string AssemblyVersionNumber { get; }

        /// <summary>
        /// Name of product reflected from assembly i.e. "Chem4Word.V3"
        /// </summary>
        public string ProductName { get; }

        /// <summary>
        /// Where the AddIn is being run from i.e. "C:\Program Files (x86)\Chem4Word.V3"
        /// </summary>
        public string DeploymentPath { get; }

        /// <summary>
        /// Common Data Path i.e. C:\ProgramData\Chem4Word.V3
        /// </summary>
        public string ProgramDataPath { get; }

        /// <summary>
        /// Local AppData Path of Product i.e. "C:\Users\{User}\AppData\Local\Chem4Word.V3"
        /// </summary>
        public string ProductAppDataPath { get; }

        /// <summary>
        /// Local AppData Path i.e. "C:\Users\{User}\AppData\Local"
        /// </summary>
        public string AppDataPath { get; }

        public C4wAddInInfo()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                // Get the assembly information
                Assembly assemblyInfo = Assembly.GetExecutingAssembly();
                ProductName = assemblyInfo.FullName.Split(',')[0];

                // Get the assembly Version
                Version productVersion = assemblyInfo.GetName().Version;
                AssemblyVersionNumber = productVersion.ToString();

                // CodeBase is the location of the installed files
                Uri uriCodeBase = new Uri(assemblyInfo.CodeBase);
                DeploymentPath = Path.GetDirectoryName(uriCodeBase.LocalPath);

                // Get the user's Local AppData Path i.e. "C:\Users\{User}\AppData\Local\" and ensure that the Chem4Word user data folders exist
                AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                ProductAppDataPath = Path.Combine(AppDataPath, ProductName);
                if (!Directory.Exists(ProductAppDataPath))
                {
                    Directory.CreateDirectory(ProductAppDataPath);
                }

                var fileBackupsPath = Path.Combine(ProductAppDataPath, "Backups");
                if (!Directory.Exists(fileBackupsPath))
                {
                    Directory.CreateDirectory(fileBackupsPath);
                }

                var telemetryPath = Path.Combine(ProductAppDataPath, "Telemetry");
                if (!Directory.Exists(telemetryPath))
                {
                    Directory.CreateDirectory(telemetryPath);
                }

                // Get ProgramData Path i.e "C:\ProgramData\Chem4Word.V3" and ensure it exists
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                ProgramDataPath = Path.Combine(programData, ProductName);

                try
                {
                    if (!Directory.Exists(ProgramDataPath))
                    {
                        Directory.CreateDirectory(ProgramDataPath);
                    }

                    // Allow all users to Modify files in this folder
                    var accessControl = Directory.GetAccessControl(ProgramDataPath);
                    var securityIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    accessControl.AddAccessRule(new FileSystemAccessRule(securityIdentifier, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    Directory.SetAccessControl(ProgramDataPath, accessControl);
                }
                catch
                {
                    // Do Nothing
                }

                // User PlugIns - Remove old folders and files used to host previous closed driver

                ClearFolder(Path.Combine(ProgramDataPath, "Plugins", "Updates"));
                ClearFolder(Path.Combine(ProgramDataPath, "Plugins"));

                // Local Function
                void ClearFolder(string folderPath)
                {
                    try
                    {
                        if (Directory.Exists(folderPath))
                        {
                            var files = Directory.GetFiles(folderPath);
                            foreach (var file in files)
                            {
                                Debug.WriteLine($"Deleting file {file}");
                                File.Delete(file);
                            }
                            Debug.WriteLine($"Deleting folder {folderPath}");
                            Directory.Delete(folderPath);
                        }
                    }
                    catch
                    {
                        // Do Nothing
                    }
                }

                // User Libraries
                var librariesPath = Path.Combine(ProgramDataPath, "Libraries");
                if (!Directory.Exists(librariesPath))
                {
                    Directory.CreateDirectory(librariesPath);
                }

                var databaseBackupsPath = Path.Combine(ProgramDataPath, "Libraries", "Backups");
                if (!Directory.Exists(databaseBackupsPath))
                {
                    Directory.CreateDirectory(databaseBackupsPath);
                }
            }
            catch (Exception exception)
            {
                RegistryHelper.StoreException(module, exception);
            }
        }
    }
}