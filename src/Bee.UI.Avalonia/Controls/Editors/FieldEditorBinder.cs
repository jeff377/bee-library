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
        /// Gets the layout field supplied to the explicit bind, or <c>null</c> when the
        /// binding came from the ambient scope or by field name only.
        /// </summary>
        public LayoutField? LayoutField { get; private set; }

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
        /// Binds explicitly, replacing any current binding. Explicit bindings take
        /// precedence over the ambient scope while the editor stays attached.
        /// </summary>
        /// <param name="dataObject">The data object that backs two-way binding.</param>
        /// <param name="fieldName">The field (column) name to bind.</param>
        /// <param name="layoutField">Optional layout field carrying rendering attributes.</param>
        public void BindExplicit(FormDataObject dataObject, string fieldName, LayoutField? layoutField)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            Unbind();
            BindCore(dataObject, fieldName, layoutField, fromAmbient: false);
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
            return DataObject?.GetField(FieldName) ?? string.Empty;
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

        private void BindCore(FormDataObject dataObject, string fieldName, LayoutField? layoutField, bool fromAmbient)
        {
            _updatingBinding = true;
            try
            {
                DataObject = dataObject;
                FieldName = fieldName;
                LayoutField = layoutField;
                FormField = dataObject.GetFormField(fieldName);
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
            if (_suppress) return;
            if (string.Equals(e.FieldName, FieldName, StringComparison.Ordinal))
                RunSuppressed(_refresh);
        }
    }
}
