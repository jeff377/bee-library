using System.Data;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Expressions;

namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// Client-side live recomputation of a form's computed fields (<see cref="FormField.ValueExpression"/>)
    /// and new-row default expressions (<see cref="FormField.DefaultValueExpression"/>), delegating to the
    /// shared <see cref="FormExpressionCalculator"/> so a field previewed on the client matches what the
    /// server writes on save. The wiring that subscribes <see cref="FormDataObject.FieldValueChanged"/> and
    /// refreshes detail grids lives in <c>FormView</c>; this service is the recompute core it drives.
    /// </summary>
    /// <remarks>
    /// A recompute writes its results straight into the bound <see cref="DataRow"/>, which re-raises
    /// <see cref="FormDataObject.FieldValueChanged"/>. The <see cref="IsRecomputing"/> flag lets the wiring
    /// ignore those self-inflicted echoes, and a computed field is never itself a recompute trigger, so a
    /// single edit produces exactly one recompute pass. Rounding uses framework-default decimal places
    /// (Tier 1): the empty <see cref="RoundingContext"/> falls back to the per-<c>NumberKind</c> defaults,
    /// so a preview differs from the saved value only where a company overrides those places — the server
    /// corrects it on save.
    /// </remarks>
    public sealed class FormLiveComputation
    {
        private readonly FormSchema _schema;
        private readonly FormExpressionCalculator _calculator;
        private readonly RoundingContext _roundingContext;
        // Dependency maps are built lazily per table (parse-once) and reused for every edit.
        private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> _dependencyByTable
            = new(StringComparer.OrdinalIgnoreCase);
        private bool _recomputing;

        /// <summary>
        /// Initializes a new instance of <see cref="FormLiveComputation"/>.
        /// </summary>
        /// <param name="schema">The form schema whose expressions drive recomputation.</param>
        /// <param name="roundingContext">
        /// The rounding context for computed numeric fields; <c>null</c> uses framework-default decimal
        /// places (Tier 1).
        /// </param>
        /// <param name="evaluator">
        /// The expression evaluator; <c>null</c> creates a client-local <see cref="DynamicExpressoEvaluator"/>
        /// (the client keeps its own compile cache, separate from the server's).
        /// </param>
        public FormLiveComputation(FormSchema schema, RoundingContext? roundingContext = null,
            IExpressionEvaluator? evaluator = null)
        {
            ArgumentNullException.ThrowIfNull(schema);
            _schema = schema;
            _calculator = new FormExpressionCalculator(evaluator ?? new DynamicExpressoEvaluator());
            _roundingContext = roundingContext ?? new RoundingContext();
        }

        /// <summary>
        /// Gets a value indicating whether a recompute pass is currently writing back computed fields.
        /// Change-event subscribers check this to ignore the writes a recompute produces (re-entrancy guard).
        /// </summary>
        public bool IsRecomputing => _recomputing;

        /// <summary>
        /// Recomputes the computed fields of <paramref name="row"/> when <paramref name="changedField"/> is a
        /// source that some computed field depends on, returning the fields whose value changed. A no-op
        /// (returns empty) when a recompute is already in progress, when the changed field is itself a
        /// computed field, or when nothing depends on it.
        /// </summary>
        /// <param name="tableName">The name of the table the row belongs to (master or detail).</param>
        /// <param name="changedField">The field the user (or a write-back) just changed.</param>
        /// <param name="row">The row to recompute.</param>
        /// <returns>The names of the computed fields whose value changed.</returns>
        public IReadOnlyList<string> Recompute(string tableName, string changedField, DataRow row)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(changedField);
            ArgumentNullException.ThrowIfNull(row);

            // A recompute's own write-backs re-raise the change event; do not recurse.
            if (_recomputing) { return []; }

            var formTable = _schema.Tables?.GetOrDefault(tableName);
            if (formTable?.Fields is null) { return []; }

            // A computed field changing is the result of a recompute, never a trigger for one.
            if (IsComputedField(formTable, changedField)) { return []; }
            // Skip the whole pass when no computed field references the changed field.
            if (!GetDependencyMap(formTable).ContainsKey(changedField)) { return []; }

            _recomputing = true;
            try
            {
                return _calculator.ApplyComputedRow(_schema, formTable, row, _roundingContext);
            }
            finally
            {
                _recomputing = false;
            }
        }

        /// <summary>
        /// Fills the default-value expressions of a freshly created <paramref name="row"/> (only where the
        /// field is currently empty), returning the fields that were filled. Complements the server's
        /// save-time defaults with an immediate display value.
        /// </summary>
        /// <param name="tableName">The name of the table the row belongs to (master or detail).</param>
        /// <param name="row">The new row to seed.</param>
        /// <returns>The names of the fields that were filled.</returns>
        public IReadOnlyList<string> ApplyDefaults(string tableName, DataRow row)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(row);

            var formTable = _schema.Tables?.GetOrDefault(tableName);
            if (formTable?.Fields is null) { return []; }

            _recomputing = true;
            try
            {
                return _calculator.ApplyDefaultRow(formTable, row);
            }
            finally
            {
                _recomputing = false;
            }
        }

        /// <summary>
        /// Prepares a freshly created <paramref name="row"/> for display: fills its default-value
        /// expressions (empty fields only), then recomputes all computed fields so any default feeding a
        /// computed field is reflected at once. Returns the fields whose value changed. The whole pass runs
        /// under the re-entrancy guard, so the write-backs raise no nested recompute.
        /// </summary>
        /// <param name="tableName">The name of the table the row belongs to (master or detail).</param>
        /// <param name="row">The new row to initialize.</param>
        /// <returns>The names of the fields that were filled or computed.</returns>
        public IReadOnlyList<string> InitializeNewRow(string tableName, DataRow row)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(row);

            var formTable = _schema.Tables?.GetOrDefault(tableName);
            if (formTable?.Fields is null) { return []; }

            _recomputing = true;
            try
            {
                var changed = new List<string>();
                changed.AddRange(_calculator.ApplyDefaultRow(formTable, row));
                foreach (var field in _calculator.ApplyComputedRow(_schema, formTable, row, _roundingContext))
                {
                    if (!changed.Contains(field, StringComparer.OrdinalIgnoreCase))
                        changed.Add(field);
                }
                return changed;
            }
            finally
            {
                _recomputing = false;
            }
        }

        private IReadOnlyDictionary<string, IReadOnlyList<string>> GetDependencyMap(FormTable formTable)
        {
            if (!_dependencyByTable.TryGetValue(formTable.TableName, out var map))
            {
                map = _calculator.BuildDependencyMap(formTable);
                _dependencyByTable[formTable.TableName] = map;
            }
            return map;
        }

        private static bool IsComputedField(FormTable formTable, string fieldName)
        {
            if (formTable.Fields is null || !formTable.Fields.Contains(fieldName)) { return false; }
            return StringUtilities.IsNotEmpty(formTable.Fields[fieldName].ValueExpression);
        }
    }
}
