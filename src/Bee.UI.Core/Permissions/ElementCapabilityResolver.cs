using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.UI.Core.Permissions
{
    /// <summary>
    /// Default <see cref="IElementCapabilityResolver"/>: a stateless, pure implementation that reads
    /// the per-model capability snapshot. Use <see cref="Default"/> for direct consumption or register
    /// it via DI to substitute a custom degradation policy.
    /// </summary>
    public sealed class ElementCapabilityResolver : IElementCapabilityResolver
    {
        /// <summary>A shared stateless instance for convenient consumption.</summary>
        public static readonly ElementCapabilityResolver Default = new();

        /// <inheritdoc />
        public bool Can(FormSchema schema, PermissionAction action, IReadOnlyDictionary<string, PermissionAction>? capabilities)
        {
            // An untagged command, an inactive snapshot, or a non-permission-bound form is unrestricted.
            if (action == PermissionAction.None) { return true; }
            if (capabilities == null) { return true; }
            string modelId = schema?.PermissionModelId ?? string.Empty;
            if (string.IsNullOrEmpty(modelId)) { return true; }

            var mask = capabilities.TryGetValue(modelId, out var allowed) ? allowed : PermissionAction.None;
            // Any-of semantics: a command tagged with combined flags (e.g. Save = Create|Update)
            // shows when the user holds any of them; the backend enforces the specific operation.
            return (mask & action) != PermissionAction.None;
        }

        /// <inheritdoc />
        public FieldCapability ResolveField(FormSchema schema, string fieldName, string tableName, IReadOnlyDictionary<string, PermissionAction>? capabilities)
        {
            var field = schema?.FindField(fieldName, tableName ?? string.Empty);
            if (field == null || field.SensitiveCategory == SensitiveCategory.None) { return FieldCapability.Allowed; }
            if (capabilities == null) { return FieldCapability.Allowed; }

            string modelId = field.SensitiveCategory.ToPermissionModelId();
            var mask = capabilities.TryGetValue(modelId, out var allowed) ? allowed : PermissionAction.None;

            // Hidden wins over read-only — no point marking editability on a field you cannot see.
            if ((mask & PermissionAction.Read) == PermissionAction.None) { return new FieldCapability(Visible: false, ReadOnly: true); }
            if ((mask & PermissionAction.Update) == PermissionAction.None) { return new FieldCapability(Visible: true, ReadOnly: true); }
            return FieldCapability.Allowed;
        }
    }
}
