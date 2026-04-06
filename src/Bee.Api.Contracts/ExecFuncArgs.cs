using System;
using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Input arguments for executing a custom method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncArgs : BusinessArgs
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecFuncArgs"/> class.
        /// </summary>
        public ExecFuncArgs()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecFuncArgs"/> class with the specified function identifier.
        /// </summary>
        /// <param name="funcID">The custom method identifier.</param>
        public ExecFuncArgs(string funcID)
        {
            FuncId = funcID;
        }

        #endregion

        /// <summary>
        /// Gets or sets the custom method identifier.
        /// </summary>
        [Key(100)]
        public string FuncId { get; set; } = string.Empty;

    }
}
