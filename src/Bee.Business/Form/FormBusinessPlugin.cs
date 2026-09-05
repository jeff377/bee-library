using Bee.Definition;

namespace Bee.Business.Form
{
    /// <summary>
    /// Base class for a business plugin: a unit of customization that runs at fixed points of a
    /// program's save and delete pipelines without replacing its business object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One class, one stage.</b> A plugin is bound to a progId in
    /// <see cref="Bee.Definition.Settings.PluginSettings"/>, and each binding names both the type
    /// and the one stage it runs at. The class must override exactly that stage and no other; any
    /// disagreement between the two refuses to load. A requirement spanning several stages is
    /// therefore several classes — the stages differ in kind (a check before saving, a side effect
    /// after it), and the file is readable for what runs where.
    /// </para>
    /// <para>
    /// <b>One instance per operation, and no state carried between stages.</b> A single
    /// <c>Save</c> (or <c>Delete</c>) constructs the plugin at most once, and instances are never
    /// shared between calls, so no locking is needed. Because a plugin has only one stage, there is
    /// no later stage of the same call to hand state to: what an <c>After</c> class needs it must
    /// read or recompute for itself.
    /// </para>
    /// <para>
    /// <b>Every stage runs outside the database transaction.</b> The transaction opens and commits
    /// inside the repository, within the business object's <c>DoSave</c> / <c>DoDelete</c> step. A
    /// plugin therefore cannot make its work atomic with the record — logic that must be belongs in
    /// a repository subclass. For the full contract see the business object's own extension points.
    /// </para>
    /// <para>
    /// <b>Throwing aborts the operation.</b> Use
    /// <see cref="Bee.Base.Exceptions.UserMessageException"/> for a message meant to reach the end
    /// user. At an <c>After</c> stage the data is already committed, so an exception fails the call
    /// against saved data: side effects there must tolerate retries, or be queued rather than
    /// performed inline. A plugin that talks to another system should decide for itself whether a
    /// failure warrants aborting the user's operation — do not let a remote system's availability
    /// determine whether a record can be saved.
    /// </para>
    /// </remarks>
    public abstract class FormBusinessPlugin
    {
        /// <summary>
        /// Initializes a new <see cref="FormBusinessPlugin"/>.
        /// </summary>
        /// <param name="ctx">The per-call business context.</param>
        /// <param name="accessToken">The access token of the call being extended.</param>
        /// <param name="progId">The program identifier this plugin is bound to.</param>
        /// <remarks>
        /// The same three-parameter shape a form repository takes. The business object itself is
        /// deliberately not passed in: it would expose its protected surface to every plugin and
        /// invite coupling to internals rather than to the context.
        /// </remarks>
        protected FormBusinessPlugin(IBeeContext ctx, Guid accessToken, string progId)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            AccessToken = accessToken;
            ProgId = progId ?? throw new ArgumentNullException(nameof(progId));
        }

        /// <summary>Gets the per-call business context.</summary>
        protected IBeeContext Context { get; }

        /// <summary>Gets the access token of the call being extended.</summary>
        protected Guid AccessToken { get; }

        /// <summary>Gets the program identifier this plugin is bound to.</summary>
        protected string ProgId { get; }

        /// <summary>
        /// Runs after the business object's before-save step, which means after the schema-driven
        /// rule engine has applied default values and computed fields.
        /// </summary>
        /// <param name="context">The save context; its data set may still be modified.</param>
        /// <remarks>
        /// The only stage at which a plugin can safely change the data: it precedes both the audit
        /// snapshot and persistence, so a change made here is written <b>and</b> audited. Reads
        /// here are not protected from concurrent change — a check that must be atomic belongs in
        /// the repository, expressed as a conditional UPDATE or a constraint.
        /// </remarks>
        public virtual void BeforeSave(SaveContext context)
        {
        }

        /// <summary>
        /// Runs after persistence and after the change audit has been written.
        /// </summary>
        /// <param name="context">The save context; <c>RefreshedDataSet</c> and <c>AffectedRows</c> are populated.</param>
        /// <remarks>
        /// Modifying <c>DataSet</c> here has no effect — the record is already saved. Modifying
        /// <c>RefreshedDataSet</c> does, because that is what the caller receives.
        /// </remarks>
        public virtual void AfterSave(SaveContext context)
        {
        }

        /// <summary>
        /// Runs after the business object's before-delete step, which means after the schema-driven
        /// guard rules.
        /// </summary>
        /// <param name="context">The delete context; <c>Snapshot</c> holds the record about to be deleted.</param>
        public virtual void BeforeDelete(DeleteContext context)
        {
        }

        /// <summary>
        /// Runs after deletion and after the delete audit has been written.
        /// </summary>
        /// <param name="context">The delete context; <c>Snapshot</c> still holds the deleted record and <c>RowsAffected</c> is populated.</param>
        /// <remarks>
        /// <c>Snapshot</c> is the only remaining view of what was deleted, which is what a plugin
        /// synchronising the deletion to another system needs — a row id alone rarely is.
        /// </remarks>
        public virtual void AfterDelete(DeleteContext context)
        {
        }
    }
}
