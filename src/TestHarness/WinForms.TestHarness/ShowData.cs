// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using ScintillaNET;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinForms.TestHarness
{
    public partial class ShowData : Form
    {
        public string Data { get; set; }

        public ShowData()
        {
            InitializeComponent();
        }

        private void OnLoad_ShowData(object sender, EventArgs e)
        {
            Timer1.Enabled = true;
        }

        private void SetBasicStyles(Scintilla scintilla)
        {
            scintilla.Lexer = Lexer.Null;

            // Show line numbers
            scintilla.Margins[0].Width = 50;
            scintilla.Margins[1].Width = 0;

            // Disable folding
            scintilla.SetProperty("fold", "0");

            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();
        }

        private void SetXmlStyles(Scintilla scintilla)
        {
            // Set the XML Lexer
            scintilla.Lexer = Lexer.Xml;

            scintilla.Styles[Style.Xml.Attribute].ForeColor = Color.Red;
            scintilla.Styles[Style.Xml.Entity].ForeColor = Color.Red;
            scintilla.Styles[Style.Xml.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Xml.Tag].ForeColor = Color.DarkRed;
            scintilla.Styles[Style.Xml.TagEnd].ForeColor = Color.DarkRed;
            scintilla.Styles[Style.Xml.DoubleString].ForeColor = Color.Blue;
            scintilla.Styles[Style.Xml.SingleString].ForeColor = Color.Blue;
        }

        private void SetXmlFolding(Scintilla scintilla)
        {
            // Enable folding
            scintilla.SetProperty("fold", "1");
            scintilla.SetProperty("fold.compact", "1");
            scintilla.SetProperty("fold.html", "1");

            // Use Margin 2 for fold markers
            scintilla.Margins[2].Type = MarginType.Symbol;
            scintilla.Margins[2].Mask = Marker.MaskFolders;
            scintilla.Margins[2].Sensitive = true;
            scintilla.Margins[2].Width = 20;

            // Reset folder markers
            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
            {
                scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
            }

            // Enable automatic folding
            scintilla.AutomaticFold = AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change;
            scintilla.SetFoldFlags(FoldFlags.LineAfterContracted);

            // Style the folder markers
            scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.Folder].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderEnd].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            foreach (Line line in scintilla.Lines)
            {
                string text = line.Text.Trim();

                switch (text)
                {
                    case "<cml:molecule>":
                        line.FoldLevelFlags = FoldLevelFlags.Header;
                        break;

                    case "<cml:atomArray>":
                    case "<cml:bondArray>":
                    case var _ when text.StartsWith("<cml:reactionScheme"):
                        line.FoldLevelFlags = FoldLevelFlags.Header;
                        line.ToggleFold();
                        break;
                }
            }
        }

        private void SetData(Scintilla scintilla)
        {
            scintilla.ReadOnly = false;
            scintilla.Text = Data;
            scintilla.ReadOnly = true;
        }

        private void OnTick_timer1(object sender, EventArgs e)
        {
            Timer1.Enabled = false;

            SetBasicStyles(Scintilla1);

            SetData(Scintilla1);

            if (Data.Contains("cml"))
            {
                SetXmlStyles(Scintilla1);
                SetXmlFolding(Scintilla1);
            }
        }
    }
}
