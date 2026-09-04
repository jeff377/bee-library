using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.Validator;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 把「哪些 API 方法不需要登入」維持成一份需要具名申報的白名單，並釘住它們在
    /// HTTP 層實際遇到的門檻。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 免登入的方法就是這個框架對外的匿名攻擊面。宣告它的地方是
    /// <see cref="ApiAccessRequirement.Anonymous"/>，而那只是方法上的一個 attribute 參數——
    /// 新增一個標了它的方法，不會有任何東西要求說明理由。這份白名單就是那個要求。
    /// </para>
    /// <para>
    /// <b>兩層各自把關，判斷來源不同。</b><see cref="ApiAuthorizationValidator"/> 在 HTTP 層
    /// 以硬編的方法名清單決定要不要 <c>Authorization</c> header；
    /// <see cref="ApiAccessValidator"/> 在 BO 層讀 attribute 決定要不要真的驗證 token。
    /// 兩者不對齊的後果寫在 <see cref="AnonymousMethods_HttpGate_IsPinned"/>：標了
    /// <c>Anonymous</c> 卻不在 HTTP 層清單裡的方法，需要一個 header，但那個 header
    /// <b>只被檢查能不能 parse 成 Guid</b>——送任意 Guid 即可通過。那道檢查不構成認證。
    /// </para>
    /// </remarks>
    public class AnonymousApiSurfaceTests
    {
        /// <summary>
        /// 免登入方法的白名單。key 為「宣告型別.方法名」，value 是它為什麼可以免登入。
        /// </summary>
        /// <remarks>
        /// 新增一項之前先問：**未登入者拿到這個回應，能知道什麼？** 回應內容才是攻擊面，
        /// 方法名不是。
        /// </remarks>
        private static readonly Dictionary<string, string> s_anonymousAllowList = new(StringComparer.Ordinal)
        {
            ["SystemBusinessObject.Ping"] =
                "連通性探測。資料庫不可用時仍須能回答，因此連 API key 都豁免；框架版本只在通過金鑰閘門後才揭露。",
            ["SystemBusinessObject.Login"] =
                "登入本身。免登入是定義上的必然；API key 仍要求，因為「哪個應用嘗試登入」正是要記錄的事。",
            ["SystemBusinessObject.GetCommonConfiguration"] =
                "客戶端啟動流程在登入前就需要它決定壓縮與加密設定。回應內容是部署層的 payload 設定，不含資料。",
            ["SystemBusinessObject.GetPackage"] =
                "套件下載，基底類別未實作、由應用自行提供。要求 Encoded 以上傳輸。",
            ["SystemBusinessObject.CreateSession"] =
                "宣告為 LocalOnly，遠端呼叫在 BO 層一律被拒——它不是匿名攻擊面的一部分，Anonymous 只對行程內呼叫有意義。",
            ["BusinessObject.ExecFuncAnonymous"] =
                "讓應用掛載自訂的匿名函式。**其攻擊面取決於應用怎麼實作**，框架端只保證它要求 Encoded 以上傳輸。",
        };

        private static Dictionary<string, ApiAccessControlAttribute> AnonymousMethods()
        {
            return typeof(BusinessObject).Assembly.GetTypes()
                .Where(t => typeof(BusinessObject).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => !m.IsSpecialName)
                .Select(m => (Method: m, Attr: ApiAccessValidator.FindAccessControl(m)))
                .Where(x => x.Attr?.AccessRequirement == ApiAccessRequirement.Anonymous)
                .ToDictionary(
                    x => $"{x.Method.DeclaringType!.Name}.{x.Method.Name}",
                    x => x.Attr!,
                    StringComparer.Ordinal);
        }

        [Fact]
        [DisplayName("每個免登入的 API 方法都必須具名申報並附上理由")]
        public void AnonymousMethods_AreAllExplicitlyAllowListed()
        {
            var actual = AnonymousMethods();

            // 掃不到任何免登入方法代表掃描壞了（至少 Login 一定是）——沒有這條，下面會變恆真。
            Assert.NotEmpty(actual);

            var undeclared = actual.Keys.Where(k => !s_anonymousAllowList.ContainsKey(k)).ToList();
            Assert.True(undeclared.Count == 0,
                "下列方法標了 Anonymous 但未申報於白名單，等於在無人審視下擴大匿名攻擊面：" +
                global::System.Environment.NewLine +
                string.Join(global::System.Environment.NewLine, undeclared) +
                global::System.Environment.NewLine +
                "補上白名單時請寫明「未登入者拿到這個回應能知道什麼」，而不只是它為什麼方便。");

            var ghosts = s_anonymousAllowList.Keys.Where(k => !actual.ContainsKey(k)).ToList();
            Assert.True(ghosts.Count == 0,
                "下列方法列在免登入白名單但已不存在或不再是 Anonymous，應一併清掉——" +
                "白名單裡的幽靈條目會讓下一次安全盤點高估或誤判攻擊面：" +
                global::System.Environment.NewLine +
                string.Join(global::System.Environment.NewLine, ghosts));

            foreach (var (name, reason) in s_anonymousAllowList)
                Assert.False(string.IsNullOrWhiteSpace(reason), $"{name} 的免登入理由不得為空。");
        }

        [Fact]
        [DisplayName("免登入方法在 HTTP 層的實際門檻：只有 Ping 與 Login 真的不需要 header")]
        public void AnonymousMethods_HttpGate_IsPinned()
        {
            var validator = new ApiAuthorizationValidator();

            // 不帶 Authorization header 時仍然通過的，才是真正不需要 header 的方法。
            static ApiAuthorizationContext WithoutHeader(string method) => new()
            {
                Method = method,
                ApiKey = "present",          // 金鑰閘門未啟用時只檢查非空
                Authorization = string.Empty,
            };

            Assert.True(validator.Validate(WithoutHeader($"{SysProgIds.System}.Ping")).IsValid);
            Assert.True(validator.Validate(WithoutHeader($"{SysProgIds.System}.Login")).IsValid);

            // 其餘標了 Anonymous 的方法，HTTP 層仍要求 header。
            string[] requireHeader =
            [
                $"{SysProgIds.System}.GetCommonConfiguration",
                $"{SysProgIds.System}.GetPackage",
                $"{SysProgIds.System}.ExecFuncAnonymous",
            ];
            foreach (var method in requireHeader)
                Assert.False(validator.Validate(WithoutHeader(method)).IsValid, method);

            // WARNING: 但那道 header 只被檢查能不能 parse 成 Guid——任意 Guid 即可通過，
            // 而 BO 層因為 attribute 是 Anonymous 也不會驗證它。這四個方法因此實際上是
            // 匿名可達的，header 不構成認證。把它釘在這裡，是為了讓任何只讀 HTTP 層
            // 白名單的人不會低估攻擊面。
            foreach (var method in requireHeader)
            {
                var result = validator.Validate(new ApiAuthorizationContext
                {
                    Method = method,
                    ApiKey = "present",
                    Authorization = $"Bearer {Guid.Empty}",
                });
                Assert.True(result.IsValid, method);
                Assert.Equal(Guid.Empty, result.AccessToken);
            }
        }
    }
}
