namespace Bee.Definition.Settings
{
    /// <summary>
    /// A point in a program's save or delete pipeline at which a business plugin runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set is closed: the read methods are not split into overridable steps, so there is
    /// nowhere else to hang a plugin. Adding a member is a public contract addition and should
    /// clear three bars — it must be distinguishable from its neighbours, have a concrete use, and
    /// not invite work into a place where it would misbehave.
    /// </para>
    /// <para>
    /// A binding declares exactly one stage, and the declared stage must be the one the class
    /// overrides. <see cref="None"/> is the sentinel for a binding that declares no stage at all:
    /// an absent XML attribute deserializes to the enum's zero value silently, so the zero value is
    /// reserved to make that case reportable rather than indistinguishable from the first real
    /// stage. Nothing accepts it — both the maintenance API and the resolver reject it.
    /// </para>
    /// </remarks>
    public enum PluginStage
    {
        /// <summary>No stage declared. Rejected wherever a binding is validated.</summary>
        None = 0,
        /// <summary>After the before-save step, before the audit snapshot and persistence.</summary>
        BeforeSave = 1,
        /// <summary>After persistence and the change audit.</summary>
        AfterSave = 2,
        /// <summary>After the before-delete guard rules, before deletion.</summary>
        BeforeDelete = 3,
        /// <summary>After deletion and the delete audit.</summary>
        AfterDelete = 4
    }
}
