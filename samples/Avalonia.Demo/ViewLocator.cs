using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Demo.ViewModels;

namespace Avalonia.Demo
{
    /// <summary>
    /// Convention-based view locator: for a <c>FooViewModel</c> it instantiates the
    /// <c>FooView</c> in the same assembly by string-replacing the type name.
    /// Wired into <see cref="App"/>'s <c>DataTemplates</c> so any
    /// <see cref="ContentControl"/> bound to a <see cref="ViewModelBase"/> renders
    /// its paired view automatically.
    /// </summary>
    public sealed class ViewLocator : IDataTemplate
    {
        /// <inheritdoc/>
        public Control? Build(object? data)
        {
            if (data is null) return null;

            var name = data.GetType().FullName!
                .Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);
            if (type is null)
                return new TextBlock { Text = "View not found: " + name };

            return (Control)Activator.CreateInstance(type)!;
        }

        /// <inheritdoc/>
        public bool Match(object? data) => data is ViewModelBase;
    }
}
