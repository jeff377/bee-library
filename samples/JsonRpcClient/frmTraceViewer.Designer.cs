namespace JsonRpcClient
{
    partial class frmTraceViewer
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
            edtDetail = new TextBox();
            gvTrace = new DataGridView();
            splitContainer1 = new SplitContainer();
            panel1 = new Panel();
            btnClearTrace = new Button();
            btnStartTrace = new Button();
            lblTraceCategory = new Label();
            edtTraceCategory = new ComboBox();
            btnStopTrace = new Button();
            ((System.ComponentModel.ISupportInitialize)gvTrace).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // edtDetail
            // 
            edtDetail.BorderStyle = BorderStyle.None;
            edtDetail.Dock = DockStyle.Fill;
            edtDetail.Location = new Point(0, 0);
            edtDetail.Multiline = true;
            edtDetail.Name = "edtDetail";
            edtDetail.ScrollBars = ScrollBars.Both;
            edtDetail.Size = new Size(800, 221);
            edtDetail.TabIndex = 7;
            // 
            // gvTrace
            // 
            gvTrace.BorderStyle = BorderStyle.None;
            gvTrace.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvTrace.Dock = DockStyle.Fill;
            gvTrace.Location = new Point(0, 38);
            gvTrace.Name = "gvTrace";
            gvTrace.Size = new Size(800, 187);
            gvTrace.TabIndex = 8;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(gvTrace);
            splitContainer1.Panel1.Controls.Add(panel1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(edtDetail);
            splitContainer1.Size = new Size(800, 450);
            splitContainer1.SplitterDistance = 225;
            splitContainer1.TabIndex = 9;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnClearTrace);
            panel1.Controls.Add(btnStartTrace);
            panel1.Controls.Add(lblTraceCategory);
            panel1.Controls.Add(edtTraceCategory);
            panel1.Controls.Add(btnStopTrace);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 38);
            panel1.TabIndex = 9;
            // 
            // btnClearTrace
            // 
            btnClearTrace.Location = new Point(320, 7);
            btnClearTrace.Name = "btnClearTrace";
            btnClearTrace.Size = new Size(75, 23);
            btnClearTrace.TabIndex = 4;
            btnClearTrace.Text = "Clear Trace";
            btnClearTrace.UseVisualStyleBackColor = true;
            btnClearTrace.Click += btnClearTrace_Click;
            // 
            // btnStartTrace
            // 
            btnStartTrace.Location = new Point(239, 7);
            btnStartTrace.Name = "btnStartTrace";
            btnStartTrace.Size = new Size(75, 23);
            btnStartTrace.TabIndex = 2;
            btnStartTrace.Text = "Start Trace";
            btnStartTrace.UseVisualStyleBackColor = true;
            btnStartTrace.Click += btnStartTrace_Click;
            // 
            // lblTraceCategory
            // 
            lblTraceCategory.AutoSize = true;
            lblTraceCategory.Location = new Point(12, 10);
            lblTraceCategory.Name = "lblTraceCategory";
            lblTraceCategory.Size = new Size(93, 15);
            lblTraceCategory.TabIndex = 1;
            lblTraceCategory.Text = "Trace Category";
            // 
            // edtTraceCategory
            // 
            edtTraceCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            edtTraceCategory.FormattingEnabled = true;
            edtTraceCategory.Location = new Point(112, 7);
            edtTraceCategory.Name = "edtTraceCategory";
            edtTraceCategory.Size = new Size(121, 23);
            edtTraceCategory.TabIndex = 0;
            // 
            // btnStopTrace
            // 
            btnStopTrace.Location = new Point(239, 7);
            btnStopTrace.Name = "btnStopTrace";
            btnStopTrace.Size = new Size(75, 23);
            btnStopTrace.TabIndex = 3;
            btnStopTrace.Text = "Stop Trace";
            btnStopTrace.UseVisualStyleBackColor = true;
            btnStopTrace.Click += btnStopTrace_Click;
            // 
            // frmTraceViewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(splitContainer1);
            Name = "frmTraceViewer";
            Text = "Trace Viewer";
            FormClosed += frmTraceViewer_FormClosed;
            Load += frmTraceViewer_Load;
            ((System.ComponentModel.ISupportInitialize)gvTrace).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox edtDetail;
        private DataGridView gvTrace;
        private SplitContainer splitContainer1;
        private Panel panel1;
        private Label lblTraceCategory;
        private ComboBox edtTraceCategory;
        private Button btnStartTrace;
        private Button btnStopTrace;
        private Button btnClearTrace;
    }
}