using Bee.Definition;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that redirects <see cref="BackendInfo.DefinePath"/> to a temporary
    /// directory for the duration of a test, then restores the original path and deletes
    /// the temp directory on dispose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Any test that calls a <c>SaveDefine</c>-family method (e.g. <c>SaveDbSchemaSettings</c>,
    /// <c>SaveSystemSettings</c>, <c>SaveTableSchema</c>) — directly via
    /// <see cref="LocalDefineAccess"/> or indirectly via <c>SystemBusinessObject.SaveDefine</c> —
    /// MUST wrap the call in <c>using var temp = new TempDefinePath();</c>, otherwise the
    /// production fixture files under <c>tests/Define/</c> will be overwritten and cause
    /// subsequent tests that read those fixtures to fail with stale or empty data.
    /// </para>
    /// <para>
    /// Single-process safe; not safe across processes (relies on the test runner running
    /// within one AppDomain). xUnit's collection-level parallelism does not affect this
    /// because <see cref="BackendInfo.DefinePath"/> is global state — tests using this
    /// helper should already participate in the <c>"Initialize"</c> collection or another
    /// collection that serializes against fixture-mutating tests.
    /// </para>
    /// </remarks>
    public sealed class TempDefinePath : IDisposable
    {
        private readonly string _original;

        /// <summary>
        /// Gets the absolute path of the temporary <c>DefinePath</c> directory created for
        /// this scope.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new temporary directory under the OS temp folder and switches
        /// <see cref="BackendInfo.DefinePath"/> to point at it.
        /// </summary>
        public TempDefinePath()
        {
            _original = BackendInfo.DefinePath;
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            BackendInfo.DefinePath = Path;
        }

        /// <summary>
        /// Restores the original <see cref="BackendInfo.DefinePath"/> and best-effort
        /// deletes the temporary directory.
        /// </summary>
        public void Dispose()
        {
            BackendInfo.DefinePath = _original;
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
