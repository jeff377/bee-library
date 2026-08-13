using System.ComponentModel;
using System.Reflection;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Attributes;

namespace Bee.Business.UnitTests.Contracts
{
    /// <summary>
    /// 守住契約軸的<b>動作</b>對稱性：每個 <c>*Actions</c> 常數與對應 BO 上的 public 方法必須
    /// 兩兩對應，且該方法確實是一個合法的 API 進入點（單一 <c>BusinessArgs</c> 參數、回傳
    /// <c>BusinessResult</c>、被 <c>[ApiAccessControl]</c> 覆蓋）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>為什麼型別對稱性的閘門擋不到這件事。</b><c>ApiContractPairingTests</c> 與
    /// <c>BusinessContractPairingTests</c> 守的是「型別有沒有配對的契約介面」，前提是那個型別
    /// 存在。但 JSON-RPC 的 method 是<b>字串</b>：<c>JsonRpcExecutor.GetMethod</c> 拿
    /// <c>action</c> 直接 <c>GetType().GetMethod(action)</c>。常數打錯一個字母，兩個 pairing
    /// 測試全綠，症狀是 runtime 的 <c>MissingMethodException</c>。
    /// </para>
    /// <para>
    /// 反向同理：BO 上多了一個帶 <c>[ApiAccessControl]</c> 的方法卻沒登記常數，它已經是可被
    /// 呼叫的 API 表面，但呼叫端只能用魔術字串——編譯器與現有測試都不會出聲。
    /// </para>
    /// <para>
    /// <c>ExecFunc</c> / <c>ExecFuncAnonymous</c> 宣告在 <see cref="BusinessObject"/> 基底、由
    /// 所有軸繼承，其常數登記在 <see cref="SystemActions"/>，故各軸的反向檢查都接受這兩個名字。
    /// </para>
    /// </remarks>
    public class ActionSurfaceTests
    {
        private static readonly string[] s_inheritedActions =
        [
            SystemActions.ExecFunc,
            SystemActions.ExecFuncAnonymous,
        ];

        /// <summary>
        /// 各軸的「常數類 → BO 型別」對應。新增軸時補一列。
        /// </summary>
        private static readonly (Type Actions, Type BusinessObject)[] s_axes =
        [
            (typeof(SystemActions), typeof(SystemBusinessObject)),
            (typeof(FormActions), typeof(FormBusinessObject)),
            (typeof(LogActions), typeof(LogBusinessObject)),
        ];

        /// <summary>
        /// 展開為 (BO 型別, action 名稱) 逐筆案例，讓失敗訊息直接指出是哪一個 action。
        /// </summary>
        public static TheoryData<Type, string> DeclaredActions()
        {
            var data = new TheoryData<Type, string>();
            foreach (var (actions, businessObject) in s_axes)
            {
                foreach (var action in ActionNames(actions))
                {
                    data.Add(businessObject, action);
                }
            }
            return data;
        }

        /// <summary>
        /// 展開為 (BO 型別, 方法名稱) 逐筆案例，涵蓋該 BO 上所有被 <c>[ApiAccessControl]</c>
        /// 覆蓋的 public 方法。
        /// </summary>
        public static TheoryData<Type, string> ExposedMethods()
        {
            var data = new TheoryData<Type, string>();
            foreach (var (_, businessObject) in s_axes)
            {
                foreach (var method in ApiMethods(businessObject))
                {
                    data.Add(businessObject, method.Name);
                }
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(DeclaredActions))]
        [DisplayName("每個 action 常數都應對應 BO 上一個合法的 API 方法")]
        public void DeclaredAction_HasMatchingApiMethod(Type businessObjectType, string action)
        {
            // 與 JsonRpcExecutor.GetMethod 同一條解析路徑：以 action 字串取 public 方法。
            var method = businessObjectType.GetMethod(action);
            Assert.True(method != null,
                $"{businessObjectType.Name} 上找不到名為 '{action}' 的 public 方法。" +
                "JsonRpcExecutor 以 action 字串直接反射取方法，對不上是 runtime 的 MissingMethodException。");

            var parameters = method!.GetParameters();
            Assert.True(parameters.Length == 1,
                $"{businessObjectType.Name}.{action} 應恰有一個參數，實際 {parameters.Length} 個。" +
                "Executor 只會傳入單一 args 物件。");

            Assert.True(typeof(BusinessArgs).IsAssignableFrom(parameters[0].ParameterType),
                $"{businessObjectType.Name}.{action} 的參數型別應繼承 BusinessArgs，實際為 " +
                $"{parameters[0].ParameterType.Name}。");

            Assert.True(typeof(BusinessResult).IsAssignableFrom(UnwrapTask(method.ReturnType)),
                $"{businessObjectType.Name}.{action} 的回傳型別應繼承 BusinessResult，實際為 " +
                $"{method.ReturnType.Name}。ApiOutputConverter 以 XxxResult → XxxResponse 的名稱慣例轉換出站結果。");

            Assert.True(FindAccessAttribute(method) != null,
                $"{businessObjectType.Name}.{action} 未被 [ApiAccessControl] 覆蓋。" +
                "ApiAccessValidator 對未宣告者一律拒絕，此 action 會在 runtime 擲 UnauthorizedAccessException。");
        }

        [Theory]
        [MemberData(nameof(ExposedMethods))]
        [DisplayName("BO 上每個對外開放的方法都應登記為 action 常數")]
        public void ExposedMethod_IsDeclaredAsAction(Type businessObjectType, string methodName)
        {
            var actions = s_axes.Single(x => x.BusinessObject == businessObjectType).Actions;
            var declared = ActionNames(actions).Concat(s_inheritedActions);

            Assert.True(declared.Contains(methodName, StringComparer.Ordinal),
                $"{businessObjectType.Name}.{methodName} 帶 [ApiAccessControl]（已是可呼叫的 API 表面），" +
                $"但 {actions.Name} 沒有對應常數。呼叫端只能靠魔術字串呼叫它。");
        }

        [Fact]
        [DisplayName("兩個方向的案例列舉都不應為空")]
        public void Enumerations_AreNotEmpty()
        {
            // 防止上面兩個 Theory 因反射條件寫錯而變成零案例的假綠燈。
            Assert.NotEmpty(DeclaredActions());
            Assert.NotEmpty(ExposedMethods());
        }

        /// <summary>
        /// 取出常數類中所有 <c>public const string</c> 的值。
        /// </summary>
        private static IEnumerable<string> ActionNames(Type actionsType)
        {
            return actionsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .OrderBy(x => x, StringComparer.Ordinal);
        }

        /// <summary>
        /// 取出 BO 上所有被 <c>[ApiAccessControl]</c> 覆蓋的 public instance 方法。
        /// </summary>
        private static IEnumerable<MethodInfo> ApiMethods(Type businessObjectType)
        {
            return businessObjectType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                            | BindingFlags.FlattenHierarchy)
                .Where(m => !m.IsSpecialName)
                .Where(m => FindAccessAttribute(m) != null)
                .OrderBy(m => m.Name, StringComparer.Ordinal);
        }

        /// <summary>
        /// 與 <c>ApiAccessValidator.FindAccessAttribute</c> 同一套三段查找：方法本身 → 被覆寫的
        /// 基底方法 → 宣告型別。這裡必須複製而非引用，因為那支是 private——複製的代價由本測試
        /// 自己承擔，總比測試用一套不同的判定規則來得好。
        /// </summary>
        private static ApiAccessControlAttribute? FindAccessAttribute(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<ApiAccessControlAttribute>();
            if (attr != null) { return attr; }

            var baseMethod = method.GetBaseDefinition();
            if (baseMethod != method)
            {
                attr = baseMethod.GetCustomAttribute<ApiAccessControlAttribute>();
                if (attr != null) { return attr; }
            }

            return method.DeclaringType?.GetCustomAttribute<ApiAccessControlAttribute>();
        }

        /// <summary>
        /// 取出實際的結果型別：非同步方法回傳 <c>Task&lt;T&gt;</c>，Executor 會 await 後取 Result。
        /// </summary>
        private static Type UnwrapTask(Type returnType)
        {
            return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                ? returnType.GetGenericArguments()[0]
                : returnType;
        }
    }
}
