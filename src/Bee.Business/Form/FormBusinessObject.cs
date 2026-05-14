using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.Form
{
    /// <summary>
    /// Form-level business logic object.
    /// </summary>
    public class FormBusinessObject : BusinessObject, IFormBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="FormBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public FormBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
            : base(ctx, accessToken, isLocalCall)
        {
            ProgId = progId;
        }

        #endregion

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result);
        }

        /// <summary>
        /// Retrieves list-view rows by executing the FormSchema-driven SELECT statement
        /// for <see cref="ProgId"/>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// <b>This version does NOT paginate.</b> Callers MUST supply a <c>Filter</c>
        /// that bounds the result set; an unbounded query against a large table loads
        /// every matching row into memory on both the server and the client. Pagination
        /// support is tracked separately (see <c>docs/plans/plan-formbo-getlist-paging.md</c>
        /// when opened) and will be added as an additive, non-breaking
        /// <c>PagingOptions</c> field on <see cref="GetListArgs"/>.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetListResult GetList(GetListArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var factory = Services.GetRequiredService<IFormRepositoryFactory>();
            var repository = factory.CreateDataFormRepository(ProgId);
            var table = repository.GetList(args.SelectFields, args.Filter, args.SortFields);

            return new GetListResult { Table = table };
        }
    }
}
