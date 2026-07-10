using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Expressions;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Evaluates a form's field expressions and rules against its <see cref="DataSet"/> / <see cref="DataRow"/>
    /// with a shared <see cref="IExpressionEvaluator"/>, rounding computed numeric fields through
    /// <see cref="NumberFormatResolver"/>. Shared by the server (before save/delete) and UI clients
    /// (live preview) so a field computed on the client yields the same result the server writes on
    /// save. Like <see cref="FormRowDefaults"/>, this is schema-driven <see cref="DataRow"/> logic with
    /// no server-only dependency, so both sides delegate here from a single place.
    /// </summary>
    /// <remarks>
    /// The full-set methods (<see cref="ApplyFieldExpressions"/> / <see cref="ValidateRules"/>) drive
    /// the server's before-save/before-delete pass over an entire data set. The row-level methods
    /// (<see cref="ApplyComputedRow"/> / <see cref="ApplyDefaultRow"/>) recompute a single row for live
    /// client preview and report which fields actually changed, and <see cref="BuildDependencyMap"/>
    /// exposes the "which edited field forces which computed field to recompute" graph the client uses
    /// to gate recomputation.
    /// </remarks>
    public sealed class FormExpressionCalculator
    {
        private readonly IExpressionEvaluator _evaluator;

        /// <summary>
        /// Initializes a new instance of <see cref="FormExpressionCalculator"/>.
        /// </summary>
        /// <param name="evaluator">The expression evaluator (compiles and caches per expression).</param>
        public FormExpressionCalculator(IExpressionEvaluator evaluator)
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            _evaluator = evaluator;
        }

        /// <summary>
        /// Fills default-value expressions on new rows and recomputes value-expression fields on
        /// new/changed rows, per table. This is the server's before-save field pass.
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="dataSet">The data set to apply expressions to.</param>
        /// <param name="roundingContext">The rounding context for computed numeric fields.</param>
        public void ApplyFieldExpressions(FormSchema schema, DataSet dataSet, RoundingContext roundingContext)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(dataSet);
            ArgumentNullException.ThrowIfNull(roundingContext);

            if (schema.Tables == null) { return; }

            foreach (var formTable in schema.Tables)
            {
                if (formTable.Fields == null) { continue; }
                var dataTable = FindDataTable(dataSet, formTable.TableName);
                if (dataTable == null) { continue; }
                ApplyTableFieldExpressions(formTable, dataTable, schema, roundingContext);
            }
        }

        /// <summary>
        /// Applies the default-value and value expressions of one table to its data rows: fills defaults on
        /// added rows and recomputes value-expression fields on added/modified rows.
        /// </summary>
        private void ApplyTableFieldExpressions(FormTable formTable, DataTable dataTable, FormSchema schema,
            RoundingContext roundingContext)
        {
            var defaultFields = formTable.Fields!
                .Where(f => StringUtilities.IsNotEmpty(f.DefaultValueExpression)).ToList();
            var computedFields = formTable.Fields!
                .Where(f => StringUtilities.IsNotEmpty(f.ValueExpression)).ToList();
            if (defaultFields.Count == 0 && computedFields.Count == 0) { return; }

            foreach (DataRow row in dataTable.Rows)
            {
                var state = row.RowState;
                if (state is DataRowState.Deleted or DataRowState.Detached) { continue; }

                if (state == DataRowState.Added && defaultFields.Count > 0)
                    ApplyDefaults(row, formTable, defaultFields);

                if (state is DataRowState.Added or DataRowState.Modified && computedFields.Count > 0)
                    ApplyComputed(row, formTable, schema, computedFields, roundingContext);
            }
        }

        /// <summary>
        /// Evaluates the enabled rules of the given trigger in order; a failing condition (that passes
        /// its applicability guard) aborts the operation with the rule's message. This is the server's
        /// before-save / before-delete validation pass; clients do not call it (the server is the
        /// authority for validation).
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="dataSet">The data set to validate.</param>
        /// <param name="trigger">The rule trigger to evaluate.</param>
        /// <exception cref="UserMessageException">A rule's condition fails; carries the rule message.</exception>
        public void ValidateRules(FormSchema schema, DataSet dataSet, FormRuleTrigger trigger)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(dataSet);

            if (schema.Rules == null) { return; }

            var rules = schema.Rules
                .Where(r => r.Enabled && r.Trigger == trigger)
                .OrderBy(r => r.Order)
                .ToList();

            foreach (var rule in rules)
            {
                var formTable = ResolveRuleTable(rule, schema);
                if (formTable == null) { continue; }
                var dataTable = FindDataTable(dataSet, formTable.TableName);
                if (dataTable == null) { continue; }
                ValidateRuleRows(rule, formTable, dataTable);
            }
        }

        /// <summary>
        /// Evaluates a single rule against every live row of its table; a row that passes the rule's
        /// applicability guard (<see cref="FormRule.When"/>) but fails its condition aborts with the message.
        /// </summary>
        /// <exception cref="UserMessageException">A row's condition fails; carries the rule message.</exception>
        private void ValidateRuleRows(FormRule rule, FormTable formTable, DataTable dataTable)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                if (row.RowState is DataRowState.Deleted or DataRowState.Detached) { continue; }

                var variables = BuildVariables(row, formTable);
                if (StringUtilities.IsNotEmpty(rule.When) &&
                    !_evaluator.Evaluate<bool>(rule.When, variables))
                {
                    continue;
                }
                if (!_evaluator.Evaluate<bool>(rule.Condition, variables))
                    throw new UserMessageException(rule.Message);
            }
        }

        /// <summary>
        /// Recomputes every value-expression field on a single row (in declaration order so a field may
        /// reference an earlier computed field) and reports the fields whose value actually changed.
        /// Used by clients for live preview; a value equal to the current cell is not rewritten, so no
        /// spurious change is reported.
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="formTable">The row's form table.</param>
        /// <param name="row">The row to recompute.</param>
        /// <param name="roundingContext">The rounding context for computed numeric fields.</param>
        /// <returns>The names of the fields whose value changed (empty when nothing changed).</returns>
        public IReadOnlyList<string> ApplyComputedRow(FormSchema schema, FormTable formTable, DataRow row,
            RoundingContext roundingContext)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(formTable);
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(roundingContext);

            if (formTable.Fields == null) { return []; }
            var computedFields = formTable.Fields
                .Where(f => StringUtilities.IsNotEmpty(f.ValueExpression)).ToList();
            if (computedFields.Count == 0) { return []; }

            return ApplyComputed(row, formTable, schema, computedFields, roundingContext);
        }

        /// <summary>
        /// Fills each default-value expression field on a single row, but only where the field is
        /// currently empty, and reports the fields that were filled. Used by clients when a new row is
        /// created (display-layer defaults, complementing the server's save-time defaults).
        /// </summary>
        /// <param name="formTable">The row's form table.</param>
        /// <param name="row">The new row to seed.</param>
        /// <returns>The names of the fields that were filled (empty when none).</returns>
        public IReadOnlyList<string> ApplyDefaultRow(FormTable formTable, DataRow row)
        {
            ArgumentNullException.ThrowIfNull(formTable);
            ArgumentNullException.ThrowIfNull(row);

            if (formTable.Fields == null) { return []; }
            var defaultFields = formTable.Fields
                .Where(f => StringUtilities.IsNotEmpty(f.DefaultValueExpression)).ToList();
            if (defaultFields.Count == 0) { return []; }

            return ApplyDefaults(row, formTable, defaultFields);
        }

        /// <summary>
        /// Builds the "edited source field → dependent computed fields" map for a table: for each
        /// value-expression field, the identifiers it references become keys pointing to that computed
        /// field. Clients use it to decide whether an edit forces a recompute. Keys compare
        /// case-insensitively, matching <see cref="DataTable"/> column lookup semantics.
        /// </summary>
        /// <param name="formTable">The form table to analyze.</param>
        /// <returns>A map from source field name to the computed fields that reference it.</returns>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> BuildDependencyMap(FormTable formTable)
        {
            ArgumentNullException.ThrowIfNull(formTable);

            var builder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (formTable.Fields == null)
                return EmptyDependencyMap;

            foreach (var field in formTable.Fields)
            {
                if (StringUtilities.IsEmpty(field.ValueExpression)) { continue; }
                foreach (var source in _evaluator.GetReferencedVariables(field.ValueExpression))
                {
                    if (!builder.TryGetValue(source, out var dependents))
                    {
                        dependents = [];
                        builder[source] = dependents;
                    }
                    if (!dependents.Contains(field.FieldName, StringComparer.OrdinalIgnoreCase))
                        dependents.Add(field.FieldName);
                }
            }

            var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in builder)
                map[pair.Key] = pair.Value;
            return map;
        }

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyDependencyMap =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fills each default-value field on a new row when it is currently empty, returning the filled
        /// field names.
        /// </summary>
        private List<string> ApplyDefaults(DataRow row, FormTable formTable, List<FormField> defaultFields)
        {
            var changed = new List<string>();
            var variables = BuildVariables(row, formTable);
            foreach (var field in defaultFields)
            {
                if (!row.Table.Columns.Contains(field.FieldName)) { continue; }
                if (!IsEmptyValue(row[field.FieldName])) { continue; }
                var clrType = ExpressionPolicy.ToClrType(field.DbType);
                var value = _evaluator.Evaluate(field.DefaultValueExpression, variables, clrType);
                var newValue = value ?? (object)DBNull.Value;
                if (Equals(newValue, row[field.FieldName])) { continue; }
                row[field.FieldName] = newValue;
                changed.Add(field.FieldName);
            }
            return changed;
        }

        /// <summary>
        /// Recomputes each value-expression field on a row. Numeric results are rounded through the
        /// number subsystem; the local variable snapshot is updated after each field so later
        /// expressions in the same row observe earlier computed values (dependency chains authored in
        /// declaration order). A result equal to the current cell is not rewritten, so live-preview
        /// callers see no spurious change.
        /// </summary>
        private List<string> ApplyComputed(DataRow row, FormTable formTable, FormSchema schema,
            List<FormField> computedFields, RoundingContext roundingContext)
        {
            var changed = new List<string>();
            var variables = BuildVariables(row, formTable);
            foreach (var field in computedFields)
            {
                if (!row.Table.Columns.Contains(field.FieldName)) { continue; }

                var clrType = ExpressionPolicy.ToClrType(field.DbType);
                var result = _evaluator.Evaluate(field.ValueExpression, variables, clrType);

                if (result is decimal numeric)
                {
                    result = NumberFormatResolver.RoundByKind(
                        numeric, field.NumberKind, roundingContext, ResolveRefCode(field, schema, variables));
                }

                var newValue = result ?? (object)DBNull.Value;
                if (!Equals(newValue, row[field.FieldName]))
                {
                    row[field.FieldName] = newValue;
                    changed.Add(field.FieldName);
                }
                variables[field.FieldName] = ExpressionPolicy.CoerceValue(result, field.DbType);
            }
            return changed;
        }

        /// <summary>
        /// Builds the expression variable map for a row, coercing each column to a non-null value of its
        /// field's CLR type. Variables are keyed by the schema field's declared name (its casing), not by
        /// the <see cref="DataColumn"/> name: the framework's <c>DataTableExtensions.AddColumn</c> stores
        /// column names uppercased, but expressions reference fields by their declared (typically
        /// lower-case) name and the engine's identifiers are case-sensitive — so keying by the column name
        /// would leave <c>quantity</c> unresolved against a <c>QUANTITY</c> column. Columns with no schema
        /// field fall back to their column name (nothing references them).
        /// </summary>
        private static Dictionary<string, object?> BuildVariables(DataRow row, FormTable formTable)
        {
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DataColumn column in row.Table.Columns)
            {
                var field = ResolveField(formTable, column.ColumnName);
                var name = field?.FieldName ?? column.ColumnName;
                var dbType = field?.DbType ?? DbTypeConverter.ToFieldDbType(column.DataType);
                variables[name] = ExpressionPolicy.CoerceValue(row[column], dbType);
            }
            return variables;
        }

        /// <summary>
        /// Resolves the schema field for a column by name (case-insensitive, matching
        /// <see cref="DataColumnCollection"/> lookup), or null when the table has no such field.
        /// </summary>
        private static FormField? ResolveField(FormTable formTable, string columnName)
        {
            return formTable.Fields != null && formTable.Fields.Contains(columnName)
                ? formTable.Fields[columnName] : null;
        }

        /// <summary>
        /// Resolves the reference code (currency for amounts, unit for quantities/weights) used to pick
        /// decimal places when rounding a computed numeric field; null for other kinds.
        /// </summary>
        private static string? ResolveRefCode(FormField field, FormSchema schema,
            Dictionary<string, object?> variables)
        {
            string? codeField = field.NumberKind switch
            {
                NumberKind.Amount => StringUtilities.IsNotEmpty(field.CurrencyField)
                    ? field.CurrencyField : schema.CurrencyField,
                NumberKind.Quantity or NumberKind.Weight => field.UnitField,
                _ => null,
            };
            if (StringUtilities.IsEmpty(codeField)) { return null; }
            return variables.TryGetValue(codeField!, out var value) ? value?.ToString() : null;
        }

        /// <summary>
        /// Resolves the table a rule targets: the master table when <see cref="FormRule.TargetTable"/>
        /// is empty, otherwise the named table (null when absent).
        /// </summary>
        private static FormTable? ResolveRuleTable(FormRule rule, FormSchema schema)
        {
            if (StringUtilities.IsEmpty(rule.TargetTable)) { return schema.MasterTable; }
            return schema.Tables != null && schema.Tables.Contains(rule.TargetTable)
                ? schema.Tables[rule.TargetTable] : null;
        }

        /// <summary>
        /// Returns the data table with the given name, or null when the data set has no such table.
        /// </summary>
        private static DataTable? FindDataTable(DataSet dataSet, string tableName)
        {
            return dataSet.Tables.Contains(tableName) ? dataSet.Tables[tableName] : null;
        }

        /// <summary>
        /// Returns true when a value is null, <see cref="DBNull"/>, or an empty string.
        /// </summary>
        private static bool IsEmptyValue(object? value)
        {
            if (value is null || value is DBNull) { return true; }
            return value is string s && s.Length == 0;
        }
    }
}
