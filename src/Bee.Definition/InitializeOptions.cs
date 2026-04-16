using System;

namespace Bee.Definition
{
    /// <summary>
    /// Initialization options.
    /// </summary>
    [Flags]
    public enum InitializeOptions
    {
        /// <summary>
        /// Backend initialization.
        /// </summary>
        Backend = 1,
        /// <summary>
        /// Frontend initialization.
        /// </summary>
        Frontend = 2,
        /// <summary>
        /// Website initialization.
        /// </summary>
        Website = 4,
        /// <summary>
        /// Background service initialization.
        /// </summary>
        Background = 8
    }
}
