using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.DataObjects;

namespace Bee.Web.Blazor.Server.UnitTests
{
    /// <summary>
    /// <see cref="FormDataObject"/> 不得再自帶一份已下沉到 <c>Bee.Api.Client</c> 的值轉換規則。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這些成員原本在本 head 與 <c>Bee.UI.Avalonia</c> 各有一份逐字副本，doc 寫著「刻意平行、
    /// 沒有任何機制強制」—— 然後就真的漂了：<c>ConvertToColumnValue</c> 一邊修好了
    /// 「不要把 <c>DBNull</c> 寫進 NOT NULL 欄位」，另一邊帶著那個 bug 繼續跑。
    /// 現在單一實作在 <c>Bee.Api.Client.FormValueBinding</c> / <c>FormDataGuard</c>。
    /// </para>
    /// <para>
    /// NOTE: 這道閘門擋的是**照原名再貼一份回來**，也就是實際發生過的那種失誤
    /// （在本 head 撞到 bug、不知道有共用實作、於是就地補一個私有方法）。
    /// 改名的副本它擋不到 —— 別把它當成完整的重複偵測。
    /// </para>
    /// </remarks>
    public class FormDataObjectSinkGateTests
    {
        private static readonly string[] s_sunkMembers =
        [
            "RequireConnector",
            "RequireMasterRowId",
            "BuildEmptyDataSet",
            "FormatForBinding",
            "ConvertToColumnValue",
            "ResolveEmptyValueForType",
        ];

        [Fact]
        [DisplayName("FormDataObject 不得重新宣告已下沉到 Bee.Api.Client 的成員")]
        public void FormDataObject_DoesNotRedeclareSunkMembers()
        {
            var declared = typeof(FormDataObject)
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToArray();

            // 對照組：確定真的讀到了成員清單，而不是空陣列讓斷言白過。
            Assert.Contains("GetField", declared, StringComparer.Ordinal);

            var redeclared = s_sunkMembers.Where(n => declared.Contains(n, StringComparer.Ordinal)).ToArray();

            Assert.True(
                redeclared.Length == 0,
                $"以下成員已下沉到 Bee.Api.Client，不該在此重新宣告：{string.Join(", ", redeclared)}");
        }
    }
}
