﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Core.Helpers
{
    public static class Constants
    {
        public const string Chem4WordVersion = "3.3";
        public const string Chem4WordVersionFiles = "files3-3";
        public const string ContentControlTitle = "Chemistry";
        public const string LegacyContentControlTitle = "chemistry";
        public const string NavigatorTaskPaneTitle = "Navigator";
        public const string LibraryTaskPaneTitle = "Library";

        public const double TopLeftOffset = 24;
        public const string OoXmlBookmarkPrefix = "C4W_";
        public const string SQLiteStandardDriver = "SQLite Standard";

        public const string DefaultSaveFormat = "pbuff";

        public const string DefaultEditorPlugIn = "ACME Structure Editor";
        public const string DefaultRendererPlugIn = "Open Office Xml Renderer V4";

        public const int DefaultCheckInterval = 7;

        // Registry Locations
        public const string Chem4WordRegistryKey = @"SOFTWARE\Chem4Word V3";

        public const string RegistryValueNameLastCheck = "Last Update Check";
        public const string RegistryValueNameVersionsBehind = "Versions Behind";
        public const string RegistryValueNameAvailableVersion = "Available Version";
        public const string RegistryValueNameAvailableIsBeta = "Available Is Beta";
        public const string RegistryValueNameEndOfLife = "End of Life";

        public const string Chem4WordSetupRegistryKey = @"SOFTWARE\Chem4Word V3\Setup";
        public const string Chem4WordUpdateRegistryKey = @"SOFTWARE\Chem4Word V3\Update";
        public const string Chem4WordMsiActionsRegistryKey = @"SOFTWARE\Chem4Word V3\MsiActions";
        public const string Chem4WordMessagesRegistryKey = @"SOFTWARE\Chem4Word V3\Messages";
        public const string Chem4WordExceptionsRegistryKey = @"SOFTWARE\Chem4Word V3\Exceptions";
        public const string Chem4WordAzureSettingsRegistryKey = @"SOFTWARE\Chem4Word V3\AzureSettings";

        public const string XmlFileHeader = "<?xml version='1.0' encoding='utf-8'?>";
        public const string DummyMachineGuid = "90160000-000F-0000-0000-0000000FF1CE";

        // Update Checks
        public const int MaximumVersionsBehind = 7;

        public const string Chem4WordTooOld = "Chem4Word is too many versions old.";
        public const string Chem4WordIsBeta = "Chem4Word Beta testing is now closed.";
        public const string WordIsNotActivated = "Micrsoft Word is not activated.";

        // Bond length limits etc
        public const double MinimumBondLength = 5;

        public const double StandardBondLength = 20;
        public const double MaximumBondLength = 95;
        public const double BondLengthTolerance = 1;

        public static readonly string[] OurDomains = { "https://www.chem4word.co.uk", "http://www.chem4word.com", "https://chem4word.azurewebsites.net" };
    }
}