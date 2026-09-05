using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// One plugin binding with its type already loaded: what
    /// <see cref="FormPluginChain.Create(string, IReadOnlyList{FormPluginBinding})"/> consumes.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="PluginBinding"/> after resolution. The definition layer names
    /// the type as a string because it must round-trip through XML; loading that name is the
    /// resolver's job, and what reaches the chain is the loaded type plus the stage the file
    /// declared for it.
    /// </remarks>
    /// <param name="Type">The loaded plugin type, deriving from <see cref="FormBusinessPlugin"/>.</param>
    /// <param name="Stage">The stage the definition declares for it.</param>
    public readonly record struct FormPluginBinding(Type Type, PluginStage Stage);
}
