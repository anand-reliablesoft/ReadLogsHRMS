using System;
using System.Windows.Forms;

namespace DataCollectionService
{
    public partial class ConfigurationForm : Form
    {
        public bool TriggerDataCollection { get; private set; }

        public ConfigurationForm()
        {
            InitializeComponent();
            TriggerDataCollection = false;
        }

        private void InitializeComponent()
        {
            this.btnStartCollection = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblInstructions = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(30, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(280, 20);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Data Collection Service - Manual Mode";
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Location = new System.Drawing.Point(30, 60);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(320, 26);
            this.lblInstructions.TabIndex = 1;
            this.lblInstructions.Text = "Click \"Start Data Collection\" to manually trigger the data collection\r\nprocess for all biometric devices.";
            // 
            // btnStartCollection
            // 
            this.btnStartCollection.Location = new System.Drawing.Point(80, 110);
            this.btnStartCollection.Name = "btnStartCollection";
            this.btnStartCollection.Size = new System.Drawing.Size(130, 30);
            this.btnStartCollection.TabIndex = 2;
            this.btnStartCollection.Text = "Start Data Collection";
            this.btnStartCollection.UseVisualStyleBackColor = true;
            this.btnStartCollection.Click += new System.EventHandler(this.btnStartCollection_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(220, 110);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ConfigurationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 161);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnStartCollection);
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Data Collection Service Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnStartCollection;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInstructions;

        private void btnStartCollection_Click(object sender, EventArgs e)
        {
            TriggerDataCollection = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            TriggerDataCollection = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
