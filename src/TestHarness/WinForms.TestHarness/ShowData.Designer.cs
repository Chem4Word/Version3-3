namespace WinForms.TestHarness
{
    partial class ShowData
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShowData));
            this.Scintilla1 = new ScintillaNET.Scintilla();
            this.Timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // Scintilla1
            // 
            this.Scintilla1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Scintilla1.Location = new System.Drawing.Point(0, 0);
            this.Scintilla1.Name = "Scintilla1";
            this.Scintilla1.Size = new System.Drawing.Size(1018, 450);
            this.Scintilla1.TabIndex = 0;
            this.Scintilla1.Text = "...";
            // 
            // Timer1
            // 
            this.Timer1.Interval = 125;
            this.Timer1.Tick += new System.EventHandler(this.OnTick_timer1);
            // 
            // ShowData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1018, 450);
            this.Controls.Add(this.Scintilla1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ShowData";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ShowCml";
            this.Load += new System.EventHandler(this.OnLoad_ShowData);
            this.ResumeLayout(false);

        }

        #endregion

        private ScintillaNET.Scintilla Scintilla1;
        private System.Windows.Forms.Timer Timer1;
    }
}