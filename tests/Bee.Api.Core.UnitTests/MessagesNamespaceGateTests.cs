using System.ComponentModel;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// `Bee.Api.Core.Messages` 底下的公開類別必須都是 wire 訊息。
    /// </summary>
    /// <remarks>
    /// <para>
    /// TypeScript 合約產生器把**該命名空間下的每個公開類別**都當成 wire 型別發佈
    /// （<c>WireContractGenerator.Generate</c> 以命名空間前綴挑選）。所以「放進這個命名空間」
    /// 等於「對外發佈成客戶端可具現的形狀」—— 那是一個沒有人宣告、卻真實存在的決定。
    /// </para>
    /// <para>
    /// 這條先前沒有守衛，代價是具體的：<c>ApiCallContext</c>（授權驗證的輸入，帶著
    /// <c>IsLocalCall</c> 這個數道第二防線都據以判斷的旗標）從不上 wire，卻被發佈進
    /// <c>messages.d.ts</c>；它還把 <c>PayloadFormat</c> 一併拖進 TS 合約成為字串聯集
    /// （<c>'Plain' | 'Encoded' | 'Encrypted'</c>），而**實際在 wire 上的是數字** ——
    /// 同名欄位、合約與 wire 互相矛盾。
    /// </para>
    /// <para>
    /// 既有的 <c>RegisteredContracts_AreReachableFromTheClosure</c> 結構上抓不到它：那道走的
    /// 是命名空間字串比對，而「在 Messages 命名空間」正是這裡要質疑的前提，不是可以拿來當
    /// 判準的事實。
    /// </para>
    /// </remarks>
    public class MessagesNamespaceGateTests
    {
        private const string MessageNamespace = "Bee.Api.Core.Messages";

        [Fact]
        [DisplayName("Messages 命名空間下的公開類別都必須是 ApiMessageBase 子類")]
        public void PublicTypesInMessagesNamespace_AreAllWireMessages()
        {
            var candidates = typeof(ApiMessageBase).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(t => t.Namespace?.StartsWith(MessageNamespace, StringComparison.Ordinal) == true)
                .ToArray();

            // 防空轉：命名空間前綴若寫錯，下面的比對會對著空集合恆真。
            Assert.True(candidates.Length > 20,
                $"只在 {MessageNamespace} 下找到 {candidates.Length} 個公開類別，這道閘門形同虛設。");

            var offenders = candidates
                .Where(t => !typeof(ApiMessageBase).IsAssignableFrom(t))
                .Select(t => t.FullName!)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"以下型別位於 {MessageNamespace} 但不是 wire 訊息，因而會被 TypeScript 合約產生器" +
                $"對外發佈成客戶端可具現的形狀：{string.Join(", ", offenders)}。" +
                "若它不上 wire，就搬到它真正所屬的命名空間。");
        }
    }
}
