namespace Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Third (terminal) step of the demo flow. Intentionally empty — the paired
    /// <c>EmployeeView</c> hosts <c>FormView ProgId="Employee"</c> directly and
    /// the form's built-in error label surfaces any backend issue, so no VM-side
    /// state is needed beyond the type marker that drives
    /// <see cref="ViewLocator"/> routing.
    /// </summary>
    public class EmployeeViewModel : ViewModelBase
    {
    }
}
