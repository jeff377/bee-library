using Bee.Definition;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that creates a per-instance <see cref="PathOptions"/> pointing at a fresh
    /// temporary directory, and (for the transitional cache layer that still reads
    /// <see cref="DefinePathInfo"/> statically) swaps the global facade for the scope's lifetime.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PR 5.2 migrated <c>FileDefineStorage</c> / <c>LocalDefineAccess</c> / <c>MasterKeyProvider</c>
    /// to ctor-inject <see cref="PathOptions"/>. Tests that construct those types directly
    /// should pass <see cref="Options"/> instead of relying on the swap. The legacy
    /// <see cref="DefinePathInfo"/> swap is preserved until PR 5.3 / 5.4 finishes migrating
    /// the cache layer and test fixtures.
    /// </para>
    /// <para>
    /// Single-process safe; not safe across processes (relies on the test runner running
    /// within one AppDomain). xUnit's collection-level parallelism does not affect this
    /// because <see cref="DefinePathInfo"/> still holds global state — tests using this
    /// helper should participate in the <c>"Initialize"</c> collection (or another that
    /// serializes against fixture-mutating tests) for the cache-layer interactions.
    /// </para>
    /// </remarks>
    public sealed class TempDefinePath : IDisposable
    {
        private readonly PathOptions _original;

        /// <summary>
        /// Gets the absolute path of the temporary <c>DefinePath</c> directory created for
        /// this scope.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the <see cref="PathOptions"/> instance bound to <see cref="Path"/>. Inject
        /// this directly into <c>FileDefineStorage</c> / <c>LocalDefineAccess</c> /
        /// <c>SystemSettingsLoader</c> calls inside the test body.
        /// </summary>
        public PathOptions Options { get; }

        /// <summary>
        /// Creates a new temporary directory under the OS temp folder, binds a fresh
        /// <see cref="PathOptions"/> to it, and switches the legacy <see cref="DefinePathInfo"/>
        /// facade to point at the same options for cache-layer compatibility.
        /// </summary>
        public TempDefinePath()
        {
            _original = DefinePathInfo.CurrentOptions;
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            Options = new PathOptions { DefinePath = Path };
            DefinePathInfo.Initialize(Options);
        }

        /// <summary>
        /// Restores the original <see cref="DefinePathInfo"/> state and best-effort
        /// deletes the temporary directory.
        /// </summary>
        public void Dispose()
        {
            DefinePathInfo.Initialize(_original);
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // 測試完整性優先於暫存清理；偶發鎖定不應讓測試失敗。
            }
        }
    }
}
