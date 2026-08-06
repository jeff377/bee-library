namespace Bee.Business.Form
{
    /// <summary>
    /// A point in a program's save or delete pipeline at which business plugins run.
    /// </summary>
    /// <remarks>
    /// The set is closed: the read methods are not split into overridable steps, so there is
    /// nowhere else to hang a plugin. Adding a member is a public contract addition and should
    /// clear three bars — it must be distinguishable from its neighbours, have a concrete use, and
    /// not invite work into a place where it would misbehave.
    /// </remarks>
    public enum FormPluginStage
    {
        /// <summary>After the before-save step, before the audit snapshot and persistence.</summary>
        BeforeSave,
        /// <summary>After persistence and the change audit.</summary>
        AfterSave,
        /// <summary>After the before-delete guard rules, before deletion.</summary>
        BeforeDelete,
        /// <summary>After deletion and the delete audit.</summary>
        AfterDelete
    }
}
