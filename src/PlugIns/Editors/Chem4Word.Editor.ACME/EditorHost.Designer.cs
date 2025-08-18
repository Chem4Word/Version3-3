namespace Chem4Word.Editor.ACME
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorHost));
            this.VerticalSplitter = new System.Windows.Forms.SplitContainer();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.editor1 = new Chem4Word.ACME.Editor();
            this.StatusPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Cancel = new System.Windows.Forms.Button();
            this.MessageFromWpf = new System.Windows.Forms.Label();
            this.Save = new System.Windows.Forms.Button();
            this.MWTDisplay = new System.Windows.Forms.Label();
            this.FormulaDisplay = new System.Windows.Forms.Label();
            this.MainToolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.VerticalSplitter)).BeginInit();
            this.VerticalSplitter.Panel1.SuspendLayout();
            this.VerticalSplitter.Panel2.SuspendLayout();
            this.VerticalSplitter.SuspendLayout();
            this.StatusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // VerticalSplitter
            // 
            this.VerticalSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VerticalSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.VerticalSplitter.IsSplitterFixed = true;
            this.VerticalSplitter.Location = new System.Drawing.Point(0, 0);
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
            this.VerticalSplitter.Panel2MinSize = 20;
            this.VerticalSplitter.Size = new System.Drawing.Size(984, 561);
            this.VerticalSplitter.SplitterDistance = 513;
            this.VerticalSplitter.TabIndex = 2;
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(984, 513);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.editor1;
            // 
            // StatusPanel
            // 
            this.StatusPanel.ColumnCount = 5;
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 144F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 144F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.StatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.StatusPanel.Controls.Add(this.Cancel, 4, 0);
            this.StatusPanel.Controls.Add(this.MessageFromWpf, 0, 0);
            this.StatusPanel.Controls.Add(this.Save, 3, 0);
            this.StatusPanel.Controls.Add(this.MWTDisplay, 1, 0);
            this.StatusPanel.Controls.Add(this.FormulaDisplay, 2, 0);
            this.StatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusPanel.Location = new System.Drawing.Point(0, 0);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Padding = new System.Windows.Forms.Padding(2);
            this.StatusPanel.RowCount = 1;
            this.StatusPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.StatusPanel.Size = new System.Drawing.Size(984, 44);
            this.StatusPanel.TabIndex = 0;
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Cancel.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.Cancel.Location = new System.Drawing.Point(907, 5);
            this.Cancel.Name = "Cancel";
            this.Cancel.Padding = new System.Windows.Forms.Padding(2);
            this.Cancel.Size = new System.Drawing.Size(72, 34);
            this.Cancel.TabIndex = 8;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.OnClick_Cancel);
            // 
            // MessageFromWpf
            // 
            this.MessageFromWpf.AutoEllipsis = true;
            this.MessageFromWpf.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MessageFromWpf.Cursor = System.Windows.Forms.Cursors.No;
            this.MessageFromWpf.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageFromWpf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageFromWpf.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.MessageFromWpf.Location = new System.Drawing.Point(5, 2);
            this.MessageFromWpf.Name = "MessageFromWpf";
            this.MessageFromWpf.Size = new System.Drawing.Size(530, 40);
            this.MessageFromWpf.TabIndex = 7;
            this.MessageFromWpf.Text = "...";
            this.MessageFromWpf.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.MainToolTip.SetToolTip(this.MessageFromWpf, "Status information");
            // 
            // Save
            // 
            this.Save.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Save.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Save.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.Save.Location = new System.Drawing.Point(829, 5);
            this.Save.Name = "Save";
            this.Save.Padding = new System.Windows.Forms.Padding(2);
            this.Save.Size = new System.Drawing.Size(72, 34);
            this.Save.TabIndex = 6;
            this.Save.Text = "OK";
            this.Save.UseVisualStyleBackColor = true;
            this.Save.Click += new System.EventHandler(this.OnClick_Save);
            // 
            // MWTDisplay
            // 
            this.MWTDisplay.AutoEllipsis = true;
            this.MWTDisplay.AutoSize = true;
            this.MWTDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MWTDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MWTDisplay.Location = new System.Drawing.Point(541, 2);
            this.MWTDisplay.Name = "MWTDisplay";
            this.MWTDisplay.Size = new System.Drawing.Size(138, 40);
            this.MWTDisplay.TabIndex = 9;
            this.MWTDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.MainToolTip.SetToolTip(this.MWTDisplay, "Combined Molecular Weight");
            // 
            // FormulaDisplay
            // 
            this.FormulaDisplay.AutoEllipsis = true;
            this.FormulaDisplay.AutoSize = true;
            this.FormulaDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.FormulaDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FormulaDisplay.Location = new System.Drawing.Point(685, 2);
            this.FormulaDisplay.Name = "FormulaDisplay";
            this.FormulaDisplay.Size = new System.Drawing.Size(138, 40);
            this.FormulaDisplay.TabIndex = 10;
            this.FormulaDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.MainToolTip.SetToolTip(this.FormulaDisplay, "Combined Molecular Formula");
            // 
            // EditorHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.VerticalSplitter);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditorHost";
            this.Text = "ACME Editor Host";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_EditorHost);
            this.Load += new System.EventHandler(this.OnLoad_EditorHost);
            this.LocationChanged += new System.EventHandler(this.OnLocationChanged_EditorHost);
            this.VerticalSplitter.Panel1.ResumeLayout(false);
            this.VerticalSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.VerticalSplitter)).EndInit();
            this.VerticalSplitter.ResumeLayout(false);
            this.StatusPanel.ResumeLayout(false);
            this.StatusPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer VerticalSplitter;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private Chem4Word.ACME.Editor editor1;
        private System.Windows.Forms.TableLayoutPanel StatusPanel;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Label MessageFromWpf;
        private System.Windows.Forms.Button Save;
        private System.Windows.Forms.Label MWTDisplay;
        private System.Windows.Forms.Label FormulaDisplay;
        private System.Windows.Forms.ToolTip MainToolTip;
    }
}