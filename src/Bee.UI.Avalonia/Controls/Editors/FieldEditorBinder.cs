using System.Data;
using Avalonia.Controls;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Shared binding engine held by every field editor control. The editors inherit
    /// from different native controls and cannot share a base class, so the binding
    /// state machine (explicit/ambient bind, event subscriptions, echo suppression)
    /// lives here and the editors forward to it.
    /// </summary>
    internal sealed class FieldEditorBinder
    {
        private readonly Control _owner;
        private readonly IFieldEditor _editor;
        private readonly Action _refresh;
        private readonly Action _applyMetadata;
        private bool _suppress;
        private bool _updatingBinding;
        private bool _attached;
        private bool _boundFromAmbient;

        /// <summary>
        /// Initializes a new instance of <see cref="FieldEditorBinder"/>.
        /// </summary>
        /// <param name="owner">The editor control; must implement <see cref="IFieldEditor"/>.</param>
        /// <param name="refresh">Pulls the bound value from the data object into the editor.</param>
        /// <param name="applyMetadata">Applies layout/schema metadata to the editor.</param>
        public FieldEditorBinder(Control owner, Action refresh, Action applyMetadata)
        {
            _owner = owner;
            _editor = owner as IFieldEditor
                ?? throw new ArgumentException("Owner must implement IFieldEditor.", nameof(owner));
            _refresh = refresh;
            _applyMetadata = applyMetadata;
        }

        /// <summary>
        /// Gets the bound data object, or <c>null</c> when unbound.
        /// </summary>
        public FormDataObject? DataObject { get; private set; }

        /// <summary>
        /// Gets the bound field name, or an empty string when unbound.
        /// </summary>
        public string FieldName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the layout field (or grid column) supplied to the explicit bind, or
        /// <c>null</c> when the binding came from the ambient scope or by field name only.
        /// </summary>
        public LayoutFieldBase? LayoutField { get; private set; }

        /// <summary>
        /// Gets the row a row-scoped bind targets, or <c>null</c> for master-row
        /// bindings.
        /// </summary>
        public DataRow? TargetRow { get; private set; }

        /// <summary>
        /// Gets the schema metadata resolved for the bound field, or <c>null</c>.
        /// </summary>
        public FormField? FormField { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the editor is currently bound.
        /// </summary>
        public bool IsBound => DataObject is not null;

        /// <summary>
        /// Gets a value indicating whether the layout marks the field read-only.
        /// </summary>
        public bool IsLayoutReadOnly => LayoutField?.ReadOnly ?? false;

        /// <summary>
        /// Determines whether the bound field is editable in the specified form
        /// mode, combining the mode, the layout read-only flag and the master
        /// field's <see cref="Bee.Definition.Layouts.LayoutField.AllowEditModes"/>.
        /// Row-scoped grid columns carry no per-mode setting (the grid gates them
        /// as a whole) and follow the mode alone.
        /// </summary>
        /// <param name="formMode">The single-record form mode.</param>
        public bool AllowsEdit(SingleFormMode formMode)
            => formMode != SingleFormMode.View
                && !IsLayoutReadOnly
                && (LayoutField is not Bee.Definition.Layouts.LayoutField field
                    || field.AllowEditModes.Allows(formMode));

        /// <summary>
        /// Binds explicitly, replacing any current binding. Explicit bindings take
        /// precedence over the ambient scope while the editor stays attached.
        /// </summary>
        /// <param name="dataObject">The data object that backs two-way binding.</param>
        /// <param name="fieldName">The field (column) name to bind.</param>
        /// <param name="layoutField">Optional layout field carrying rendering attributes.</param>
        public void BindExplicit(FormDataObject dataObject, string fieldName, LayoutFieldBase? layoutField)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            Unbind();
            BindCore(dataObject, fieldName, layoutField, fromAmbient: false);
        }

        /// <summary>
        /// Binds to a specific row (master or detail), replacing any current binding.
        /// Used by the row edit form; value reads and write-backs target
        /// <paramref name="row"/> and metadata resolves against the row's table.
        /// </summary>
        /// <param name="dataObject">The data object that backs two-way binding.</param>
        /// <param name="field">The layout field / grid column carrying rendering attributes.</param>
        /// <param name="row">The row to bind.</param>
        public void BindRow(FormDataObject dataObject, LayoutFieldBase field, DataRow row)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(row);
            Unbind();
            BindCore(dataObject, field.FieldName, field, fromAmbient: false, row);
        }

        /// <summary>
        /// Releases the binding and unsubscribes from data object events.
        /// </summary>
        public void Unbind()
        {
            if (DataObject is not null)
            {
                DataObject.DataSetReplaced -= OnDataSetReplaced;
                DataObject.FieldValueChanged -= OnFieldValueChanged;
            }
            DataObject = null;
            LayoutField = null;
            FormField = null;
            TargetRow = null;
            FieldName = string.Empty;
            _boundFromAmbient = false;
        }

        /// <summary>
        /// Notifies the binder that the editor attached to the logical tree, enabling
        /// ambient binding through <see cref="FormScope"/>.
        /// </summary>
        public void NotifyAttached()
        {
            _attached = true;
            TryAmbientBind();
        }

        /// <summary>
        /// Notifies the binder that the editor detached from the logical tree.
        /// The binding is released so editors discarded by a host rebuild do not keep
        /// the <see cref="FormDataObject"/> alive through event subscriptions;
        /// re-attaching restores ambient bindings automatically.
        /// </summary>
        public void NotifyDetached()
        {
            _attached = false;
            Unbind();
        }

        /// <summary>
        /// Re-evaluates the ambient binding after the editor's field name or the
        /// ambient <see cref="FormScope.DataObjectProperty"/> changed.
        /// </summary>
        public void OnBindingContextChanged()
        {
            if (_updatingBinding || !_attached) return;
            TryAmbientBind();
        }

        /// <summary>
        /// Forwards an ambient <see cref="FormScope.FormModeProperty"/> change to the editor.
        /// </summary>
        /// <param name="formMode">The new form mode.</param>
        public void OnFormModeChanged(SingleFormMode formMode)
        {
            _editor.SetControlState(formMode);
        }

        /// <summary>
        /// Reads the bound field value as a binding string; empty when unbound.
        /// </summary>
        public string GetValue()
        {
            if (DataObject is null) return string.Empty;
            return TargetRow is not null
                ? DataObject.GetField(TargetRow, FieldName)
                : DataObject.GetField(FieldName);
        }

        /// <summary>
        /// Writes the editor value back to the data object. No-op while the editor is
        /// being refreshed from the source (echo suppression) or when unbound.
        /// </summary>
        /// <param name="value">The editor value rendered as a binding string.</param>
        public void WriteBack(string? value)
        {
            if (_suppress || DataObject is null) return;
            _suppress = true;
            try
            {
                if (TargetRow is not null)
                    DataObject.SetField(TargetRow, FieldName, value);
                else
                    DataObject.SetField(FieldName, value);
            }
            finally
            {
                _suppress = false;
            }
        }

        private void TryAmbientBind()
        {
            // An explicit binding wins over the ambient scope.
            if (IsBound && !_boundFromAmbient) return;

            var ambient = _owner.GetValue(FormScope.DataObjectProperty);
            var fieldName = _editor.FieldName;
            if (IsBound)
            {
                if (ReferenceEquals(DataObject, ambient)
                    && string.Equals(FieldName, fieldName, StringComparison.Ordinal))
                {
                    return;
                }
                Unbind();
            }
            if (ambient is null || string.IsNullOrWhiteSpace(fieldName)) return;
            BindCore(ambient, fieldName, layoutField: null, fromAmbient: true);
        }

        private void BindCore(FormDataObject dataObject, string fieldName, LayoutFieldBase? layoutField, bool fromAmbient, DataRow? row = null)
        {
            _updatingBinding = true;
            try
            {
                DataObject = dataObject;
                FieldName = fieldName;
                LayoutField = layoutField;
                TargetRow = row;
                FormField = row is not null
                    ? dataObject.GetFormField(row.Table.TableName, fieldName)
                    : dataObject.GetFormField(fieldName);
                _boundFromAmbient = fromAmbient;
                if (!string.Equals(_editor.FieldName, fieldName, StringComparison.Ordinal))
                    _editor.FieldName = fieldName;
                dataObject.DataSetReplaced += OnDataSetReplaced;
                dataObject.FieldValueChanged += OnFieldValueChanged;
                RunSuppressed(_applyMetadata);
                RunSuppressed(_refresh);
                _editor.SetControlState(_owner.GetValue(FormScope.FormModeProperty));
            }
            finally
            {
                _updatingBinding = false;
            }
        }

        private void RunSuppressed(Action action)
        {
            _suppress = true;
            try
            {
                action();
            }
            finally
            {
                _suppress = false;
            }
        }

        private void OnDataSetReplaced(object? sender, EventArgs e)
        {
            RunSuppressed(_refresh);
        }

        private void OnFieldValueChanged(object? sender, FieldValueChangedEventArgs e)
        {
            // `_suppress` is true while this editor itself is writing back, so a
            // self-originated event is ignored; changes to the same field that other
            // parties wrote (for example lookup write-backs) refresh the editor.
            // Master bindings filter by the master table name; row bindings match the
            // exact target row. Comparisons are case-insensitive to follow DataTable
            // semantics — `AddColumn` stores column names uppercase while
            // wire-deserialized tables keep the original casing.
            if (_suppress) return;
            if (TargetRow is not null)
            {
                if (!ReferenceEquals(e.Row, TargetRow)) return;
            }
            else if (!string.Equals(e.TableName, DataObject?.MasterTable.TableName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (string.Equals(e.FieldName, FieldName, StringComparison.OrdinalIgnoreCase))
                RunSuppressed(_refresh);
        }
    }
}
