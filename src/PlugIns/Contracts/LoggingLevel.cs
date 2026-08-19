// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;

namespace IChem4Word.Contracts
{
    public enum LoggingLevel
    {
        [Description("Verbose")]
        Verbose,

        [Description("Debug")]
        Debug,

        [Description("Information")]
        Information,

        [Description("Warning")]
        Warning,

        [Description("Warning (Data)")]
        WarningData,

        [Description("Warning (CML)")]
        WarningCml,

        [Description("Error")]
        Error,

        [Description("Search Failure")]
        SearchFailure,

        [Description("Search Failure (Data)")]
        SearchFailureData,

        [Description("Exception")]
        Exception,

        [Description("Exception (Data)")]
        ExceptionData,

        [Description("Exception (CML)")]
        ExceptionCml,

        [Description("StackTrace")]
        StackTrace,

        [Description("Action")]
        Action,

        [Description("Update")]
        Update,

        [Description("Msi Actions")]
        MsiActions,

        [Description("Setup")]
        Setup,

        [Description("Read Manual")]
        ReadManual,

        [Description("AutomaticUpdate")]
        AutomaticUpdate,

        [Description("Integrity")]
        Integrity,

        [Description("Timing")]
        Timing,
    }
}
