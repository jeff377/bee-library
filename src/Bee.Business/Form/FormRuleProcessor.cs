using System.Data;
using Bee.Base.Expressions;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Business.Form
{
    /// <summary>
    /// Default <see cref="IFormRuleProcessor"/>: a thin server-side adapter over the shared
    /// <see cref="FormExpressionCalculator"/>. The calculator (in <c>Bee.Definition.Forms</c>) holds the
    /// portable field-expression and rule logic so the server's before-save/before-delete pass and a
    /// UI client's live preview evaluate through one code path and cannot drift.
    /// </summary>
    public sealed class FormRuleProcessor : IFormRuleProcessor
    {
        private readonly FormExpressionCalculator _calculator;

        /// <summary>
        /// Initializes a new instance of <see cref="FormRuleProcessor"/>.
        /// </summary>
        /// <param name="evaluator">The expression evaluator.</param>
        public FormRuleProcessor(IExpressionEvaluator evaluator)
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            _calculator = new FormExpressionCalculator(evaluator);
        }

        /// <inheritdoc />
        public void ApplyBeforeSave(FormSchema schema, DataSet dataSet, RoundingContext roundingContext, string timeZoneId = "")
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(dataSet);
            ArgumentNullException.ThrowIfNull(roundingContext);

            _calculator.ApplyFieldExpressions(schema, dataSet, roundingContext, timeZoneId);
            _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave, timeZoneId);
        }

        /// <inheritdoc />
        public void ApplyBeforeDelete(FormSchema schema, DataSet snapshot, string timeZoneId = "")
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(snapshot);

            _calculator.ValidateRules(schema, snapshot, FormRuleTrigger.BeforeDelete, timeZoneId);
        }
    }
}
