using System.ComponentModel;

namespace Bee.Business.UnitTests.Contracts
{
    /// <summary>
    /// 守住契約軸的 <b>BO 側</b>對稱性：<c>Bee.Business</c> 下每個 <c>BusinessArgs</c> /
    /// <c>BusinessResult</c> 子型別，都必須實作 <c>Bee.Api.Contracts</c> 中對應的 <c>I*</c> 契約介面
    /// （<c>XxxArgs</c> → <c>IXxxRequest</c>、<c>XxxResult</c> → <c>IXxxResponse</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>為什麼這件事需要閘門守著。</b>API 型別與 BO 型別之間的雙向轉換由
    /// <c>ApiInputConverter.Convert</c> 承擔（入站由 <c>JsonRpcExecutor</c> 呼叫、出站由
    /// <c>ApiOutputConverter</c> 呼叫），而它是<b>以反射逐一比對屬性名稱來複製</b>的。
    /// 名稱對不上就靜默跳過——不擲例外、不警告，呼叫看起來成功但該欄位是空的。
    /// </para>
    /// <para>
    /// 契約介面正是讓這個複製「保證完整」的唯一機制：<c>LoginRequest</c> 與 <c>LoginArgs</c>
    /// 都實作 <c>ILoginRequest</c>，編譯器就逼兩邊帶同一組成員。少掉介面，那條路徑的
    /// 屬性複製就沒有任何東西看守。
    /// </para>
    /// <para>
    /// wire 側早有對應的 <c>ApiContractPairingTests</c>；BO 側先前沒有，於是
    /// <c>GetDepartmentTreeArgs</c> 漏了契約介面而無人察覺——本測試即為該缺口的補強。
    /// </para>
    /// </remarks>
    public class BusinessContractPairingTests
    {
        /// <summary>
        /// 取得所有需要配對契約介面的 BO 參數 / 結果型別。
        /// </summary>
        public static TheoryData<Type> BusinessDtoTypes()
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(BusinessArgs).Assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
                .Where(t => typeof(BusinessArgs).IsAssignableFrom(t) || typeof(BusinessResult).IsAssignableFrom(t))
                .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                data.Add(type);
            }
            return data;
        }

        /// <summary>
        /// 由 BO 型別名推導其契約介面名。<c>Args</c> 對 <c>Request</c>、<c>Result</c> 對
        /// <c>Response</c>——因為契約描述的是 wire 訊息，而 BO 用的是自己的詞彙。
        /// </summary>
        private static string? ExpectedContractName(Type boType)
        {
            if (typeof(BusinessArgs).IsAssignableFrom(boType) && boType.Name.EndsWith("Args", StringComparison.Ordinal))
                return "I" + boType.Name[..^"Args".Length] + "Request";
            if (typeof(BusinessResult).IsAssignableFrom(boType) && boType.Name.EndsWith("Result", StringComparison.Ordinal))
                return "I" + boType.Name[..^"Result".Length] + "Response";
            return null;
        }

        [Theory]
        [MemberData(nameof(BusinessDtoTypes))]
        [DisplayName("每個 BusinessArgs / BusinessResult 都應實作對應的 I* 契約介面")]
        public void BusinessDto_ImplementsMatchingContractInterface(Type boType)
        {
            var expected = ExpectedContractName(boType);
            Assert.True(expected != null,
                $"{boType.Name} 未循 XxxArgs / XxxResult 命名慣例，無法推導契約介面名。");

            var implemented = boType.GetInterfaces().Any(i => i.Name == expected);
            Assert.True(implemented,
                $"{boType.FullName} 應實作 {expected}。API 與 BO 之間的屬性複製是靠名稱比對、" +
                "對不上會靜默丟欄位，契約介面是唯一在編譯期擋下這件事的機制。");
        }

        [Fact]
        [DisplayName("BO 參數 / 結果型別清單不得為空（防空轉）")]
        public void BusinessDtoTypes_IsNotEmpty()
        {
            // 上面的 Theory 是反射列舉驅動的：`BusinessArgs` 換組件、或 IsPublic 這類條件寫失準，
            // 回傳零筆時 xUnit 的 Theory 零案例即通過，整個閘門會變成恆綠而沒有任何徵兆。
            Assert.NotEmpty(BusinessDtoTypes());
        }
    }
}
