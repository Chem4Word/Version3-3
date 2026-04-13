// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Renderer.OoXmlV4.Enums;
using IChem4Word.Contracts;
using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Chem4Word.Renderer.OoXmlV4
{
    public partial class OoXmlV4Settings : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public string SettingsPath { get; set; }

        public OoXmlV4Options RendererOptions { get; set; }

        private bool _dirty;
        private bool _inhibitEvents;

        public OoXmlV4Settings()
        {
            InitializeComponent();
        }

        private void OnLoad_Settings(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!PointHelper.PointIsEmpty(TopLeft))
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                    Screen screen = Screen.FromControl(this);
                    Point sensible = PointHelper.SensibleTopLeft(new Point(Left, Top), screen, Width, Height);
                    Left = (int)sensible.X;
                    Top = (int)sensible.Y;
                }

                TabPage tab1 = tabControlEx.TabPages[nameof(Rendering)];
                if (!tab1.HasChildren)
                {
                    tabControlEx.TabPages.Remove(Rendering);
                }

                RestoreControls();

#if DEBUG
#else
                tabControlEx.TabPages.Remove(Debug);
#endif
                _dirty = false;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_Ok(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                _dirty = false;
                RendererOptions.Save();
                DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnClick_SetDefaults(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                DialogResult dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == DialogResult.OK)
                {
                    RendererOptions.RestoreDefaults();
                    RestoreControls();
                    _dirty = true;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void RestoreControls()
        {
            _inhibitEvents = true;

            ConvexHullMode.Items.Clear();
            ConvexHullMode.Items.Add(HullType.BoundingBoxes);
            ConvexHullMode.Items.Add(HullType.SimpleHull);
            ConvexHullMode.Items.Add(HullType.ComplexHull);

            ClipCrossingBonds.Checked = RendererOptions.ClipCrossingBonds;

            // Debugging Options
            ClipBondLines.Checked = RendererOptions.ClipBondLines;
            ConvexHullMode.SelectedItem = RendererOptions.HullMode;
            ShowCharacterBox.Checked = RendererOptions.ShowCharacterBoundingBoxes;
            ShowMoleculeBox.Checked = RendererOptions.ShowMoleculeBoundingBoxes;
            ShowRingCentres.Checked = RendererOptions.ShowRingCentres;
            ShowAtomPositions.Checked = RendererOptions.ShowAtomPositions;
            ShowConvexHulls.Checked = RendererOptions.ShowHulls;
            ShowDoubleBondTrimmingLines.Checked = RendererOptions.ShowDoubleBondTrimmingLines;
            ShowBondDirection.Checked = RendererOptions.ShowBondDirection;
            ShowCharacterGroupsBox.Checked = RendererOptions.ShowCharacterGroupBoundingBoxes;
            ShowBondCrossingPoints.Checked = RendererOptions.ShowBondCrossingPoints;

            _inhibitEvents = false;
        }

        private void OnFormClosing_Settings(object sender, FormClosingEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                // Nothing to do here ...
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            if (_dirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");
                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;

                    case DialogResult.Yes:
                        RendererOptions.Save();
                        DialogResult = DialogResult.OK;
                        break;

                    case DialogResult.No:
                        DialogResult = DialogResult.Cancel;
                        break;
                }
            }
        }

        private void OnCheckedChanged_ClipLines(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ClipBondLines = ClipBondLines.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowRingCentres(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowRingCentres = ShowRingCentres.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowMoleculeBox(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowMoleculeBoundingBoxes = ShowMoleculeBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowCharacterBox(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowCharacterBoundingBoxes = ShowCharacterBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowAtomCentres(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowAtomPositions = ShowAtomPositions.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowConvexHulls(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowHulls = ShowConvexHulls.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowDoubleBondTrimmingLines(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowDoubleBondTrimmingLines = ShowDoubleBondTrimmingLines.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowBondDirection(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowBondDirection = ShowBondDirection.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowCharacterGroupsBox(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowCharacterGroupBoundingBoxes = ShowCharacterGroupsBox.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ShowBondCrossingPoints(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ShowBondCrossingPoints = ShowBondCrossingPoints.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnCheckedChanged_ClipCrossingBonds(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                RendererOptions.ClipCrossingBonds = ClipCrossingBonds.Checked;
                _dirty = true;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void OnSelectedIndexChanged_ConvexHullMode(object sender, EventArgs e)
        {
            if (!_inhibitEvents)
            {
                if (ConvexHullMode.SelectedItem is HullType type)
                {
                    if (RendererOptions.HullMode != type)
                    {
                        RendererOptions.HullMode = type;
                        _dirty = true;
                    }
                }
            }
        }
    }
}
