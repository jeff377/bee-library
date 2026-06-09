using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Demo.Views
{
    /// <summary>
    /// Application main window. Hosts the navigation <see cref="ContentControl"/>
    /// bound to <c>CurrentView</c> on <see cref="ViewModels.MainWindowViewModel"/>;
    /// the actual page swapping is driven entirely by the VM.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>Loads the XAML.</summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
