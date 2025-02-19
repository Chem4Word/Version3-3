// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using Newtonsoft.Json;

namespace Chem4Word.Model2
{
    public class RenderingOptions
    {
        public bool ExplicitC { get; set; }

        public HydrogenLabels ExplicitH { get; set; } = HydrogenLabels.HeteroAndTerminal;

        public bool ShowMoleculeGrouping { get; set; } = true;

        public bool ShowColouredAtoms { get; set; } = true;

        public double DefaultBondLength { get; set; } = Constants.StandardBondLength;

        /// <summary>
        /// Creates Rendering Options with system defaults
        /// </summary>
        public RenderingOptions()
        {
        }

        /// <summary>
        /// Creates rending options from a json string
        /// </summary>
        /// <param name="json"></param>
        public RenderingOptions(string json)
        {
            var temp = JsonConvert.DeserializeObject<RenderingOptions>(json);
            ExplicitC = temp.ExplicitC;
            ExplicitH = temp.ExplicitH;
            ShowColouredAtoms = temp.ShowColouredAtoms;
            ShowMoleculeGrouping = temp.ShowMoleculeGrouping;
            DefaultBondLength = temp.DefaultBondLength;
        }

        /// <summary>
        /// Creates Rendering Options with setting from the model
        /// </summary>
        /// <param name="model"></param>
        public RenderingOptions(Model model)
        {
            ExplicitC = model.ExplicitC;
            ExplicitH = model.ExplicitH;
            ShowColouredAtoms = model.ShowColouredAtoms;
            ShowMoleculeGrouping = model.ShowMoleculeGrouping;
            DefaultBondLength = model.MeanBondLength;
        }

        /// <summary>
        /// Exports rendering options as json
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        /// <summary>
        /// Make a copy of the current options
        /// </summary>
        /// <returns></returns>
        public RenderingOptions Copy() =>
            new RenderingOptions
            {
                ShowColouredAtoms = ShowColouredAtoms,
                ShowMoleculeGrouping = ShowMoleculeGrouping,
                ExplicitC = ExplicitC,
                ExplicitH = ExplicitH,
                DefaultBondLength = DefaultBondLength
            };

        /// <summary>
        /// Are the two objects identical
        /// </summary>
        /// <param name="options">Options to compare to</param>
        /// <returns>true if they are the same</returns>
        public bool IsEqualTo(RenderingOptions options) =>
            ExplicitC == options.ExplicitC
            && ExplicitH == options.ExplicitH
            && ShowMoleculeGrouping == options.ShowMoleculeGrouping
            && ShowColouredAtoms == options.ShowColouredAtoms
            && DefaultBondLength == options.DefaultBondLength;
    }
}