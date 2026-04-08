using System;
using Bee.Definition;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Input arguments for saving definition data.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineArgs : BusinessArgs
    {
        /// <summary>
        /// Gets or sets the definition data type.
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the definition data as an XML string.
        /// </summary>
        [Key(101)]
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the keys used to locate where the definition data is saved.
        /// </summary>
        [Key(102)]
        public string[] Keys { get; set; } = null;
    }
}
