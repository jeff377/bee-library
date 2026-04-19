using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core
{
    /// <summary>
    /// API request type for executing a custom method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncRequest : ApiRequest, IExecFuncRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecFuncRequest"/> class.
        /// </summary>
        public ExecFuncRequest()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecFuncRequest"/> class with the specified function identifier.
        /// </summary>
        /// <param name="funcID">The custom method identifier.</param>
        public ExecFuncRequest(string funcID)
        {
            FuncId = funcID;
        }

        /// <summary>
        /// Gets or sets the custom method identifier.
        /// </summary>
        [Key(100)]
        public string FuncId { get; set; } = string.Empty;
    }
}
