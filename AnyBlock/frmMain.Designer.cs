namespace AnyBlock
{
    partial class frmMain
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
            this.tvRanges = new System.Windows.Forms.TreeView();
            this.lbRules = new System.Windows.Forms.ListBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblCycle = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tvRanges
            // 
            this.tvRanges.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tvRanges.CheckBoxes = true;
            this.tvRanges.HideSelection = false;
            this.tvRanges.Location = new System.Drawing.Point(12, 12);
            this.tvRanges.Name = "tvRanges";
            this.tvRanges.Size = new System.Drawing.Size(370, 449);
            this.tvRanges.TabIndex = 0;
            this.tvRanges.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.tvRanges_AfterCheck);
            // 
            // lbRules
            // 
            this.lbRules.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbRules.FormattingEnabled = true;
            this.lbRules.Location = new System.Drawing.Point(388, 12);
            this.lbRules.Name = "lbRules";
            this.lbRules.Size = new System.Drawing.Size(351, 407);
            this.lbRules.TabIndex = 1;
            this.lbRules.SelectedIndexChanged += new System.EventHandler(this.lbRules_SelectedIndexChanged);
            this.lbRules.DoubleClick += new System.EventHandler(this.lbRules_DoubleClick);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(665, 438);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblCycle
            // 
            this.lblCycle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCycle.AutoSize = true;
            this.lblCycle.Location = new System.Drawing.Point(388, 443);
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Size = new System.Drawing.Size(256, 13);
            this.lblCycle.TabIndex = 3;
            this.lblCycle.Text = "Double Click an Entry to cycle through the Directions";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(752, 473);
            this.Controls.Add(this.lblCycle);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lbRules);
            this.Controls.Add(this.tvRanges);
            this.Name = "frmMain";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView tvRanges;
        private System.Windows.Forms.ListBox lbRules;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblCycle;
    }
}