// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Enums;
using Chem4Word.Core.Enums;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2.Enums;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Wpf.UI.Sandbox.Forms
{
    /// <summary>
    /// Interaction logic for CompassTesting.xaml
    /// </summary>
    public partial class CompassTesting : Window
    {
        public CompassTesting()
        {
            InitializeComponent();

            // Set some initial values
            Compass1.SelectedCompassPoint = CompassPoints.East;
            Compass2.SelectedCompassPoint = CompassPoints.West;

            Compass3.SelectedElectronValues = new Dictionary<CompassPoints, ElectronType>
                                              {
                                                  {
                                                      CompassPoints.NorthEast, ElectronType.LonePair
                                                  }
                                              };
        }

        private void OnCompassValueChanged_Compass(object sender, WpfEventArgs e)
        {
            if (sender is Compass compass)
            {
                switch (compass.CompassControlType)
                {
                    case CompassControlType.Hydrogens:
                        Status1.Text = $"Selected direction is {compass.SelectedCompassPoint}";
                        break;

                    case CompassControlType.FunctionalGroups:
                        Status2.Text = $"Selected direction is {compass.SelectedCompassPoint}";
                        break;

                    case CompassControlType.Electrons:
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine($"{compass.SelectedElectronValues.Count} Electron Values selected");
                        foreach (KeyValuePair<CompassPoints, ElectronType> keyValuePair in compass.SelectedElectronValues)
                        {
                            stringBuilder.AppendLine($"  {keyValuePair.Key} is {keyValuePair.Value}");
                        }

                        Status3.Text = stringBuilder.ToString();
                        break;
                }
            }
        }
    }
}
