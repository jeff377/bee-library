namespace DbUpgrade
{
    partial class frmMainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblDatabase = new Label();
            edtDatabases = new ComboBox();
            btnExecute = new Button();
            lblMessage = new Label();
            barStatus = new StatusStrip();
            lblConnectType = new ToolStripStatusLabel();
            btnSaveLog = new Button();
            barStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblDatabase
            // 
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new Point(12, 23);
            lblDatabase.Name = "lblDatabase";
            lblDatabase.Size = new Size(43, 14);
            lblDatabase.TabIndex = 0;
            lblDatabase.Text = "資料庫";
            // 
            // edtDatabases
            // 
            edtDatabases.DropDownStyle = ComboBoxStyle.DropDownList;
            edtDatabases.FormattingEnabled = true;
            edtDatabases.Location = new Point(61, 20);
            edtDatabases.Name = "edtDatabases";
            edtDatabases.Size = new Size(385, 22);
            edtDatabases.TabIndex = 1;
            // 
            // btnExecute
            // 
            btnExecute.Location = new Point(371, 58);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(75, 23);
            btnExecute.TabIndex = 2;
            btnExecute.Text = "執行升級";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += this.btnExecute_Click;
            // 
            // lblMessage
            // 
            lblMessage.AutoSize = true;
            lblMessage.Location = new Point(12, 82);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(55, 14);
            lblMessage.TabIndex = 3;
            lblMessage.Text = "執行訊息";
            lblMessage.Visible = false;
            // 
            // barStatus
            // 
            barStatus.Items.AddRange(new ToolStripItem[] { lblConnectType });
            barStatus.Location = new Point(0, 107);
            barStatus.Name = "barStatus";
            barStatus.Size = new Size(466, 22);
            barStatus.TabIndex = 4;
            barStatus.Text = "statusStrip1";
            // 
            // lblConnectType
            // 
            lblConnectType.Name = "lblConnectType";
            lblConnectType.Size = new Size(55, 17);
            lblConnectType.Text = "連線方式";
            // 
            // btnSaveLog
            // 
            btnSaveLog.Location = new Point(290, 58);
            btnSaveLog.Name = "btnSaveLog";
            btnSaveLog.Size = new Size(75, 23);
            btnSaveLog.TabIndex = 5;
            btnSaveLog.Text = "儲存記錄";
            btnSaveLog.UseVisualStyleBackColor = true;
            btnSaveLog.Visible = false;
            btnSaveLog.Click += this.btnSaveLog_Click;
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 14F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(466, 129);
            this.Controls.Add(btnSaveLog);
            this.Controls.Add(barStatus);
            this.Controls.Add(lblMessage);
            this.Controls.Add(btnExecute);
            this.Controls.Add(edtDatabases);
            this.Controls.Add(lblDatabase);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMainForm";
            this.Text = "資料庫升級工具";
            this.Load += this.frmMainForm_Load;
            barStatus.ResumeLayout(false);
            barStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label lblDatabase;
        private ComboBox edtDatabases;
        private Button btnExecute;
        private Label lblMessage;
        private StatusStrip barStatus;
        private ToolStripStatusLabel lblConnectType;
        private Button btnSaveLog;
    }
}
