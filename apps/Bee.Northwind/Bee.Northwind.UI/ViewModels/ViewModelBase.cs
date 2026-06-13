using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.Northwind.UI.ViewModels;

/// <summary>
/// Marker base type used by <see cref="ViewLocator"/> to recognise our view models.
/// Derives from <see cref="ObservableObject"/> so child VMs get the standard
/// <c>INotifyPropertyChanged</c> surface for free.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
