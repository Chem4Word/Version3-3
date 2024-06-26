﻿using Chem4Word.Core.UI.Controls;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    partial class PubChemSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PubChemSettings));
            this.btnSetDefaults = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtPubChemRestUri = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPubChemWsUri = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nudResultsPerCall = new System.Windows.Forms.NumericUpDown();
            this.nudDisplayOrder = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudResultsPerCall)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDisplayOrder)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSetDefaults
            // 
            this.btnSetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetDefaults.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSetDefaults.Location = new System.Drawing.Point(386, 178);
            this.btnSetDefaults.Margin = new System.Windows.Forms.Padding(6);
            this.btnSetDefaults.Name = "btnSetDefaults";
            this.btnSetDefaults.Size = new System.Drawing.Size(80, 27);
            this.btnSetDefaults.TabIndex = 11;
            this.btnSetDefaults.Text = "Defaults";
            this.btnSetDefaults.UseVisualStyleBackColor = true;
            this.btnSetDefaults.Click += new System.EventHandler(this.btnSetDefaults_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOk.Location = new System.Drawing.Point(479, 178);
            this.btnOk.Margin = new System.Windows.Forms.Padding(6);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 27);
            this.btnOk.TabIndex = 10;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 102);
            this.label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 20);
            this.label5.TabIndex = 28;
            this.label5.Text = "Rest API Url";
            // 
            // txtPubChemRestUri
            // 
            this.txtPubChemRestUri.Location = new System.Drawing.Point(165, 99);
            this.txtPubChemRestUri.Margin = new System.Windows.Forms.Padding(5);
            this.txtPubChemRestUri.Name = "txtPubChemRestUri";
            this.txtPubChemRestUri.Size = new System.Drawing.Size(331, 27);
            this.txtPubChemRestUri.TabIndex = 27;
            this.txtPubChemRestUri.Text = "https://pubchem.ncbi.nlm.nih.gov/";
            this.txtPubChemRestUri.TextChanged += new System.EventHandler(this.txtPubChemRestUri_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 61);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 20);
            this.label3.TabIndex = 26;
            this.label3.Text = "WebService Url";
            // 
            // txtPubChemWsUri
            // 
            this.txtPubChemWsUri.Location = new System.Drawing.Point(165, 58);
            this.txtPubChemWsUri.Margin = new System.Windows.Forms.Padding(5);
            this.txtPubChemWsUri.Name = "txtPubChemWsUri";
            this.txtPubChemWsUri.Size = new System.Drawing.Size(331, 27);
            this.txtPubChemWsUri.TabIndex = 25;
            this.txtPubChemWsUri.Text = "https://eutils.ncbi.nlm.nih.gov/";
            this.txtPubChemWsUri.TextChanged += new System.EventHandler(this.txtPubChemWsUri_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 145);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 20);
            this.label1.TabIndex = 29;
            this.label1.Text = "Results per call";
            // 
            // nudResultsPerCall
            // 
            this.nudResultsPerCall.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudResultsPerCall.Location = new System.Drawing.Point(165, 142);
            this.nudResultsPerCall.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudResultsPerCall.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudResultsPerCall.Name = "nudResultsPerCall";
            this.nudResultsPerCall.Size = new System.Drawing.Size(70, 27);
            this.nudResultsPerCall.TabIndex = 30;
            this.nudResultsPerCall.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nudResultsPerCall.ValueChanged += new System.EventHandler(this.nudResultsPerCall_ValueChanged);
            // 
            // nudDisplayOrder
            // 
            this.nudDisplayOrder.Location = new System.Drawing.Point(165, 16);
            this.nudDisplayOrder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudDisplayOrder.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDisplayOrder.Name = "nudDisplayOrder";
            this.nudDisplayOrder.Size = new System.Drawing.Size(70, 27);
            this.nudDisplayOrder.TabIndex = 32;
            this.nudDisplayOrder.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudDisplayOrder.ValueChanged += new System.EventHandler(this.nudDisplayOrder_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 19);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 20);
            this.label2.TabIndex = 31;
            this.label2.Text = "Display Order";
            // 
            // PubChemSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 220);
            this.Controls.Add(this.nudDisplayOrder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nudResultsPerCall);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtPubChemRestUri);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtPubChemWsUri);
            this.Controls.Add(this.btnSetDefaults);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PubChemSettings";
            this.Text = "PubChem Search - Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
            this.Load += new System.EventHandler(this.Settings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudResultsPerCall)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDisplayOrder)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnSetDefaults;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtPubChemRestUri;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPubChemWsUri;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudResultsPerCall;
        private System.Windows.Forms.NumericUpDown nudDisplayOrder;
        private System.Windows.Forms.Label label2;
    }
}