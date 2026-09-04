using System.ComponentModel;
using System.Reflection;

namespace Bee.Business.UnitTests.Contracts
{
    /// <summary>
    /// 每個 BO 建構子的 <c>isLocalCall</c> 預設值必須是 <c>false</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 預設 <c>true</c> 的意思是：直接建構 BO —— 唯一繞過 <c>ApiAccessValidator</c> 的路徑 ——
    /// 預設被當成可信的行程內呼叫。而數個方法的第二道防線正是以 <c>IsLocalCall</c> 為條件
    /// （免 deployment admin 鑄造 API key、授予 deployment admin、寫入 server-only 定義），
    /// 於是那幾道守衛對預設路徑全數放行；**只有刻意寫 `isLocalCall: false` 的呼叫端會被擋，
    /// 而那是最不需要擋的一種**。
    /// </para>
    /// <para>
    /// 用反射掃全部子類而非逐一列名：新增一個 BO 家族時，這道閘門自動涵蓋它。
    /// 這正是原本缺的東西 —— 五個建構子各自宣告預設值，沒有任何機制要求它們一致。
    /// </para>
    /// </remarks>
    public class LocalCallDefaultGateTests
    {
        [Fact]
        [DisplayName("所有 BusinessObject 建構子的 isLocalCall 預設值必須為 false")]
        public void EveryBusinessObjectConstructor_DefaultsIsLocalCallToFalse()
        {
            var boTypes = typeof(BusinessObject).Assembly.GetTypes()
                .Where(t => typeof(BusinessObject).IsAssignableFrom(t))
                .ToArray();

            // 防空轉：型別載不到時下面的迴圈一圈都不跑。
            Assert.Contains(typeof(BusinessObject), boTypes);

            var offenders = new List<string>();
            int checkedCount = 0;
            foreach (var type in boTypes)
            {
                foreach (var ctor in type.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var parameter = ctor.GetParameters().FirstOrDefault(p => p.Name == "isLocalCall");
                    if (parameter is null || !parameter.HasDefaultValue) { continue; }

                    checkedCount++;
                    if (!Equals(parameter.DefaultValue, false))
                    {
                        offenders.Add($"{type.Name} (預設 {parameter.DefaultValue})");
                    }
                }
            }

            // 第二道防空轉：真的有帶預設值的建構子被檢查到，而不是條件寫錯導致全被 continue 掉。
            Assert.True(checkedCount > 0, "沒有任何帶 isLocalCall 預設值的建構子被檢查到，這道閘門形同虛設。");

            Assert.True(
                offenders.Count == 0,
                "以下 BO 建構子把 isLocalCall 預設為 true，等於讓直接建構的呼叫端預設被當成可信的" +
                $"行程內呼叫，繞過以 IsLocalCall 為條件的第二道防線：{string.Join(", ", offenders)}。");
        }
    }
}
