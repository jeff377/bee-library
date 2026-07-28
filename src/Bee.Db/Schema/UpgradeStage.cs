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
        /// <remarks>
        /// Read-only: the statements are computed by schema comparison and executed verbatim, so
        /// handing out a mutable list would let a caller inject arbitrary SQL into a plan that has
        /// already been reviewed.
        /// </remarks>
        public IReadOnlyList<string> Statements { get; }
    }
}
