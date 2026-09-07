using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Writes a record back, the only scenario that reaches a transaction and a write lock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each virtual user updates its own record rather than inserting new ones. Inserting would
    /// grow the table throughout the run, so the read scenarios measured alongside it would face a
    /// different amount of data at the end than at the start; giving each user its own row also
    /// keeps them from contending on the same lock, which would measure the contention rather than
    /// the write.
    /// </para>
    /// <para>
    /// The record is fetched once per virtual user and reused as a template, so the timed call is
    /// the save alone rather than a read followed by a save.
    /// </para>
    /// </remarks>
    public sealed class SaveScenario : IScenario
    {
        private readonly ConcurrentDictionary<int, Lazy<Task<DataSet>>> _templates = new();
        private readonly VirtualUserPool _pool;
        private readonly string _progId;
        private readonly Guid[] _rowIds;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="pool">The signed-in user pool.</param>
        /// <param name="progId">The program to write to.</param>
        /// <param name="rowIds">Keys to distribute across the virtual users.</param>
        public SaveScenario(VirtualUserPool pool, string progId, IReadOnlyCollection<Guid> rowIds)
        {
            ArgumentNullException.ThrowIfNull(rowIds);
            if (rowIds.Count == 0)
            {
                throw new ArgumentException(
                    "No keys were collected, so there is nothing to update. Run 'prepare' first, " +
                    "or disable the scenario.", nameof(rowIds));
            }

            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _progId = string.IsNullOrWhiteSpace(progId) ? "Customer" : progId;
            _rowIds = [.. rowIds];
        }

        /// <inheritdoc/>
        public string Name => "Save";

        /// <inheritdoc/>
        public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            var user = await _pool.GetAsync(context.VirtualUserIndex).ConfigureAwait(false);
            var connector = user.CreateFormConnector(_progId);

            var template = await _templates.GetOrAdd(
                context.VirtualUserIndex,
                index => new Lazy<Task<DataSet>>(() => FetchTemplateAsync(index))).Value
                .ConfigureAwait(false);

            var dataSet = template.Copy();
            Touch(dataSet, context.Iteration);

            await connector.SaveAsync(dataSet).ConfigureAwait(false);
        }

        private async Task<DataSet> FetchTemplateAsync(int virtualUserIndex)
        {
            var user = await _pool.GetAsync(virtualUserIndex).ConfigureAwait(false);
            var connector = user.CreateFormConnector(_progId);

            var rowId = _rowIds[virtualUserIndex % _rowIds.Length];
            var response = await connector.GetDataAsync(rowId).ConfigureAwait(false);

            return response.DataSet
                ?? throw new InvalidOperationException(
                    $"Reading '{_progId}' record {rowId} returned no data set, so this scenario " +
                    "has no template to write back.");
        }

        /// <summary>
        /// Marks one column changed so the save has something to write.
        /// </summary>
        /// <param name="dataSet">The copied data set.</param>
        /// <param name="iteration">The iteration number, written into the value.</param>
        internal static void Touch(DataSet dataSet, long iteration)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0) { return; }

            var row = dataSet.Tables[0].Rows[0];
            if (!row.Table.Columns.Contains("sys_name")) { return; }

            // A row copied out of a read is Unchanged; assigning moves it to Modified, which is
            // what makes the save write anything at all.
            row["sys_name"] = "updated-" + iteration.ToString(CultureInfo.InvariantCulture);
        }
    }
}
