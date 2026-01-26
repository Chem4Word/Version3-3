using Chem4Word.Core.UI.Controls;

namespace Chem4Word.Renderer.OoXmlV4
{
    partial class OoXmlV4Settings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OoXmlV4Settings));
            this.tabControlEx = new Chem4Word.Core.UI.Controls.TabControlEx();
            this.Rendering = new System.Windows.Forms.TabPage();
            this.Debug = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ClipCrossingBonds = new System.Windows.Forms.CheckBox();
            this.ClipBondLines = new System.Windows.Forms.CheckBox();
            this.ShowBondDirection = new System.Windows.Forms.CheckBox();
            this.ShowDoubleBondTrimmingLines = new System.Windows.Forms.CheckBox();
            this.ShowBondCrossingPoints = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ShowCharacterGroupsBox = new System.Windows.Forms.CheckBox();
            this.ShowCharacterBox = new System.Windows.Forms.CheckBox();
            this.ShowAtomPositions = new System.Windows.Forms.CheckBox();
            this.ShowConvexHulls = new System.Windows.Forms.CheckBox();
            this.ShowRingCentres = new System.Windows.Forms.CheckBox();
            this.ShowMoleculeBox = new System.Windows.Forms.CheckBox();
            this.SetDefaults = new System.Windows.Forms.Button();
            this.Ok = new System.Windows.Forms.Button();
            this.tabControlEx.SuspendLayout();
            this.Debug.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlEx
            // 
            this.tabControlEx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlEx.Controls.Add(this.Rendering);
            this.tabControlEx.Controls.Add(this.Debug);
            this.tabControlEx.Location = new System.Drawing.Point(16, 17);
            this.tabControlEx.Margin = new System.Windows.Forms.Padding(4);
            this.tabControlEx.Name = "tabControlEx";
            this.tabControlEx.SelectedIndex = 0;
            this.tabControlEx.Size = new System.Drawing.Size(718, 477);
            this.tabControlEx.TabIndex = 0;
            // 
            // Rendering
            // 
            this.Rendering.BackColor = System.Drawing.SystemColors.Control;
            this.Rendering.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Rendering.Location = new System.Drawing.Point(0, 24);
            this.Rendering.Margin = new System.Windows.Forms.Padding(4);
            this.Rendering.Name = "Rendering";
            this.Rendering.Padding = new System.Windows.Forms.Padding(4);
            this.Rendering.Size = new System.Drawing.Size(718, 453);
            this.Rendering.TabIndex = 0;
            this.Rendering.Text = "Rendering";
            // 
            // Debug
            // 
            this.Debug.BackColor = System.Drawing.SystemColors.Control;
            this.Debug.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Debug.Controls.Add(this.groupBox2);
            this.Debug.Controls.Add(this.groupBox1);
            this.Debug.Controls.Add(this.ShowRingCentres);
            this.Debug.Controls.Add(this.ShowMoleculeBox);
            this.Debug.Location = new System.Drawing.Point(0, 24);
            this.Debug.Margin = new System.Windows.Forms.Padding(4);
            this.Debug.Name = "Debug";
            this.Debug.Padding = new System.Windows.Forms.Padding(4);
            this.Debug.Size = new System.Drawing.Size(718, 453);
            this.Debug.TabIndex = 1;
            this.Debug.Text = "Debug";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.ClipCrossingBonds);
            this.groupBox2.Controls.Add(this.ClipBondLines);
            this.groupBox2.Controls.Add(this.ShowBondDirection);
            this.groupBox2.Controls.Add(this.ShowDoubleBondTrimmingLines);
            this.groupBox2.Controls.Add(this.ShowBondCrossingPoints);
            this.groupBox2.Location = new System.Drawing.Point(321, 7);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(286, 204);
            this.groupBox2.TabIndex = 29;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Bonds";
            // 
            // ClipCrossingBonds
            // 
            this.ClipCrossingBonds.AutoSize = true;
            this.ClipCrossingBonds.Checked = true;
            this.ClipCrossingBonds.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ClipCrossingBonds.Location = new System.Drawing.Point(7, 26);
            this.ClipCrossingBonds.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ClipCrossingBonds.Name = "ClipCrossingBonds";
            this.ClipCrossingBonds.Size = new System.Drawing.Size(179, 23);
            this.ClipCrossingBonds.TabIndex = 27;
            this.ClipCrossingBonds.Text = "Clip Crossing Bond Lines";
            this.ClipCrossingBonds.UseVisualStyleBackColor = true;
            this.ClipCrossingBonds.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ClipCrossingBonds);
            // 
            // ClipBondLines
            // 
            this.ClipBondLines.AutoSize = true;
            this.ClipBondLines.Checked = true;
            this.ClipBondLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ClipBondLines.Location = new System.Drawing.Point(7, 92);
            this.ClipBondLines.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ClipBondLines.Name = "ClipBondLines";
            this.ClipBondLines.Size = new System.Drawing.Size(241, 23);
            this.ClipBondLines.TabIndex = 13;
            this.ClipBondLines.Text = "Clip bond lines [Atom Convex Hull]";
            this.ClipBondLines.UseVisualStyleBackColor = true;
            this.ClipBondLines.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ClipLines);
            // 
            // ShowBondDirection
            // 
            this.ShowBondDirection.AutoSize = true;
            this.ShowBondDirection.Checked = true;
            this.ShowBondDirection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowBondDirection.Location = new System.Drawing.Point(7, 158);
            this.ShowBondDirection.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowBondDirection.Name = "ShowBondDirection";
            this.ShowBondDirection.Size = new System.Drawing.Size(154, 23);
            this.ShowBondDirection.TabIndex = 24;
            this.ShowBondDirection.Text = "Show bond direction";
            this.ShowBondDirection.UseVisualStyleBackColor = true;
            this.ShowBondDirection.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowBondDirection);
            // 
            // ShowDoubleBondTrimmingLines
            // 
            this.ShowDoubleBondTrimmingLines.AutoSize = true;
            this.ShowDoubleBondTrimmingLines.Checked = true;
            this.ShowDoubleBondTrimmingLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowDoubleBondTrimmingLines.Location = new System.Drawing.Point(7, 125);
            this.ShowDoubleBondTrimmingLines.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowDoubleBondTrimmingLines.Name = "ShowDoubleBondTrimmingLines";
            this.ShowDoubleBondTrimmingLines.Size = new System.Drawing.Size(234, 23);
            this.ShowDoubleBondTrimmingLines.TabIndex = 23;
            this.ShowDoubleBondTrimmingLines.Text = "Show double bond trimming lines";
            this.ShowDoubleBondTrimmingLines.UseVisualStyleBackColor = true;
            this.ShowDoubleBondTrimmingLines.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowDoubleBondTrimmingLines);
            // 
            // ShowBondCrossingPoints
            // 
            this.ShowBondCrossingPoints.AutoSize = true;
            this.ShowBondCrossingPoints.Checked = true;
            this.ShowBondCrossingPoints.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowBondCrossingPoints.Location = new System.Drawing.Point(7, 59);
            this.ShowBondCrossingPoints.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowBondCrossingPoints.Name = "ShowBondCrossingPoints";
            this.ShowBondCrossingPoints.Size = new System.Drawing.Size(243, 23);
            this.ShowBondCrossingPoints.TabIndex = 26;
            this.ShowBondCrossingPoints.Text = "Show crossing bond clipping Points";
            this.ShowBondCrossingPoints.UseVisualStyleBackColor = true;
            this.ShowBondCrossingPoints.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowBondCrossingPoints);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ShowCharacterGroupsBox);
            this.groupBox1.Controls.Add(this.ShowCharacterBox);
            this.groupBox1.Controls.Add(this.ShowAtomPositions);
            this.groupBox1.Controls.Add(this.ShowConvexHulls);
            this.groupBox1.Location = new System.Drawing.Point(7, 7);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(297, 204);
            this.groupBox1.TabIndex = 28;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Atoms / Characters";
            // 
            // ShowCharacterGroupsBox
            // 
            this.ShowCharacterGroupsBox.AutoSize = true;
            this.ShowCharacterGroupsBox.Checked = true;
            this.ShowCharacterGroupsBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowCharacterGroupsBox.Location = new System.Drawing.Point(17, 127);
            this.ShowCharacterGroupsBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowCharacterGroupsBox.Name = "ShowCharacterGroupsBox";
            this.ShowCharacterGroupsBox.Size = new System.Drawing.Size(269, 23);
            this.ShowCharacterGroupsBox.TabIndex = 25;
            this.ShowCharacterGroupsBox.Text = "Show BoundingBox of character groups";
            this.ShowCharacterGroupsBox.UseVisualStyleBackColor = true;
            this.ShowCharacterGroupsBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowCharacterGroupsBox);
            // 
            // ShowCharacterBox
            // 
            this.ShowCharacterBox.AutoSize = true;
            this.ShowCharacterBox.Checked = true;
            this.ShowCharacterBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowCharacterBox.Location = new System.Drawing.Point(17, 59);
            this.ShowCharacterBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowCharacterBox.Name = "ShowCharacterBox";
            this.ShowCharacterBox.Size = new System.Drawing.Size(228, 23);
            this.ShowCharacterBox.TabIndex = 19;
            this.ShowCharacterBox.Text = "Show BoundingBox of characters";
            this.ShowCharacterBox.UseVisualStyleBackColor = true;
            this.ShowCharacterBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowCharacterBox);
            // 
            // ShowAtomPositions
            // 
            this.ShowAtomPositions.AutoSize = true;
            this.ShowAtomPositions.Checked = true;
            this.ShowAtomPositions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowAtomPositions.Location = new System.Drawing.Point(17, 25);
            this.ShowAtomPositions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowAtomPositions.Name = "ShowAtomPositions";
            this.ShowAtomPositions.Size = new System.Drawing.Size(156, 23);
            this.ShowAtomPositions.TabIndex = 21;
            this.ShowAtomPositions.Text = "Show atom positions";
            this.ShowAtomPositions.UseVisualStyleBackColor = true;
            this.ShowAtomPositions.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowAtomCentres);
            // 
            // ShowConvexHulls
            // 
            this.ShowConvexHulls.AutoSize = true;
            this.ShowConvexHulls.Checked = true;
            this.ShowConvexHulls.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowConvexHulls.Location = new System.Drawing.Point(17, 93);
            this.ShowConvexHulls.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowConvexHulls.Name = "ShowConvexHulls";
            this.ShowConvexHulls.Size = new System.Drawing.Size(216, 23);
            this.ShowConvexHulls.TabIndex = 22;
            this.ShowConvexHulls.Text = "Show ConvexHull of characters";
            this.ShowConvexHulls.UseVisualStyleBackColor = true;
            this.ShowConvexHulls.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowConvexHulls);
            // 
            // ShowRingCentres
            // 
            this.ShowRingCentres.AutoSize = true;
            this.ShowRingCentres.Checked = true;
            this.ShowRingCentres.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowRingCentres.Location = new System.Drawing.Point(24, 231);
            this.ShowRingCentres.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowRingCentres.Name = "ShowRingCentres";
            this.ShowRingCentres.Size = new System.Drawing.Size(210, 23);
            this.ShowRingCentres.TabIndex = 20;
            this.ShowRingCentres.Text = "Show centre of detected rings";
            this.ShowRingCentres.UseVisualStyleBackColor = true;
            this.ShowRingCentres.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowRingCentres);
            // 
            // ShowMoleculeBox
            // 
            this.ShowMoleculeBox.AutoSize = true;
            this.ShowMoleculeBox.Checked = true;
            this.ShowMoleculeBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowMoleculeBox.Location = new System.Drawing.Point(24, 264);
            this.ShowMoleculeBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShowMoleculeBox.Name = "ShowMoleculeBox";
            this.ShowMoleculeBox.Size = new System.Drawing.Size(234, 23);
            this.ShowMoleculeBox.TabIndex = 18;
            this.ShowMoleculeBox.Text = "Show BoundingBox of molecule(s)";
            this.ShowMoleculeBox.UseVisualStyleBackColor = true;
            this.ShowMoleculeBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_ShowMoleculeBox);
            // 
            // SetDefaults
            // 
            this.SetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SetDefaults.Location = new System.Drawing.Point(522, 505);
            this.SetDefaults.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SetDefaults.Name = "SetDefaults";
            this.SetDefaults.Size = new System.Drawing.Size(102, 37);
            this.SetDefaults.TabIndex = 13;
            this.SetDefaults.Text = "Defaults";
            this.SetDefaults.UseVisualStyleBackColor = true;
            this.SetDefaults.Click += new System.EventHandler(this.OnClick_SetDefaults);
            // 
            // Ok
            // 
            this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Ok.Location = new System.Drawing.Point(632, 505);
            this.Ok.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(102, 37);
            this.Ok.TabIndex = 12;
            this.Ok.Text = "OK";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.OnClick_Ok);
            // 
            // OoXmlV4Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(746, 558);
            this.Controls.Add(this.SetDefaults);
            this.Controls.Add(this.Ok);
            this.Controls.Add(this.tabControlEx);
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OoXmlV4Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_Settings);
            this.Load += new System.EventHandler(this.OnLoad_Settings);
            this.tabControlEx.ResumeLayout(false);
            this.Debug.ResumeLayout(false);
            this.Debug.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControlEx tabControlEx;
        private System.Windows.Forms.TabPage Rendering;
        private System.Windows.Forms.TabPage Debug;
        private System.Windows.Forms.Button SetDefaults;
        private System.Windows.Forms.Button Ok;
        private System.Windows.Forms.CheckBox ClipBondLines;
        private System.Windows.Forms.CheckBox ShowRingCentres;
        private System.Windows.Forms.CheckBox ShowCharacterBox;
        private System.Windows.Forms.CheckBox ShowMoleculeBox;
        private System.Windows.Forms.CheckBox ShowAtomPositions;
        private System.Windows.Forms.CheckBox ShowConvexHulls;
        private System.Windows.Forms.CheckBox ShowDoubleBondTrimmingLines;
        private System.Windows.Forms.CheckBox ShowBondDirection;
        private System.Windows.Forms.CheckBox ShowCharacterGroupsBox;
        private System.Windows.Forms.CheckBox ShowBondCrossingPoints;
        private System.Windows.Forms.CheckBox ClipCrossingBonds;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}