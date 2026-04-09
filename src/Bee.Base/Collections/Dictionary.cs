using System;
using System.Collections.Generic;

namespace Bee.Base.Collections
{
    /// <summary>
    /// A key-value collection with case-insensitive string keys.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    public class Dictionary<T> : Dictionary<string, T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Dictionary{T}"/>.
        /// </summary>
        public Dictionary() : base(StringComparer.CurrentCultureIgnoreCase) { }
    }
}
