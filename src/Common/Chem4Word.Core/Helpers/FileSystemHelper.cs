// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Chem4Word.Core.Helpers
{
    public static class FileSystemHelper
    {
        /// <summary>
        /// Checks to see if the user has write permission on a folder
        /// </summary>
        /// <param name="path">Folder to check</param>
        /// <returns>true if user can write to the folder</returns>
        public static bool UserHasWritePermission(string path)
        {
            var tempFile = Path.Combine(path, Guid.NewGuid().ToString("N")) + ".tmp";

            try
            {
                File.Create(tempFile).Close();
                File.Delete(tempFile);
                return true;
            }
            catch
            {
                // If anything goes wrong assume user can't read and write to the folder
                Debug.WriteLine($"Access denied to {path}");
                return false;
            }
        }

        /// <summary>
        /// Obtains a writable path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Path of where the user has write access</returns>
        public static string GetWritablePath(string path)
        {
            // 1. Check supplied path
            if (!string.IsNullOrEmpty(path) && UserHasWritePermission(path))
            {
                return path;
            }

            // 2. Executable path
            var assemblyInfo = Assembly.GetExecutingAssembly();
            var uriCodeBase = new Uri(assemblyInfo.CodeBase);
            path = Path.GetDirectoryName(uriCodeBase.LocalPath);
            if (UserHasWritePermission(path))
            {
                return path;
            }

            // 3. Local AppData Path i.e. "C:\Users\{User}\AppData\Local\"
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return UserHasWritePermission(path) ? path : null;
        }
    }
}