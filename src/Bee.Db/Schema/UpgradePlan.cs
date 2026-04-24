namespace Bee.Db.Schema
{
    /// <summary>
    /// The execution plan for a table schema upgrade: the chosen mode, the ordered stages to run,
    /// and any warnings surfaced during planning.
    /// </summary>
    public class UpgradePlan
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UpgradePlan"/>.
        /// </summary>
        /// <param name="mode">The execution mode.</param>
        /// <param name="stages">The ordered list of stages to execute; defaults to empty.</param>
        /// <param name="warnings">Warnings surfaced during planning (e.g. narrowing changes); defaults to empty.</param>
        public UpgradePlan(UpgradeExecutionMode mode, IEnumerable<UpgradeStage>? stages = null, IEnumerable<string>? warnings = null)
        {
            Mode = mode;
            Stages = stages == null ? [] : [.. stages];
            Warnings = warnings == null ? [] : [.. warnings];
        }

        /// <summary>
        /// Gets the execution mode.
        /// </summary>
        public UpgradeExecutionMode Mode { get; }

        /// <summary>
        /// Gets the ordered list of stages. Each stage runs in its own transaction.
        /// </summary>
        public List<UpgradeStage> Stages { get; }

        /// <summary>
        /// Gets the warnings surfaced during planning (e.g. narrowing changes that were permitted).
        /// </summary>
        public List<string> Warnings { get; }

        /// <summary>
        /// Gets a value indicating whether the plan has nothing to execute.
        /// </summary>
        public bool IsEmpty => Mode == UpgradeExecutionMode.NoChange;

        /// <summary>
        /// Gets all SQL statements across every stage, in execution order.
        /// </summary>
        public IEnumerable<string> AllStatements
        {
            get
            {
                foreach (var stage in Stages)
                {
                    foreach (var sql in stage.Statements)
                        yield return sql;
                }
            }
        }
    }
}
