// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Chem4Word.Core.Helpers
{
    public static class FileHelper
    {
        // Remove any illegal windows characters
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Backs up a file
        /// </summary>
        /// <param name="file">File to be backed up</param>
        /// <param name="directory">Path to back up to</param>
        /// <param name="addPrefix">Prefix to add</param>
        /// <param name="moveFile">true if file is to be moved</param>
        /// <returns></returns>
        public static string BackupFile(FileInfo file, DirectoryInfo directory, bool addPrefix, bool moveFile)
        {
            var destination = Path.Combine(directory.FullName, file.Name);

            if (addPrefix)
            {
                destination = Path.Combine(directory.FullName, $"{SafeDate.ToIsoFilePrefix(DateTime.UtcNow)} - {file.Name}");
            }

            if (!File.Exists(destination))
            {
                File.Copy(file.FullName, destination, true);
                if (moveFile)
                {
                    File.Delete(file.FullName);
                }
            }
            else
            {
                // This could be recursive ...
                Debugger.Break();

                var info = new FileInfo(destination);
                BackupFile(info, directory, addPrefix, moveFile);
            }

            return destination;
        }

        /// <summary>
        /// Check to see if file is binary by checking if the first 8k characters contains at least n null characters
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="requiredConsecutiveNul"></param>
        /// <returns>true if file is detected as binary</returns>
        public static bool IsBinary(string filePath, int requiredConsecutiveNul = 1)
        {
            const int charsToCheck = 8096;
            const char nulChar = '\0';

            var nulCount = 0;

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using (var streamReader = new StreamReader(fileStream))
            {
                for (var i = 0; i < charsToCheck; i++)
                {
                    if (streamReader.EndOfStream)
                    {
                        return false;
                    }

                    if ((char)streamReader.Read() == nulChar)
                    {
                        nulCount++;

                        if (nulCount >= requiredConsecutiveNul)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        nulCount = 0;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see if a file is in use (locked)
        /// </summary>
        /// <param name="filePath">File to check</param>
        /// <param name="message">Message if locked</param>
        /// <returns>true if file is in use</returns>
        public static bool FileIsInUse(string filePath, out string message)
        {
            var result = false;
            message = string.Empty;

            try
            {
                using (new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // If we can open the file with exclusive access, it's not in use.
                }
            }
            catch (IOException exception)
            {
                // If an IOException is thrown, the file is in use.
                message = $"IOException: {exception.Message}";
                result = true;
            }
            catch (Exception exception)
            {
                message = $"Exception: {exception.Message}";
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Suggests a file name based on Chem4Word Guid (or Library id), Formula, Quick Name
        /// </summary>
        /// <param name="properties">dictionary of properties</param>
        /// <returns>string with suggested file name</returns>
        public static string SuggestedFileName(Dictionary<string, string> properties)
        {
            // ToDo: Add some bullet proofing

            List<string> parts = new List<string> { "Chem4Word" };

            if (properties.TryGetValue("Id", out string id))
            {
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                }

                if (!string.IsNullOrEmpty(id))
                {
                    parts.Add(id);
                }
            }

            if (properties.TryGetValue("Formula", out string formula))
            {
                formula = formula.Replace(" ", "");
                formula = formula.Replace(".", "");
                formula = new string(formula.Where(c => !InvalidChars.Contains(c)).ToArray());

                if (!string.IsNullOrEmpty(formula))
                {
                    parts.Add(formula);
                }
            }

            if (properties.TryGetValue("QuickName", out string quickName))
            {
                quickName = quickName.Replace(".", "");
                quickName = quickName.Replace(@"\", "");

                quickName = new string(quickName.Where(c => !InvalidChars.Contains(c)).ToArray());
                if (!string.IsNullOrEmpty(quickName))
                {
                    parts.Add(quickName);
                }
            }

            string result = string.Join("-", parts);

            // ToDo: Make this check the full file path
            return result.Substring(0, Math.Min(result.Length, 200));
        }
    }
}
