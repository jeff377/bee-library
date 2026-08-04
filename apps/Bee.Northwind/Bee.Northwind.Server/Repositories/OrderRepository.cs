using Bee.Base;
using Bee.Db;
using Bee.Repository;
using Bee.Repository.Form;

namespace Bee.Northwind.Server.Repositories;

/// <summary>
/// <see cref="IOrderRepository"/> over the company database, extending the framework's
/// schema-driven CRUD with the order's two hand-written queries.
/// </summary>
/// <remarks>
/// <para>
/// Deriving from <see cref="DataFormRepository"/> is what keeps the CRUD surface intact: the base
/// builds every statement from <c>Order.FormSchema.xml</c>, and this class only adds what a schema
/// cannot state. It is also the framework's construction contract — the factory rejects a bound
/// type that does not derive from it.
/// </para>
/// <para>
/// The target database is not named here. <see cref="RepositoryBase"/> resolves it from the form
/// schema's <c>CategoryId</c> (<c>company</c> for orders) and the session, so the query below
/// reaches the right company's data without this class knowing how many companies exist. The
/// business object used to run these two statements itself against
/// <c>ResolveDatabaseId(DbScope.Common)</c> — a company table read through the common route, which
/// this demo got away with only because both categories map to the same SQLite file.
/// </para>
/// <para>
/// The constructor takes exactly the framework's three parameters. A subclass that needs its own
/// dependencies adds interface-typed ones and the factory injects them from DI, but it must not add
/// a second <c>string</c> or <c>Guid</c> — those are already supplied, leaving nothing to bind a
/// second one to.
/// </para>
/// </remarks>
public sealed class OrderRepository : DataFormRepository, IOrderRepository
{
    /// <summary>
    /// Initializes a new <see cref="OrderRepository"/>.
    /// </summary>
    /// <param name="ctx">The shared repository context.</param>
    /// <param name="accessToken">The current request's access token.</param>
    /// <param name="progId">The program identifier (expected to be "Order").</param>
    public OrderRepository(IRepositoryContext ctx, Guid accessToken, string progId)
        : base(ctx, accessToken, progId)
    {
    }

    /// <inheritdoc/>
    public string GetStoredStatus(Guid rowId)
    {
        if (rowId == Guid.Empty) { return string.Empty; }

        var spec = new DbCommandSpec(DbCommandKind.Scalar,
            "SELECT status FROM ft_order WHERE sys_rowid = {0}", rowId);
        return ValueUtilities.CStr(CreateDbAccess().Execute(spec).Scalar ?? string.Empty);
    }

    /// <inheritdoc/>
    public string? GetMaxOrderNumber(string likePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(likePrefix);

        var spec = new DbCommandSpec(DbCommandKind.Scalar,
            "SELECT MAX(sys_id) FROM ft_order WHERE sys_id LIKE {0}", likePrefix);
        return CreateDbAccess().Execute(spec).Scalar as string;
    }
}
