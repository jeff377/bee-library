namespace JsonRpcClient
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
            btnInitialize = new Button();
            btnHello = new Button();
            edtEndpoint = new TextBox();
            label1 = new Label();
            edtLog = new TextBox();
            btnLogin = new Button();
            panel1 = new Panel();
            panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnInitialize
            // 
            btnInitialize.Location = new Point(12, 56);
            btnInitialize.Name = "btnInitialize";
            btnInitialize.Size = new Size(100, 23);
            btnInitialize.TabIndex = 1;
            btnInitialize.Text = "Initialize";
            btnInitialize.UseVisualStyleBackColor = true;
            btnInitialize.Click += this.btnInitialize_Click;
            // 
            // btnHello
            // 
            btnHello.Location = new Point(12, 114);
            btnHello.Name = "btnHello";
            btnHello.Size = new Size(100, 23);
            btnHello.TabIndex = 4;
            btnHello.Text = "Hello";
            btnHello.UseVisualStyleBackColor = true;
            btnHello.Click += this.btnHello_Click;
            // 
            // edtEndpoint
            // 
            edtEndpoint.Location = new Point(12, 27);
            edtEndpoint.Name = "edtEndpoint";
            edtEndpoint.Size = new Size(254, 23);
            edtEndpoint.TabIndex = 3;
            edtEndpoint.Text = "https://localhost:7056/api";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(150, 15);
            label1.TabIndex = 2;
            label1.Text = "Endpoint (Local/Remote)";
            // 
            // edtLog
            // 
            edtLog.Dock = DockStyle.Fill;
            edtLog.Location = new Point(281, 0);
            edtLog.Multiline = true;
            edtLog.Name = "edtLog";
            edtLog.ScrollBars = ScrollBars.Both;
            edtLog.Size = new Size(503, 561);
            edtLog.TabIndex = 6;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(12, 85);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(100, 23);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += this.btnLogin_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnLogin);
            panel1.Controls.Add(edtEndpoint);
            panel1.Controls.Add(btnInitialize);
            panel1.Controls.Add(btnHello);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(281, 561);
            panel1.TabIndex = 8;
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(784, 561);
            this.Controls.Add(edtLog);
            this.Controls.Add(panel1);
            this.Name = "frmMainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "JSON-RPC Client";
            this.Load += this.frmMainForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
        private Button btnInitialize;
        private TextBox edtEndpoint;
        private Label label1;
        private Button btnHello;
        private TextBox edtLog;
        private Button btnLogin;
        private Panel panel1;
    }
}
