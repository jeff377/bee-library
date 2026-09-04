using Microsoft.Data.SqlClient;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Settings;

namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// 一份只屬於單一測試類別的 <see cref="DatabaseSettings"/>，供直接建構
    /// <c>DbConnectionManagerService</c> 用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這個型別存在的理由是一次實測到的 flaky。<c>DbConnectionManagerTests</c> 與
    /// <c>DbAccessFactoryTests</c> 原本都拿 <c>IDefineAccess.GetDatabaseSettings()</c>
    /// 回傳的<b>快取實例</b>做 <c>Items.Add/Remove</c>，兩個類別各自掛
    /// <c>IClassFixture</c>、xUnit 平行執行 —— <c>KeyCollectionBase&lt;T&gt;</c> 不是
    /// 執行緒安全的，<c>RemoveItem</c> 取完 <c>this[index]</c> 後 <c>List.RemoveAt(index)</c>
    /// 就撞上索引越界。同一份測試連跑兩次，第二次紅。
    /// </para>
    /// <para>
    /// 當時的類別註解寫著「使用唯一 databaseId 以避免與其他測試共用的全域快取互相干擾」——
    /// <b>唯一的 key 只避開 key 碰撞，避不開集合層級的競態</b>。
    /// </para>
    /// <para>
    /// 修法沒有選「把兩個類別掛同一個 <c>[Collection]</c> 序列化」，而是根本不碰快取：
    /// <c>.claude/rules/definition.md</c> 要求 process-wide 快取實例載入後不可異動，
    /// 序列化只是讓違規不再產生症狀。而且這兩個類別測的是連線字串組裝，本來就不需要資料庫。
    /// </para>
    /// </remarks>
    internal sealed class IsolatedDatabaseSettingsProvider : IDatabaseSettingsProvider
    {
        public DatabaseSettings Settings { get; } = new();

        /// <inheritdoc/>
        public DatabaseSettings Get() => Settings;

        /// <inheritdoc/>
        public DatabaseItem GetItem(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId));
            if (Settings.Items == null || !Settings.Items.Contains(databaseId))
                throw new KeyNotFoundException($"DatabaseItem '{databaseId}' not found.");

            return Settings.Items[databaseId];
        }

        /// <inheritdoc/>
        public void ValidateRequired()
        {
            if (Settings.Items == null || !Settings.Items.Contains(DbCategoryIds.Common))
                throw new InvalidOperationException(
                    $"DatabaseSettings must contain a DatabaseItem with Id='{DbCategoryIds.Common}'.");
        }
    }

    /// <summary>
    /// 讓不掛 fixture 的測試也拿得到 <see cref="DatabaseType.SQLServer"/> 的 provider factory。
    /// </summary>
    /// <remarks>
    /// <c>DbConnectionManagerService.CreateConnectionInfo</c> 會向 <see cref="DbProviderRegistry"/>
    /// 取 factory，而那是 process-wide 的註冊表、平常由 fixture 在啟動時填。註冊的是
    /// <c>SqlClientFactory.Instance</c> —— 與 fixture 註冊的是同一個實例，所以重複註冊是冪等的，
    /// 不會改變任何其他測試看到的值。<see cref="DbProviderRegistry"/> 自己的 WARNING 也明載
    /// 測試會反覆註冊、其他測試同時讀，backing store 因此必須是 concurrent 的。
    /// </remarks>
    internal static class TestDbProviders
    {
        internal static void EnsureSqlServerRegistered()
            => DbProviderRegistry.Register(DatabaseType.SQLServer, SqlClientFactory.Instance);
    }
}
