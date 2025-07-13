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
            menuTestDbConnection = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            menuGenerateMasterKey = new ToolStripMenuItem();
            menuGenApiEncryptionKey = new ToolStripMenuItem();
            menuGenCookieEncryptionKey = new ToolStripMenuItem();
            menuGenConfigEncryptionKey = new ToolStripMenuItem();
            menuGenDatabaseEncryptionKey = new ToolStripMenuItem();
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
            menuBar.ImageScalingSize = new Size(20, 20);
            menuBar.Items.AddRange(new ToolStripItem[] { menuFile, menuTools });
            menuBar.Location = new Point(0, 0);
            menuBar.Name = "menuBar";
            menuBar.Padding = new Padding(7, 3, 0, 3);
            menuBar.Size = new Size(782, 30);
            menuBar.TabIndex = 0;
            menuBar.Text = "menuStrip1";
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuLoadSystemSettings, menuLoadDatabaseSettings, toolStripMenuItem1, tbSave, toolStripMenuItem2, tbExit });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(65, 24);
            menuFile.Text = "File(&F)";
            // 
            // menuLoadSystemSettings
            // 
            menuLoadSystemSettings.Name = "menuLoadSystemSettings";
            menuLoadSystemSettings.ShortcutKeys = Keys.Control | Keys.O;
            menuLoadSystemSettings.Size = new Size(315, 26);
            menuLoadSystemSettings.Text = "Open  System Settings";
            menuLoadSystemSettings.Click += this.menuLoadSystemSettings_Click;
            // 
            // menuLoadDatabaseSettings
            // 
            menuLoadDatabaseSettings.Name = "menuLoadDatabaseSettings";
            menuLoadDatabaseSettings.ShortcutKeys = Keys.Control | Keys.D;
            menuLoadDatabaseSettings.Size = new Size(315, 26);
            menuLoadDatabaseSettings.Text = "Open Database Settings";
            menuLoadDatabaseSettings.Click += this.menuLoadDatabaseSettings_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(312, 6);
            // 
            // tbSave
            // 
            tbSave.Name = "tbSave";
            tbSave.ShortcutKeys = Keys.Control | Keys.S;
            tbSave.Size = new Size(315, 26);
            tbSave.Text = "Save Current Settings";
            tbSave.Click += this.tbSave_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(312, 6);
            // 
            // tbExit
            // 
            tbExit.Name = "tbExit";
            tbExit.ShortcutKeys = Keys.Alt | Keys.F4;
            tbExit.Size = new Size(315, 26);
            tbExit.Text = "Exit(&X)";
            tbExit.Click += this.tbExit_Click;
            // 
            // menuTools
            // 
            menuTools.DropDownItems.AddRange(new ToolStripItem[] { menuApiConnect, menuTestDbConnection, toolStripSeparator1, menuGenerateMasterKey, menuGenApiEncryptionKey, menuGenCookieEncryptionKey, menuGenConfigEncryptionKey, menuGenDatabaseEncryptionKey });
            menuTools.Name = "menuTools";
            menuTools.Size = new Size(78, 24);
            menuTools.Text = "Tools(&T)";
            // 
            // menuApiConnect
            // 
            menuApiConnect.Name = "menuApiConnect";
            menuApiConnect.ShortcutKeys = Keys.Control | Keys.M;
            menuApiConnect.Size = new Size(359, 26);
            menuApiConnect.Text = "Set API Connection Endpoint...";
            menuApiConnect.Click += this.tbApiConnect_Click;
            // 
            // menuTestDbConnection
            // 
            menuTestDbConnection.Name = "menuTestDbConnection";
            menuTestDbConnection.ShortcutKeys = Keys.Control | Keys.T;
            menuTestDbConnection.Size = new Size(359, 26);
            menuTestDbConnection.Text = "Test Database Connection";
            menuTestDbConnection.Click += this.menuTestDbConnection_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(356, 6);
            // 
            // menuGenerateMasterKey
            // 
            menuGenerateMasterKey.Name = "menuGenerateMasterKey";
            menuGenerateMasterKey.ShortcutKeys = Keys.Control | Keys.G;
            menuGenerateMasterKey.Size = new Size(359, 26);
            menuGenerateMasterKey.Text = "Generate Master Key...";
            menuGenerateMasterKey.Click += this.menuGenerateMasterKey_Click;
            // 
            // menuGenApiEncryptionKey
            // 
            menuGenApiEncryptionKey.Name = "menuGenApiEncryptionKey";
            menuGenApiEncryptionKey.Size = new Size(359, 26);
            menuGenApiEncryptionKey.Text = "Generate API Encryption Key";
            menuGenApiEncryptionKey.Click += this.menuGenApiEncryptionKey_Click;
            // 
            // menuGenCookieEncryptionKey
            // 
            menuGenCookieEncryptionKey.Name = "menuGenCookieEncryptionKey";
            menuGenCookieEncryptionKey.Size = new Size(359, 26);
            menuGenCookieEncryptionKey.Text = "Generate Cookie Encryption Key";
            menuGenCookieEncryptionKey.Click += this.menuGenCookieEncryptionKey_Click;
            // 
            // menuGenConfigEncryptionKey
            // 
            menuGenConfigEncryptionKey.Name = "menuGenConfigEncryptionKey";
            menuGenConfigEncryptionKey.Size = new Size(359, 26);
            menuGenConfigEncryptionKey.Text = "Generate Config Encryption Key";
            menuGenConfigEncryptionKey.Click += this.menuGenConfigEncryptionKey_Click;
            // 
            // menuGenDatabaseEncryptionKey
            // 
            menuGenDatabaseEncryptionKey.Name = "menuGenDatabaseEncryptionKey";
            menuGenDatabaseEncryptionKey.Size = new Size(359, 26);
            menuGenDatabaseEncryptionKey.Text = "Generate Database Encryption Key";
            menuGenDatabaseEncryptionKey.Click += this.menuGenDatabaseEncryptionKey_Click;
            // 
            // statusBar
            // 
            statusBar.ImageScalingSize = new Size(20, 20);
            statusBar.Items.AddRange(new ToolStripItem[] { lblConnectType });
            statusBar.Location = new Point(0, 568);
            statusBar.Name = "statusBar";
            statusBar.Padding = new Padding(1, 0, 16, 0);
            statusBar.Size = new Size(782, 25);
            statusBar.TabIndex = 1;
            statusBar.Text = "statusStrip1";
            // 
            // lblConnectType
            // 
            lblConnectType.Name = "lblConnectType";
            lblConnectType.Size = new Size(69, 19);
            lblConnectType.Text = "連線方式";
            // 
            // treeView
            // 
            treeView.Dock = DockStyle.Fill;
            treeView.Location = new Point(0, 0);
            treeView.Margin = new Padding(3, 4, 3, 4);
            treeView.Name = "treeView";
            treeView.PropertyGrid = propertyGrid;
            treeView.Size = new Size(249, 538);
            treeView.TabIndex = 2;
            // 
            // propertyGrid
            // 
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 0);
            propertyGrid.Margin = new Padding(3, 4, 3, 4);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(528, 538);
            propertyGrid.TabIndex = 3;
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 30);
            splitContainer.Margin = new Padding(3, 4, 3, 4);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(treeView);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(propertyGrid);
            splitContainer.Size = new Size(782, 538);
            splitContainer.SplitterDistance = 249;
            splitContainer.SplitterWidth = 5;
            splitContainer.TabIndex = 4;
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new SizeF(8F, 18F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(782, 593);
            this.Controls.Add(splitContainer);
            this.Controls.Add(statusBar);
            this.Controls.Add(menuBar);
            this.MainMenuStrip = menuBar;
            this.Margin = new Padding(3, 4, 3, 4);
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
        private ToolStripMenuItem menuTestDbConnection;
        private ToolStripMenuItem menuGenerateMasterKey;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem menuGenApiEncryptionKey;
        private ToolStripMenuItem menuGenCookieEncryptionKey;
        private ToolStripMenuItem menuGenConfigEncryptionKey;
        private ToolStripMenuItem menuGenDatabaseEncryptionKey;
    }
}
