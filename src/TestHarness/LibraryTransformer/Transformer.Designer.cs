namespace LibraryTransformer
{
    partial class Transformer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Transformer));
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ToCml = new System.Windows.Forms.Button();
            this.ToPb = new System.Windows.Forms.Button();
            this.progressBar1 = new Chem4Word.Core.UI.Controls.CustomProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(12, 12);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(776, 129);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Display Name";
            this.columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "File Name";
            this.columnHeader2.Width = 400;
            // 
            // ToCml
            // 
            this.ToCml.Location = new System.Drawing.Point(13, 147);
            this.ToCml.Name = "ToCml";
            this.ToCml.Size = new System.Drawing.Size(75, 23);
            this.ToCml.TabIndex = 2;
            this.ToCml.Text = "To CML";
            this.ToCml.UseVisualStyleBackColor = true;
            this.ToCml.Click += new System.EventHandler(this.ToCml_Click);
            // 
            // ToPb
            // 
            this.ToPb.Location = new System.Drawing.Point(94, 147);
            this.ToPb.Name = "ToPb";
            this.ToPb.Size = new System.Drawing.Size(75, 23);
            this.ToPb.TabIndex = 3;
            this.ToPb.Text = "To PB";
            this.ToPb.UseVisualStyleBackColor = true;
            this.ToPb.Click += new System.EventHandler(this.ToPb_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.Color.Transparent;
            this.progressBar1.GradiantPosition = Chem4Word.Core.UI.Controls.CustomProgressBar.GradiantArea.None;
            this.progressBar1.Image = null;
            this.progressBar1.Location = new System.Drawing.Point(12, 206);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.RoundedCorners = false;
            this.progressBar1.ShowPercentage = true;
            this.progressBar1.Size = new System.Drawing.Size(776, 23);
            this.progressBar1.Text = "customProgressBar1";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 180);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(776, 23);
            this.label1.TabIndex = 5;
            this.label1.Text = "...";
            // 
            // Transformer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 241);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.ToPb);
            this.Controls.Add(this.ToCml);
            this.Controls.Add(this.listView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Transformer";
            this.Text = "Transformer";
            this.Load += new System.EventHandler(this.Transformer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Button ToCml;
        private System.Windows.Forms.Button ToPb;
        private Chem4Word.Core.UI.Controls.CustomProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
    }
}

