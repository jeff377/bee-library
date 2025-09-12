using Bee.Base;
using Bee.UI.WinForms;
using System.Data;

namespace JsonRpcClient
{
    /// <summary>
    /// Trace Viewer。
    /// </summary>
    public partial class frmTraceViewer : Form, ITraceDisplayForm
    {
        private DataTable? _traceTable = null;

        public frmTraceViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Form load event handler.
        /// </summary>
        private void frmTraceViewer_Load(object sender, EventArgs e)
        {
            _traceTable = CreaetTraceTable();
            gvTrace.DataSource = _traceTable;
            gvTrace.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gvTrace.ReadOnly = true;
            gvTrace.AllowUserToAddRows = false; 
            
            gvTrace.SelectionChanged += gvTrace_FocusedRowChanged;
            gvTrace.CellFormatting += gvTrace_CellFormatting;

            if (gvTrace.Columns.Contains("Detail"))
                gvTrace.Columns["Detail"].Visible = false;

            var writer = new FormTraceWriter(this);
            SysInfo.TraceListener = new TraceListener(TraceLayer.All, writer);

            AdjustColumnWidths();
        }

        /// <summary>
        /// Creates and returns a new DataTable instance for storing trace information.
        /// </summary>
        private DataTable CreaetTraceTable()
        {
            var table = new DataTable("Trace");
            table.Columns.Add("Time", typeof(DateTime));
            table.Columns.Add("Layer", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Kind", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("DurationMs", typeof(int));
            table.Columns.Add("Detail", typeof(string));
            return table;
        }

        /// <summary>
        /// Form closed event handler.
        /// </summary>
        private void frmTraceViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            SysInfo.TraceListener = null;
        }

        /// <summary>
        /// Displays a trace event in the log area of the form.
        /// </summary>
        public void AppendTrace(TraceEvent evt)
        {
            if (_traceTable == null) return;
            var row = _traceTable.NewRow();
            row["Time"] = evt.Time.DateTime;
            row["Layer"] = evt.Layer.ToString();
            row["Name"] = evt.Name;
            row["Kind"] = evt.Kind.ToString();
            row["Status"] = evt.Status.ToString();
            row["DurationMs"] = evt.DurationMs;
            row["Detail"] = evt.Detail;
            _traceTable.Rows.Add(row);

            //string message = $"Time : {evt.Time:yyyy/MM/dd HH:mm:ss}\r\nLayer : {evt.Layer}\r\nName : {evt.Name}\r\nKind : {evt.Kind}\r\n";
            //if (StrFunc.IsNotEmpty(evt.Detail))
            //    message += $"Detail : \r\n{evt.Detail}\r\n";
            //if (evt.Kind == TraceEventKind.End)
            //    message += $"Duration : {evt.DurationMs:F0} ms\r\n";
            //message += "-------------------------------------------------------------------------\r\n";
            //edtLog.AppendText(message + Environment.NewLine);
        }

        private void gvTrace_FocusedRowChanged(object? sender, EventArgs e)
        {
            if (gvTrace.CurrentRow != null)
            {
                var row = gvTrace.CurrentRow.DataBoundItem as DataRowView;
                if (row != null && row.Row.Table.Columns.Contains("Detail"))
                {
                    edtDetail.Text = row.Row["Detail"]?.ToString() ?? string.Empty;
                }
                else
                {
                    edtDetail.Text = string.Empty;
                }
            }
            else
            {
                edtDetail.Text = string.Empty;
            }
        }

        private void gvTrace_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gvTrace.Rows[e.RowIndex].DataBoundItem is DataRowView row)
            {
                if (BaseFunc.CStr(row.Row["Layer"]) == "ApiClient" && BaseFunc.CStr(row.Row["Kind"]) == "Start")
                {
                    if (e.CellStyle != null) e.CellStyle.ForeColor = Color.Blue;
                }
            }
        }

        private void AdjustColumnWidths()
        {
            foreach (DataGridViewColumn column in gvTrace.Columns)
            {
                // 自動調整欄寬，根據內容與標題
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

    }
}
