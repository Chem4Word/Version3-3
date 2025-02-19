namespace Chem4Word.UI
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
            this.UpdateNow = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.Info = new System.Windows.Forms.Label();
            this.UpdateLater = new System.Windows.Forms.Button();
            this.ReleasesPage = new System.Windows.Forms.LinkLabel();
            this.AntiVirus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // UpdateNow
            // 
            this.UpdateNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateNow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.UpdateNow.Location = new System.Drawing.Point(672, 522);
            this.UpdateNow.Name = "UpdateNow";
            this.UpdateNow.Size = new System.Drawing.Size(100, 27);
            this.UpdateNow.TabIndex = 0;
            this.UpdateNow.Text = "Update Now";
            this.UpdateNow.UseVisualStyleBackColor = true;
            this.UpdateNow.Click += new System.EventHandler(this.OnClick_UpdateNow);
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
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.OnLinkClicked_RichTextBox);
            // 
            // Info
            // 
            this.Info.AutoSize = true;
            this.Info.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Info.Location = new System.Drawing.Point(12, 9);
            this.Info.Name = "Info";
            this.Info.Size = new System.Drawing.Size(110, 19);
            this.Info.TabIndex = 2;
            this.Info.Text = "Update available";
            // 
            // UpdateLater
            // 
            this.UpdateLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateLater.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.UpdateLater.Location = new System.Drawing.Point(566, 522);
            this.UpdateLater.Name = "UpdateLater";
            this.UpdateLater.Size = new System.Drawing.Size(100, 27);
            this.UpdateLater.TabIndex = 3;
            this.UpdateLater.Text = "Update Later";
            this.UpdateLater.UseVisualStyleBackColor = true;
            this.UpdateLater.Click += new System.EventHandler(this.OnClick_UpdateLater);
            // 
            // ReleasesPage
            // 
            this.ReleasesPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ReleasesPage.AutoSize = true;
            this.ReleasesPage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ReleasesPage.Location = new System.Drawing.Point(12, 531);
            this.ReleasesPage.Name = "ReleasesPage";
            this.ReleasesPage.Size = new System.Drawing.Size(490, 19);
            this.ReleasesPage.TabIndex = 5;
            this.ReleasesPage.TabStop = true;
            this.ReleasesPage.Text = "Click here to download directly from the releases page if automatic update fails";
            this.ReleasesPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnLinkClicked_ReleasesPage);
            // 
            // AntiVirus
            // 
            this.AntiVirus.AutoSize = true;
            this.AntiVirus.Location = new System.Drawing.Point(16, 514);
            this.AntiVirus.Name = "AntiVirus";
            this.AntiVirus.Size = new System.Drawing.Size(469, 13);
            this.AntiVirus.TabIndex = 6;
            this.AntiVirus.Text = "NB: If you encounter any permission errors, please temporarily disable your Anti-" +
    "Virus and try again.";
            // 
            // AutomaticUpdate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.AntiVirus);
            this.Controls.Add(this.ReleasesPage);
            this.Controls.Add(this.UpdateLater);
            this.Controls.Add(this.Info);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.UpdateNow);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AutomaticUpdate";
            this.Text = "Update Available";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_AutomaticUpdate);
            this.Load += new System.EventHandler(this.OnLoad_AutomaticUpdate);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button UpdateNow;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Label Info;
        private System.Windows.Forms.Button UpdateLater;
        private System.Windows.Forms.LinkLabel ReleasesPage;
        private System.Windows.Forms.Label AntiVirus;
    }
}