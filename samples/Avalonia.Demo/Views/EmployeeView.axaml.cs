using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Demo.Views
{
    /// <summary>
    /// Code-behind for <see cref="EmployeeView"/>. Only loads the XAML — the
    /// page just hosts a <c>FormView</c> with <c>ProgId="Employee"</c> and
    /// lets <see cref="Bee.UI.Core.ClientInfo"/> fallback do the rest.
    /// </summary>
    public partial class EmployeeView : UserControl
    {
        /// <summary>Loads the XAML.</summary>
        public EmployeeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
