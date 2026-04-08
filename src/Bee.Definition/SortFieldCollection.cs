using Bee.Definition.Collections;
using System;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A collection of sort fields.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SortFieldCollection : MessagePackCollectionBase<SortField>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SortFieldCollection"/>.
        /// </summary>
        public SortFieldCollection()
        { }
    }
}
