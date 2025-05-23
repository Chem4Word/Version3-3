﻿namespace Chem4Word.UI
{
    partial class SystemInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemInfo));
            this.Information = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Information
            // 
            this.Information.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Information.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Information.Location = new System.Drawing.Point(12, 12);
            this.Information.Multiline = true;
            this.Information.Name = "Information";
            this.Information.ReadOnly = true;
            this.Information.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Information.Size = new System.Drawing.Size(560, 337);
            this.Information.TabIndex = 0;
            this.Information.WordWrap = false;
            // 
            // SystemInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.Information);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SystemInfo";
            this.Text = "System Info";
            this.Load += new System.EventHandler(this.OnLoad_SystemInfo);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Information;
    }
}