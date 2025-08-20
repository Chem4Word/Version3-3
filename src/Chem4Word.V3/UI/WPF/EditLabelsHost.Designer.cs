using Chem4Word.ACME.Controls;

namespace Chem4Word.UI.WPF
{
    partial class EditLabelsHost
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditLabelsHost));
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.labelsEditor1 = new Chem4Word.ACME.LabelsEditor();
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.StatusPanel = new Chem4Word.Core.UI.Controls.EditorHostStatusPanel();
            this.LayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(3, 5);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(963, 476);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.labelsEditor1;
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
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.LayoutPanel.Size = new System.Drawing.Size(969, 536);
            this.LayoutPanel.TabIndex = 1;
            // 
            // StatusPanel
            // 
            this.StatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusPanel.Label1Bold = true;
            this.StatusPanel.Label1Colour = System.Drawing.Color.Red;
            this.StatusPanel.Label1Text = "...";
            this.StatusPanel.Label1ToolTip = "";
            this.StatusPanel.Label2Text = "...";
            this.StatusPanel.Label2ToolTip = "";
            this.StatusPanel.Label2Visible = false;
            this.StatusPanel.Label3Text = "...";
            this.StatusPanel.Label3ToolTip = "";
            this.StatusPanel.Label3Visible = false;
            this.StatusPanel.Location = new System.Drawing.Point(3, 489);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Size = new System.Drawing.Size(963, 44);
            this.StatusPanel.TabIndex = 1;
            this.StatusPanel.OnClickOkEventHandler += new System.EventHandler(this.OnClick_Ok);
            this.StatusPanel.OnClickCancelEventHandler += new System.EventHandler(this.OnClick_Cancel);
            // 
            // EditLabelsHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(969, 536);
            this.Controls.Add(this.LayoutPanel);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditLabelsHost";
            this.Text = "Edit Labels";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_EditLabelsHost);
            this.Load += new System.EventHandler(this.OnLoad_EditLabelsHost);
            this.LayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private ACME.LabelsEditor labelsEditor1;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private Core.UI.Controls.EditorHostStatusPanel StatusPanel;
    }
}