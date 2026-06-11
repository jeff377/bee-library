using Avalonia;
using Avalonia.Controls;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Attached inherited properties that give field editors an ambient binding scope.
    /// Set <see cref="DataObjectProperty"/> once on a container and every descendant
    /// editor with a <see cref="IBindFieldControl.FieldName"/> binds itself when it
    /// attaches to the logical tree; <see cref="FormModeProperty"/> propagates the
    /// form mode so editors adjust their enabled/read-only state in one place.
    /// </summary>
    public sealed class FormScope
    {
        private FormScope()
        {
        }

        /// <summary>
        /// Identifies the ambient <see cref="FormDataObject"/> attached property.
        /// Inherited down the logical tree.
        /// </summary>
        public static readonly AttachedProperty<FormDataObject?> DataObjectProperty =
            AvaloniaProperty.RegisterAttached<FormScope, Control, FormDataObject?>(
                "DataObject", defaultValue: null, inherits: true);

        /// <summary>
        /// Identifies the ambient <see cref="SingleFormMode"/> attached property.
        /// Inherited down the logical tree; defaults to <see cref="SingleFormMode.Edit"/>.
        /// </summary>
        public static readonly AttachedProperty<SingleFormMode> FormModeProperty =
            AvaloniaProperty.RegisterAttached<FormScope, Control, SingleFormMode>(
                "FormMode", defaultValue: SingleFormMode.Edit, inherits: true);

        /// <summary>
        /// Gets the ambient <see cref="FormDataObject"/> for <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to read the value from.</param>
        public static FormDataObject? GetDataObject(Control element)
        {
            return element.GetValue(DataObjectProperty);
        }

        /// <summary>
        /// Sets the ambient <see cref="FormDataObject"/> on <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to set the value on.</param>
        /// <param name="value">The data object shared with descendant editors.</param>
        public static void SetDataObject(Control element, FormDataObject? value)
        {
            element.SetValue(DataObjectProperty, value);
        }

        /// <summary>
        /// Gets the ambient <see cref="SingleFormMode"/> for <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to read the value from.</param>
        public static SingleFormMode GetFormMode(Control element)
        {
            return element.GetValue(FormModeProperty);
        }

        /// <summary>
        /// Sets the ambient <see cref="SingleFormMode"/> on <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to set the value on.</param>
        /// <param name="value">The form mode shared with descendant editors.</param>
        public static void SetFormMode(Control element, SingleFormMode value)
        {
            element.SetValue(FormModeProperty, value);
        }
    }
}
