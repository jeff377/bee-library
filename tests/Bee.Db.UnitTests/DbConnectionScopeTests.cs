using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace Bee.Db.UnitTests
{
    public class DbConnectionScopeTests
    {
        [Fact]
        [DisplayName("Create externalConnection=null 且 factory=null 應擲 ArgumentNullException")]
        public void Create_NullFactoryAndNullExternal_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DbConnectionScope.Create(null, null!, "irrelevant"));
        }

        [Fact]
        [DisplayName("CreateAsync externalConnection=null 且 factory=null 應擲 ArgumentNullException")]
        public async Task CreateAsync_NullFactoryAndNullExternal_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await DbConnectionScope.CreateAsync(null, null!, "irrelevant"));
        }

        [Fact]
        [DisplayName("外部連線為 Open 狀態時 Create 不會重新開啟，且 Dispose 不關閉連線")]
        public void Create_ExternalOpenConnection_NotReopenedNotClosed()
        {
            var fake = new FakeDbConnection { CurrentState = ConnectionState.Open };

            using (var scope = DbConnectionScope.Create(fake, null!, "ignored"))
            {
                Assert.Same(fake, scope.Connection);
                Assert.Equal(0, fake.OpenCount);
            }

            Assert.Equal(0, fake.DisposeCount);
            Assert.Equal(ConnectionState.Open, fake.State);
        }

        [Fact]
        [DisplayName("外部連線為 Closed 狀態時 Create 會開啟，但 Dispose 不關閉")]
        public void Create_ExternalClosedConnection_OpenedButNotDisposed()
        {
            var fake = new FakeDbConnection { CurrentState = ConnectionState.Closed };

            using (var scope = DbConnectionScope.Create(fake, null!, "ignored"))
            {
                Assert.Same(fake, scope.Connection);
                Assert.Equal(1, fake.OpenCount);
            }

            Assert.Equal(0, fake.DisposeCount);
        }

        [Fact]
        [DisplayName("CreateAsync 外部連線為 Closed 狀態時應呼叫 OpenAsync")]
        public async Task CreateAsync_ExternalClosedConnection_OpensAsync()
        {
            var fake = new FakeDbConnection { CurrentState = ConnectionState.Closed };

            using (var scope = await DbConnectionScope.CreateAsync(fake, null!, "ignored"))
            {
                Assert.Same(fake, scope.Connection);
                Assert.Equal(1, fake.OpenAsyncCount);
            }

            Assert.Equal(0, fake.DisposeCount);
        }

        // 最小可用的 DbConnection 實作，用來模擬 State 與 Open/Dispose 行為
        private sealed class FakeDbConnection : DbConnection
        {
            public ConnectionState CurrentState { get; set; } = ConnectionState.Closed;
            public int OpenCount { get; private set; }
            public int OpenAsyncCount { get; private set; }
            public int DisposeCount { get; private set; }

            [System.Diagnostics.CodeAnalysis.AllowNull]
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => string.Empty;
            public override string DataSource => string.Empty;
            public override string ServerVersion => "0.0";
            public override ConnectionState State => CurrentState;

            public override void ChangeDatabase(string databaseName) { }
            public override void Close() => CurrentState = ConnectionState.Closed;
            public override void Open()
            {
                OpenCount++;
                CurrentState = ConnectionState.Open;
            }

            public override Task OpenAsync(System.Threading.CancellationToken cancellationToken)
            {
                OpenAsyncCount++;
                CurrentState = ConnectionState.Open;
                return Task.CompletedTask;
            }

            protected override DbCommand CreateDbCommand() => throw new NotSupportedException();

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
                => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) DisposeCount++;
                base.Dispose(disposing);
            }
        }
    }
}
