using System.ComponentModel;
using System.Reflection;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Providers;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 閘門：<see cref="BackendDefaultTypes"/> 的每個常數都必須解析得到型別，且滿足它該滿足的契約。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這九個常數是 assembly-qualified 的**字串**，指向 <c>Bee.Business</c>、<c>Bee.ObjectCaching</c>、
    /// <c>Bee.Repository</c> 內的具象型別。編譯期看不到這些邊，相依圖也畫不出來——把
    /// <c>MemoryCacheProvider</c> 改名或搬家，這裡不會有任何錯誤，只會在宿主啟動時炸。
    /// </para>
    /// <para>
    /// **為何不改成 <c>typeof(X).AssemblyQualifiedName</c>：** 那需要 <c>Bee.Definition</c> 參考那三個
    /// 組件，而它被 <c>BEE9001</c> 鎖住（ADR-038）。**為何不把常數搬到組裝層：** 它們被
    /// <c>BackendComponents</c> 當成 <c>[DefaultValue(...)]</c> 的**屬性引數**使用，而屬性引數必須是
    /// 該組件看得到的編譯期常數；搬走就得連 <c>BackendComponents</c> 一起搬，那是公開設定型別。
    /// </para>
    /// <para>
    /// 因此結構上補不了，改以本閘門把「會靜默腐爛的字串」變成「跑測試就會紅的契約」。
    /// </para>
    /// </remarks>
    public class BackendDefaultTypesGateTests
    {
        /// <summary>
        /// 每個常數必須滿足的契約。**新增常數就必須在此登記**，否則
        /// <see cref="EveryConstant_HasADeclaredContract"/> 會紅。
        /// </summary>
        private static readonly Dictionary<string, Type> s_expectedContracts = new(StringComparer.Ordinal)
        {
            [nameof(BackendDefaultTypes.ApiEncryptionKeyProvider)] = typeof(IApiEncryptionKeyProvider),
            [nameof(BackendDefaultTypes.AccessTokenValidator)] = typeof(IAccessTokenValidator),
            [nameof(BackendDefaultTypes.CacheProvider)] = typeof(ICacheProvider),
            [nameof(BackendDefaultTypes.CacheDataSourceProvider)] = typeof(ICacheDataSourceProvider),
            [nameof(BackendDefaultTypes.DefineStorage)] = typeof(IDefineStorage),
            [nameof(BackendDefaultTypes.DefineAccess)] = typeof(IDefineAccess),
            [nameof(BackendDefaultTypes.SessionInfoService)] = typeof(ISessionInfoService),
            [nameof(BackendDefaultTypes.CompanyInfoService)] = typeof(ICompanyInfoService),
            [nameof(BackendDefaultTypes.RepositoryFactory)] = typeof(IRepositoryFactory),
        };

        public static TheoryData<string, string> Constants
        {
            get
            {
                var data = new TheoryData<string, string>();
                foreach (var (name, value) in ReadConstants())
                    data.Add(name, value);
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Constants))]
        [DisplayName("每個預設型別常數都必須解析得到型別，且可具現化為它的契約")]
        public void Constant_ResolvesToATypeSatisfyingItsContract(string name, string typeName)
        {
            var contract = s_expectedContracts[name];

            // `Type.GetType` 只找得到已載入的組件；先各觸碰一個型別把三個組件帶進來。
            ForceLoadBackendAssemblies();
            var type = Type.GetType(typeName, throwOnError: false);

            Assert.True(type != null,
                $"BackendDefaultTypes.{name} 指向 '{typeName}'，但解析不到型別。" +
                "這個常數是宿主啟動時的預設值，字串腐爛只會在執行期現形。");
            Assert.True(contract.IsAssignableFrom(type),
                $"BackendDefaultTypes.{name} 指向 {type!.FullName}，但它沒有實作 {contract.Name}。");
        }

        [Fact]
        [DisplayName("每個常數都必須在契約表中登記（新增常數不得略過本閘門）")]
        public void EveryConstant_HasADeclaredContract()
        {
            var declared = ReadConstants().Select(c => c.Name).ToList();

            var unregistered = declared.Where(n => !s_expectedContracts.ContainsKey(n)).ToList();
            var stale = s_expectedContracts.Keys.Where(n => !declared.Contains(n, StringComparer.Ordinal)).ToList();

            Assert.True(unregistered.Count == 0,
                $"下列常數未在 s_expectedContracts 登記，因此不受本閘門保護：{string.Join(", ", unregistered)}");
            Assert.True(stale.Count == 0,
                $"下列登記項已無對應常數：{string.Join(", ", stale)}");
        }

        [Fact]
        [DisplayName("閘門確實掃到了常數（防止空迴圈恆真）")]
        public void Gate_IsNotVacuous()
        {
            // 常數改為別的成員形式（如 static readonly）時，反射條件會落空而讓上面兩條變成空迴圈。
            Assert.True(ReadConstants().Count >= 9,
                $"只掃到 {ReadConstants().Count} 個常數，反射條件可能已與型別實際形狀不符。");
        }

        private static List<(string Name, string Value)> ReadConstants() =>
            typeof(BackendDefaultTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// 各觸碰一個型別，確保三個目標組件已載入——否則 <see cref="Type.GetType(string, bool)"/>
        /// 會對尚未載入的組件回傳 <c>null</c>，讓閘門誤報。
        /// </summary>
        private static void ForceLoadBackendAssemblies()
        {
            _ = typeof(Bee.Business.BusinessObjectFactory);
            _ = typeof(Bee.ObjectCaching.CacheDefineAccess);
            _ = typeof(Bee.Repository.Factories.RepositoryFactory);
        }
    }
}
