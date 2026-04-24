namespace Bee.Db.Schema
{
    /// <summary>
    /// An ordered group of SQL statements that form one transactional step of an <see cref="UpgradePlan"/>.
    /// </summary>
    public class UpgradeStage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UpgradeStage"/>.
        /// </summary>
        /// <param name="kind">The stage kind.</param>
        /// <param name="statements">The SQL statements to execute in order within this stage.</param>
        public UpgradeStage(UpgradeStageKind kind, IEnumerable<string> statements)
        {
            Kind = kind;
            Statements = [.. statements];
        }

        /// <summary>
        /// Gets the stage kind.
        /// </summary>
        public UpgradeStageKind Kind { get; }

        /// <summary>
        /// Gets the SQL statements to execute, in order.
        /// </summary>
        public List<string> Statements { get; }
    }
}
