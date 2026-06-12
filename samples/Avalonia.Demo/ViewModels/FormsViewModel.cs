namespace Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Third (terminal) step of the demo flow. Intentionally empty — the paired
    /// <c>FormsView</c> hosts one <c>FormView</c> per demo form (Employee /
    /// Department / Project) in a tab strip, and each form's built-in error label
    /// surfaces any backend issue, so no VM-side state is needed beyond the type
    /// marker that drives <see cref="ViewLocator"/> routing.
    /// </summary>
    public class FormsViewModel : ViewModelBase
    {
    }
}
