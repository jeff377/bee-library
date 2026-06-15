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
    /// Required-field validation: a customer, at least one detail line, and every line with a
    /// product and a positive quantity. Each violation raises a user-facing message.
    /// </summary>
    public static void Validate(DataSet dataSet)
    {
        var master = MasterRow(dataSet);
        if (ValueUtilities.CGuid(master["customer_rowid"]) == Guid.Empty)
            throw new UserMessageException("Please select a customer for the order.");

        var details = ActiveDetails(dataSet).ToList();
        if (details.Count == 0)
            throw new UserMessageException("The order must have at least one detail line.");

        foreach (var row in details)
        {
            if (ValueUtilities.CGuid(row["product_rowid"]) == Guid.Empty)
                throw new UserMessageException("Every detail line must select a product.");
            if (ValueUtilities.CInt(row["quantity"]) <= 0)
                throw new UserMessageException("Every detail line must have a quantity greater than zero.");
        }
    }

    /// <summary>
    /// Recomputes every line amount and the master total from quantity, unit price, and discount —
    /// authoritatively, so a forged client total never persists. Returns the computed total.
    /// </summary>
    /// <remarks>
    /// Each computed value is written back only when it actually differs from the current cell.
    /// Assigning an equal value through the <c>DataRow</c> indexer still flips an Unchanged row to
    /// Modified, which on save would reach the framework's UPDATE builder with no changed columns
    /// and raise "UPDATE would be empty". Reloading and re-saving an order must not mark untouched
    /// detail lines dirty.
    /// </remarks>
    public static decimal ComputeAmounts(DataSet dataSet)
    {
        var master = MasterRow(dataSet);
        decimal total = 0m;
        foreach (var row in ActiveDetails(dataSet))
        {
            var amount = OrderRules.LineAmount(
                ValueUtilities.CInt(row["quantity"]),
                ValueUtilities.CDecimal(row["unit_price"]),
                ValueUtilities.CDecimal(row["discount"]));
            if (ValueUtilities.CDecimal(row["amount"]) != amount)
                row["amount"] = amount;
            total += amount;
        }
        if (ValueUtilities.CDecimal(master["total_amount"]) != total)
            master["total_amount"] = total;
        return total;
    }
}
