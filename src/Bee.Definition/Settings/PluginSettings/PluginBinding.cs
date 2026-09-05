namespace Bee.Definition.Settings
{
    /// <summary>
    /// One resolved plugin binding: the assembly-qualified type name and the stage it is declared
    /// to run at.
    /// </summary>
    /// <remarks>
    /// A value copy rather than the <see cref="PluginItem"/> itself. The item belongs to a cached
    /// <see cref="PluginSettings"/> instance shared by every session, and handing it out would
    /// invite callers to mutate it.
    /// </remarks>
    /// <param name="Type">The assembly-qualified type name of the plugin.</param>
    /// <param name="Stage">The pipeline stage the binding declares.</param>
    public readonly record struct PluginBinding(string Type, PluginStage Stage);
}
