using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Sorting
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
