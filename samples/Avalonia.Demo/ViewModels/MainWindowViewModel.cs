using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Top-level VM that drives the Connection → Login → Forms flow by swapping
    /// <see cref="CurrentView"/>. Each child VM gets a callback action to advance to
    /// the next step; using callbacks (rather than CLR events) keeps the navigation
    /// graph explicit at construction time and avoids forgetting to unsubscribe.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets or sets the currently displayed view model. <see cref="ViewLocator"/>
        /// in <see cref="App"/>'s data templates resolves this to a concrete view.
        /// </summary>
        [ObservableProperty]
        private ViewModelBase _currentView;

        /// <summary>
        /// Initialises the navigation graph and lands on the Connection step.
        /// </summary>
        public MainWindowViewModel()
        {
            _currentView = new ConnectionViewModel(NavigateToLogin);
        }

        private void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(NavigateToForms);
        }

        private void NavigateToForms()
        {
            CurrentView = new FormsViewModel();
        }
    }
}
