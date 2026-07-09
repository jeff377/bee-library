namespace Bee.Definition.Forms
{
    /// <summary>
    /// The lifecycle point at which a <see cref="FormRule"/> is evaluated by the rule engine.
    /// </summary>
    public enum FormRuleTrigger
    {
        /// <summary>
        /// Evaluated before the data set is persisted. A failing rule aborts the save and
        /// surfaces its message to the user. This is the default trigger.
        /// </summary>
        BeforeSave = 0,

        /// <summary>
        /// Evaluated before a record is deleted. A failing rule aborts the delete and
        /// surfaces its message to the user.
        /// </summary>
        BeforeDelete,
    }
}
