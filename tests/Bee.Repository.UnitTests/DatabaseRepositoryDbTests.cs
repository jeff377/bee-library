using System.ComponentModel;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Provider;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="IDatabaseRepository"/> 需要資料庫的補強測試：
    /// 涵蓋 <see cref="IDatabaseRepository.UpgradeTableSchema"/> 正常執行路徑。
    /// </summary>
    [Collection("Initialize")]
    public class DatabaseRepositoryDbTests
    {
        private static IDatabaseRepository CreateRepository()
            => new SystemRepositoryProvider().DatabaseRepository;

        [DbFact]
        [DisplayName("UpgradeTableSchema 傳入有效參數應正常執行並回傳布林結果")]
        public void UpgradeTableSchema_ValidArgs_DoesNotThrow()
        {
            var repo = CreateRepository();

            var ex = Record.Exception(() => repo.UpgradeTableSchema("common", "common", "st_user"));

            Assert.Null(ex);
        }
    }
}
