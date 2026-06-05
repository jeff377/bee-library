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

            var errors = new List<string>();
            foreach (var schema in schemas)
            {
                // PermissionModelId must reference an existing model (when declared).
                if (!string.IsNullOrEmpty(schema.PermissionModelId)
                    && (models.Models == null || !models.Models.Contains(schema.PermissionModelId)))
                {
                    errors.Add($"Form '{schema.ProgId}': PermissionModelId '{schema.PermissionModelId}' does not exist in the permission registry.");
                }

                // The master table may mark at most one Owner column and one Dept column.
                var master = schema.MasterTable;
                if (master?.Fields == null) { continue; }

                int owners = master.Fields.Count(f => f.ScopeRole == ScopeRole.Owner);
                int depts = master.Fields.Count(f => f.ScopeRole == ScopeRole.Dept);
                if (owners > 1)
                    errors.Add($"Form '{schema.ProgId}': master table marks {owners} Owner columns; at most one is allowed.");
                if (depts > 1)
                    errors.Add($"Form '{schema.ProgId}': master table marks {depts} Dept columns; at most one is allowed.");
            }
            return errors;
        }
    }
}
