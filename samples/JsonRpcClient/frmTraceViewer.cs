using Bee.Api.Core;
using Bee.Base;
using Bee.Connect;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;
using System.Data;

namespace JsonRpcClient
{
    /// <summary>
    /// Trace Viewer.
    /// </summary>
    public partial class frmTraceViewer : Form, ITraceDisplayForm
    {
        private DataTable? _traceTable = null;
        private bool _enableTrace = false;

        public frmTraceViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Form load event handler.
        /// </summary>
        private void frmTraceViewer_Load(object sender, EventArgs e)
        {
            edtTraceCategory.Items.Clear();
            edtTraceCategory.Items.Add(TraceCategories.General);
            edtTraceCategory.Items.Add(TraceCategories.JsonRpc);
            edtTraceCategory.SelectedIndex = 0;

            _traceTable = CreaetTraceTable();
            gvTrace.DataSource = _traceTable;
            gvTrace.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gvTrace.ReadOnly = true;
            gvTrace.AllowUserToAddRows = false;

            gvTrace.SelectionChanged += gvTrace_FocusedRowChanged;
            gvTrace.CellFormatting += gvTrace_CellFormatting;

            if (gvTrace.Columns.Contains("Detail"))
                gvTrace.Columns["Detail"].Visible = false;

            SysInfo.TraceListener = new TraceListener(new FormTraceWriter(this));

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
            if (!_enableTrace || _traceTable == null) return;

            bool isMatch;
            if (edtTraceCategory.Text == TraceCategories.General)
                isMatch = evt.Category == string.Empty || evt.Category.Equals(TraceCategories.General);
            else
                isMatch = evt.Category.Equals(TraceCategories.JsonRpc);

            if (!isMatch) { return; }

            string detail = evt.Detail;
            if (evt.Tag is JsonRpcRequest request)
                detail = GetJsonRpcRequest(request);
            else if (evt.Tag is JsonRpcResponse response)
                detail = GetJsonRpcResponse(response);

            var row = _traceTable.NewRow();
            row["Time"] = evt.Time.DateTime;
            row["Layer"] = evt.Layer.ToString();
            row["Name"] = evt.Name;
            row["Kind"] = evt.Kind.ToString();
            row["Status"] = evt.Status.ToString();
            row["DurationMs"] = evt.DurationMs;
            row["Detail"] = detail;
            _traceTable.Rows.Add(row);
        }

        /// <summary>
        /// Converts a JsonRpcRequest to a detailed string including raw JSON and curl command.
        /// </summary>
        private string GetJsonRpcRequest(JsonRpcRequest request)
        {
            // Build raw JSON
            var rawJson = request.ToJson();

            // Build curl command
            var endpoint = StrFunc.IsNotEmpty(ApiClientContext.Endpoint) ? ApiClientContext.Endpoint : "http://localhost/api/jsonrpc";
            var authHeader = $"Bearer {ClientInfo.AccessToken}";

            var curl = "curl -X POST "
                     + $"\"{endpoint}\" "
                     + "-H \"Content-Type: application/json\" \n"
                     + $"-H \"X-Api-Key: {ApiClientContext.ApiKey}\" \n"
                     + $"-H \"Authorization: {authHeader}\" \n"
                     + $"--data '{rawJson}'";

            var detail =
                "=== JSON-RPC ===\n"
                + rawJson + "\n\n"
                + "=== curl ===\n"
                + curl;
            return detail.Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Formats a JSON-RPC response object into a detailed string representation.
        /// </summary>
        private string GetJsonRpcResponse(JsonRpcResponse response)
        {
            // Build raw JSON
            var rawJson = response.ToJson();
            var detail =
                "=== JSON-RPC ===\n"
                + rawJson;
            return detail.Replace("\n", Environment.NewLine); ;
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

        /// <summary>
        /// Adjusts the column widths of the DataGridView based on content and header.
        /// </summary>
        private void AdjustColumnWidths()
        {
            foreach (DataGridViewColumn column in gvTrace.Columns)
            {
                // Auto adjust column width based on content and header
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        private void btnStartTrace_Click(object sender, EventArgs e)
        {
            _enableTrace = true;
            btnStartTrace.Visible = false;
            edtTraceCategory.Enabled = false;
        }

        private void btnStopTrace_Click(object sender, EventArgs e)
        {
            _enableTrace = false;
            btnStartTrace.Visible = true;
            edtTraceCategory.Enabled = true;
        }

        private void btnClearTrace_Click(object sender, EventArgs e)
        {
            _traceTable?.Rows.Clear();
        }


    }
}
