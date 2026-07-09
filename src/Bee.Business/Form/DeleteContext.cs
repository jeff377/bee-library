using System.Data;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Carries the state of a single <see cref="FormBusinessObject.Delete(DeleteArgs)"/> call through
    /// its <see cref="FormBusinessObject.DoBeforeDelete"/> / <see cref="FormBusinessObject.DoDelete"/>
    /// / <see cref="FormBusinessObject.DoAfterDelete"/> steps.
    /// </summary>
    public sealed class DeleteContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeleteContext"/>.
        /// </summary>
        /// <param name="args">The delete arguments.</param>
        /// <param name="repository">The resolved form repository.</param>
        /// <param name="scopeFilter">The record-scope filter applied to the delete.</param>
        /// <param name="schema">The form schema driving delete rules.</param>
        public DeleteContext(DeleteArgs args, IDataFormRepository repository, FilterNode? scopeFilter, FormSchema schema)
        {
            Args = args;
            Repository = repository;
            ScopeFilter = scopeFilter;
            Schema = schema;
        }

        /// <summary>
        /// Gets the delete arguments.
        /// </summary>
        public DeleteArgs Args { get; }

        /// <summary>
        /// Gets the resolved form repository.
        /// </summary>
        public IDataFormRepository Repository { get; }

        /// <summary>
        /// Gets the record-scope filter applied to the delete.
        /// </summary>
        public FilterNode? ScopeFilter { get; }

        /// <summary>
        /// Gets the form schema driving delete rules.
        /// </summary>
        public FormSchema Schema { get; }

        /// <summary>
        /// Gets or sets the pre-delete snapshot of the record (master + details). Loaded once and
        /// shared by the change audit and any <c>BeforeDelete</c> rules; null when neither needs it.
        /// </summary>
        public DataSet? Snapshot { get; set; }

        /// <summary>
        /// Gets or sets the number of rows affected by the delete. Set by
        /// <see cref="FormBusinessObject.DoDelete"/>.
        /// </summary>
        public int RowsAffected { get; set; }
    }
}
