using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Core
{
    /// <summary>
    /// Interface for classes that have a Tag property.
    /// </summary>
    public interface ITagProperty
    {
        /// <summary>
        /// Gets or sets additional information stored in the tag.
        /// </summary>
        object Tag { get; set; }
    }
}
