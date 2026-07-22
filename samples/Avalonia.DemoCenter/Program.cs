namespace Avalonia.DemoCenter
{
    /// <summary>
    /// Process entry point. The demo center is fully in-memory (no backend, no Bee
    /// client singletons), so it only configures the Avalonia classic-desktop lifetime.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Application entry point. <c>STAThread</c> is required by Windows for
        /// Avalonia to drive native dialogs / drag-drop / clipboard.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Builds the Avalonia <see cref="AppBuilder"/>. Kept as a separate method
        /// so previewer / visual-tree tooling can reuse the same setup.
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
