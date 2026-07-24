using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Cell-building half of <see cref="GridControl"/> (cell rendering, lookup cells, in-cell
    /// editors and value formatting). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class GridControl
    {
        private static bool IsAlwaysOnEditor(ControlType controlType)
            => controlType is ControlType.CheckEdit or ControlType.DropDownEdit
                or ControlType.DateEdit or ControlType.YearMonthEdit;

        /// <summary>
        /// Resolves the relation metadata for a lookup column: a
        /// <see cref="ControlType.ButtonEdit"/> column whose schema field carries
        /// <c>RelationProgId</c> on the bound table. Returns <c>null</c> in list mode
        /// (no data object) — lookup editing needs the data object write path.
        /// </summary>
        private FormField? TryGetLookupFormField(LayoutColumn column)
        {
            if (column.ControlType != ControlType.ButtonEdit) return null;
            var dataObject = _binder.DataObject;
            var tableName = TableName;
            if (dataObject is null || string.IsNullOrEmpty(tableName)) return null;
            var field = dataObject.GetFormField(tableName, column.FieldName);
            return field is not null && !string.IsNullOrEmpty(field.RelationProgId) ? field : null;
        }

        // Lookup cells rest as the composed display-field text — typically
        // "<id> <name>", id only for transactional targets, empty when no display
        // field resolves (never the raw Guid); a click on an editable cell opens
        // the lookup dialog and the selection writes back through the data object.
        private Control BuildLookupCell(DataRowView? rowView, LayoutColumn column, FormField lookupField)
        {
            var displayFields = SplitDisplayFields(column.DisplayFields);
            if (displayFields.Length == 0)
                displayFields = [.. lookupField.GetDisplayFields()];
            var text = new TextBlock
            {
                Text = ComposeDisplayText(rowView, displayFields, column.DisplayFormat, column.NumberFormat),
                Margin = new Thickness(8, 4),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            };

            var canEdit = !_grid.IsReadOnly && !column.ReadOnly && rowView is not null;
            if (!canEdit) return text;

            // Mirror the standalone ButtonEdit: an editable lookup cell shows a trailing
            // magnifier icon as the "opens a dialog" cue. The text fills the remaining
            // width so the icon stays pinned to the right edge.
            text.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            var icon = BuildLookupCellIcon();
            DockPanel.SetDock(icon, Dock.Right);
            var content = new DockPanel { LastChildFill = true };
            content.Children.Add(icon);
            content.Children.Add(text);

            // Transparent (not null) background: a null background excludes the empty
            // area beside the text from hit-testing.
            var host = new Border
            {
                Background = Brushes.Transparent,
                Child = content,
            };
            host.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                // Only one inline editor at a time: opening the dialog closes a
                // lingering editor in another cell.
                _endActiveInlineEdit?.Invoke();
                await OpenLookupCellAsync(rowView!.Row, lookupField).ConfigureAwait(true);
            };
            return host;
        }

        // Trailing magnifier icon for an editable lookup cell, matching the muted tone
        // of the standalone ButtonEdit's icon. `Geometry.Parse` and `Cursor` both need
        // platform services that are absent when unit tests build cells without an
        // Avalonia platform, so they are created on first visual attach instead.
        private static PathIcon BuildLookupCellIcon()
        {
            var icon = new PathIcon
            {
                Width = 14,
                Height = 14,
                Opacity = 0.65,
                Margin = new Thickness(4, 0, 8, 0),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            };
            icon.AttachedToVisualTree += (_, _) =>
            {
                icon.Data ??= Geometry.Parse(LookupIconGeometry);
                icon.Cursor ??= new Cursor(StandardCursorType.Hand);
            };
            return icon;
        }

        private static string[] SplitDisplayFields(string displayFields)
            => string.IsNullOrEmpty(displayFields)
                ? []
                : displayFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Joins the non-empty display-field values (e.g. "D001 - Engineering").
        private static string ComposeDisplayText(
            DataRowView? rowView, string[] displayFields, string displayFormat, string numberFormat)
        {
            if (displayFields.Length == 0) return string.Empty;
            return LookupDisplay.Compose(displayFields
                .Select(f => FormatCell(rowView, f, displayFormat, numberFormat)));
        }

        private async Task OpenLookupCellAsync(DataRow row, FormField lookupField)
        {
            var dataObject = _binder.DataObject;
            if (dataObject is null) return;

            var progId = string.IsNullOrEmpty(lookupField.LookupProgId)
                ? lookupField.RelationProgId
                : lookupField.LookupProgId;
            try
            {
                var selected = await LookupDialog.ShowAsync(this, progId).ConfigureAwait(true);
                if (selected is null) return;
                dataObject.ApplyLookupSelection(lookupField, selected, row);
                // Realized text cells capture their value at template build;
                // re-realize and scroll back to the affected row (same as a
                // committed edit form).
                RefreshAndFocusRow(row);
            }
            catch (Exception ex)
            {
                // UI boundary: an async pointer handler must not crash the app;
                // surface the failure as the grid's tooltip.
                ToolTip.SetTip(this, ex.Message);
            }
        }

        // Display template for the popup-based editor columns. Boolean cells render a
        // centred checkbox in every state (a disabled checkbox reads better than
        // "True"/"False" text); the other types rest as plain formatted text and swap
        // in their editor on click — the swap is managed here, not by the DataGrid
        // edit pipeline, so opening the editor's popup cannot tear the editor down.
        private Control BuildInteractiveCell(DataRowView? rowView, LayoutColumn column)
        {
            var canEdit = !_grid.IsReadOnly && !column.ReadOnly && rowView is not null;

            if (column.ControlType == ControlType.CheckEdit)
            {
                CheckBox checkBox;
                if (canEdit)
                {
                    checkBox = (CheckBox)BuildCellEditor(rowView, column);
                }
                else
                {
                    var value = string.Equals(
                        FormatCell(rowView, column.FieldName, string.Empty, string.Empty),
                        bool.TrueString, StringComparison.OrdinalIgnoreCase);
                    checkBox = new CheckBox { IsChecked = value, IsEnabled = false };
                }
                checkBox.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
                checkBox.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
                return checkBox;
            }

            if (!canEdit)
            {
                return new TextBlock
                {
                    Text = FormatCellForColumn(rowView, column),
                    Margin = new Thickness(8, 4),
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            // canEdit is true only when rowView is not null (see its definition above),
            // so the swap-cell branch always has a non-null row.
            return BuildSwapCell(rowView!, column);
        }

        // Click-to-edit host: rests as the original text rendering, swaps to the
        // editor on pointer press and swaps back when the edit session ends, re-reading
        // the row so the committed value is what gets displayed.
        private ContentControl BuildSwapCell(DataRowView rowView, LayoutColumn column)
        {
            var host = new ContentControl
            {
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                // Transparent (not null) background: a null background excludes the
                // empty area beside the text from hit-testing, so only the characters
                // themselves would react to the click.
                Background = Brushes.Transparent,
            };

            void ShowDisplay() => host.Content = new TextBlock
            {
                Text = FormatCellForColumn(rowView, column),
                Margin = new Thickness(8, 4),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            };

            host.PointerPressed += (_, _) =>
            {
                if (host.Content is not TextBlock) return;
                // Only one inline editor at a time: starting an edit here closes a
                // lingering editor in another cell (e.g. a dismissed date pick).
                _endActiveInlineEdit?.Invoke();

                var editor = BuildCellEditor(rowView, column);
                editor.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
                Action end = null!;
                end = () =>
                {
                    if (host.Content is TextBlock) return;
                    ShowDisplay();
                    if (ReferenceEquals(_endActiveInlineEdit, end))
                        _endActiveInlineEdit = null;
                };
                WireInlineEditEnd(editor, end);
                host.Content = editor;
                _endActiveInlineEdit = end;
            };

            ShowDisplay();
            return host;
        }

        private static void WireInlineEditEnd(Control editor, Action endEdit)
        {
            switch (editor)
            {
                case ComboBox combo:
                    // Open the dropdown so a single click on the cell goes straight to
                    // picking an option — deferred past the click that swapped the
                    // editor in, otherwise the same pointer interaction closes the
                    // dropdown again immediately. Only a close that follows a real
                    // open ends the edit (guards the same race on the way out).
                    var opened = false;
                    combo.AttachedToVisualTree += (_, _) =>
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            opened = true;
                            combo.IsDropDownOpen = true;
                        });
                    combo.PropertyChanged += (_, e) =>
                    {
                        if (e.Property != ComboBox.IsDropDownOpenProperty) return;
                        if (combo.IsDropDownOpen)
                            opened = true;
                        else if (opened)
                            endEdit();
                    };
                    break;
                case DatePicker picker:
                    // Pop the spinner flyout right away so a single click on the cell
                    // goes straight to picking. DatePicker has no public open API;
                    // raising Click on the template's flyout button is the supported
                    // route in. Background priority defers past the layout pass so the
                    // flyout positions against the realized picker.
                    picker.AttachedToVisualTree += (_, _) =>
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            picker.ApplyTemplate();
                            var flyoutButton = global::Avalonia.VisualTree.VisualExtensions
                                .FindDescendantOfType<Button>(picker);
                            flyoutButton?.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                        }, global::Avalonia.Threading.DispatcherPriority.Background);
                    // Commit-driven swap-back only: confirming a date changes
                    // SelectedDate (the write-back hook subscribed first, so the row
                    // is updated before the swap-back re-reads it). LostFocus is NOT
                    // wired — the spinner flyout takes focus and would tear the editor
                    // down mid-pick. A dismissed flyout leaves the editor in place
                    // until the next inline edit or EndEdit closes it.
                    picker.PropertyChanged += (_, e) =>
                    {
                        if (e.Property == DatePicker.SelectedDateProperty)
                            endEdit();
                    };
                    break;
                default:
                    editor.LostFocus += (_, _) => endEdit();
                    break;
            }
        }

        // Builds the in-cell editor for the column's ControlType. The writes go
        // straight to the DataRow (the ADR-020 binding limitation applies to editing
        // templates too); invalid partial input keeps the last valid value.
        private Control BuildCellEditor(DataRowView? rowView, LayoutColumn column)
        {
            if (rowView is null) return new TextBlock();
            var fieldName = column.FieldName;
            var dataColumn = rowView.Row.Table.Columns.Contains(fieldName)
                ? rowView.Row.Table.Columns[fieldName]
                : null;
            if (dataColumn is null) return new TextBlock();

            var current = FormatCell(rowView, fieldName, string.Empty, string.Empty);

            if (column.ControlType == ControlType.CheckEdit)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = string.Equals(current, bool.TrueString, StringComparison.OrdinalIgnoreCase),
                };
                checkBox.IsCheckedChanged += (_, _) =>
                    WriteCell(rowView, dataColumn, (checkBox.IsChecked ?? false).ToString());
                return checkBox;
            }

            if (column.ControlType is ControlType.DateEdit or ControlType.YearMonthEdit)
            {
                var format = column.ControlType == ControlType.YearMonthEdit ? "yyyy-MM" : "yyyy-MM-dd";
                var picker = new DatePicker
                {
                    DayVisible = column.ControlType != ControlType.YearMonthEdit,
                    SelectedDate = DateEdit.ParseToOffset(current),
                };
                // `SelectedDateChanged` is not raised reliably for programmatic
                // writes; hook the property change instead (same approach as the
                // TextBox editor).
                picker.PropertyChanged += (_, e) =>
                {
                    if (e.Property == DatePicker.SelectedDateProperty)
                        WriteCell(rowView, dataColumn, picker.SelectedDate?.DateTime.ToString(format, CultureInfo.InvariantCulture));
                };
                return picker;
            }

            if (column.ControlType == ControlType.DropDownEdit
                && _binder.DataObject?.GetFormField(TableName, fieldName)?.ListItems is { Count: > 0 } items)
            {
                var options = items.ToList();
                var combo = new ComboBox
                {
                    ItemsSource = options,
                    // Same selection-box pitfall as DropDownEdit: a recycling template
                    // starves the collapsed combo of its content instance.
                    DisplayMemberBinding = new global::Avalonia.Data.Binding(
                        nameof(Bee.Definition.Collections.ListItem.Text)),
                    SelectedItem = options.FirstOrDefault(i => string.Equals(i.Value, current, StringComparison.Ordinal)),
                };
                combo.SelectionChanged += (_, _) =>
                    WriteCell(rowView, dataColumn, (combo.SelectedItem as Bee.Definition.Collections.ListItem)?.Value);
                return combo;
            }

            var textBox = new TextBox { Text = current };
            // Commit on leaving the cell editor (or Enter) rather than per keystroke, matching
            // the standalone TextEdit — so a field's value (and any dependent recalculation)
            // is written once the entry is complete. This LostFocus runs before the swap-back
            // one wired by WireInlineEditEnd (subscribed first), so the row is updated before
            // the display re-reads it.
            textBox.LostFocus += (_, _) => WriteCell(rowView, dataColumn, textBox.Text);
            textBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    WriteCell(rowView, dataColumn, textBox.Text);
            };
            return textBox;
        }

        private static void WriteCell(DataRowView rowView, DataColumn column, string? value)
        {
            if (!TryConvertCellValue(value, column, out var converted)) return;
            if (Equals(converted, rowView.Row[column])) return;
            // The write raises FieldValueChanged and marks dirty through the data
            // object's DataTable event bridge.
            rowView.Row[column] = converted;
        }

        private static bool TryConvertCellValue(string? value, DataColumn column, out object converted)
        {
            try
            {
                converted = FormDataObject.ConvertToColumnValue(value, column);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                // Partial keystroke input (for example "12." in a decimal column)
                // cannot convert yet; the cell keeps its last valid value until the
                // input parses.
                converted = DBNull.Value;
                return false;
            }
        }

        private static string FormatCell(DataRowView? row, string fieldName, string displayFormat, string numberFormat)
        {
            if (row is null) return string.Empty;
            var dataRow = row.Row;
            if (!dataRow.Table.Columns.Contains(fieldName)) return string.Empty;
            return CellValueFormatter.Format(dataRow[fieldName], displayFormat, numberFormat);
        }

        // Currency-aware cell text: an Amount column resolves its decimals per row from the referenced
        // currency (see ResolveCellNumberFormat); every other column uses the column's delivered formats.
        private string FormatCellForColumn(DataRowView? row, LayoutColumn column)
        {
            if (row is null) return string.Empty;
            var dataRow = row.Row;
            if (!dataRow.Table.Columns.Contains(column.FieldName)) return string.Empty;
            string numberFormat = ResolveCellNumberFormat(dataRow, column);
            return CellValueFormatter.Format(dataRow[column.FieldName], column.DisplayFormat, numberFormat);
        }

        // Reference-bound columns (amounts by currency, quantities/weights by unit) are not baked at
        // delivery — resolve their format from the row's reference value and the client master. Other
        // kinds (and the no-master / no-reference cases) keep the delivered format.
        private string ResolveCellNumberFormat(DataRow dataRow, LayoutColumn column)
        {
            var source = NumberKindProfile.GetDecimalsSource(column.NumberKind);

            if (source == DecimalsSource.Currency && CurrencySettings is not null)
            {
                string code = ResolveCellReferenceCode(dataRow, column.CurrencyField, DefaultCurrencyCode);
                return NumberFormatResolver.ResolveFormat(
                    column.NumberKind, new RoundingContext { CurrencySettings = CurrencySettings }, code);
            }

            if (source == DecimalsSource.Unit && UnitSettings is not null)
            {
                string code = ResolveCellReferenceCode(dataRow, column.UnitField, string.Empty);
                if (StringUtilities.IsNotEmpty(code))
                    return NumberFormatResolver.ResolveFormat(
                        column.NumberKind, new RoundingContext { UnitSettings = UnitSettings }, code);
            }

            return column.NumberFormat;
        }

        // Per-row reference code: the column's reference field (currency key / unit) on this row →
        // the supplied fallback. Empty resolves to the framework fallback downstream.
        private static string ResolveCellReferenceCode(DataRow dataRow, string referenceField, string fallback)
        {
            if (StringUtilities.IsNotEmpty(referenceField)
                && dataRow.Table.Columns.Contains(referenceField))
            {
                string rowCode = ValueUtilities.CStr(dataRow[referenceField]);
                if (StringUtilities.IsNotEmpty(rowCode)) { return rowCode; }
            }
            return fallback;
        }

        private void OnSelectionChangedCore(object? sender, SelectionChangedEventArgs e)
        {
            var handler = RowSelected;
            if (handler is null) return;
            if (_grid.SelectedItem is not DataRowView rowView) return;
            if (!TryGetRowId(rowView.Row, out var rowId)) return;
            handler(this, rowId);
        }

        private static IEnumerable<LayoutColumn> EnumerateVisibleColumns(LayoutGrid layout)
            => layout.Columns?.Where(c => c.Visible) ?? Enumerable.Empty<LayoutColumn>();

        private static bool TryGetRowId(DataRow row, out Guid rowId)
        {
            rowId = Guid.Empty;
            if (!row.Table.Columns.Contains(SysFields.RowId)) return false;
            var raw = row[SysFields.RowId];
            if (raw is null || raw == DBNull.Value) return false;
            if (raw is Guid g) { rowId = g; return true; }
            return Guid.TryParse(raw.ToString(), out rowId);
        }
    }
}
