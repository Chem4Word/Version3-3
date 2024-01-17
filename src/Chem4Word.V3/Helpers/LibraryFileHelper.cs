﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Chem4Word.Helpers
{
    public class LibraryFileHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod()?.DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private string _programDataPath;

        public LibraryFileHelper(IChem4WordTelemetry telemetry, string programDataPath)
        {
            _telemetry = telemetry;
            _programDataPath = programDataPath;

            EnsureFoldersExist();
        }

        public ListOfLibraries GetListOfLibraries(bool silent = false)
        {
            var module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod()?.Name}()";

            var sw = new Stopwatch();
            sw.Start();

            var result = new ListOfLibraries();
            var backupDirectory = new DirectoryInfo(Path.Combine(_programDataPath, "Libraries", "Backups"));
            var librariesDirectory = new DirectoryInfo(Path.Combine(_programDataPath, "Libraries"));

            var settingsFile = Path.Combine(_programDataPath, "Libraries.json");
            if (File.Exists(settingsFile))
            {
                var text = File.ReadAllText(settingsFile);
                result = JsonConvert.DeserializeObject<ListOfLibraries>(text);
            }
            else
            {
                // Move V3.2 Libraries
                result.DefaultLocation = librariesDirectory.FullName;
                var librariesPath = result.DefaultLocation;

                foreach (var file in Directory.GetFiles(_programDataPath, "*.db"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Name.StartsWith("20"))
                    {
                        // Move files '20*.db' to backups folder
                        _telemetry.Write(module, "Information", $"Moving {fileInfo.Name} to Backups folder");
                        FileHelper.BackupFile(fileInfo, backupDirectory, false, true);
                    }
                    else
                    {
                        // Move other files to Libraries folder
                        _telemetry.Write(module, "Information", $"Moving {fileInfo.Name} to Libraries folder");
                        var library = FileHelper.BackupFile(fileInfo, librariesDirectory, false, true);

                        // Add it's details
                        var details = new DatabaseDetails
                        {
                            Connection = library,
                            DisplayName = fileInfo.Name.Replace(fileInfo.Extension, ""),
                            Driver = Constants.SQLiteStandardDriver,
                            ShortFileName = fileInfo.Name
                        };
                        result.AvailableDatabases.Add(details);
                        result.SelectedLibrary = details.DisplayName;
                    }
                }

                // if not file exists 'Starter Library.db', then create it
                var starterPath = Path.Combine(librariesPath, "Starter Library.db");
                if (!File.Exists(starterPath))
                {
                    _telemetry.Write(module, "Information", "Creating 'Starter Library'");

                    var stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Starter Library.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                    }
                }

                // Add it into the list
                var starterDetails = new DatabaseDetails
                {
                    Connection = starterPath,
                    DisplayName = "Starter Library",
                    Driver = Constants.SQLiteStandardDriver,
                    ShortFileName = "Starter Library.db"
                };
                result.AvailableDatabases.Add(starterDetails);

                // if not file exists 'Plant Essential Oils.db', then create it
                var essentialOilsPath = Path.Combine(librariesPath, "Plant Essential Oils.db");
                if (!File.Exists(essentialOilsPath))
                {
                    _telemetry.Write(module, "Information", "Creating 'Plant Essential Oils'");

                    var stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Plant Essential Oils.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                    }
                }

                // Add it into the list
                var essentialOilsDetails = new DatabaseDetails
                {
                    Connection = essentialOilsPath,
                    DisplayName = "Plant Essential Oils",
                    Driver = Constants.SQLiteStandardDriver,
                    ShortFileName = "Plant Essential Oils.db"
                };
                result.AvailableDatabases.Add(essentialOilsDetails);

                if (string.IsNullOrEmpty(result.SelectedLibrary))
                {
                    result.SelectedLibrary = result.AvailableDatabases.FirstOrDefault()?.DisplayName;
                }

                SaveSettingsFile(result);
            }

            if (result != null)
            {
                foreach (var database in result.AvailableDatabases.ToList())
                {
                    _telemetry.Write(module, "Information", $"Reading properties of '{database.DisplayName}'");
                    var library = new Core.SqLite.Library(_telemetry, database.Connection, backupDirectory.FullName, Globals.Chem4WordV3.WordTopLeft);
                    if (library.Database.FileExists && library.Database.IsSqliteDatabase && library.Database.IsChem4Word)
                    {
                        database.Properties = library.Database.Properties;
                        database.IsReadOnly = library.Database.IsReadOnly;
                    }
                    else
                    {
                        result.AvailableDatabases.Remove(database);
                    }
                }

                if (result.AvailableDatabases.Any())
                {
                    var selectedDatabase = result.AvailableDatabases.FirstOrDefault(d => d.DisplayName.Equals(result.SelectedLibrary));
                    if (selectedDatabase == null)
                    {
                        var firstDatabase = result.AvailableDatabases.FirstOrDefault();
                        if (firstDatabase != null)
                        {
                            result.SelectedLibrary = firstDatabase.DisplayName;
                            SaveSettingsFile(result);
                        }
                    }
                }
                else
                {
                    DeleteSettingsFile();
                    result = null;
                }
            }

            sw.Stop();
            if (!silent)
            {
                _telemetry.Write(module, "Timing", $"Took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
            }

            return result;
        }

        private void DeleteSettingsFile()
        {
            var settingsFile = Path.Combine(_programDataPath, "Libraries.json");
            File.Delete(settingsFile);
        }

        public void SaveSettingsFile(ListOfLibraries libraries)
        {
            var settingsFile = Path.Combine(_programDataPath, "Libraries.json");
            var text = JsonConvert.SerializeObject(libraries, Formatting.Indented);
            File.WriteAllText(settingsFile, text);
        }

        private void EnsureFoldersExist()
        {
            var path = Path.Combine(_programDataPath, "Libraries");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}