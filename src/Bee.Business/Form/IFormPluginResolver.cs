namespace Bee.Business.Form
{
    /// <summary>
    /// Resolves the plugin chain bound to a program, with the tenant customization layer applied.
    /// </summary>
    public interface IFormPluginResolver
    {
        /// <summary>
        /// Resolves the chain for a program: the base plugins first, then the customization's.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves the base layer only.</param>
        /// <param name="progId">The program identifier.</param>
        /// <returns>The resolved chain; <see cref="FormPluginChain.Empty"/> when nothing is bound.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a bound type will not load, or does not derive from
        /// <see cref="FormBusinessPlugin"/>.
        /// </exception>
        FormPluginChain Resolve(string customizeId, string progId);
    }
}
