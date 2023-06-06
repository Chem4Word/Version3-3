// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
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

            var settingsFile = Path.Combine(_programDataPath, "Libraries.json");
            if (File.Exists(settingsFile))
            {
                var text = File.ReadAllText(settingsFile);
                result = JsonConvert.DeserializeObject<ListOfLibraries>(text);
            }
            else
            {
                // Move V3.2 Libraries
                result.DefaultLocation = Path.Combine(_programDataPath, "Libraries");
                var librariesPath = result.DefaultLocation;
                foreach (var file in Directory.GetFiles(_programDataPath, "*.db"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Name.StartsWith("20"))
                    {
                        _telemetry.Write(module, "Information", $"Moving {fileInfo.Name} to Backups folder");
                        // if any files '20*.db' exist, move them to backups folder
                        File.Move(file, Path.Combine(_programDataPath, "Libraries", "Backups", fileInfo.Name));
                    }
                    else
                    {
                        // if file 'Library.db' exists, move it
                        _telemetry.Write(module, "Information", $"Moving {fileInfo.Name} to Libraries folder");
                        File.Move(file, Path.Combine(_programDataPath, "Libraries", fileInfo.Name));
                        var details = new DatabaseDetails
                        {
                            Connection = Path.Combine(_programDataPath, "Libraries", fileInfo.Name),
                            DisplayName = fileInfo.Name.Replace(fileInfo.Extension, ""),
                            Driver = Constants.SQLiteStandardDriver,
                            ShortFileName = fileInfo.Name
                        };
                        result.AvailableDatabases.Add(details);
                        result.SelectedLibrary = details.DisplayName;
                    }
                }

                // if not file exists 'Starter Library.db', then create it
                var path1 = Path.Combine(librariesPath, "Starter Library.db");
                if (!File.Exists(path1))
                {
                    _telemetry.Write(module, "Information", "Creating 'Starter Library'");

                    var stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Starter Library.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                        var details = new DatabaseDetails
                        {
                            Connection = path1,
                            DisplayName = "Starter Library",
                            Driver = Constants.SQLiteStandardDriver,
                            ShortFileName = "Starter Library.db"
                        };
                        result.AvailableDatabases.Add(details);
                    }
                }

                // if not file exists 'Plant Essential Oils.db', then create it
                var path2 = Path.Combine(librariesPath, "Plant Essential Oils.db");
                if (!File.Exists(path2))
                {
                    _telemetry.Write(module, "Information", "Creating 'Plant Essential Oils'");

                    var stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Plant Essential Oils.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                        var details = new DatabaseDetails
                        {
                            Connection = path2,
                            DisplayName = "Plant Essential Oils",
                            Driver = Constants.SQLiteStandardDriver,
                            ShortFileName = "Plant Essential Oils.db"
                        };
                        result.AvailableDatabases.Add(details);
                    }
                }

                if (string.IsNullOrEmpty(result.SelectedLibrary))
                {
                    result.SelectedLibrary = result.AvailableDatabases.FirstOrDefault()?.DisplayName;
                }

                SaveFile(result);
            }

            if (result != null)
            {
                // Read in all Properties for each database
                // We should be able to always use the standard driver if the database is one of our SQLite ones.
                var driver = Globals.Chem4WordV3.GetDriverPlugIn(Constants.SQLiteStandardDriver);
                if (driver != null)
                {
                    foreach (var database in result.AvailableDatabases.ToList())
                    {
                        if (File.Exists(database.Connection))
                        {
                            _telemetry.Write(module, "Information", $"Reading properties of '{database.DisplayName}'");

                            driver.DatabaseDetails = new DatabaseDetails
                                                     {
                                                         DisplayName = database.DisplayName,
                                                         Connection = database.Connection,
                                                         Driver = database.Driver,
                                                         ShortFileName = database.ShortFileName
                                                     };
                            database.Properties = driver.GetProperties();
                            database.IsReadOnly = driver.GetDatabaseFileProperties(driver.DatabaseDetails).IsReadOnly;
                        }
                        else
                        {
                            result.AvailableDatabases.Remove(database);
                        }
                    }
                }

                var selectedDatabase = result.AvailableDatabases.FirstOrDefault(d => d.DisplayName.Equals(result.SelectedLibrary));
                if (selectedDatabase == null)
                {
                    var firstDatabase = result.AvailableDatabases.FirstOrDefault();
                    if (firstDatabase != null)
                    {
                        result.SelectedLibrary = firstDatabase.DisplayName;
                        SaveFile(result);
                    }
                }
            }

            sw.Stop();
            if (!silent)
            {
                _telemetry.Write(module, "Timing", $"Took {SafeDouble.AsString0(sw.ElapsedMilliseconds)}ms");
            }
            return result;
        }

        public void SaveFile(ListOfLibraries libraries)
        {
            // Write new 'Libraries.json' file
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