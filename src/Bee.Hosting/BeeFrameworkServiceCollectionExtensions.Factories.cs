using Bee.Base;
using Bee.Business.Providers;
using Bee.Definition;
using Bee.ObjectCaching;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting
{
    /// <summary>
    /// Constructing the pluggable services `AddBeeFramework` registers, from the type names the
    /// backend configuration names.
    /// </summary>
    /// <remarks>
    /// Kept apart from the registration list so that reading "what gets registered" does not mean
    /// scrolling through "how each one is built". `SecurityKeys` lives here — it exists only as the
    /// return shape of `DecryptSecurityKeys`.
    /// </remarks>
    public static partial class BeeFrameworkServiceCollectionExtensions
    {
        /// <summary>
        /// Resolves the configured <see cref="IDefineAccess"/> implementation. Supports
        /// <c>(IDefineStorage, PathOptions, ICacheContainer, byte[], ICustomizeDefineReader)</c>
        /// (used by <c>CacheDefineAccess</c> with the customization overlay),
        /// <c>(IDefineStorage, PathOptions, ICacheContainer, byte[])</c>,
        /// <c>(IDefineStorage, PathOptions)</c>, <c>(IDefineStorage)</c> (legacy), and
        /// parameterless ctors.
        /// </summary>
        private static IDefineAccess ResolveDefineAccess(string? typeName, IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey, ICustomizeDefineReader customizeReader)
        {
            var resolvedName = string.IsNullOrWhiteSpace(typeName) ? BackendDefaultTypes.DefineAccess : typeName;
            var type = AssemblyLoader.GetType(resolvedName)
                ?? throw new InvalidOperationException($"IDefineAccess type '{resolvedName}' not found.");

            var ctorWithReader = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions), typeof(ICacheContainer), typeof(byte[]), typeof(ICustomizeDefineReader) });
            if (ctorWithReader != null)
                return (IDefineAccess)ctorWithReader.Invoke(new object[] { storage, paths, cache, configEncryptionKey, customizeReader });

            var ctorFull = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions), typeof(ICacheContainer), typeof(byte[]) });
            if (ctorFull != null)
                return (IDefineAccess)ctorFull.Invoke(new object[] { storage, paths, cache, configEncryptionKey });

            var ctorPaths = type.GetConstructor(new[] { typeof(IDefineStorage), typeof(PathOptions) });
            if (ctorPaths != null)
                return (IDefineAccess)ctorPaths.Invoke(new object[] { storage, paths });

            var ctorWithStorage = type.GetConstructor(new[] { typeof(IDefineStorage) });
            if (ctorWithStorage != null)
                return (IDefineAccess)ctorWithStorage.Invoke(new object[] { storage });

            return (IDefineAccess?)Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to construct IDefineAccess: {resolvedName}");
        }

        /// <summary>
        /// Constructs the configured <see cref="IDefineStorage"/> implementation. Prefers
        /// the <c>(PathOptions)</c> ctor (used by <see cref="FileDefineStorage"/> after
        /// Phase 5 PR 5.2); falls back to a parameterless ctor for legacy implementations.
        /// </summary>
        private static IDefineStorage CreateDefineStorage(string? configured, string fallback, IServiceProvider sp, PathOptions paths)
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"IDefineStorage type '{typeName}' not found.");

            // Prefer an (IServiceProvider) ctor — used by DB-backed storage (e.g. DbDefineStorage),
            // which resolves its dependencies lazily to avoid a construction cycle through
            // IDbConnectionManager → IDatabaseSettingsProvider → IDefineAccess → IDefineStorage.
            var ctorWithServiceProvider = type.GetConstructor(new[] { typeof(IServiceProvider) });
            if (ctorWithServiceProvider != null)
                return (IDefineStorage)ctorWithServiceProvider.Invoke(new object[] { sp });

            var ctorWithPaths = type.GetConstructor(new[] { typeof(PathOptions) });
            if (ctorWithPaths != null)
                return (IDefineStorage)ctorWithPaths.Invoke(new object[] { paths });

            return (AssemblyLoader.CreateInstance(typeName) as IDefineStorage)
                ?? throw new InvalidOperationException($"Failed to construct IDefineStorage: {typeName}");
        }

        /// <summary>
        /// Creates a configurable service whose implementation type is read from configuration.
        /// Tries DI-aware construction first (ctor params resolved from <paramref name="sp"/>);
        /// falls back to parameterless construction via <see cref="AssemblyLoader"/>.
        /// </summary>
        private static T CreateConfigurableService<T>(IServiceProvider sp, string? configured, string fallback)
            where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for service '{typeof(T).Name}'.");

            // Try DI-aware construction first — ActivatorUtilities resolves any ctor parameters
            // from the service provider. Falls back to AssemblyLoader.CreateInstance for legacy
            // parameterless ctors.
            try
            {
                return (T)ActivatorUtilities.CreateInstance(sp, type);
            }
            catch (InvalidOperationException)
            {
                return (AssemblyLoader.CreateInstance(typeName) as T)
                    ?? throw new InvalidOperationException($"Failed to construct {typeof(T).Name}: {typeName}");
            }
        }

        /// <summary>
        /// Creates the configured <see cref="IApiEncryptionKeyProvider"/>. The static and derived
        /// providers receive the decrypted API key byte[] directly (as the shared key and as root
        /// key material respectively), the derived one falling back to the master key when no API
        /// key is configured; the dynamic provider relies on <see cref="ISessionInfoService"/>
        /// resolved through DI.
        /// </summary>
        private static IApiEncryptionKeyProvider CreateApiEncryptionKeyProvider(IServiceProvider sp, string? configured, SecurityKeys keys)
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? BackendDefaultTypes.ApiEncryptionKeyProvider : configured;
            var type = AssemblyLoader.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found for IApiEncryptionKeyProvider.");

            if (type == typeof(StaticApiEncryptionKeyProvider))
                return new StaticApiEncryptionKeyProvider(keys.ApiEncryptionKey);
            if (type == typeof(DerivedApiEncryptionKeyProvider))
            {
                // This is the default provider, so a deployment that never configured
                // ApiEncryptionKey lands here. Falling back to a root key derived from the master
                // key keeps it working out of the box; an explicitly configured key wins.
                return keys.ApiEncryptionKey.Length > 0
                    ? new DerivedApiEncryptionKeyProvider(keys.ApiEncryptionKey)
                    : DerivedApiEncryptionKeyProvider.FromMasterKey(keys.MasterKey);
            }
            return (IApiEncryptionKeyProvider)ActivatorUtilities.CreateInstance(sp, type);
        }

        /// <summary>
        /// Decrypts the security keys the framework consumes from <paramref name="settings"/> in one
        /// pass using the master key. Empty entries map to empty byte arrays so downstream
        /// crypto paths see a consistent "no key configured" sentinel.
        /// </summary>
        /// <remarks>
        /// NOTE: <c>SecurityKeySettings.CookieEncryptionKey</c> and
        /// <c>SecurityKeySettings.DatabaseEncryptionKey</c> are deliberately not decrypted here.
        /// Nothing in the framework reads either one, so decrypting them produced two byte arrays
        /// that were dropped on the floor. Add them back when a consumer exists — not before, or
        /// the bundle grows fields again with nowhere to go.
        /// </remarks>
        private static SecurityKeys DecryptSecurityKeys(SecurityKeySettings settings, string definePath, bool autoCreateMasterKey)
        {
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource, definePath, autoCreateMasterKey);

            return new SecurityKeys(
                MasterKey: masterKey,
                ApiEncryptionKey: Decrypt(masterKey, settings.ApiEncryptionKey),
                ConfigEncryptionKey: Decrypt(masterKey, settings.ConfigEncryptionKey));

            static byte[] Decrypt(byte[] masterKey, string? encryptedKey)
                => StringUtilities.IsNotEmpty(encryptedKey)
                    ? EncryptionKeyProtector.DecryptEncryptedKey(masterKey, encryptedKey!)
                    : Array.Empty<byte>();
        }

        /// <summary>
        /// Decrypted security keys bundle. Each field is the 64-byte combined AES + HMAC
        /// key (or empty when not configured).
        /// </summary>
        private readonly record struct SecurityKeys(
            byte[] MasterKey,
            byte[] ApiEncryptionKey,
            byte[] ConfigEncryptionKey);
    }
}
