using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

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
        /// When <see cref="GetListArgs.Paging"/> is <c>null</c> the query is unpaged
        /// and callers should supply a <c>Filter</c> that bounds the result set,
        /// otherwise an unbounded query against a large table loads every matching
        /// row into memory on both the server and the client. Set <c>Paging</c> to
        /// page through large result sets.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetListResult GetList(GetListArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var repository = CreateDataFormRepository(ProgId);
            var listResult = repository.GetList(args.SelectFields, args.Filter, args.SortFields, args.Paging);

            return new GetListResult
            {
                Table = listResult.Table,
                Paging = listResult.Paging,
            };
        }
    }
}
