using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Bee.Base;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.ButtonEdit"/>: a <see cref="TextEdit"/>
    /// with a trailing lookup icon. When the bound field carries relation metadata
    /// (<c>FormField.RelationProgId</c>), the editor runs the built-in lookup flow:
    /// the text shows the resolved display field, the icon opens
    /// <see cref="LookupDialog"/>, a selection writes the row id and mapped fields
    /// back through the data object, and Delete / Backspace clears the selection.
    /// Without relation metadata the icon only raises <see cref="ButtonClick"/> and
    /// the editor behaves like a plain <see cref="TextEdit"/>.
    /// </summary>
    /// <remarks>
    /// In lookup mode the text box is always read-only — the display value is not
    /// hand-editable; the lookup flow is the only write path. The icon button follows
    /// the form mode (disabled in View mode or on a read-only layout field).
    /// </remarks>
    public class ButtonEdit : TextEdit
    {
        // Magnifier glyph taken from Semi.Avalonia `SemiIconSearchStroked` (MIT),
        // embedded so the editor renders the same under any application theme.
        private const string SearchIconGeometry =
            "M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0Zm-1.1 6.32a8 8 0 1 1 1.41-1.41l5.4 5.38a1 1 0 0 1-1.42 1.42l-5.38-5.39Z";

        private readonly PathIcon _icon;
        private readonly Button _button;
        private bool _allowLookupEdit;

        /// <summary>
        /// Initializes a new instance of <see cref="ButtonEdit"/> with the embedded
        /// lookup icon.
        /// </summary>
        public ButtonEdit()
        {
            _icon = new PathIcon
            {
                Width = 14,
                Height = 14,
                // Match the muted tone date/time pickers use for their inner icon.
                Opacity = 0.65,
            };
            // Follow the editor's own text colour instead of the button theme's
            // foreground (accent-coloured under Semi), so the glyph stays the muted
            // grey of a DatePicker icon in both light and dark variants.
            _icon[!ForegroundProperty] = this[!ForegroundProperty];
            // A chromeless button keeps click/automation semantics while only the
            // glyph stays visible; local values beat the theme's hover/pressed
            // setters, so no button chrome shows up on interaction.
            _button = new Button
            {
                Content = _icon,
                Focusable = false,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2),
                MinWidth = 0,
                MinHeight = 0,
            };
            _button.Click += async (_, _) => await OnButtonClickAsync().ConfigureAwait(true);
            InnerRightContent = _button;
        }

        /// <summary>
        /// Raised when the embedded icon is clicked on an editor without relation
        /// metadata; the lookup flow of relation-bound editors replaces it.
        /// </summary>
        public event EventHandler? ButtonClick;

        /// <summary>
        /// Raised when the built-in lookup flow fails (schema load or dialog error).
        /// Without a subscriber the failure surfaces as the editor's tooltip.
        /// </summary>
        public event EventHandler<Exception>? LookupFailed;

        /// <summary>
        /// Gets a value indicating whether the bound field carries relation metadata
        /// and therefore uses the built-in lookup flow.
        /// </summary>
        public bool HasLookup =>
            Binder.FormField is { } formField && StringUtilities.IsNotEmpty(formField.RelationProgId);

        /// <inheritdoc />
        public override void SetControlState(SingleFormMode formMode)
        {
            if (!HasLookup)
            {
                base.SetControlState(formMode);
                return;
            }
            _allowLookupEdit = Binder.AllowsEdit(formMode);
            // The display text is never hand-editable; the icon button alone follows
            // the effective edit permission.
            IsReadOnly = true;
            _button.IsEnabled = _allowLookupEdit;
        }

        /// <inheritdoc />
        protected override bool ShouldWriteBackText => !HasLookup;

        /// <inheritdoc />
        protected override void RefreshFromSource()
        {
            if (!HasLookup)
            {
                base.RefreshFromSource();
                return;
            }
            // Lookup editors show the display field; a relation field without a
            // resolvable display field shows an empty string rather than the raw Guid.
            var displayField = ResolveDisplayFieldName();
            var dataObject = Binder.DataObject;
            if (string.IsNullOrEmpty(displayField) || dataObject is null)
            {
                Text = string.Empty;
                return;
            }
            Text = Binder.TargetRow is not null
                ? dataObject.GetField(Binder.TargetRow, displayField)
                : dataObject.GetField(displayField);
        }

        /// <inheritdoc />
        protected override void ApplyMetadata()
        {
            base.ApplyMetadata();
            // Changes to the display field (e.g. a lookup write-back) must refresh
            // this editor even though it binds the row-id field.
            var displayField = HasLookup ? ResolveDisplayFieldName() : string.Empty;
            Binder.WatchFieldName = string.IsNullOrEmpty(displayField) ? null : displayField;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            // A single property hook covers every path that turns the editor
            // read-only — `SetControlState`, layout metadata and direct assignment —
            // so the icon cannot fire on a read-only editor. Lookup-bound editors are
            // permanently read-only, so their button follows the form mode instead.
            if (change.Property == IsReadOnlyProperty)
                _button.IsEnabled = HasLookup ? _allowLookupEdit : !IsReadOnly;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Delete / Backspace clears the lookup selection (row id + mapped fields).
            if (HasLookup && _allowLookupEdit && e.Key is Key.Delete or Key.Back)
            {
                e.Handled = true;
                if (Binder.DataObject is { } dataObject && Binder.FormField is { } field)
                    dataObject.ClearLookupSelection(field, Binder.TargetRow);
                return;
            }
            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // `Geometry.Parse` and `Cursor` both need platform services that are
            // absent when unit tests construct editors without an Avalonia platform,
            // so they are created on first visual attach instead of in the constructor.
            _icon.Data ??= Geometry.Parse(SearchIconGeometry);
            _button.Cursor ??= new Cursor(StandardCursorType.Hand);
        }

        private async Task OnButtonClickAsync()
        {
            if (!HasLookup)
            {
                ButtonClick?.Invoke(this, EventArgs.Empty);
                return;
            }
            await OpenLookupAsync().ConfigureAwait(true);
        }

        private async Task OpenLookupAsync()
        {
            var field = Binder.FormField;
            var dataObject = Binder.DataObject;
            if (field is null || dataObject is null || !_allowLookupEdit) return;

            var progId = StringUtilities.IsNotEmpty(field.LookupProgId)
                ? field.LookupProgId
                : field.RelationProgId;
            try
            {
                var selected = await LookupDialog.ShowAsync(this, progId).ConfigureAwait(true);
                if (selected is null) return;
                dataObject.ApplyLookupSelection(field, selected, Binder.TargetRow);
            }
            catch (Exception ex)
            {
                // UI boundary: an async click handler must not crash the app; surface
                // the failure to the host (or the tooltip as a last resort).
                var failureHandler = LookupFailed;
                if (failureHandler is not null)
                    failureHandler.Invoke(this, ex);
                else
                    ToolTip.SetTip(this, ex.Message);
            }
        }

        private string ResolveDisplayFieldName()
        {
            // A layout-level override wins over the schema-level resolution.
            var fromLayout = Binder.LayoutField?.DisplayField;
            if (!string.IsNullOrEmpty(fromLayout)) return fromLayout!;
            return Binder.FormField?.GetDisplayField() ?? string.Empty;
        }
    }
}
