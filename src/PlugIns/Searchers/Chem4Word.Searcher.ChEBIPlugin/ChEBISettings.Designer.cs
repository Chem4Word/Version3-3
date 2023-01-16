namespace Chem4Word.Searcher.ChEBIPlugin
{
    partial class ChEBISettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChEBISettings));
            this.nudDisplayOrder = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nudResultsPerCall = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtChebiWsUri = new System.Windows.Forms.TextBox();
            this.btnSetDefaults = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudDisplayOrder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudResultsPerCall)).BeginInit();
            this.SuspendLayout();
            // 
            // nudDisplayOrder
            // 
            this.nudDisplayOrder.Location = new System.Drawing.Point(165, 16);
            this.nudDisplayOrder.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.nudDisplayOrder.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDisplayOrder.Name = "nudDisplayOrder";
            this.nudDisplayOrder.Size = new System.Drawing.Size(81, 27);
            this.nudDisplayOrder.TabIndex = 42;
            this.nudDisplayOrder.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudDisplayOrder.ValueChanged += new System.EventHandler(this.nudDisplayOrder_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 19);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 20);
            this.label2.TabIndex = 41;
            this.label2.Text = "Display Order";
            // 
            // nudResultsPerCall
            // 
            this.nudResultsPerCall.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudResultsPerCall.Location = new System.Drawing.Point(165, 96);
            this.nudResultsPerCall.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.nudResultsPerCall.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudResultsPerCall.Name = "nudResultsPerCall";
            this.nudResultsPerCall.Size = new System.Drawing.Size(81, 27);
            this.nudResultsPerCall.TabIndex = 40;
            this.nudResultsPerCall.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nudResultsPerCall.ValueChanged += new System.EventHandler(this.nudResultsPerCall_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 99);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 20);
            this.label1.TabIndex = 39;
            this.label1.Text = "Maximum Results";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 60);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 20);
            this.label3.TabIndex = 36;
            this.label3.Text = "WebService Url";
            // 
            // txtChebiWsUri
            // 
            this.txtChebiWsUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtChebiWsUri.Location = new System.Drawing.Point(165, 56);
            this.txtChebiWsUri.Margin = new System.Windows.Forms.Padding(6);
            this.txtChebiWsUri.Name = "txtChebiWsUri";
            this.txtChebiWsUri.Size = new System.Drawing.Size(422, 27);
            this.txtChebiWsUri.TabIndex = 35;
            this.txtChebiWsUri.Text = "https://www.ebi.ac.uk/webservices/chebi/2.0/webservice";
            this.txtChebiWsUri.TextChanged += new System.EventHandler(this.txtChebiWsUri_TextChanged);
            // 
            // btnSetDefaults
            // 
            this.btnSetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetDefaults.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSetDefaults.Location = new System.Drawing.Point(413, 156);
            this.btnSetDefaults.Margin = new System.Windows.Forms.Padding(7);
            this.btnSetDefaults.Name = "btnSetDefaults";
            this.btnSetDefaults.Size = new System.Drawing.Size(80, 27);
            this.btnSetDefaults.TabIndex = 34;
            this.btnSetDefaults.Text = "Defaults";
            this.btnSetDefaults.UseVisualStyleBackColor = true;
            this.btnSetDefaults.Click += new System.EventHandler(this.btnSetDefaults_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOk.Location = new System.Drawing.Point(507, 156);
            this.btnOk.Margin = new System.Windows.Forms.Padding(7);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 27);
            this.btnOk.TabIndex = 33;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // ChEBISettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(603, 199);
            this.Controls.Add(this.nudDisplayOrder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nudResultsPerCall);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtChebiWsUri);
            this.Controls.Add(this.btnSetDefaults);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.Name = "ChEBISettings";
            this.Text = "ChEBI Search - Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
            this.Load += new System.EventHandler(this.Settings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudDisplayOrder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudResultsPerCall)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown nudDisplayOrder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudResultsPerCall;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtChebiWsUri;
        private System.Windows.Forms.Button btnSetDefaults;
        private System.Windows.Forms.Button btnOk;
    }
}