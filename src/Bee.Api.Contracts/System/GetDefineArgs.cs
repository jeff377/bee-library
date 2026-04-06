using System;
using Bee.Define;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Input arguments for retrieving definition data.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineArgs : BusinessArgs
    {
        /// <summary>
        /// Gets or sets the definition data type.
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the keys used to locate the definition data.
        /// </summary>
        [Key(101)]
        public string[] Keys { get; set; } = null;
    }
}
