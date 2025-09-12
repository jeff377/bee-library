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
            btnShowTraceViewer = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnInitialize
            // 
            btnInitialize.Location = new Point(12, 56);
            btnInitialize.Name = "btnInitialize";
            btnInitialize.Size = new Size(100, 23);
            btnInitialize.TabIndex = 1;
            btnInitialize.Text = "Initialize";
            btnInitialize.UseVisualStyleBackColor = true;
            btnInitialize.Click += btnInitialize_Click;
            // 
            // btnHello
            // 
            btnHello.Location = new Point(12, 114);
            btnHello.Name = "btnHello";
            btnHello.Size = new Size(100, 23);
            btnHello.TabIndex = 4;
            btnHello.Text = "Hello";
            btnHello.UseVisualStyleBackColor = true;
            btnHello.Click += btnHello_Click;
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
            btnLogin.Click += btnLogin_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnShowTraceViewer);
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
            panel1.Name = "panel1";
            panel1.Size = new Size(281, 561);
            panel1.TabIndex = 8;
            // 
            // btnHelloLocal
            // 
            btnHelloLocal.Location = new Point(12, 201);
            btnHelloLocal.Name = "btnHelloLocal";
            btnHelloLocal.Size = new Size(100, 23);
            btnHelloLocal.TabIndex = 10;
            btnHelloLocal.Text = "HelloLocal";
            btnHelloLocal.UseVisualStyleBackColor = true;
            btnHelloLocal.Click += btnHelloLocal_Click;
            // 
            // btnHelloEncrypted
            // 
            btnHelloEncrypted.Location = new Point(12, 172);
            btnHelloEncrypted.Name = "btnHelloEncrypted";
            btnHelloEncrypted.Size = new Size(100, 23);
            btnHelloEncrypted.TabIndex = 9;
            btnHelloEncrypted.Text = "HelloEncrypted";
            btnHelloEncrypted.UseVisualStyleBackColor = true;
            btnHelloEncrypted.Click += btnHelloEncrypted_Click;
            // 
            // btnHelloEncoded
            // 
            btnHelloEncoded.Location = new Point(12, 143);
            btnHelloEncoded.Name = "btnHelloEncoded";
            btnHelloEncoded.Size = new Size(100, 23);
            btnHelloEncoded.TabIndex = 8;
            btnHelloEncoded.Text = "HelloEncoded";
            btnHelloEncoded.UseVisualStyleBackColor = true;
            btnHelloEncoded.Click += btnHelloEncoded_Click;
            // 
            // btnShowTraceViewer
            // 
            btnShowTraceViewer.Location = new Point(12, 230);
            btnShowTraceViewer.Name = "btnShowTraceViewer";
            btnShowTraceViewer.Size = new Size(157, 23);
            btnShowTraceViewer.TabIndex = 11;
            btnShowTraceViewer.Text = "Show TraceViewer";
            btnShowTraceViewer.UseVisualStyleBackColor = true;
            btnShowTraceViewer.Click += btnShowTraceViewer_Click;
            // 
            // frmMainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(edtLog);
            Controls.Add(panel1);
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
        private Button btnShowTraceViewer;
    }
}
