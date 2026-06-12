using Avalonia.Controls;

namespace Avalonia.Demo.Views
{
    /// <summary>
    /// Code-behind for <see cref="FormsView"/>. Only loads the XAML — the page
    /// hosts one <c>FormView</c> per demo form in a tab strip and the framework
    /// controls handle data access end to end.
    /// </summary>
    public partial class FormsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormsView"/>.
        /// </summary>
        public FormsView()
        {
            InitializeComponent();
        }
    }
}
