namespace WinForms.TestHarness
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
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.StatusPanel = new Chem4Word.Core.UI.Controls.EditorHostStatusPanel();
            this.LayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.ColumnCount = 1;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.Controls.Add(this.elementHost1, 0, 0);
            this.LayoutPanel.Controls.Add(this.StatusPanel, 0, 1);
            this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 2;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.LayoutPanel.Size = new System.Drawing.Size(1001, 520);
            this.LayoutPanel.TabIndex = 4;
            // 
            // elementHost1
            // 
            this.elementHost1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(3, 4);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(995, 464);
            this.elementHost1.TabIndex = 2;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // StatusPanel
            // 
            this.StatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusPanel.Label1Bold = false;
            this.StatusPanel.Label1Colour = System.Drawing.SystemColors.WindowText;
            this.StatusPanel.Label1Text = "...";
            this.StatusPanel.Label1ToolTip = "Status information";
            this.StatusPanel.Label2Text = "...";
            this.StatusPanel.Label2ToolTip = "Combined Molecular Weight";
            this.StatusPanel.Label2Visible = true;
            this.StatusPanel.Label3Text = "...";
            this.StatusPanel.Label3ToolTip = "Combined Molecular Formula";
            this.StatusPanel.Label3Visible = true;
            this.StatusPanel.Location = new System.Drawing.Point(3, 475);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Size = new System.Drawing.Size(995, 42);
            this.StatusPanel.TabIndex = 1;
            this.StatusPanel.OnClickOkEventHandler += new System.EventHandler(this.OnClickOk_StatusPanel);
            this.StatusPanel.OnClickCancelEventHandler += new System.EventHandler(this.OnClickCancel_StatusPanel);
            // 
            // EditorHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1001, 520);
            this.Controls.Add(this.LayoutPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditorHost";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Editor Host (Test Harness)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_EditorHost);
            this.Load += new System.EventHandler(this.OnLoad_EditorHost);
            this.LayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private Chem4Word.Core.UI.Controls.EditorHostStatusPanel StatusPanel;
    }
}

