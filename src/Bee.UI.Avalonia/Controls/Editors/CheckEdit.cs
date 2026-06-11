using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.CheckEdit"/>: a <see cref="CheckBox"/>
    /// two-way bound to a boolean <see cref="FormDataObject"/> field.
    /// </summary>
    public class CheckEdit : CheckBox, IFieldEditor
    {
        /// <summary>
        /// Identifies the <see cref="FieldName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> FieldNameProperty =
            AvaloniaProperty.Register<CheckEdit, string>(nameof(FieldName), string.Empty);

        private readonly FieldEditorBinder _binder;

        static CheckEdit()
        {
            FieldNameProperty.Changed.AddClassHandler<CheckEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.DataObjectProperty.Changed.AddClassHandler<CheckEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<CheckEdit>((o, e) => o._binder.OnFormModeChanged((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CheckEdit"/>.
        /// </summary>
        public CheckEdit()
        {
            _binder = new FieldEditorBinder(this, RefreshFromSource, ApplyMetadata);
            IsCheckedChanged += (_, _) => _binder.WriteBack((IsChecked ?? false).ToString());
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(CheckBox);

        /// <summary>
        /// Gets or sets the bound field (column) name.
        /// </summary>
        public string FieldName
        {
            get => GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the checked state as the bound field value.
        /// </summary>
        public object? FieldValue
        {
            get => IsChecked ?? false;
            set => IsChecked = value is bool flag
                ? flag
                : string.Equals(value?.ToString(), bool.TrueString, StringComparison.OrdinalIgnoreCase);
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
        public void Unbind()
        {
            _binder.Unbind();
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            IsEnabled = formMode != SingleFormMode.View && !_binder.IsLayoutReadOnly;
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
            IsChecked = string.Equals(_binder.GetValue(), bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyMetadata()
        {
            IsEnabled = !_binder.IsLayoutReadOnly;
        }
    }
}
