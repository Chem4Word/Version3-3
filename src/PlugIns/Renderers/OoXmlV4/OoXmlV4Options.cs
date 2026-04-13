// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Chem4Word.Renderer.OoXmlV4
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OoXmlV4Options
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

        // Standard - None

        // Debugging
        [JsonProperty]
        public bool ClipCrossingBonds { get; set; }

        [JsonProperty]
        public bool ClipBondLines { get; set; }

        [JsonProperty]
        public HullType HullMode { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowDoubleBondTrimmingLines { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowBondDirection { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowCharacterBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowCharacterGroupBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowMoleculeBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowAtomPositions { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowHulls { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowRingCentres { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowBondCrossingPoints { get; set; }

        // Not serialized
        public string SettingsPath { get; set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Load clean set of Chem4Word Options with default values
        /// </summary>
        public OoXmlV4Options()
        {
            Errors = new List<string>();
            RestoreDefaults();
        }

        /// <summary>
        /// Load set of OoXmlV4 options
        /// </summary>
        /// <param name="path">Folder where the OoXmlV4 options are to reside - pass null to load from default path</param>
        public OoXmlV4Options(string path)
        {
            SettingsPath = path;
            Errors = new List<string>();
            Load();
        }

        /// <summary>
        /// Load the OoXmlV4 Options from the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Load()
        {
            try
            {
                string path = FileSystemHelper.GetWritablePath(SettingsPath);

                if (!string.IsNullOrEmpty(path))
                {
                    string optionsFile = GetFileName(path);

                    if (File.Exists(optionsFile))
                    {
                        try
                        {
                            string contents = File.ReadAllText(optionsFile);
                            OoXmlV4Options options = JsonConvert.DeserializeObject<OoXmlV4Options>(contents);
                            SetValuesFromCopy(options);

                            string temp = JsonConvert.SerializeObject(options, Formatting.Indented);
                            if (!contents.Equals(temp))
                            {
                                // Auto fix the file if required
                                PersistOptions(optionsFile);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                            Errors.Add(exception.Message);
                            Errors.Add(exception.StackTrace);

                            RestoreDefaults();
                            PersistOptions(optionsFile);
                        }
                    }
                    else
                    {
                        RestoreDefaults();
                        PersistOptions(optionsFile);
                    }
                }
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        /// <summary>
        /// Save the OoXmlV4 Options to the path defined in SettingsPath using defaults if this is null or empty string
        /// </summary>
        public void Save()
        {
            string path = FileSystemHelper.GetWritablePath(SettingsPath);
            if (!string.IsNullOrEmpty(path))
            {
                string optionsFile = GetFileName(path);
                PersistOptions(optionsFile);
            }
        }

        private void PersistOptions(string optionsFile)
        {
            try
            {
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(optionsFile, contents);
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
                Errors.Add(exception.StackTrace);
            }
        }

        private void SetValuesFromCopy(OoXmlV4Options copy)
        {
            // Main User Options
            // None

            // Debugging Options
            ClipCrossingBonds = copy.ClipCrossingBonds;
            ClipBondLines = copy.ClipBondLines;
            HullMode = copy.HullMode;
            ShowDoubleBondTrimmingLines = copy.ShowDoubleBondTrimmingLines;
            ShowCharacterBoundingBoxes = copy.ShowCharacterBoundingBoxes;
            ShowMoleculeBoundingBoxes = copy.ShowMoleculeBoundingBoxes;
            ShowRingCentres = copy.ShowRingCentres;
            ShowAtomPositions = copy.ShowAtomPositions;
            ShowHulls = copy.ShowHulls;
            ShowBondDirection = copy.ShowBondDirection;
            ShowCharacterGroupBoundingBoxes = copy.ShowCharacterGroupBoundingBoxes;
            ShowBondCrossingPoints = copy.ShowBondCrossingPoints;
        }

        private string GetFileName(string path)
        {
            string fileName = $"{_product}.json";
            string optionsFile = Path.Combine(path, fileName);
            return optionsFile;
        }

        public OoXmlV4Options Clone()
        {
            OoXmlV4Options clone = new OoXmlV4Options();

            // Copy serialized properties
            clone.SetValuesFromCopy(this);

            clone.SettingsPath = SettingsPath;

            return clone;
        }

        public void RestoreDefaults()
        {
            // Main User Options
            // None

            // Debugging Options
            ClipCrossingBonds = false;
            ClipBondLines = true;
            HullMode = HullType.SimpleHull;
            ShowDoubleBondTrimmingLines = false;
            ShowCharacterBoundingBoxes = false;
            ShowMoleculeBoundingBoxes = false;
            ShowRingCentres = false;
            ShowAtomPositions = false;
            ShowHulls = false;
            ShowBondDirection = false;
            ShowCharacterGroupBoundingBoxes = false;
            ShowBondCrossingPoints = false;
        }
    }
}
