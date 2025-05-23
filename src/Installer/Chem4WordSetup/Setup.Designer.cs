﻿namespace Chem4WordSetup
{
    partial class Setup
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Setup));
            this.Action = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.Information = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.WordRunning = new Chem4WordSetup.TaskIndicator();
            this.WindowsInstalled = new Chem4WordSetup.TaskIndicator();
            this.WordInstalled = new Chem4WordSetup.TaskIndicator();
            this.AddInInstalled = new Chem4WordSetup.TaskIndicator();
            this.VstoInstalled = new Chem4WordSetup.TaskIndicator();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // Action
            // 
            this.Action.Location = new System.Drawing.Point(395, 275);
            this.Action.Margin = new System.Windows.Forms.Padding(4);
            this.Action.Name = "Action";
            this.Action.Size = new System.Drawing.Size(86, 26);
            this.Action.TabIndex = 1;
            this.Action.Text = "Install";
            this.Action.UseVisualStyleBackColor = true;
            this.Action.Click += new System.EventHandler(this.OnClick_Action);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(175, 275);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(212, 26);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 4;
            // 
            // Information
            // 
            this.Information.BackColor = System.Drawing.Color.Transparent;
            this.Information.Location = new System.Drawing.Point(175, 213);
            this.Information.Name = "Information";
            this.Information.Size = new System.Drawing.Size(306, 52);
            this.Information.TabIndex = 5;
            this.Information.Text = "Click on Install to start downloading and installing the required components.";
            // 
            // timer1
            // 
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.OnTick_timer1);
            // 
            // WordRunning
            // 
            this.WordRunning.BackColor = System.Drawing.Color.Transparent;
            this.WordRunning.Description = "Microsoft Word is running";
            this.WordRunning.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WordRunning.Indicator = global::Chem4WordSetup.Properties.Resources.Question;
            this.WordRunning.Location = new System.Drawing.Point(178, 90);
            this.WordRunning.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.WordRunning.Name = "WordRunning";
            this.WordRunning.Size = new System.Drawing.Size(306, 34);
            this.WordRunning.TabIndex = 8;
            // 
            // WindowsInstalled
            // 
            this.WindowsInstalled.BackColor = System.Drawing.Color.Transparent;
            this.WindowsInstalled.Description = "Windows 7 or newer";
            this.WindowsInstalled.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WindowsInstalled.Indicator = global::Chem4WordSetup.Properties.Resources.Question;
            this.WindowsInstalled.Location = new System.Drawing.Point(178, 6);
            this.WindowsInstalled.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.WindowsInstalled.Name = "WindowsInstalled";
            this.WindowsInstalled.Size = new System.Drawing.Size(306, 34);
            this.WindowsInstalled.TabIndex = 7;
            // 
            // WordInstalled
            // 
            this.WordInstalled.BackColor = System.Drawing.Color.Transparent;
            this.WordInstalled.Description = "Microsoft Word 2000 or newer";
            this.WordInstalled.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WordInstalled.Indicator = global::Chem4WordSetup.Properties.Resources.Question;
            this.WordInstalled.Location = new System.Drawing.Point(178, 48);
            this.WordInstalled.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.WordInstalled.Name = "WordInstalled";
            this.WordInstalled.Size = new System.Drawing.Size(306, 34);
            this.WordInstalled.TabIndex = 6;
            // 
            // AddInInstalled
            // 
            this.AddInInstalled.BackColor = System.Drawing.Color.Transparent;
            this.AddInInstalled.Description = "Chemistry Add-In for Microsoft Word";
            this.AddInInstalled.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddInInstalled.Indicator = global::Chem4WordSetup.Properties.Resources.Question;
            this.AddInInstalled.Location = new System.Drawing.Point(178, 174);
            this.AddInInstalled.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.AddInInstalled.Name = "AddInInstalled";
            this.AddInInstalled.Size = new System.Drawing.Size(306, 34);
            this.AddInInstalled.TabIndex = 3;
            // 
            // VstoInstalled
            // 
            this.VstoInstalled.BackColor = System.Drawing.Color.Transparent;
            this.VstoInstalled.Description = "Visual Studio Tools for Office Runtime";
            this.VstoInstalled.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.VstoInstalled.Indicator = global::Chem4WordSetup.Properties.Resources.Question;
            this.VstoInstalled.Location = new System.Drawing.Point(178, 132);
            this.VstoInstalled.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.VstoInstalled.Name = "VstoInstalled";
            this.VstoInstalled.Size = new System.Drawing.Size(306, 34);
            this.VstoInstalled.TabIndex = 2;
            // 
            // timer2
            // 
            this.timer2.Interval = 250;
            this.timer2.Tick += new System.EventHandler(this.OnTick_timer2);
            // 
            // Setup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Chem4WordSetup.Properties.Resources.WixUIDialog;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(491, 312);
            this.Controls.Add(this.WordRunning);
            this.Controls.Add(this.WindowsInstalled);
            this.Controls.Add(this.WordInstalled);
            this.Controls.Add(this.Information);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.AddInInstalled);
            this.Controls.Add(this.VstoInstalled);
            this.Controls.Add(this.Action);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Setup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Setup Chemistry Add-In for Microsoft Word";
            this.Load += new System.EventHandler(this.OnLoad_Setup);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Action;
        private TaskIndicator VstoInstalled;
        private TaskIndicator AddInInstalled;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label Information;
        private System.Windows.Forms.Timer timer1;
        private TaskIndicator WordInstalled;
        private TaskIndicator WindowsInstalled;
        private TaskIndicator WordRunning;
        private System.Windows.Forms.Timer timer2;
    }
}