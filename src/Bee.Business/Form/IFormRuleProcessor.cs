using System.Data;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Business.Form
{
    /// <summary>
    /// Applies a form's declarative expressions and rules to a data set: default-value and computed
    /// field expressions plus <c>BeforeSave</c> / <c>BeforeDelete</c> validation rules. Lets a form
    /// express field computation and validation through definitions instead of hand-written
    /// business-object code.
    /// </summary>
    public interface IFormRuleProcessor
    {
        /// <summary>
        /// Applies the before-save pipeline to <paramref name="dataSet"/>: fills default-value
        /// expressions on new rows, recomputes value-expression fields on new/changed rows (rounding
        /// numeric results via the number subsystem), then evaluates <c>BeforeSave</c> rules. A failing
        /// rule throws <see cref="Bee.Base.Exceptions.UserMessageException"/> to abort the save.
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="dataSet">The data set being saved (mutated in place).</param>
        /// <param name="roundingContext">The rounding context used to round computed numeric fields.</param>
        void ApplyBeforeSave(FormSchema schema, DataSet dataSet, RoundingContext roundingContext);

        /// <summary>
        /// Evaluates the form's <c>BeforeDelete</c> rules against the pre-delete
        /// <paramref name="snapshot"/>. A failing rule throws
        /// <see cref="Bee.Base.Exceptions.UserMessageException"/> to abort the delete.
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="snapshot">The pre-delete record snapshot (master + details).</param>
        void ApplyBeforeDelete(FormSchema schema, DataSet snapshot);
    }
}
