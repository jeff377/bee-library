using System.Globalization;
using Bee.Definition.Collections;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.DataObjects;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.Components
{
    /// <summary>
    /// Code-behind for <c>DynamicForm.razor</c>. Renders the master section(s) of a
    /// <see cref="FormLayout"/> by dispatching each <see cref="LayoutField"/> to the input
    /// element appropriate to its <see cref="ControlType"/>.
    /// </summary>
    /// <remarks>
    /// Phase 1a is layout-only and renders the master area only. Detail grids
    /// (<see cref="FormLayout.Details"/>) are wired up in Phase 1b together with
    /// <c>DynamicGrid</c>.
    /// </remarks>
    public partial class DynamicForm : ComponentBase
    {
        private static readonly ListItem[] _emptyOptions = Array.Empty<ListItem>();

        /// <summary>
        /// Gets or sets the form layout that drives the rendering loop.
        /// </summary>
        [Parameter]
        public FormLayout? Layout { get; set; }

        /// <summary>
        /// Gets or sets the data object that backs two-way binding for each input.
        /// </summary>
        [Parameter]
        public FormDataObject? DataObject { get; set; }

        /// <summary>
        /// Gets or sets the inline id prefix applied to every rendered input. Useful for
        /// hosting pages that embed multiple <see cref="DynamicForm"/> instances on the
        /// same page and need unique DOM ids.
        /// </summary>
        [Parameter]
        public string IdPrefix { get; set; } = "bee-form";

        private IEnumerable<LayoutSection> EnumerateSections()
            => Layout?.Sections ?? Enumerable.Empty<LayoutSection>();

        private static IEnumerable<LayoutField> EnumerateFields(LayoutSection section)
            => section.Fields?.Where(f => f.Visible) ?? Enumerable.Empty<LayoutField>();

        private IEnumerable<ListItem> EnumerateOptions(LayoutField field)
        {
            var formField = DataObject?.GetFormField(field.FieldName);
            return formField?.ListItems ?? (IEnumerable<ListItem>)_emptyOptions;
        }

        private string FieldInputId(LayoutField field)
            => string.Create(CultureInfo.InvariantCulture, $"{IdPrefix}-{field.FieldName}");

        private string BuildGridStyle()
        {
            var columns = Layout?.ColumnCount ?? 1;
            if (columns < 1) columns = 1;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"display:grid;grid-template-columns:repeat({columns},minmax(0,1fr));gap:8px");
        }

        private static string BuildFieldStyle(LayoutField field)
        {
            var rowSpan = field.RowSpan < 1 ? 1 : field.RowSpan;
            var colSpan = field.ColumnSpan < 1 ? 1 : field.ColumnSpan;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"grid-row:span {rowSpan};grid-column:span {colSpan}");
        }
    }
}
