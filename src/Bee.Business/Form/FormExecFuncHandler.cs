using System;

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
        public static void Hello(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Hello", "Hello form-level BusinessObject");
        }
    }
}
