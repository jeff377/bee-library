using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Expressions;

namespace Bee.Business.Form
{
    /// <summary>
    /// Default <see cref="IFormRuleProcessor"/>: evaluates a form's field expressions and rules with a
    /// shared <see cref="IExpressionEvaluator"/> and rounds computed numeric fields through
    /// <see cref="NumberFormatResolver"/>.
    /// </summary>
    public sealed class FormRuleProcessor : IFormRuleProcessor
    {
        private readonly IExpressionEvaluator _evaluator;

        /// <summary>
        /// Initializes a new instance of <see cref="FormRuleProcessor"/>.
        /// </summary>
        /// <param name="evaluator">The expression evaluator.</param>
        public FormRuleProcessor(IExpressionEvaluator evaluator)
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            _evaluator = evaluator;
        }

        /// <inheritdoc />
        public void ApplyBeforeSave(FormSchema schema, DataSet dataSet, RoundingContext roundingContext)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(dataSet);
            ArgumentNullException.ThrowIfNull(roundingContext);

            ApplyFieldExpressions(schema, dataSet, roundingContext);
            ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave);
        }

        /// <inheritdoc />
        public void ApplyBeforeDelete(FormSchema schema, DataSet snapshot)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(snapshot);

            ValidateRules(schema, snapshot, FormRuleTrigger.BeforeDelete);
        }

        /// <summary>
        /// Fills default-value expressions on new rows and recomputes value-expression fields on
        /// new/changed rows, per table.
        /// </summary>
        private void ApplyFieldExpressions(FormSchema schema, DataSet dataSet, RoundingContext roundingContext)
        {
            if (schema.Tables == null) { return; }

            foreach (var formTable in schema.Tables)
            {
                if (formTable.Fields == null) { continue; }
                var dataTable = FindDataTable(dataSet, formTable.TableName);
                if (dataTable == null) { continue; }

                var defaultFields = formTable.Fields
                    .Where(f => StringUtilities.IsNotEmpty(f.DefaultValueExpression)).ToList();
                var computedFields = formTable.Fields
                    .Where(f => StringUtilities.IsNotEmpty(f.ValueExpression)).ToList();
                if (defaultFields.Count == 0 && computedFields.Count == 0) { continue; }

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
        }

        /// <summary>
        /// Fills each default-value field on a new row, but only when the field is currently empty.
        /// </summary>
        private void ApplyDefaults(DataRow row, FormTable formTable, List<FormField> defaultFields)
        {
            var variables = BuildVariables(row, formTable);
            foreach (var field in defaultFields)
            {
                if (!IsEmptyValue(row[field.FieldName])) { continue; }
                var clrType = ExpressionPolicy.ToClrType(field.DbType);
                var value = _evaluator.Evaluate(field.DefaultValueExpression, variables, clrType);
                row[field.FieldName] = value ?? (object)DBNull.Value;
            }
        }

        /// <summary>
        /// Recomputes each value-expression field on a row. Numeric results are rounded through the
        /// number subsystem; the local variable snapshot is updated after each field so later
        /// expressions in the same row observe earlier computed values (dependency chains authored in
        /// declaration order).
        /// </summary>
        private void ApplyComputed(DataRow row, FormTable formTable, FormSchema schema,
            List<FormField> computedFields, RoundingContext roundingContext)
        {
            var variables = BuildVariables(row, formTable);
            foreach (var field in computedFields)
            {
                var clrType = ExpressionPolicy.ToClrType(field.DbType);
                var result = _evaluator.Evaluate(field.ValueExpression, variables, clrType);

                if (result is decimal numeric)
                {
                    result = NumberFormatResolver.RoundByKind(
                        numeric, field.NumberKind, roundingContext, ResolveRefCode(field, schema, variables));
                }

                row[field.FieldName] = result ?? (object)DBNull.Value;
                variables[field.FieldName] = ExpressionPolicy.CoerceValue(result, field.DbType);
            }
        }

        /// <summary>
        /// Evaluates the enabled rules of the given trigger in order; a failing condition (that
        /// passes its applicability guard) aborts the operation with the rule's message.
        /// </summary>
        private void ValidateRules(FormSchema schema, DataSet dataSet, FormRuleTrigger trigger)
        {
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
        }

        /// <summary>
        /// Builds the expression variable map for a row: every column exposed by its column name,
        /// coerced to a non-null value of the field's CLR type.
        /// </summary>
        private static Dictionary<string, object?> BuildVariables(DataRow row, FormTable formTable)
        {
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DataColumn column in row.Table.Columns)
            {
                variables[column.ColumnName] = ExpressionPolicy.CoerceValue(
                    row[column], ResolveFieldDbType(formTable, column));
            }
            return variables;
        }

        /// <summary>
        /// Resolves a column's <see cref="FieldDbType"/> from the schema field when present, otherwise
        /// from the column's CLR type.
        /// </summary>
        private static FieldDbType ResolveFieldDbType(FormTable formTable, DataColumn column)
        {
            if (formTable.Fields != null && formTable.Fields.Contains(column.ColumnName))
                return formTable.Fields[column.ColumnName].DbType;
            return DbTypeConverter.ToFieldDbType(column.DataType);
        }

        /// <summary>
        /// Resolves the reference code (currency for amounts, unit for quantities/weights) used to
        /// pick decimal places when rounding a computed numeric field; null for other kinds.
        /// </summary>
        private static string? ResolveRefCode(FormField field, FormSchema schema,
            IReadOnlyDictionary<string, object?> variables)
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
