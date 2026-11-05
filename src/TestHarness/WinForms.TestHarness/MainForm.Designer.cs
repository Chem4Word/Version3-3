namespace WinForms.TestHarness
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.LoadStructure = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.EditWithAcme = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.Undo = new System.Windows.Forms.Button();
            this.Redo = new System.Windows.Forms.Button();
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DisplayHost = new System.Windows.Forms.Integration.ElementHost();
            this.RedoHost = new System.Windows.Forms.Integration.ElementHost();
            this.UndoHost = new System.Windows.Forms.Integration.ElementHost();
            this.Information1 = new System.Windows.Forms.Label();
            this.EditCml = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SmilesString = new System.Windows.Forms.TextBox();
            this.ParseSmiles = new System.Windows.Forms.Button();
            this.LayoutStructureV2 = new System.Windows.Forms.Button();
            this.CalculateProperties = new System.Windows.Forms.Button();
            this.LayoutStructureV1 = new System.Windows.Forms.Button();
            this.SearchOpsin = new System.Windows.Forms.Button();
            this.EditLabels = new System.Windows.Forms.Button();
            this.SearchChEBI = new System.Windows.Forms.Button();
            this.SearchPubChem = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ChangeOoXmlSettings = new System.Windows.Forms.Button();
            this.ShowCml = new System.Windows.Forms.Button();
            this.SaveStructure = new System.Windows.Forms.Button();
            this.ClearChemistry = new System.Windows.Forms.Button();
            this.RenderOoXml = new System.Windows.Forms.Button();
            this.Information2 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.LayoutPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadStructure
            // 
            this.LoadStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadStructure.Enabled = false;
            this.LoadStructure.Location = new System.Drawing.Point(12, 570);
            this.LoadStructure.Name = "LoadStructure";
            this.LoadStructure.Size = new System.Drawing.Size(75, 23);
            this.LoadStructure.TabIndex = 0;
            this.LoadStructure.Text = "Load";
            this.LoadStructure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LoadStructure.UseVisualStyleBackColor = true;
            this.LoadStructure.Click += new System.EventHandler(this.OnClick_LoadStructure);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // EditWithAcme
            // 
            this.EditWithAcme.Enabled = false;
            this.EditWithAcme.Location = new System.Drawing.Point(6, 18);
            this.EditWithAcme.Name = "EditWithAcme";
            this.EditWithAcme.Size = new System.Drawing.Size(75, 23);
            this.EditWithAcme.TabIndex = 2;
            this.EditWithAcme.Text = "ACME";
            this.EditWithAcme.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditWithAcme.UseVisualStyleBackColor = true;
            this.EditWithAcme.Click += new System.EventHandler(this.OnClick_EditWithAcme);
            // 
            // Undo
            // 
            this.Undo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Undo.Enabled = false;
            this.Undo.Location = new System.Drawing.Point(186, 570);
            this.Undo.Name = "Undo";
            this.Undo.Size = new System.Drawing.Size(75, 23);
            this.Undo.TabIndex = 11;
            this.Undo.Text = "Undo";
            this.Undo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Undo.UseVisualStyleBackColor = true;
            this.Undo.Click += new System.EventHandler(this.OnClick_Undo);
            // 
            // Redo
            // 
            this.Redo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Redo.Enabled = false;
            this.Redo.Location = new System.Drawing.Point(186, 628);
            this.Redo.Name = "Redo";
            this.Redo.Size = new System.Drawing.Size(75, 23);
            this.Redo.TabIndex = 12;
            this.Redo.Text = "Redo";
            this.Redo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Redo.UseVisualStyleBackColor = true;
            this.Redo.Click += new System.EventHandler(this.OnClick_Redo);
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutPanel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.LayoutPanel.ColumnCount = 3;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.Controls.Add(this.DisplayHost, 1, 0);
            this.LayoutPanel.Controls.Add(this.RedoHost, 2, 0);
            this.LayoutPanel.Controls.Add(this.UndoHost, 0, 0);
            this.LayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 1;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.Size = new System.Drawing.Size(1160, 513);
            this.LayoutPanel.TabIndex = 13;
            // 
            // DisplayHost
            // 
            this.DisplayHost.BackColor = System.Drawing.Color.White;
            this.DisplayHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayHost.Location = new System.Drawing.Point(278, 3);
            this.DisplayHost.Name = "DisplayHost";
            this.DisplayHost.Size = new System.Drawing.Size(604, 507);
            this.DisplayHost.TabIndex = 1;
            this.DisplayHost.Text = "centreHost";
            this.DisplayHost.Child = null;
            // 
            // RedoHost
            // 
            this.RedoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RedoHost.Location = new System.Drawing.Point(888, 3);
            this.RedoHost.Name = "RedoHost";
            this.RedoHost.Size = new System.Drawing.Size(269, 507);
            this.RedoHost.TabIndex = 2;
            this.RedoHost.Text = "rightHost";
            this.RedoHost.Child = null;
            // 
            // UndoHost
            // 
            this.UndoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UndoHost.Location = new System.Drawing.Point(3, 3);
            this.UndoHost.Name = "UndoHost";
            this.UndoHost.Size = new System.Drawing.Size(269, 507);
            this.UndoHost.TabIndex = 3;
            this.UndoHost.Text = "leftHost";
            this.UndoHost.Child = null;
            // 
            // Information1
            // 
            this.Information1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Information1.AutoSize = true;
            this.Information1.Location = new System.Drawing.Point(12, 532);
            this.Information1.Name = "Information1";
            this.Information1.Size = new System.Drawing.Size(16, 13);
            this.Information1.TabIndex = 14;
            this.Information1.Text = "...";
            // 
            // EditCml
            // 
            this.EditCml.Enabled = false;
            this.EditCml.Location = new System.Drawing.Point(6, 72);
            this.EditCml.Name = "EditCml";
            this.EditCml.Size = new System.Drawing.Size(75, 23);
            this.EditCml.TabIndex = 15;
            this.EditCml.Text = "Edit CML";
            this.EditCml.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditCml.UseVisualStyleBackColor = true;
            this.EditCml.Click += new System.EventHandler(this.OnClick_EditCml);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.SmilesString);
            this.groupBox1.Controls.Add(this.ParseSmiles);
            this.groupBox1.Controls.Add(this.LayoutStructureV2);
            this.groupBox1.Controls.Add(this.CalculateProperties);
            this.groupBox1.Controls.Add(this.LayoutStructureV1);
            this.groupBox1.Controls.Add(this.SearchOpsin);
            this.groupBox1.Controls.Add(this.EditLabels);
            this.groupBox1.Controls.Add(this.SearchChEBI);
            this.groupBox1.Controls.Add(this.SearchPubChem);
            this.groupBox1.Controls.Add(this.EditWithAcme);
            this.groupBox1.Controls.Add(this.EditCml);
            this.groupBox1.Location = new System.Drawing.Point(759, 549);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(413, 102);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edit / Search / WebServices";
            // 
            // SmilesString
            // 
            this.SmilesString.AcceptsReturn = true;
            this.SmilesString.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SmilesString.Location = new System.Drawing.Point(8, 46);
            this.SmilesString.Name = "SmilesString";
            this.SmilesString.Size = new System.Drawing.Size(396, 20);
            this.SmilesString.TabIndex = 23;
            this.SmilesString.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnKeyPress_SmilesString);
            // 
            // ParseSmiles
            // 
            this.ParseSmiles.Enabled = false;
            this.ParseSmiles.Location = new System.Drawing.Point(330, 18);
            this.ParseSmiles.Name = "ParseSmiles";
            this.ParseSmiles.Size = new System.Drawing.Size(75, 23);
            this.ParseSmiles.TabIndex = 22;
            this.ParseSmiles.Text = "SMILES";
            this.ParseSmiles.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ParseSmiles.UseVisualStyleBackColor = true;
            this.ParseSmiles.Click += new System.EventHandler(this.OnClick_ParseSmiles);
            // 
            // LayoutStructureV2
            // 
            this.LayoutStructureV2.Enabled = false;
            this.LayoutStructureV2.Location = new System.Drawing.Point(330, 72);
            this.LayoutStructureV2.Name = "LayoutStructureV2";
            this.LayoutStructureV2.Size = new System.Drawing.Size(75, 23);
            this.LayoutStructureV2.TabIndex = 21;
            this.LayoutStructureV2.Text = "Layout V2";
            this.LayoutStructureV2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.LayoutStructureV2.UseVisualStyleBackColor = true;
            this.LayoutStructureV2.Click += new System.EventHandler(this.OnClick_LayoutStructureV2);
            // 
            // CalculateProperties
            // 
            this.CalculateProperties.Enabled = false;
            this.CalculateProperties.Location = new System.Drawing.Point(168, 72);
            this.CalculateProperties.Name = "CalculateProperties";
            this.CalculateProperties.Size = new System.Drawing.Size(75, 23);
            this.CalculateProperties.TabIndex = 17;
            this.CalculateProperties.Text = "Properties";
            this.CalculateProperties.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CalculateProperties.UseVisualStyleBackColor = true;
            this.CalculateProperties.Click += new System.EventHandler(this.OnClick_CalculateProperties);
            // 
            // LayoutStructureV1
            // 
            this.LayoutStructureV1.Enabled = false;
            this.LayoutStructureV1.Location = new System.Drawing.Point(249, 72);
            this.LayoutStructureV1.Name = "LayoutStructureV1";
            this.LayoutStructureV1.Size = new System.Drawing.Size(75, 23);
            this.LayoutStructureV1.TabIndex = 20;
            this.LayoutStructureV1.Text = "Layout V1";
            this.LayoutStructureV1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.LayoutStructureV1.UseVisualStyleBackColor = true;
            this.LayoutStructureV1.Click += new System.EventHandler(this.OnClick_LayoutStructureV1);
            // 
            // SearchOpsin
            // 
            this.SearchOpsin.Enabled = false;
            this.SearchOpsin.Location = new System.Drawing.Point(249, 18);
            this.SearchOpsin.Name = "SearchOpsin";
            this.SearchOpsin.Size = new System.Drawing.Size(75, 23);
            this.SearchOpsin.TabIndex = 5;
            this.SearchOpsin.Text = "OPSIN";
            this.SearchOpsin.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchOpsin.UseVisualStyleBackColor = true;
            this.SearchOpsin.Click += new System.EventHandler(this.OnClick_SearchOpsin);
            // 
            // EditLabels
            // 
            this.EditLabels.Enabled = false;
            this.EditLabels.Location = new System.Drawing.Point(87, 72);
            this.EditLabels.Name = "EditLabels";
            this.EditLabels.Size = new System.Drawing.Size(75, 23);
            this.EditLabels.TabIndex = 16;
            this.EditLabels.Text = "Labels";
            this.EditLabels.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditLabels.UseVisualStyleBackColor = true;
            this.EditLabels.Click += new System.EventHandler(this.OnClick_EditLabels);
            // 
            // SearchChEBI
            // 
            this.SearchChEBI.Enabled = false;
            this.SearchChEBI.Location = new System.Drawing.Point(87, 18);
            this.SearchChEBI.Name = "SearchChEBI";
            this.SearchChEBI.Size = new System.Drawing.Size(75, 23);
            this.SearchChEBI.TabIndex = 3;
            this.SearchChEBI.Text = "ChEBI";
            this.SearchChEBI.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchChEBI.UseVisualStyleBackColor = true;
            this.SearchChEBI.Click += new System.EventHandler(this.OnClick_SearchChEBI);
            // 
            // SearchPubChem
            // 
            this.SearchPubChem.Enabled = false;
            this.SearchPubChem.Location = new System.Drawing.Point(168, 18);
            this.SearchPubChem.Name = "SearchPubChem";
            this.SearchPubChem.Size = new System.Drawing.Size(75, 23);
            this.SearchPubChem.TabIndex = 4;
            this.SearchPubChem.Text = "PubChem";
            this.SearchPubChem.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchPubChem.UseVisualStyleBackColor = true;
            this.SearchPubChem.Click += new System.EventHandler(this.OnClick_SearchPubChem);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.ChangeOoXmlSettings);
            this.groupBox2.Location = new System.Drawing.Point(279, 570);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(108, 81);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Settings ...";
            // 
            // ChangeOoXmlSettings
            // 
            this.ChangeOoXmlSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ChangeOoXmlSettings.Enabled = false;
            this.ChangeOoXmlSettings.Location = new System.Drawing.Point(15, 19);
            this.ChangeOoXmlSettings.Name = "ChangeOoXmlSettings";
            this.ChangeOoXmlSettings.Size = new System.Drawing.Size(75, 23);
            this.ChangeOoXmlSettings.TabIndex = 13;
            this.ChangeOoXmlSettings.Text = "OoXml";
            this.ChangeOoXmlSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ChangeOoXmlSettings.UseVisualStyleBackColor = true;
            this.ChangeOoXmlSettings.Click += new System.EventHandler(this.OnClick_ChangeOoXmlSettings);
            // 
            // ShowCml
            // 
            this.ShowCml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowCml.Enabled = false;
            this.ShowCml.Location = new System.Drawing.Point(12, 599);
            this.ShowCml.Name = "ShowCml";
            this.ShowCml.Size = new System.Drawing.Size(156, 23);
            this.ShowCml.TabIndex = 18;
            this.ShowCml.Text = "View CML";
            this.ShowCml.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ShowCml.UseVisualStyleBackColor = true;
            this.ShowCml.Click += new System.EventHandler(this.OnClick_ShowCml);
            // 
            // SaveStructure
            // 
            this.SaveStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SaveStructure.Enabled = false;
            this.SaveStructure.Location = new System.Drawing.Point(93, 570);
            this.SaveStructure.Name = "SaveStructure";
            this.SaveStructure.Size = new System.Drawing.Size(75, 23);
            this.SaveStructure.TabIndex = 19;
            this.SaveStructure.Text = "Save ...";
            this.SaveStructure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.SaveStructure.UseVisualStyleBackColor = true;
            this.SaveStructure.Click += new System.EventHandler(this.SaveStructure_Click);
            // 
            // ClearChemistry
            // 
            this.ClearChemistry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ClearChemistry.Enabled = false;
            this.ClearChemistry.Location = new System.Drawing.Point(186, 599);
            this.ClearChemistry.Name = "ClearChemistry";
            this.ClearChemistry.Size = new System.Drawing.Size(75, 23);
            this.ClearChemistry.TabIndex = 0;
            this.ClearChemistry.Text = "Clear";
            this.ClearChemistry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ClearChemistry.Click += new System.EventHandler(this.OnClick_ClearChemistry);
            // 
            // RenderOoXml
            // 
            this.RenderOoXml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RenderOoXml.Enabled = false;
            this.RenderOoXml.Location = new System.Drawing.Point(12, 628);
            this.RenderOoXml.Name = "RenderOoXml";
            this.RenderOoXml.Size = new System.Drawing.Size(156, 23);
            this.RenderOoXml.TabIndex = 21;
            this.RenderOoXml.Text = "Render in Word";
            this.RenderOoXml.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RenderOoXml.UseVisualStyleBackColor = true;
            this.RenderOoXml.Click += new System.EventHandler(this.OnClick_RenderOoXml);
            // 
            // Information2
            // 
            this.Information2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Information2.AutoSize = true;
            this.Information2.Location = new System.Drawing.Point(12, 549);
            this.Information2.Name = "Information2";
            this.Information2.Size = new System.Drawing.Size(16, 13);
            this.Information2.TabIndex = 22;
            this.Information2.Text = "...";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.OnTick_Timer1);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 656);
            this.Controls.Add(this.Information2);
            this.Controls.Add(this.RenderOoXml);
            this.Controls.Add(this.ClearChemistry);
            this.Controls.Add(this.SaveStructure);
            this.Controls.Add(this.ShowCml);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Information1);
            this.Controls.Add(this.LayoutPanel);
            this.Controls.Add(this.Redo);
            this.Controls.Add(this.Undo);
            this.Controls.Add(this.LoadStructure);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ACME Test Bed";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_MainForm);
            this.Load += new System.EventHandler(this.OnLoad_MainForm);
            this.LayoutPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoadStructure;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Integration.ElementHost DisplayHost;
        private System.Windows.Forms.Button EditWithAcme;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button Undo;
        private System.Windows.Forms.Button Redo;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost RedoHost;
        private System.Windows.Forms.Integration.ElementHost UndoHost;
        private System.Windows.Forms.Label Information1;
        private System.Windows.Forms.Button EditCml;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button ShowCml;
        private System.Windows.Forms.Button SaveStructure;
        private System.Windows.Forms.Button ClearChemistry;
        private System.Windows.Forms.Button EditLabels;
        private System.Windows.Forms.Button LayoutStructureV1;
        private System.Windows.Forms.Button RenderOoXml;
        private System.Windows.Forms.Button ChangeOoXmlSettings;
        private System.Windows.Forms.Button SearchChEBI;
        private System.Windows.Forms.Button SearchOpsin;
        private System.Windows.Forms.Button SearchPubChem;
        private System.Windows.Forms.Button CalculateProperties;
        private System.Windows.Forms.Label Information2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button LayoutStructureV2;
        private System.Windows.Forms.Button ParseSmiles;
        private System.Windows.Forms.TextBox SmilesString;
    }
}

