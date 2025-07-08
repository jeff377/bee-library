namespace Bee.UI.WinForms
{
    partial class frmConnect
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
            edtEndpoint.Location = new Point(22, 32);
            edtEndpoint.Name = "edtEndpoint";
            edtEndpoint.Size = new Size(360, 22);
            edtEndpoint.TabIndex = 0;
            // 
            // lblEndpoint
            // 
            lblEndpoint.AutoSize = true;
            lblEndpoint.Location = new Point(22, 15);
            lblEndpoint.Name = "lblEndpoint";
            lblEndpoint.Size = new Size(56, 14);
            lblEndpoint.TabIndex = 1;
            lblEndpoint.Text = "Endpoint";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(226, 71);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += this.btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(307, 71);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += this.btnCancel_Click;
            // 
            // frmConnect
            // 
            this.AutoScaleDimensions = new SizeF(7F, 14F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(405, 104);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOK);
            this.Controls.Add(lblEndpoint);
            this.Controls.Add(edtEndpoint);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmConnect";
            this.ShowIcon = false;
            this.Text = "Set API Connection Mode";
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