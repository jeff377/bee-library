using System.ComponentModel;
using System.Reflection;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Contracts
{
    /// <summary>
    /// 守住「每個保留字 progId 都真的能被 <see cref="BusinessObjectFactory"/> 建出來」。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>為什麼既有的閘門擋不到這件事。</b><c>ActionSurfaceTests</c> 守的是 action 常數與方法的
    /// 對稱、<c>BoApiSurfaceTests</c> 守的是公開表面、<c>ReservedProgIdResolutionTests</c> 守的是
    /// progId 解析得到哪個<b>型別</b>——三者都在「型別已經被建出來」之後才有意義。而
    /// <c>BusinessObjectFactory.CreateBusinessObject</c> 是用 <c>Activator.CreateInstance</c> 固定
    /// 傳四個引數建構的，<b>C# 的建構子不會被繼承</b>：子類少宣告一個參數，前面三道閘門全綠，
    /// 症狀是 runtime 的 <c>MissingMethodException</c>，而且發生在方法查找<b>之前</b>，
    /// 對呼叫端呈現為 <c>InternalError</c>。
    /// </para>
    /// <para>
    /// 這正是 4.25.0 的 <c>AuditRule</c> 發生過的事：它只宣告了三參數建構子，於是那張隨框架出貨的
    /// 稽核規則維護表單遠端完全不可達，而唯一的測試是直接 <c>new</c> 出來的、從不經過工廠，
    /// 所以出貨時整個套件是綠的。
    /// </para>
    /// <para>
    /// <b>兩層互補，刻意重疊。</b><c>CreateBusinessObject_*</c> 走真實工廠，是最貼近實際失敗的一層，
    /// 但它需要資料庫容器；<c>DefaultType_DeclaresConstructorMatchingTheBase</c> 是純反射、無外部相依，
    /// 容器不在時仍然守得住。兩者都<b>不硬編工廠傳的引數形狀</b>——前者根本不需要知道，後者從
    /// <see cref="BusinessObject"/> 基底的建構子推導。抄一份形狀下來就又多了一個會漂的來源。
    /// </para>
    /// </remarks>
    public class ReservedProgIdConstructionTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public ReservedProgIdConstructionTests(SharedDbFixture fx) { _fx = fx; }

        private IBusinessObjectFactory Factory => _fx.GetRequiredService<IBusinessObjectFactory>();

        public static TheoryData<string> ReservedProgIds()
        {
            var data = new TheoryData<string>();
            foreach (var binding in Bee.Business.ReservedProgIds.All)
                data.Add(binding.ProgId);
            return data;
        }

        [Theory]
        [MemberData(nameof(ReservedProgIds))]
        [DisplayName("每個保留字 progId 都應能經 BusinessObjectFactory 建出對應的 BO")]
        public void CreateBusinessObject_EveryReservedProgId_Succeeds(string progId)
        {
            var binding = Bee.Business.ReservedProgIds.Find(progId);
            Assert.NotNull(binding);

            var bo = Factory.CreateBusinessObject(Guid.NewGuid(), progId, isLocalCall: true);

            // 斷言用 ExpectedBaseType 而非 DefaultType：部署可以在註冊表把保留字綁到自己的子類，
            // 那是合法的，而不論綁到哪一個，它都必須滿足該 progId 的基底約束。
            Assert.IsAssignableFrom(binding!.ExpectedBaseType, bo);
        }

        [Theory]
        [MemberData(nameof(ReservedProgIds))]
        [DisplayName("每個保留字 progId 建出的 BO 都應保留 isLocalCall=false")]
        public void CreateBusinessObject_EveryReservedProgId_PreservesRemoteFlag(string progId)
        {
            var bo = Factory.CreateBusinessObject(Guid.NewGuid(), progId, isLocalCall: false);

            var businessObject = Assert.IsAssignableFrom<BusinessObject>(bo);
            Assert.False(businessObject.IsLocalCall);
        }

        [Theory]
        [MemberData(nameof(ReservedProgIds))]
        [DisplayName("每個保留字 progId 的預設 BO 都應宣告與基底相同參數形狀的建構子（不需容器）")]
        public void DefaultType_DeclaresConstructorMatchingTheBase(string progId)
        {
            var binding = Bee.Business.ReservedProgIds.Find(progId);
            Assert.NotNull(binding);

            // 期望形狀從 BusinessObject 基底自己推導，不從工廠抄一份下來——抄下來就又多一個會漂的來源。
            var expected = typeof(BusinessObject)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(c => c.GetParameters().Select(p => p.ParameterType).ToArray())
                .OrderByDescending(types => types.Length)
                .First();

            var actual = binding!.DefaultType.GetConstructor(expected);

            Assert.True(
                actual is not null,
                $"{binding.DefaultType.Name} 沒有 ({string.Join(", ", expected.Select(t => t.Name))}) 建構子，" +
                "BusinessObjectFactory 會擲 MissingMethodException，該 progId 遠端不可達。");
        }

        [Fact]
        [DisplayName("保留字 progId 清單不得為空（防空轉：清單空掉時上面三個 Theory 會恆綠）")]
        public void ReservedProgIds_AreNotEmpty()
        {
            Assert.NotEmpty(Bee.Business.ReservedProgIds.All);
        }
    }
}
