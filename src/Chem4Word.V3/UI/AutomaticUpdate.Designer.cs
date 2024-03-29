﻿namespace Chem4Word.UI
{
    partial class AutomaticUpdate
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutomaticUpdate));
            this.btnUpdateNow = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnUpdateLater = new System.Windows.Forms.Button();
            this.linkReleasesPage = new System.Windows.Forms.LinkLabel();
            this.lblAntiVirus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnUpdateNow
            // 
            this.btnUpdateNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdateNow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUpdateNow.Location = new System.Drawing.Point(672, 522);
            this.btnUpdateNow.Name = "btnUpdateNow";
            this.btnUpdateNow.Size = new System.Drawing.Size(100, 27);
            this.btnUpdateNow.TabIndex = 0;
            this.btnUpdateNow.Text = "Update Now";
            this.btnUpdateNow.UseVisualStyleBackColor = true;
            this.btnUpdateNow.Click += new System.EventHandler(this.OnUpdateNowClick);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.richTextBox1.Location = new System.Drawing.Point(12, 36);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(760, 469);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.Text = "";
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.OnRichTextBoxLinkClicked);
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblInfo.Location = new System.Drawing.Point(12, 9);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(110, 19);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "Update available";
            // 
            // btnUpdateLater
            // 
            this.btnUpdateLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdateLater.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUpdateLater.Location = new System.Drawing.Point(566, 522);
            this.btnUpdateLater.Name = "btnUpdateLater";
            this.btnUpdateLater.Size = new System.Drawing.Size(100, 27);
            this.btnUpdateLater.TabIndex = 3;
            this.btnUpdateLater.Text = "Update Later";
            this.btnUpdateLater.UseVisualStyleBackColor = true;
            this.btnUpdateLater.Click += new System.EventHandler(this.OnUpdateLaterClick);
            // 
            // linkReleasesPage
            // 
            this.linkReleasesPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.linkReleasesPage.AutoSize = true;
            this.linkReleasesPage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.linkReleasesPage.Location = new System.Drawing.Point(12, 531);
            this.linkReleasesPage.Name = "linkReleasesPage";
            this.linkReleasesPage.Size = new System.Drawing.Size(490, 19);
            this.linkReleasesPage.TabIndex = 5;
            this.linkReleasesPage.TabStop = true;
            this.linkReleasesPage.Text = "Click here to download directly from the releases page if automatic update fails";
            this.linkReleasesPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnReleasesPageLinkClicked);
            // 
            // lblAntiVirus
            // 
            this.lblAntiVirus.AutoSize = true;
            this.lblAntiVirus.Location = new System.Drawing.Point(16, 514);
            this.lblAntiVirus.Name = "lblAntiVirus";
            this.lblAntiVirus.Size = new System.Drawing.Size(469, 13);
            this.lblAntiVirus.TabIndex = 6;
            this.lblAntiVirus.Text = "NB: If you encounter any permission errors, please temporarily disable your Anti-" +
    "Virus and try again.";
            // 
            // AutomaticUpdate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.lblAntiVirus);
            this.Controls.Add(this.linkReleasesPage);
            this.Controls.Add(this.btnUpdateLater);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.btnUpdateNow);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AutomaticUpdate";
            this.Text = "Update Available";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutomaticUpdate_FormClosing);
            this.Load += new System.EventHandler(this.AutomaticUpdate_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUpdateNow;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnUpdateLater;
        private System.Windows.Forms.LinkLabel linkReleasesPage;
        private System.Windows.Forms.Label lblAntiVirus;
    }
}