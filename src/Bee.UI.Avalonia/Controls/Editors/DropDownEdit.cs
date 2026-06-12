using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Bee.Definition.Collections;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.DropDownEdit"/>: a <see cref="ComboBox"/>
    /// that loads its options from <c>FormField.ListItems</c> and binds the selected
    /// <see cref="ListItem.Value"/> to a <see cref="FormDataObject"/> field.
    /// </summary>
    public class DropDownEdit : ComboBox, IFieldEditor
    {
        /// <summary>
        /// Identifies the <see cref="FieldName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> FieldNameProperty =
            AvaloniaProperty.Register<DropDownEdit, string>(nameof(FieldName), string.Empty);

        private readonly FieldEditorBinder _binder;

        static DropDownEdit()
        {
            FieldNameProperty.Changed.AddClassHandler<DropDownEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.DataObjectProperty.Changed.AddClassHandler<DropDownEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<DropDownEdit>((o, e) => o._binder.OnFormModeChanged((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DropDownEdit"/>.
        /// </summary>
        public DropDownEdit()
        {
            _binder = new FieldEditorBinder(this, RefreshFromSource, ApplyMetadata);
            // NOTE: A recycling FuncDataTemplate hands the same TextBlock instance to
            // both the dropdown item and the selection box, and a control cannot live
            // in two places — the collapsed combo then fails to show the picked value.
            // DisplayMemberBinding materialises per-container content instead.
            DisplayMemberBinding = new global::Avalonia.Data.Binding(nameof(ListItem.Text));
            SelectionChanged += (_, _) => _binder.WriteBack((SelectedItem as ListItem)?.Value);
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(ComboBox);

        /// <summary>
        /// Gets or sets the bound field (column) name.
        /// </summary>
        public string FieldName
        {
            get => GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected <see cref="ListItem.Value"/> as the bound field value.
        /// </summary>
        public object? FieldValue
        {
            get => (SelectedItem as ListItem)?.Value;
            set => SelectFromValue(value?.ToString());
        }

        /// <inheritdoc />
        public void Bind(FormDataObject dataObject, LayoutField field)
        {
            ArgumentNullException.ThrowIfNull(field);
            _binder.BindExplicit(dataObject, field.FieldName, field);
        }

        /// <inheritdoc />
        public void Bind(FormDataObject dataObject, string fieldName)
        {
            _binder.BindExplicit(dataObject, fieldName, layoutField: null);
        }

        /// <inheritdoc />
        public void Bind(FormDataObject dataObject, LayoutFieldBase field, System.Data.DataRow row)
        {
            _binder.BindRow(dataObject, field, row);
        }

        /// <inheritdoc />
        public void Unbind()
        {
            _binder.Unbind();
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            IsEnabled = _binder.AllowsEdit(formMode);
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _binder.NotifyAttached();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            _binder.NotifyDetached();
        }

        private void RefreshFromSource()
        {
            SelectFromValue(_binder.GetValue());
        }

        private void ApplyMetadata()
        {
            IsEnabled = !_binder.IsLayoutReadOnly;
            if (_binder.FormField?.ListItems is { } items)
                ItemsSource = items.ToList();
        }

        private void SelectFromValue(string? value)
        {
            SelectedItem = ItemsSource?.OfType<ListItem>()
                .FirstOrDefault(i => string.Equals(i.Value, value, StringComparison.Ordinal));
        }
    }
}
