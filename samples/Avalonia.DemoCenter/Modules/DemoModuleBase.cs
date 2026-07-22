using Avalonia.Controls;

namespace Avalonia.DemoCenter.Modules
{
    /// <summary>
    /// Base class for demo modules. Implements <see cref="GetSourceText"/> by reading the
    /// module's own <c>.cs</c> from an embedded resource, so the View Source panel always
    /// shows the real, current source.
    /// </summary>
    /// <remarks>
    /// The csproj embeds <c>Modules/**/*.cs</c> as resources. With the project's
    /// <c>RootNamespace</c> mirroring the folder layout (enforced by the code-style folder
    /// ↔ namespace rule), a type's embedded resource name equals its full type name plus
    /// <c>.cs</c> — e.g. <c>Avalonia.DemoCenter.Modules.EditorsComparisonModule.cs</c>.
    /// </remarks>
    public abstract class DemoModuleBase : IDemoModule
    {
        /// <inheritdoc/>
        public abstract string Category { get; }

        /// <inheritdoc/>
        public abstract string Title { get; }

        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public abstract Control BuildView();

        /// <summary>
        /// The embedded-resource logical name of this module's source file. Defaults to the
        /// full type name plus <c>.cs</c>; override only when a module's source lives in a
        /// file whose name differs from the type.
        /// </summary>
        protected virtual string SourceResourceName => GetType().FullName + ".cs";

        /// <inheritdoc/>
        public string GetSourceText()
        {
            var assembly = GetType().Assembly;
            var resourceName = SourceResourceName;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                var available = string.Join(Environment.NewLine, assembly.GetManifestResourceNames());
                return $"// Source resource not found: {resourceName}"
                    + Environment.NewLine + "// Available resources:" + Environment.NewLine + available;
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
