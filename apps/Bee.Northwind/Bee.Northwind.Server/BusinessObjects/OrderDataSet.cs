using System.Data;
using Bee.Base;
using Bee.Base.Exceptions;

namespace Bee.Northwind.Server.BusinessObjects;

/// <summary>
/// Database-free <c>DataSet</c> operations for the order form: locating the master / detail
/// rows, required-field validation, and authoritative amount calculation. Kept separate from
/// <see cref="OrderBO"/> (which owns the database-dependent number / status rules) so this
/// pure-in-memory logic is unit-testable against a hand-built <c>DataSet</c>.
/// </summary>
internal static class OrderDataSet
{
    // DataSet table names are FormTable.TableName; the master equals ProgId by framework invariant.
    public const string MasterTable = "Order";
    public const string DetailTable = "OrderDetail";

    /// <summary>Returns the single active (non-deleted) master row.</summary>
    public static DataRow MasterRow(DataSet dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        var table = dataSet.Tables[MasterTable]
            ?? throw new UserMessageException("The order is missing its master record.");
        foreach (DataRow row in table.Rows)
        {
            if (row.RowState != DataRowState.Deleted) { return row; }
        }
        throw new UserMessageException("The order is missing its master record.");
    }

    /// <summary>Enumerates the active (non-deleted) detail rows.</summary>
    public static IEnumerable<DataRow> ActiveDetails(DataSet dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        var table = dataSet.Tables[DetailTable];
        if (table == null) { yield break; }
        foreach (DataRow row in table.Rows)
        {
            if (row.RowState != DataRowState.Deleted) { yield return row; }
        }
    }

    /// <summary>Whether any detail row is added, modified, or deleted.</summary>
    public static bool HasDetailEdits(DataSet dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        var table = dataSet.Tables[DetailTable];
        if (table == null) { return false; }
        foreach (DataRow row in table.Rows)
        {
            if (row.RowState != DataRowState.Unchanged) { return true; }
        }
        return false;
    }

    /// <summary>
    /// Aggregate validation the declarative rule model cannot yet express: an order must have at
    /// least one detail line. Per-row required-field checks (customer, product, positive quantity)
    /// are now declarative <c>FormRule</c>s in <c>Order.FormSchema.xml</c>.
    /// </summary>
    public static void RequireAtLeastOneDetail(DataSet dataSet)
    {
        if (!ActiveDetails(dataSet).Any())
            throw new UserMessageException("The order must have at least one detail line.");
    }

    /// <summary>
    /// Sums the (already computed and rounded) line amounts into the master total — authoritatively,
    /// so a forged client total never persists. Returns the computed total.
    /// </summary>
    /// <remarks>
    /// The per-line <c>amount</c> is computed by the framework rule engine from the field's
    /// <c>ValueExpression</c> (rounded per <c>NumberKind</c>) before this runs, so summing the
    /// rounded lines preserves the round-then-sum invariant. This aggregate stays in code because
    /// a cross-row SUM is not yet expressible as a field expression.
    ///
    /// The total is written back only when it actually differs from the current cell. Assigning an
    /// equal value through the <c>DataRow</c> indexer still flips an Unchanged master to Modified,
    /// which on save would reach the framework's UPDATE builder with no changed columns and raise
    /// "UPDATE would be empty".
    /// </remarks>
    public static decimal ComputeTotal(DataSet dataSet)
    {
        var master = MasterRow(dataSet);
        decimal total = 0m;
        foreach (var row in ActiveDetails(dataSet))
        {
            total += ValueUtilities.CDecimal(row["amount"]);
        }
        if (ValueUtilities.CDecimal(master["total_amount"]) != total)
            master["total_amount"] = total;
        return total;
    }
}
