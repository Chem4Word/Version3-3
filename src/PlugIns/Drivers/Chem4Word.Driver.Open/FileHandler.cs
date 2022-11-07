// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Chem4Word.Driver.Open
{
    [Obsolete]
    public class FileHandler
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly IChem4WordTelemetry _telemetry;
        private string _programDataPath;

        public FileHandler(IChem4WordTelemetry telemetry, string programDataPath)
        {
            _telemetry = telemetry;
            _programDataPath = programDataPath;

            EnsureFoldersExist();
        }

        private void EnsureFoldersExist()
        {
            var path = Path.Combine(_programDataPath, "Libraries");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(_programDataPath, "Backups");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        internal void CreateFilesIfRequired()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var seed = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Libraries.json");
            var libraries = JsonConvert.DeserializeObject<ListOfLibraries>(seed);

            var settingsPath = Path.Combine(_programDataPath, "Libraries.json");
            if (!File.Exists(settingsPath))
            {
                // ToDo: Change all of this
                string legacyLibrary = Path.Combine(_programDataPath, Constants.LegacyLibraryFileName);
                if (File.Exists(legacyLibrary))
                {
                    // Move existing File and backups
                    _telemetry.Write(module, "Information", "Moving legacy Library database");
                }
                else
                {
                    string librariesPath = Path.Combine(_programDataPath, "Libraries");

                    // Create new files
                    _telemetry.Write(module, "Information", "Copying 'Starter Library' database");
                    Stream stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Starter Library.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                    }
                    _telemetry.Write(module, "Information", "Copying 'Plant Essential Oils' database");
                    stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Plant Essential Oils.zip");
                    if (stream != null)
                    {
                        var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(librariesPath);
                    }
                }
            }

            // Write libraries.json to disk
            var json = JsonConvert.SerializeObject(libraries, Formatting.Indented);
            File.WriteAllText(settingsPath, json);
        }

        public ListOfLibraries GetLibraries()
        {
            var libraries = new ListOfLibraries();
            var settingsPath = Path.Combine(_programDataPath, "Libraries.json");
            if (File.Exists(settingsPath))
            {
                string contents = File.ReadAllText(settingsPath);
                libraries = JsonConvert.DeserializeObject<ListOfLibraries>(contents);
            }

            return libraries;
        }
    }
}