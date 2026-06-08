using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Bee.DefineEditor.Services;

namespace Bee.DefineEditor.Markup;

/// <summary>
/// XAML markup extension that binds a property to a localized string. Usage:
/// <code>
/// &lt;TextBlock Text="{loc:Loc Menu_File}"/&gt;
/// </code>
/// </summary>
/// <remarks>
/// Returns an <see cref="IObservable{T}"/> wrapped via Avalonia's
/// <c>.ToBinding()</c> extension. This is the pattern community Avalonia
/// localization libraries (e.g. Jeek.Avalonia.Localization) use — it's the
/// binding shape whose value Avalonia genuinely re-pushes to the bound target
/// whenever the producer emits a new value, including for bindings nested
/// inside DataTemplate-instantiated content that earlier
/// <see cref="System.ComponentModel.INotifyPropertyChanged"/>-based
/// approaches couldn't refresh.
/// </remarks>
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new LocalizedStringObservable(LocalizationService.Current, Key).ToBinding();
    }

    private sealed class LocalizedStringObservable : IObservable<string>
    {
        private readonly LocalizationService _service;
        private readonly string _key;

        public LocalizedStringObservable(LocalizationService service, string key)
        {
            _service = service;
            _key = key;
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            void Publish() => observer.OnNext(_service[_key]);

            EventHandler<CultureInfo> handler = (_, _) => Publish();

            Publish();
            _service.CultureChanged += handler;

            return new Subscription(_service, handler);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly LocalizationService _service;
            private readonly EventHandler<CultureInfo> _handler;

            public Subscription(LocalizationService service, EventHandler<CultureInfo> handler)
            {
                _service = service;
                _handler = handler;
            }

            public void Dispose() => _service.CultureChanged -= _handler;
        }
    }
}
