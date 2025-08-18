namespace Chem4Word.Editor.SimpleWpfEditor
{
    partial class EditorHost
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorHost));
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.cmlEditor1 = new Chem4Word.ACME.CmlEditor();
            this.VerticalSplitter = new System.Windows.Forms.SplitContainer();
            this.StatusPanel = new System.Windows.Forms.Panel();
            this.Save = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.VerticalSplitter)).BeginInit();
            this.VerticalSplitter.Panel1.SuspendLayout();
            this.VerticalSplitter.Panel2.SuspendLayout();
            this.VerticalSplitter.SuspendLayout();
            this.StatusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(840, 499);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.cmlEditor1;
            // 
            // VerticalSplitter
            // 
            this.VerticalSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VerticalSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.VerticalSplitter.IsSplitterFixed = true;
            this.VerticalSplitter.Location = new System.Drawing.Point(0, 0);
            this.VerticalSplitter.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.VerticalSplitter.Name = "VerticalSplitter";
            this.VerticalSplitter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // VerticalSplitter.Panel1
            // 
            this.VerticalSplitter.Panel1.Controls.Add(this.elementHost1);
            // 
            // VerticalSplitter.Panel2
            // 
            this.VerticalSplitter.Panel2.Controls.Add(this.StatusPanel);
            this.VerticalSplitter.Size = new System.Drawing.Size(840, 561);
            this.VerticalSplitter.SplitterDistance = 499;
            this.VerticalSplitter.SplitterWidth = 5;
            this.VerticalSplitter.TabIndex = 1;
            // 
            // StatusPanel
            // 
            this.StatusPanel.Controls.Add(this.Save);
            this.StatusPanel.Controls.Add(this.Cancel);
            this.StatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusPanel.Location = new System.Drawing.Point(0, 0);
            this.StatusPanel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Size = new System.Drawing.Size(840, 57);
            this.StatusPanel.TabIndex = 3;
            // 
            // Save
            // 
            this.Save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Save.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Save.Location = new System.Drawing.Point(672, 18);
            this.Save.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.Save.Name = "Save";
            this.Save.Size = new System.Drawing.Size(75, 27);
            this.Save.TabIndex = 1;
            this.Save.Text = "OK";
            this.Save.UseVisualStyleBackColor = true;
            this.Save.Click += new System.EventHandler(this.OnClick_Save);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Cancel.Location = new System.Drawing.Point(753, 18);
            this.Cancel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 27);
            this.Cancel.TabIndex = 0;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.OnClick_Cancel);
            // 
            // EditorHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(840, 561);
            this.Controls.Add(this.VerticalSplitter);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditorHost";
            this.Text = "EditorHost";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_EditorHost);
            this.Load += new System.EventHandler(this.OnLoad_EditorHost);
            this.VerticalSplitter.Panel1.ResumeLayout(false);
            this.VerticalSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.VerticalSplitter)).EndInit();
            this.VerticalSplitter.ResumeLayout(false);
            this.StatusPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private System.Windows.Forms.SplitContainer VerticalSplitter;
        private System.Windows.Forms.Panel StatusPanel;
        private System.Windows.Forms.Button Save;
        private System.Windows.Forms.Button Cancel;
        private ACME.CmlEditor cmlEditor1;
    }
}