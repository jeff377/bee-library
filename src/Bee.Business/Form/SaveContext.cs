using System.Data;
using Bee.Definition.Forms;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Carries the state of a single <see cref="FormBusinessObject.Save(SaveArgs)"/> call through its
    /// <see cref="FormBusinessObject.DoBeforeSave"/> / <see cref="FormBusinessObject.DoSave"/> /
    /// <see cref="FormBusinessObject.DoAfterSave"/> steps, so overrides can read the inputs and the
    /// persistence result without re-resolving the repository or schema.
    /// </summary>
    public sealed class SaveContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SaveContext"/>.
        /// </summary>
        /// <param name="args">The save arguments.</param>
        /// <param name="dataSet">The data set being persisted (never null).</param>
        /// <param name="repository">The resolved form repository.</param>
        /// <param name="schema">The form schema driving persistence and rules.</param>
        public SaveContext(SaveArgs args, DataSet dataSet, IDataFormRepository repository, FormSchema schema)
        {
            Args = args;
            DataSet = dataSet;
            Repository = repository;
            Schema = schema;
        }

        /// <summary>
        /// Gets the save arguments.
        /// </summary>
        public SaveArgs Args { get; }

        /// <summary>
        /// Gets the data set being persisted. Before-save steps may mutate it (for example to fill
        /// computed or default-valued fields) prior to persistence.
        /// </summary>
        public DataSet DataSet { get; }

        /// <summary>
        /// Gets the resolved form repository.
        /// </summary>
        public IDataFormRepository Repository { get; }

        /// <summary>
        /// Gets the form schema driving persistence and rule evaluation.
        /// </summary>
        public FormSchema Schema { get; }

        /// <summary>
        /// Gets or sets the refreshed data set produced by persistence (server-generated field
        /// values written back). Set by <see cref="FormBusinessObject.DoSave"/>.
        /// </summary>
        public DataSet? RefreshedDataSet { get; set; }

        /// <summary>
        /// Gets or sets the affected row counts per table produced by persistence. Set by
        /// <see cref="FormBusinessObject.DoSave"/>.
        /// </summary>
        public Dictionary<string, int> AffectedRows { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
