using Bee.Definition.Forms;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Validates the line-A binding between forms and the permission registry. Intended to
    /// run as a load-time / startup full-scan so a broken binding never reaches production.
    /// </summary>
    public static class PermissionBindingValidator
    {
        /// <summary>
        /// Validates a set of form schemas against the permission registry. Returns one
        /// message per violation (empty when valid). Checks that each form's
        /// <see cref="FormSchema.PermissionModelId"/> references an existing model, and that
        /// a form's master table marks at most one <see cref="ScopeRole.Owner"/> column and
        /// at most one <see cref="ScopeRole.Dept"/> column.
        /// </summary>
        /// <param name="schemas">The form schemas to validate.</param>
        /// <param name="models">The permission model registry.</param>
        /// <returns>The list of validation errors; empty when valid.</returns>
        public static IReadOnlyList<string> Validate(IEnumerable<FormSchema> schemas, PermissionModels models)
        {
            ArgumentNullException.ThrowIfNull(schemas);
            ArgumentNullException.ThrowIfNull(models);

            // NOTE: Kept as one method despite SonarCloud `S3776` (cognitive complexity 25 vs 15).
            // The body is a flat list of independent validation rules applied per schema, each simple
            // and read top to bottom. Splitting into per-rule helpers would add indirection without
            // making any single check clearer. Tracked for human review in
            // `docs/.sonar-fix-state/skip.json`.
            var errors = new List<string>();
            foreach (var schema in schemas)
            {
                // PermissionModelId must reference an existing model (when declared).
                if (!string.IsNullOrEmpty(schema.PermissionModelId)
                    && (models.Models == null || !models.Models.Contains(schema.PermissionModelId)))
                {
                    errors.Add($"Form '{schema.ProgId}': PermissionModelId '{schema.PermissionModelId}' does not exist in the permission registry.");
                }

                // Record scope is master-only: a detail (non-master) table must not mark any scope
                // role. Such a column would be silently ignored by the resolver, so flag it as a
                // configuration error at load time rather than letting it mislead.
                if (schema.Tables != null)
                {
                    foreach (FormTable table in schema.Tables)
                    {
                        if (string.Equals(table.TableName, schema.ProgId, StringComparison.OrdinalIgnoreCase)) { continue; }
                        if (table.Fields != null && table.Fields.Any(field => field.ScopeRole != ScopeRole.None))
                            errors.Add($"Form '{schema.ProgId}': detail table '{table.TableName}' marks a ScopeRole column; record scope is master-only.");
                    }
                }

                // The master table may mark at most one Owner column and one Dept column.
                var master = schema.MasterTable;
                if (master?.Fields == null) { continue; }

                int owners = master.Fields.Count(field => field.ScopeRole == ScopeRole.Owner);
                int depts = master.Fields.Count(field => field.ScopeRole == ScopeRole.Dept);
                if (owners > 1)
                    errors.Add($"Form '{schema.ProgId}': master table marks {owners} Owner columns; at most one is allowed.");
                if (depts > 1)
                    errors.Add($"Form '{schema.ProgId}': master table marks {depts} Dept columns; at most one is allowed.");
            }
            return errors;
        }
    }
}
