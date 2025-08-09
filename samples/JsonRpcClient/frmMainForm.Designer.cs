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
            btnHelloLocal = new Button();
            btnHelloEncrypted = new Button();
            btnHelloEncoded = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnInitialize
            // 
            btnInitialize.Location = new Point(15, 71);
            btnInitialize.Margin = new Padding(4);
            btnInitialize.Name = "btnInitialize";
            btnInitialize.Size = new Size(129, 29);
            btnInitialize.TabIndex = 1;
            btnInitialize.Text = "Initialize";
            btnInitialize.UseVisualStyleBackColor = true;
            btnInitialize.Click += btnInitialize_Click;
            // 
            // btnHello
            // 
            btnHello.Location = new Point(15, 144);
            btnHello.Margin = new Padding(4);
            btnHello.Name = "btnHello";
            btnHello.Size = new Size(129, 29);
            btnHello.TabIndex = 4;
            btnHello.Text = "Hello";
            btnHello.UseVisualStyleBackColor = true;
            btnHello.Click += btnHello_Click;
            // 
            // edtEndpoint
            // 
            edtEndpoint.Location = new Point(15, 34);
            edtEndpoint.Margin = new Padding(4);
            edtEndpoint.Name = "edtEndpoint";
            edtEndpoint.Size = new Size(325, 27);
            edtEndpoint.TabIndex = 3;
            edtEndpoint.Text = "https://localhost:7056/api";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 11);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(182, 19);
            label1.TabIndex = 2;
            label1.Text = "Endpoint (Local/Remote)";
            // 
            // edtLog
            // 
            edtLog.Dock = DockStyle.Fill;
            edtLog.Location = new Point(361, 0);
            edtLog.Margin = new Padding(4);
            edtLog.Multiline = true;
            edtLog.Name = "edtLog";
            edtLog.ScrollBars = ScrollBars.Both;
            edtLog.Size = new Size(647, 711);
            edtLog.TabIndex = 6;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(15, 108);
            btnLogin.Margin = new Padding(4);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(129, 29);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnHelloLocal);
            panel1.Controls.Add(btnHelloEncrypted);
            panel1.Controls.Add(btnHelloEncoded);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnLogin);
            panel1.Controls.Add(edtEndpoint);
            panel1.Controls.Add(btnInitialize);
            panel1.Controls.Add(btnHello);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(361, 711);
            panel1.TabIndex = 8;
            // 
            // btnHelloLocal
            // 
            btnHelloLocal.Location = new Point(15, 255);
            btnHelloLocal.Margin = new Padding(4);
            btnHelloLocal.Name = "btnHelloLocal";
            btnHelloLocal.Size = new Size(129, 29);
            btnHelloLocal.TabIndex = 10;
            btnHelloLocal.Text = "HelloLocal";
            btnHelloLocal.UseVisualStyleBackColor = true;
            btnHelloLocal.Click += btnHelloLocal_Click;
            // 
            // btnHelloEncrypted
            // 
            btnHelloEncrypted.Location = new Point(15, 218);
            btnHelloEncrypted.Margin = new Padding(4);
            btnHelloEncrypted.Name = "btnHelloEncrypted";
            btnHelloEncrypted.Size = new Size(129, 29);
            btnHelloEncrypted.TabIndex = 9;
            btnHelloEncrypted.Text = "HelloEncrypted";
            btnHelloEncrypted.UseVisualStyleBackColor = true;
            btnHelloEncrypted.Click += btnHelloEncrypted_Click;
            // 
            // btnHelloEncoded
            // 
            btnHelloEncoded.Location = new Point(15, 181);
            btnHelloEncoded.Margin = new Padding(4);
            btnHelloEncoded.Name = "btnHelloEncoded";
            btnHelloEncoded.Size = new Size(129, 29);
            btnHelloEncoded.TabIndex = 8;
            btnHelloEncoded.Text = "HelloEncoded";
            btnHelloEncoded.UseVisualStyleBackColor = true;
            btnHelloEncoded.Click += btnHelloEncoded_Click;
            // 
            // frmMainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1008, 711);
            Controls.Add(edtLog);
            Controls.Add(panel1);
            Margin = new Padding(4);
            Name = "frmMainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "JSON-RPC Client";
            Load += frmMainForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnInitialize;
        private TextBox edtEndpoint;
        private Label label1;
        private Button btnHello;
        private TextBox edtLog;
        private Button btnLogin;
        private Panel panel1;
        private Button btnHelloLocal;
        private Button btnHelloEncrypted;
        private Button btnHelloEncoded;
    }
}
