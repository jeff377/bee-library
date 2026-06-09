using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Demo.Views
{
    /// <summary>
    /// Code-behind for <see cref="ConnectionView"/>. Only loads the XAML — all
    /// behaviour lives on <see cref="ViewModels.ConnectionViewModel"/>.
    /// </summary>
    public partial class ConnectionView : UserControl
    {
        /// <summary>Loads the XAML.</summary>
        public ConnectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
