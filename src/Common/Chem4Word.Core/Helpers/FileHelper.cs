// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;

namespace Chem4Word.Core.Helpers
{
    public static class FileHelper
    {
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
    }
}