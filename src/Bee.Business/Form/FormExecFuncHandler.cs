using Bee.Business.Attributes;
using Bee.Definition.Collections;
using Bee.Definition.Security;

namespace Bee.Business.Form
{
    /// <summary>
    /// Custom method handler for form-level business logic objects.
    /// </summary>
    internal class FormExecFuncHandler : IExecFuncHandler
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="FormExecFuncHandler"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public FormExecFuncHandler(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// A hello test method.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        /// <remarks>
        /// Declared explicitly as `Authenticated` to preserve the behaviour this method had while it
        /// carried no attribute at all. Its system-level counterpart is `Anonymous`; the two have
        /// always differed, so this is deliberately not aligned with it.
        /// </remarks>
        [ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
        public static void Hello(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Hello", "Hello form-level BusinessObject");
        }
    }
}
