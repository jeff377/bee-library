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
            ((System.ComponentModel.ISupportInitialize)gvTrace).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // edtDetail
            // 
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
            gvTrace.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvTrace.Dock = DockStyle.Fill;
            gvTrace.Location = new Point(0, 0);
            gvTrace.Name = "gvTrace";
            gvTrace.Size = new Size(800, 225);
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
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(edtDetail);
            splitContainer1.Size = new Size(800, 450);
            splitContainer1.SplitterDistance = 225;
            splitContainer1.TabIndex = 9;
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
            ResumeLayout(false);
        }

        #endregion

        private TextBox edtDetail;
        private DataGridView gvTrace;
        private SplitContainer splitContainer1;
    }
}