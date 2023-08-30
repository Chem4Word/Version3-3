// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace IChem4Word.Contracts
{
    public interface IChem4WordLibraryBase
    {
        string Name { get; }

        string Description { get; }

        string FileName { get; set; }

        string BackupFolder { get; set; }

        Point TopLeft { get; set; }

        IChem4WordTelemetry Telemetry { get; set; }
    }
}