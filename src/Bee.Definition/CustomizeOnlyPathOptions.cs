namespace Bee.Definition
{
    /// <summary>
    /// Path options for the tenant customization-override layer. Resolves the three
    /// customizable artifact paths (Language, FormLayout, ProgramSettings) strictly under
    /// <c>{CustomizePath}/{custCode}/</c> and never falls back to the base
    /// <see cref="PathOptions.DefinePath"/>.
    /// </summary>
    /// <remarks>
    /// Only <see cref="GetLanguageFilePath"/>, <see cref="GetFormLayoutFilePath"/> and
    /// <see cref="GetProgramSettingsFilePath"/> are overridden — the override layer serves only
    /// those three types. Any other path method inherited from <see cref="PathOptions"/> resolves
    /// against an empty <see cref="PathOptions.DefinePath"/> and must not be used by override-layer code.
    /// </remarks>
    public sealed class CustomizeOnlyPathOptions : PathOptions
    {
        private readonly string _customizeRoot;

        /// <summary>
        /// Initializes a new <see cref="CustomizeOnlyPathOptions"/> rooted at
        /// <c>{customizePath}/{custCode}</c>.
        /// </summary>
        /// <param name="customizePath">The customization root directory (<see cref="PathOptions.CustomizePath"/>).</param>
        /// <param name="custCode">The tenant customization code; becomes the per-tenant subfolder name.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="customizePath"/> or <paramref name="custCode"/> is empty, or when
        /// <paramref name="custCode"/> would escape the customization root (path traversal).
        /// </exception>
        public CustomizeOnlyPathOptions(string customizePath, string custCode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(customizePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(custCode);

            // The custCode arrives from the identity chain and is relatively trusted, but it
            // becomes a directory name, so it must be validated before being combined into a
            // filesystem path (see scanning.md, Path Traversal section). Both slash variants are
            // rejected regardless of platform: a backslash is a separator on Windows and is
            // suspicious in a customization code everywhere else.
            if (custCode.Contains("..", StringComparison.Ordinal)
                || custCode.Contains('/')
                || custCode.Contains('\\'))
            {
                throw new ArgumentException($"The customization code '{custCode}' contains illegal path characters.", nameof(custCode));
            }

            string root = System.IO.Path.GetFullPath(customizePath);
            string resolved = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, custCode));
            string rootWithSeparator = root.EndsWith(System.IO.Path.DirectorySeparatorChar)
                ? root
                : root + System.IO.Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(rootWithSeparator, StringComparison.Ordinal))
                throw new ArgumentException($"The customization code '{custCode}' resolves outside the customization root.", nameof(custCode));

            _customizeRoot = resolved;
        }

        /// <inheritdoc/>
        public override string GetProgramSettingsFilePath()
            => System.IO.Path.Combine(_customizeRoot, "ProgramSettings.xml");

        /// <inheritdoc/>
        public override string GetFormLayoutFilePath(string layoutId)
            => System.IO.Path.Combine(_customizeRoot, "FormLayout", $"{layoutId}.FormLayout.xml");

        /// <inheritdoc/>
        public override string GetLanguageFilePath(string lang, string ns)
            => System.IO.Path.Combine(_customizeRoot, "Language", lang, $"{ns}.Language.xml");
    }
}
