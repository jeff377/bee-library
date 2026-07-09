using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Business.Form;
using Bee.Db;
using Bee.Definition;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Northwind.Server.BusinessObjects;

/// <summary>
/// Application-layer business object for the <c>Order</c> form — the demo's deliberate
/// "pro-code boundary" example. Most of the order's logic is now declarative: the line
/// <c>amount</c> is a computed field (<c>ValueExpression</c>) and the required-field checks are
/// <c>FormRule</c>s in <c>Order.FormSchema.xml</c>. This class overrides <see cref="GetNewData"/>
/// to seed the order date and <see cref="DoBeforeSave"/> only for the rules a per-row expression
/// cannot yet express: the at-least-one-detail aggregate, the master total (a cross-row SUM), the
/// status-transition guard (needs the stored status), and order-number generation (needs the
/// database sequence).
/// </summary>
/// <remarks>
/// Registered for progId <c>Order</c> via <c>Define/ProgramSettings.xml</c>; the framework's
/// <c>ProgramSettingsFormBoTypeResolver</c> loads this type by its assembly-qualified name. The
/// pure rules live in <see cref="OrderRules"/> and the in-memory DataSet logic in
/// <see cref="OrderDataSet"/> (both unit-tested); this class owns only the database-dependent
/// parts — reading the stored status and the per-month number sequence.
/// </remarks>
public sealed class OrderBO : FormBusinessObject
{
    /// <summary>
    /// Initializes a new instance. The signature mirrors <see cref="FormBusinessObject"/> so the
    /// factory's <c>Activator.CreateInstance</c> can resolve the constructor.
    /// </summary>
    /// <param name="ctx">The per-call context.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="progId">The program identifier (expected to be "Order").</param>
    /// <param name="isLocalCall">Whether the call originates from a local source.</param>
    public OrderBO(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
    {
    }

    /// <inheritdoc/>
    public override GetNewDataResult GetNewData(GetNewDataArgs args)
    {
        var result = base.GetNewData(args);

        // Seed the order date to today; the status default ("Draft") comes from the FormField.
        var master = result.DataSet?.Tables[OrderDataSet.MasterTable];
        if (master is { Rows.Count: > 0 } && master.Columns.Contains("order_date"))
            master.Rows[0]["order_date"] = DateTime.Today;

        return result;
    }

    /// <inheritdoc/>
    protected override void DoBeforeSave(SaveContext context)
    {
        // Declarative rules run first: the per-line amount (ValueExpression) and the customer /
        // product / quantity required-field checks (FormRule) declared in Order.FormSchema.xml.
        base.DoBeforeSave(context);

        var dataSet = context.DataSet;
        var master = OrderDataSet.MasterRow(dataSet);

        // The rules a per-row field expression cannot yet express stay in code:
        OrderDataSet.RequireAtLeastOneDetail(dataSet);   // aggregate — needs a detail-line count
        EnforceStatusRules(dataSet, master);             // needs the stored status from the database
        AssignOrderNumber(master);                       // needs the database number sequence
        OrderDataSet.ComputeTotal(dataSet);              // aggregate — SUM of the rounded line amounts
    }

    /// <summary>
    /// Status-transition guard for an existing order: the move from the stored status to the
    /// submitted one must be valid, and once the stored status is Confirmed the details are
    /// locked. New orders (Added master) bypass this — their initial status is governed only by
    /// the field default.
    /// </summary>
    private void EnforceStatusRules(DataSet dataSet, DataRow master)
    {
        if (master.RowState == DataRowState.Added) { return; }

        var storedStatus = QueryStoredStatus(ValueUtilities.CGuid(master["sys_rowid"]));
        if (string.IsNullOrEmpty(storedStatus)) { return; }

        var newStatus = ValueUtilities.CStr(master["status"]);
        if (!OrderRules.IsValidTransition(storedStatus, newStatus))
            throw new UserMessageException($"Cannot change order status from '{storedStatus}' to '{newStatus}'.");

        if (OrderRules.DetailsLocked(storedStatus) && OrderDataSet.HasDetailEdits(dataSet))
            throw new UserMessageException($"Order details are locked once the order is '{storedStatus}'.");
    }

    /// <summary>
    /// Assigns the order number <c>ORD-yyyyMM-NNN</c> to a new order whose number is still blank.
    /// </summary>
    /// <remarks>
    /// The sequence is derived from the current per-month maximum read just before insert. Under
    /// concurrent inserts two callers could read the same maximum and collide; the unique index on
    /// <c>sys_id</c> would then reject the second. A production system would allocate from a
    /// transactional sequence table — out of scope for the demo (see the plan's framework-feedback
    /// list).
    /// </remarks>
    private void AssignOrderNumber(DataRow master)
    {
        if (master.RowState != DataRowState.Added) { return; }
        if (!string.IsNullOrEmpty(ValueUtilities.CStr(master["sys_id"]))) { return; }

        var yearMonth = DateTime.Today.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var currentMax = QueryMaxOrderNumber($"ORD-{yearMonth}-%");
        master["sys_id"] = OrderRules.NextOrderNumber(yearMonth, currentMax);
    }

    /// <summary>Reads the persisted status of an order by its row id (empty string when absent).</summary>
    private string QueryStoredStatus(Guid rowId)
    {
        if (rowId == Guid.Empty) { return string.Empty; }
        var spec = new DbCommandSpec(DbCommandKind.Scalar,
            "SELECT status FROM ft_order WHERE sys_rowid = {0}", rowId);
        return ValueUtilities.CStr(CommonDbAccess().Execute(spec).Scalar ?? string.Empty);
    }

    /// <summary>Reads the highest existing order number matching the month's LIKE prefix.</summary>
    private string? QueryMaxOrderNumber(string likePrefix)
    {
        var spec = new DbCommandSpec(DbCommandKind.Scalar,
            "SELECT MAX(sys_id) FROM ft_order WHERE sys_id LIKE {0}", likePrefix);
        return CommonDbAccess().Execute(spec).Scalar as string;
    }

    /// <summary>Creates a <see cref="DbAccess"/> bound to the shared "common" database.</summary>
    private DbAccess CommonDbAccess()
        => Services.GetRequiredService<IDbAccessFactory>().Create(ResolveDatabaseId(DbScope.Common));
}
