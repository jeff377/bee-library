using Bee.Definition.Settings;

namespace Bee.Definition.Storage
{
    /// <summary>
    /// Writes the tenant customization-override layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separate from <see cref="ICustomizeDefineReader"/> because the layer is overwhelmingly
    /// read-only: customization artifacts are produced by deployment tooling and read at runtime.
    /// <see cref="PluginSettings"/> is the exception — a deployment maintains its plugin bindings
    /// through the framework's own API — so exactly one write accessor exists rather than a mirror
    /// of the reader.
    /// </para>
    /// <para>
    /// The writer is responsible for invalidating whatever the reader caches for that tenant. It
    /// carries no authorization of its own: reaching it already required a local call.
    /// </para>
    /// </remarks>
    public interface ICustomizeDefineWriter
    {
        /// <summary>
        /// Stores a tenant's business plugin bindings, replacing whatever the tenant had.
        /// </summary>
        /// <param name="customizeId">The tenant customization code.</param>
        /// <param name="settings">The bindings to store.</param>
        void SaveCustomizePluginSettings(string customizeId, PluginSettings settings);
    }
}
