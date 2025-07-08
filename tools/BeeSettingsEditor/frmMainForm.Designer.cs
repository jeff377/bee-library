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
            menuFile = new ToolStripMenuItem();
            menuLoadSystemSettings = new ToolStripMenuItem();
            menuLoadDatabaseSettings = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            tbSave = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            tbExit = new ToolStripMenuItem();
            menuTools = new ToolStripMenuItem();
            menuApiConnect = new ToolStripMenuItem();
            tbTestConnection = new ToolStripMenuItem();
            menuGenerateMasterKey = new ToolStripMenuItem();
            statusBar = new StatusStrip();
            lblConnectType = new ToolStripStatusLabel();
            treeView = new Bee.UI.WinForms.BeeTreeView();
            propertyGrid = new Bee.UI.WinForms.BeePropertyGrid();
            splitContainer = new SplitContainer();
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
            menuBar.Items.AddRange(new ToolStripItem[] { menuFile, menuTools });
            menuBar.Location = new Point(0, 0);
            menuBar.Name = "menuBar";
            menuBar.Size = new Size(584, 24);
            menuBar.TabIndex = 0;
            menuBar.Text = "menuStrip1";
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuLoadSystemSettings, menuLoadDatabaseSettings, toolStripMenuItem1, tbSave, toolStripMenuItem2, tbExit });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(52, 20);
            menuFile.Text = "File(&F)";
            // 
            // menuLoadSystemSettings
            // 
            menuLoadSystemSettings.Name = "menuLoadSystemSettings";
            menuLoadSystemSettings.ShortcutKeys = Keys.Control | Keys.O;
            menuLoadSystemSettings.Size = new Size(264, 22);
            menuLoadSystemSettings.Text = "Open  System Settings...";
            menuLoadSystemSettings.Click += this.menuLoadSystemSettings_Click;
            // 
            // menuLoadDatabaseSettings
            // 
            menuLoadDatabaseSettings.Name = "menuLoadDatabaseSettings";
            menuLoadDatabaseSettings.ShortcutKeys = Keys.Control | Keys.D;
            menuLoadDatabaseSettings.Size = new Size(264, 22);
            menuLoadDatabaseSettings.Text = "Open Database Settings...";
            menuLoadDatabaseSettings.Click += this.tbDatabaseSettings_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(261, 6);
            // 
            // tbSave
            // 
            tbSave.Name = "tbSave";
            tbSave.ShortcutKeys = Keys.Control | Keys.S;
            tbSave.Size = new Size(264, 22);
            tbSave.Text = "Save Current Settings";
            tbSave.Click += this.tbSave_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(261, 6);
            // 
            // tbExit
            // 
            tbExit.Name = "tbExit";
            tbExit.ShortcutKeys = Keys.Alt | Keys.F4;
            tbExit.Size = new Size(264, 22);
            tbExit.Text = "Exit(&X)";
            tbExit.Click += this.tbExit_Click;
            // 
            // menuTools
            // 
            menuTools.DropDownItems.AddRange(new ToolStripItem[] { menuApiConnect, tbTestConnection, menuGenerateMasterKey });
            menuTools.Name = "menuTools";
            menuTools.Size = new Size(65, 20);
            menuTools.Text = "Tools(&T)";
            // 
            // menuApiConnect
            // 
            menuApiConnect.Name = "menuApiConnect";
            menuApiConnect.ShortcutKeys = Keys.Control | Keys.M;
            menuApiConnect.Size = new Size(275, 22);
            menuApiConnect.Text = "Set API Connection Mode...";
            menuApiConnect.Click += this.tbApiConnect_Click;
            // 
            // tbTestConnection
            // 
            tbTestConnection.Name = "tbTestConnection";
            tbTestConnection.ShortcutKeys = Keys.Control | Keys.T;
            tbTestConnection.Size = new Size(275, 22);
            tbTestConnection.Text = "Test Database Connection";
            tbTestConnection.Click += this.tbTestConnection_Click;
            // 
            // menuGenerateMasterKey
            // 
            menuGenerateMasterKey.Name = "menuGenerateMasterKey";
            menuGenerateMasterKey.ShortcutKeys = Keys.Control | Keys.G;
            menuGenerateMasterKey.Size = new Size(275, 22);
            menuGenerateMasterKey.Text = "Generate Master Key...";
            menuGenerateMasterKey.Click += this.menuGenerateMasterKey_Click;
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
            this.Text = "Bee.NET Settings Editor";
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
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuTools;
        private ToolStripMenuItem menuLoadSystemSettings;
        private ToolStripMenuItem menuLoadDatabaseSettings;
        private Bee.UI.WinForms.BeeTreeView treeView;
        private Bee.UI.WinForms.BeePropertyGrid propertyGrid;
        private SplitContainer splitContainer;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem tbSave;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem tbExit;
        private ToolStripMenuItem menuApiConnect;
        private ToolStripStatusLabel lblConnectType;
        private ToolStripMenuItem tbTestConnection;
        private ToolStripMenuItem menuGenerateMasterKey;
    }
}
