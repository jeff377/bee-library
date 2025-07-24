namespace Bee.UI.WinForms
{
    partial class ApiConnectForm
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
            edtEndpoint = new TextBox();
            lblEndpoint = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            this.SuspendLayout();
            // 
            // edtEndpoint
            // 
            edtEndpoint.Location = new Point(25, 41);
            edtEndpoint.Margin = new Padding(3, 4, 3, 4);
            edtEndpoint.Name = "edtEndpoint";
            edtEndpoint.Size = new Size(411, 26);
            edtEndpoint.TabIndex = 0;
            // 
            // lblEndpoint
            // 
            lblEndpoint.AutoSize = true;
            lblEndpoint.Location = new Point(25, 19);
            lblEndpoint.Name = "lblEndpoint";
            lblEndpoint.Size = new Size(63, 18);
            lblEndpoint.TabIndex = 1;
            lblEndpoint.Text = "Endpoint";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(258, 91);
            btnOK.Margin = new Padding(3, 4, 3, 4);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(86, 30);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += this.btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(351, 91);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(86, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += this.btnCancel_Click;
            // 
            // ApiConnectForm
            // 
            this.AcceptButton = btnOK;
            this.AutoScaleDimensions = new SizeF(8F, 18F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(463, 134);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOK);
            this.Controls.Add(lblEndpoint);
            this.Controls.Add(edtEndpoint);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Margin = new Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiConnectForm";
            this.ShowIcon = false;
            this.Text = "Set API Connection Endpoint";
            this.Load += this.frmConnect_Load;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private TextBox edtEndpoint;
        private Label lblEndpoint;
        private Button btnOK;
        private Button btnCancel;
    }
}