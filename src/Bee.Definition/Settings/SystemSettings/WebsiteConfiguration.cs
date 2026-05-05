using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Website parameters and environment settings.
    /// </summary>
    [Description("Website parameters and environment settings.")]
    [TreeNode("Website")]
    public class WebsiteConfiguration
    {
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
