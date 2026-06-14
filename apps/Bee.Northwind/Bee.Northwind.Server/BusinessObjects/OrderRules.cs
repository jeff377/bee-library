using System.Globalization;

namespace Bee.Northwind.Server.BusinessObjects;

/// <summary>
/// Pure order business rules — order-number formatting, line-amount arithmetic, and
/// status-transition validity. Kept free of <c>DataSet</c> / database dependencies so the
/// rules are unit-testable in isolation; <see cref="OrderBO"/> is the thin orchestrator
/// that reads the <c>DataSet</c>, queries the database, and applies these rules.
/// </summary>
internal static class OrderRules
{
    /// <summary>Draft — editable, the initial status of a new order.</summary>
    public const string StatusDraft = "Draft";

    /// <summary>Confirmed — committed; details are locked against further edits.</summary>
    public const string StatusConfirmed = "Confirmed";

    /// <summary>Shipped — fulfilled; the terminal status.</summary>
    public const string StatusShipped = "Shipped";

    /// <summary>The lifecycle order of the statuses, low to high.</summary>
    private static readonly string[] s_sequence = { StatusDraft, StatusConfirmed, StatusShipped };

    /// <summary>
    /// Returns the lifecycle rank of <paramref name="status"/> (0 = Draft), or -1 when the
    /// value is not a known status.
    /// </summary>
    public static int StatusRank(string status)
        => Array.FindIndex(s_sequence, s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Whether moving from <paramref name="from"/> to <paramref name="to"/> is allowed: the
    /// status may stay the same or advance by exactly one step (Draft → Confirmed → Shipped).
    /// Backward moves and skipping a step (Draft → Shipped) are rejected; an unknown value on
    /// either side is invalid.
    /// </summary>
    public static bool IsValidTransition(string from, string to)
    {
        var f = StatusRank(from);
        var t = StatusRank(to);
        if (f < 0 || t < 0) { return false; }
        return t == f || t == f + 1;
    }

    /// <summary>
    /// Whether an order at <paramref name="storedStatus"/> has its details locked — true once
    /// the order is Confirmed or later.
    /// </summary>
    public static bool DetailsLocked(string storedStatus) => StatusRank(storedStatus) >= StatusRank(StatusConfirmed);

    /// <summary>
    /// The line amount: <c>quantity × unitPrice × (1 - discount)</c>. <paramref name="discount"/>
    /// is a fraction (0.10 = 10% off).
    /// </summary>
    public static decimal LineAmount(int quantity, decimal unitPrice, decimal discount)
        => quantity * unitPrice * (1m - discount);

    /// <summary>
    /// Builds the next order number for a year-month, formatted <c>ORD-yyyyMM-NNN</c>.
    /// <paramref name="currentMax">currentMax</paramref> is the highest existing number for that
    /// month (e.g. <c>"ORD-202606-007"</c>) or <c>null</c> when the month has none yet; the
    /// trailing sequence is incremented (or starts at 001). A malformed
    /// <paramref name="currentMax"/> restarts the sequence rather than throwing — the LIKE
    /// query that feeds it only ever returns values of this shape.
    /// </summary>
    public static string NextOrderNumber(string yearMonth, string? currentMax)
    {
        var next = 1;
        if (!string.IsNullOrEmpty(currentMax))
        {
            var lastDash = currentMax.LastIndexOf('-');
            if (lastDash >= 0
                && int.TryParse(currentMax.AsSpan(lastDash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                next = n + 1;
            }
        }
        return string.Create(CultureInfo.InvariantCulture, $"ORD-{yearMonth}-{next:D3}");
    }
}
