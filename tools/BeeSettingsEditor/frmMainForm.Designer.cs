namespace SettingsEditor
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
            menuBar = new MenuStrip();
            檔案FToolStripMenuItem = new ToolStripMenuItem();
            tbSystemSettings = new ToolStripMenuItem();
            tbDatabaseSettings = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            tbSave = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            tbExit = new ToolStripMenuItem();
            工具TToolStripMenuItem = new ToolStripMenuItem();
            tbConnect = new ToolStripMenuItem();
            statusBar = new StatusStrip();
            lblConnectType = new ToolStripStatusLabel();
            treeView = new Bee.UI.WinForms.BeeTreeView();
            propertyGrid = new Bee.UI.WinForms.BeePropertyGrid();
            splitContainer = new SplitContainer();
            tbTestConnection = new ToolStripMenuItem();
            menuBar.SuspendLayout();
            statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuBar
            // 
            menuBar.Items.AddRange(new ToolStripItem[] { 檔案FToolStripMenuItem, 工具TToolStripMenuItem });
            menuBar.Location = new Point(0, 0);
            menuBar.Name = "menuBar";
            menuBar.Size = new Size(584, 24);
            menuBar.TabIndex = 0;
            menuBar.Text = "menuStrip1";
            // 
            // 檔案FToolStripMenuItem
            // 
            檔案FToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tbSystemSettings, tbDatabaseSettings, toolStripMenuItem1, tbSave, toolStripMenuItem2, tbExit });
            檔案FToolStripMenuItem.Name = "檔案FToolStripMenuItem";
            檔案FToolStripMenuItem.Size = new Size(57, 20);
            檔案FToolStripMenuItem.Text = "檔案(&F)";
            // 
            // tbSystemSettings
            // 
            tbSystemSettings.Name = "tbSystemSettings";
            tbSystemSettings.Size = new Size(134, 22);
            tbSystemSettings.Text = "系統設定";
            tbSystemSettings.Click += this.tbSystemSettings_Click;
            // 
            // tbDatabaseSettings
            // 
            tbDatabaseSettings.Name = "tbDatabaseSettings";
            tbDatabaseSettings.Size = new Size(134, 22);
            tbDatabaseSettings.Text = "資料庫設定";
            tbDatabaseSettings.Click += this.tbDatabaseSettings_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(131, 6);
            // 
            // tbSave
            // 
            tbSave.Name = "tbSave";
            tbSave.Size = new Size(134, 22);
            tbSave.Text = "儲存(&S)";
            tbSave.Click += this.tbSave_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(131, 6);
            // 
            // tbExit
            // 
            tbExit.Name = "tbExit";
            tbExit.Size = new Size(134, 22);
            tbExit.Text = "結束(&X)";
            tbExit.Click += this.tbExit_Click;
            // 
            // 工具TToolStripMenuItem
            // 
            工具TToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tbConnect, tbTestConnection });
            工具TToolStripMenuItem.Name = "工具TToolStripMenuItem";
            工具TToolStripMenuItem.Size = new Size(58, 20);
            工具TToolStripMenuItem.Text = "工具(&T)";
            // 
            // tbConnect
            // 
            tbConnect.Name = "tbConnect";
            tbConnect.Size = new Size(180, 22);
            tbConnect.Text = "連線設定";
            tbConnect.Click += this.tbConnect_Click;
            // 
            // statusBar
            // 
            statusBar.Items.AddRange(new ToolStripItem[] { lblConnectType });
            statusBar.Location = new Point(0, 389);
            statusBar.Name = "statusBar";
            statusBar.Size = new Size(584, 22);
            statusBar.TabIndex = 1;
            statusBar.Text = "statusStrip1";
            // 
            // lblConnectType
            // 
            lblConnectType.Name = "lblConnectType";
            lblConnectType.Size = new Size(55, 17);
            lblConnectType.Text = "連線方式";
            // 
            // treeView
            // 
            treeView.Dock = DockStyle.Fill;
            treeView.Location = new Point(0, 0);
            treeView.Name = "treeView";
            treeView.PropertyGrid = propertyGrid;
            treeView.Size = new Size(240, 365);
            treeView.TabIndex = 2;
            // 
            // propertyGrid
            // 
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 0);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(340, 365);
            propertyGrid.TabIndex = 3;
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 24);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(treeView);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(propertyGrid);
            splitContainer.Size = new Size(584, 365);
            splitContainer.SplitterDistance = 240;
            splitContainer.TabIndex = 4;
            // 
            // tbTestConnection
            // 
            tbTestConnection.Name = "tbTestConnection";
            tbTestConnection.Size = new Size(180, 22);
            tbTestConnection.Text = "測試資料庫連線";
            tbTestConnection.Click += this.tbTestConnection_Click;
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 14F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(584, 411);
            this.Controls.Add(splitContainer);
            this.Controls.Add(statusBar);
            this.Controls.Add(menuBar);
            this.MainMenuStrip = menuBar;
            this.Name = "frmMainForm";
            this.Text = "設定檔編輯器";
            this.Load += this.frmMainForm_Load;
            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private MenuStrip menuBar;
        private StatusStrip statusBar;
        private ToolStripMenuItem 檔案FToolStripMenuItem;
        private ToolStripMenuItem 工具TToolStripMenuItem;
        private ToolStripMenuItem tbSystemSettings;
        private ToolStripMenuItem tbDatabaseSettings;
        private Bee.UI.WinForms.BeeTreeView treeView;
        private Bee.UI.WinForms.BeePropertyGrid propertyGrid;
        private SplitContainer splitContainer;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem tbSave;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem tbExit;
        private ToolStripMenuItem tbConnect;
        private ToolStripStatusLabel lblConnectType;
        private ToolStripMenuItem tbTestConnection;
    }
}
