using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Demo.ViewModels;
using Avalonia.Demo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.Demo
{
    /// <summary>
    /// Application root. <see cref="OnFrameworkInitializationCompleted"/> creates the
    /// <see cref="MainWindow"/> and seeds it with a <see cref="MainWindowViewModel"/>;
    /// navigation between Connection → Login → Employee is then driven by the VM.
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc/>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
