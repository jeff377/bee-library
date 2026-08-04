using Bee.Repository.Abstractions.Form;

namespace Bee.Northwind.Server.Repositories;

/// <summary>
/// Data access for the <c>Order</c> form: the framework's CRUD surface plus the two queries
/// <see cref="BusinessObjects.OrderBO"/> needs that no declaration can express.
/// </summary>
/// <remarks>
/// <b>The style template for a form's own repository.</b> It <i>extends</i>
/// <see cref="IDataFormRepository"/> rather than replacing it, because <c>FormBusinessObject</c>
/// reaches CRUD through the base contract — a repository that dropped it would still have to
/// implement every member, only without saying so. The two members below are the whole reason this
/// interface exists: everything else the order needs is generated from <c>Order.FormSchema.xml</c>.
/// <para>
/// Bound to the progId in <c>Define/ProgramSettings.xml</c> via <c>ProgramItem.Repository</c>,
/// alongside the <c>BusinessObject</c> binding on the same entry. One progId, one business object,
/// one repository.
/// </para>
/// </remarks>
public interface IOrderRepository : IDataFormRepository
{
    /// <summary>
    /// Reads the persisted status of an order.
    /// </summary>
    /// <param name="rowId">The order's row id.</param>
    /// <returns>The stored status, or an empty string when the order does not exist.</returns>
    /// <remarks>
    /// The submitted status cannot be trusted for a transition check — it is the value being
    /// proposed. The guard needs the one already in the database.
    /// </remarks>
    string GetStoredStatus(Guid rowId);

    /// <summary>
    /// Reads the highest existing order number matching a <c>LIKE</c> prefix.
    /// </summary>
    /// <param name="likePrefix">The prefix pattern, e.g. <c>"ORD-202608-%"</c>.</param>
    /// <returns>The highest matching number, or <c>null</c> when the month has none yet.</returns>
    string? GetMaxOrderNumber(string likePrefix);
}
