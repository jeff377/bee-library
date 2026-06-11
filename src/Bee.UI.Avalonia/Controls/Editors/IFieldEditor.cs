using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Contract shared by the field editor controls that pair each
    /// <see cref="ControlType"/> with an Avalonia control bound to a
    /// <see cref="FormDataObject"/> field. Extends the definition-layer
    /// <see cref="IBindFieldControl"/> / <see cref="IUIControl"/> contracts with the
    /// explicit binding surface used by <c>DynamicForm</c> and code-behind callers.
    /// </summary>
    /// <remarks>
    /// Editors can alternatively bind ambiently: place them under a container whose
    /// <see cref="FormScope.DataObjectProperty"/> is set and assign
    /// <see cref="IBindFieldControl.FieldName"/>; the editor binds itself when it
    /// attaches to the logical tree. Detaching from the tree releases the binding,
    /// so explicitly bound editors that are re-attached must be bound again.
    /// </remarks>
    public interface IFieldEditor : IBindFieldControl, IUIControl
    {
        /// <summary>
        /// Binds the editor to <paramref name="dataObject"/> using the field name and
        /// rendering attributes (read-only state, spans) carried by <paramref name="field"/>.
        /// </summary>
        /// <param name="dataObject">The data object that backs two-way binding.</param>
        /// <param name="field">The layout field that drives editor metadata.</param>
        void Bind(FormDataObject dataObject, LayoutField field);

        /// <summary>
        /// Binds the editor to <paramref name="dataObject"/> by field name only.
        /// Schema metadata (<see cref="Bee.Definition.Forms.FormField"/>) still applies;
        /// layout-level attributes do not.
        /// </summary>
        /// <param name="dataObject">The data object that backs two-way binding.</param>
        /// <param name="fieldName">The field (column) name to bind.</param>
        void Bind(FormDataObject dataObject, string fieldName);

        /// <summary>
        /// Releases the binding and unsubscribes from data object events.
        /// </summary>
        void Unbind();
    }
}
