using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.TextEdit"/>: a <see cref="TextBox"/>
    /// two-way bound to a <see cref="FormDataObject"/> field, applying
    /// <c>FormField.MaxLength</c> and the layout read-only flag automatically.
    /// </summary>
    public class TextEdit : TextBox, IFieldEditor
    {
        /// <summary>
        /// Identifies the <see cref="FieldName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> FieldNameProperty =
            AvaloniaProperty.Register<TextEdit, string>(nameof(FieldName), string.Empty);

        private readonly FieldEditorBinder _binder;

        static TextEdit()
        {
            FieldNameProperty.Changed.AddClassHandler<TextEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.DataObjectProperty.Changed.AddClassHandler<TextEdit>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<TextEdit>((o, e) => o._binder.OnFormModeChanged((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextEdit"/>.
        /// </summary>
        public TextEdit()
        {
            _binder = new FieldEditorBinder(this, RefreshFromSource, ApplyMetadata);
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(TextBox);

        /// <summary>
        /// Gets the binder that drives the field binding. Derived editors use it to
        /// reach layout/schema metadata.
        /// </summary>
        internal FieldEditorBinder Binder => _binder;

        /// <summary>
        /// Gets or sets the bound field (column) name.
        /// </summary>
        public string FieldName
        {
            get => GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the editor value as the bound field value.
        /// </summary>
        public object? FieldValue
        {
            get => Text;
            set => Text = value as string ?? value?.ToString() ?? string.Empty;
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
        public virtual void SetControlState(SingleFormMode formMode)
        {
            var readOnly = !_binder.AllowsEdit(formMode);
            IsReadOnly = readOnly;
            ApplyReadOnlyAppearance(readOnly);
        }

        /// <summary>
        /// Applies the read-only field appearance. In read-only mode the four-sided box
        /// border collapses to a single bottom line and the fill goes transparent, so a
        /// form viewed read-only reads as a clean record rather than a grid of input
        /// boxes. Editable mode clears the local overrides and restores the theme values.
        /// </summary>
        /// <remarks>
        /// The bottom line uses a constant light brush set as a local value, so it stays
        /// visible at rest (not only on hover) and keeps the value separated from its
        /// caption. The state is driven by the effective edit permission rather than
        /// <see cref="TextBox.IsReadOnly"/>, because lookup editors keep the text box
        /// permanently read-only while still being editable through their dialog.
        /// </remarks>
        /// <param name="readOnly">Whether the editor is in the read-only view state.</param>
        protected void ApplyReadOnlyAppearance(bool readOnly)
        {
            if (readOnly)
            {
                BorderBrush = ReadOnlyFieldVisual.UnderlineBrush;
                BorderThickness = new Thickness(0, 0, 0, 1);
                Background = Brushes.Transparent;
            }
            else
            {
                ClearValue(BorderBrushProperty);
                ClearValue(BorderThicknessProperty);
                ClearValue(BackgroundProperty);
            }
        }

        /// <summary>
        /// Whether a <see cref="TextBox.Text"/> change writes back to the bound field.
        /// Lookup editors display a different field than the one they bind, so they
        /// suppress the write-back (the lookup flow writes through the data object).
        /// </summary>
        protected virtual bool ShouldWriteBackText => true;

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            // `TextChanged` is not raised reliably for programmatic writes, so the
            // write-back hooks the property change instead; the binder's suppression
            // flag keeps source-driven refreshes from echoing back.
            if (change.Property == TextProperty && ShouldWriteBackText)
                _binder.WriteBack(Text);
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

        /// <summary>
        /// Pulls the bound value from the data object into the editor.
        /// </summary>
        protected virtual void RefreshFromSource()
        {
            Text = _binder.GetValue();
        }

        /// <summary>
        /// Applies layout/schema metadata to the editor.
        /// </summary>
        protected virtual void ApplyMetadata()
        {
            IsReadOnly = _binder.IsLayoutReadOnly;
            if (_binder.FormField is { MaxLength: > 0 } formField)
                MaxLength = formField.MaxLength;
        }
    }
}
