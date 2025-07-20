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
            label2 = new Label();
            btnHello = new Button();
            edtEndpoint = new TextBox();
            label1 = new Label();
            edtLog = new TextBox();
            btnLogin = new Button();
            this.SuspendLayout();
            // 
            // btnInitialize
            // 
            btnInitialize.Location = new Point(15, 89);
            btnInitialize.Margin = new Padding(4);
            btnInitialize.Name = "btnInitialize";
            btnInitialize.Size = new Size(129, 29);
            btnInitialize.TabIndex = 1;
            btnInitialize.Text = "Initialize";
            btnInitialize.UseVisualStyleBackColor = true;
            btnInitialize.Click += this.btnInitialize_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(99, 41);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(309, 19);
            label2.TabIndex = 5;
            label2.Text = "Note: Enter DefinePath for local connection.";
            // 
            // btnHello
            // 
            btnHello.Location = new Point(13, 163);
            btnHello.Margin = new Padding(4);
            btnHello.Name = "btnHello";
            btnHello.Size = new Size(129, 29);
            btnHello.TabIndex = 4;
            btnHello.Text = "Hello";
            btnHello.UseVisualStyleBackColor = true;
            btnHello.Click += this.btnHello_Click;
            // 
            // edtEndpoint
            // 
            edtEndpoint.Location = new Point(99, 8);
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
            label1.Size = new Size(72, 19);
            label1.TabIndex = 2;
            label1.Text = "Endpoint";
            // 
            // edtLog
            // 
            edtLog.Location = new Point(451, 15);
            edtLog.Margin = new Padding(4);
            edtLog.Multiline = true;
            edtLog.Name = "edtLog";
            edtLog.ScrollBars = ScrollBars.Both;
            edtLog.Size = new Size(607, 445);
            edtLog.TabIndex = 6;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(13, 126);
            btnLogin.Margin = new Padding(4);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(129, 29);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += this.btnLogin_Click;
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new SizeF(9F, 19F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1075, 504);
            this.Controls.Add(btnLogin);
            this.Controls.Add(edtLog);
            this.Controls.Add(btnHello);
            this.Controls.Add(label2);
            this.Controls.Add(btnInitialize);
            this.Controls.Add(label1);
            this.Controls.Add(edtEndpoint);
            this.Margin = new Padding(4);
            this.Name = "frmMainForm";
            this.Text = "JSON-RPC Client";
            this.Load += this.frmMainForm_Load;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
        private Button btnInitialize;
        private TextBox edtEndpoint;
        private Label label1;
        private Button btnHello;
        private Label label2;
        private TextBox edtLog;
        private Button btnLogin;
    }
}
