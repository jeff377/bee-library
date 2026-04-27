namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Interface for a UI control.
    /// </summary>
    public interface IUIControl
    {
        /// <summary>
        /// Sets the control state based on the form mode.
        /// </summary>
        /// <param name="formMode">The single-record form mode.</param>
        void SetControlState(SingleFormMode formMode);
    }
}
