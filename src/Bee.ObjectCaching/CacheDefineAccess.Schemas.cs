using Bee.Definition.Customization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Typed accessors for schema-shaped definitions: table schema, form schema, form layout and language resource.
    /// </summary>
    public partial class CacheDefineAccess
    {
        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string categoryId, string tableName)
        {
            return _cache.TableSchema.Get(categoryId, tableName)!;
        }

        /// <summary>
        /// Saves the table schema.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema)
        {
            // Save the table schema, then invalidate the cache
            _storage.SaveTableSchema(categoryId, tableSchema);
            _cache.TableSchema.Remove(categoryId, tableSchema.TableName);
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public FormSchema GetFormSchema(string progId)
        {
            return _cache.FormSchema.Get(progId)!;
        }

        /// <summary>
        /// Saves the form schema definition.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            ArgumentNullException.ThrowIfNull(formSchema);

            // CategoryId is required: it determines which DbCategory the schema's
            // tables belong to. Reject persistence of schemas without it.
            _ = TableSchemaGenerator.GetCategoryId(formSchema);

            // Save the form schema, then invalidate the cache
            _storage.SaveFormSchema(formSchema);
            _cache.FormSchema.Remove(formSchema.ProgId);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        /// <exception cref="InvalidOperationException">Thrown when no layout is stored under that identifier.</exception>
        /// <remarks>
        /// Kept mandatory even though the storage layer reports a missing layout as <c>null</c>:
        /// callers of this overload ask for a layout that must exist (the raw-definition path
        /// behind <c>GetDefine</c>). Callers that need to inspect "absent" before deciding what it
        /// means — the customization overlay, which reads it as "this tenant overrides nothing" —
        /// use <see cref="FindFormLayout"/>.
        /// </remarks>
        public FormLayout GetFormLayout(string layoutId)
        {
            return _cache.FormLayout.Get(layoutId)
                ?? throw new InvalidOperationException($"FormLayout '{layoutId}' not found.");
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier, applying the tenant
        /// customization overlay (whole-file selection) for the supplied customization code.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string customizeId, string layoutId)
        {
            return FindFormLayout(customizeId, layoutId)
                ?? throw new InvalidOperationException($"FormLayout '{layoutId}' not found.");
        }

        /// <summary>
        /// Looks up the form layout definition for the supplied customization code and layout
        /// identifier, returning <c>null</c> when neither layer stores one.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="layoutId">The layout identifier.</param>
        /// <returns>The customization layout, else the base layout, else <c>null</c>.</returns>
        /// <remarks>
        /// The optional counterpart of <see cref="GetFormLayout(string, string)"/>, for callers that
        /// need to see "neither layer stores one" rather than an exception. The runtime path reports
        /// that as a configuration error — layouts are authored at design time, never generated on
        /// the fly. Whole-file selection, same as the mandatory overload: a customization layout
        /// wins outright and the two layers are never merged.
        /// </remarks>
        public FormLayout? FindFormLayout(string customizeId, string layoutId)
        {
            var custom = !string.IsNullOrEmpty(customizeId) && _customizeReader is not null
                ? _customizeReader.GetCustomizeFormLayout(customizeId, layoutId)
                : null;
            // Which layer wins is decided by CustomizeOverlay — the same class a client runs over
            // the two copies it fetched, so both ends select identically.
            return CustomizeOverlay.PickFormLayout(custom, _cache.FormLayout.Get(layoutId));
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            // Save the form layout, then invalidate the cache
            _storage.SaveFormLayout(formLayout);
            _cache.FormLayout.Remove(formLayout.LayoutId);
        }

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public LanguageResource GetLanguage(string lang, string ns)
        {
            return _cache.LanguageResource.Get(lang, ns)!;
        }

        /// <summary>
        /// Saves the language resource.
        /// </summary>
        /// <param name="resource">The language resource.</param>
        public void SaveLanguage(LanguageResource resource)
        {
            ArgumentNullException.ThrowIfNull(resource);

            // Save the language resource, then invalidate the cache
            _storage.SaveLanguage(resource);
            _cache.LanguageResource.Remove(resource.Lang, resource.Namespace);
        }
    }
}
