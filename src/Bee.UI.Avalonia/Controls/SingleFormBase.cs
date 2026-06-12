using Avalonia;
using Avalonia.Controls;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Base class for single-record data forms — the only form kind that carries a
    /// <see cref="SingleFormMode"/>. Owns the <see cref="FormMode"/> property and
    /// broadcasts every change through <see cref="FormScope.FormModeProperty"/>, so
    /// all descendant field editors and grids switch to the state appropriate for
    /// the mode (<see cref="IUIControl.SetControlState"/>). When and why the mode
    /// changes (load → View, new → Add, save → View) is the derived form's
    /// responsibility, tied to its CRUD flow.
    /// </summary>
    public abstract class SingleFormBase : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="FormMode"/> styled property.
        /// </summary>
        public static readonly StyledProperty<SingleFormMode> FormModeProperty =
            AvaloniaProperty.Register<SingleFormBase, SingleFormMode>(
                nameof(FormMode), SingleFormMode.View);

        static SingleFormBase()
        {
            FormModeProperty.Changed.AddClassHandler<SingleFormBase>((o, e) =>
            {
                var formMode = (SingleFormMode)e.NewValue!;
                FormScope.SetFormMode(o, formMode);
                o.OnFormModeChanged(formMode);
            });
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SingleFormBase"/> in
        /// <see cref="SingleFormMode.View"/> mode.
        /// </summary>
        protected SingleFormBase()
        {
            // The ambient scope defaults to Edit so standalone editors outside any
            // data form stay editable. A data form owns the mode, so the scope is
            // pinned to the initial View here — the property change handler cannot
            // cover this because the default value never raises a change.
            FormScope.SetFormMode(this, FormMode);
        }

        /// <summary>
        /// Gets or sets the form mode. Defaults to <see cref="SingleFormMode.View"/>;
        /// every change is broadcast to descendant editors and grids through the
        /// ambient <see cref="FormScope"/>.
        /// </summary>
        public SingleFormMode FormMode
        {
            get => GetValue(FormModeProperty);
            set => SetValue(FormModeProperty, value);
        }

        /// <summary>
        /// Called after the form mode changed and was broadcast to the scope.
        /// Derived forms refresh mode-dependent chrome (toolbar enablement) here.
        /// </summary>
        /// <param name="formMode">The new form mode.</param>
        protected virtual void OnFormModeChanged(SingleFormMode formMode)
        {
        }
    }
}
