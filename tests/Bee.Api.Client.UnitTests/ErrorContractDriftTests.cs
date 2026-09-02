using System.ComponentModel;
using System.Globalization;
using Bee.Api.Client.UnitTests.Connectors;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Exceptions;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 守衛錯誤契約兩端之間的漂移。
    /// </summary>
    /// <remarks>
    /// 伺服端 <c>JsonRpcExecutor.MapException</c> 把例外映成錯誤碼，呼叫端
    /// <c>ApiConnector.FinalizeResponse</c> 再把錯誤碼映回例外。兩者互為反函數，
    /// 但**編譯器不會把它們綁在一起**：伺服端多回一個碼而呼叫端沒跟上，那個碼會安靜落到
    /// 通用分支，例外型別 doc 上承諾的 <c>catch</c> 從此永遠不會進去，編譯得過、測試也未必測得到。
    /// <para>
    /// 這不是假想的失敗樣態。<see cref="JsonRpcErrorCode.ReplayRejected"/> 就是這樣漂掉的：
    /// 伺服端四處丟 <see cref="ReplayRejectedException"/>、映成 -32005，呼叫端整整少了一條分支。
    /// 本測試是「兩端必須一致」這條規則的唯一自動化把關。
    /// </para>
    /// <para>
    /// 本測試同時要求**每個錯誤碼都被分類**。新增列舉成員而不歸類，這裡就會紅 ——
    /// 目的是逼出一次決策（這個碼要不要有呼叫端型別），而不是讓它默默存在。
    /// </para>
    /// </remarks>
    public class ErrorContractDriftTests
    {
        /// <summary>
        /// 有專屬例外型別的錯誤碼：伺服端由該型別映出此碼，呼叫端須把此碼重建回該型別。
        /// </summary>
        private static readonly (JsonRpcErrorCode Code, Type ExceptionType)[] s_reconstructedCodes =
        [
            (JsonRpcErrorCode.UserMessage, typeof(UserMessageException)),
            (JsonRpcErrorCode.PermissionDenied, typeof(ForbiddenException)),
            (JsonRpcErrorCode.CompanyAccessDenied, typeof(CompanyAccessDeniedException)),
            (JsonRpcErrorCode.CompanyNotEntered, typeof(CompanyNotEnteredException)),
            (JsonRpcErrorCode.ReplayRejected, typeof(ReplayRejectedException)),
        ];

        /// <summary>
        /// 刻意不重建的錯誤碼：它們產生於執行器之外（傳輸層、剖析層），或訊息本就不該給使用者看，
        /// 呼叫端一律落到通用分支。
        /// </summary>
        private static readonly JsonRpcErrorCode[] s_transportOnlyCodes =
        [
            JsonRpcErrorCode.ParseError,
            JsonRpcErrorCode.InvalidRequest,
            JsonRpcErrorCode.InternalError,
        ];

        /// <summary>
        /// 目前全 repo 沒有任何產生者的錯誤碼 —— 已知技術債，尚未決定要補產生者還是移除成員。
        /// </summary>
        /// <remarks>
        /// 尤其 <see cref="JsonRpcErrorCode.Unauthorized"/>：認證失敗實際走
        /// <c>ApiAuthorizationValidator</c> 回 <see cref="JsonRpcErrorCode.InvalidRequest"/>
        /// 加 HTTP 401，這個碼從未上過線。列在這裡是為了讓它**被看見**；
        /// 要讓本測試轉綠而把新碼塞進這個桶，正是本測試要防的事。
        /// </remarks>
        private static readonly JsonRpcErrorCode[] s_noProducerCodes =
        [
            JsonRpcErrorCode.MethodNotFound,
            JsonRpcErrorCode.InvalidParams,
            JsonRpcErrorCode.Unauthorized,
        ];

        /// <summary>
        /// 伺服端白名單上、會與 <see cref="UserMessageException"/> 一起收斂成
        /// <see cref="JsonRpcErrorCode.UserMessage"/> 的過渡期 BCL 例外。
        /// </summary>
        private static readonly Type[] s_userMessageWhitelist =
        [
            typeof(UnauthorizedAccessException),
            typeof(ArgumentException),
            typeof(ArgumentNullException),
            typeof(InvalidOperationException),
            typeof(NotSupportedException),
            typeof(FormatException),
        ];

        public static IEnumerable<object[]> ReconstructedCodes =>
            s_reconstructedCodes.Select(pair => new object[] { pair.Code, pair.ExceptionType });

        public static IEnumerable<object[]> TransportOnlyCodes =>
            s_transportOnlyCodes.Select(code => new object[] { code });

        public static IEnumerable<object[]> UserMessageWhitelist =>
            s_userMessageWhitelist.Select(type => new object[] { type });

        [Fact]
        [DisplayName("每個 JsonRpcErrorCode 成員都必須被分類且只分類一次（新增成員不得默默略過）")]
        public void ErrorCodeClassification_CoversEveryMemberExactlyOnce()
        {
            var declared = Enum.GetValues<JsonRpcErrorCode>().ToHashSet();
            var classified = s_reconstructedCodes.Select(pair => pair.Code)
                .Concat(s_transportOnlyCodes)
                .Concat(s_noProducerCodes)
                .ToList();

            var unclassified = declared.Except(classified).ToList();
            var duplicated = classified.GroupBy(code => code)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.True(unclassified.Count == 0,
                $"這些錯誤碼沒有被分類，請決定它屬於哪一類：{string.Join(", ", unclassified)}");
            Assert.True(duplicated.Count == 0,
                $"這些錯誤碼被分到多個類別：{string.Join(", ", duplicated)}");
            Assert.Empty(classified.Except(declared));
        }

        [Fact]
        [DisplayName("三個分類桶都不得為空，且釘住各自的代表成員（防止分類檢查變成恆真）")]
        public void ErrorCodeClassification_BucketsAreNotVacuous()
        {
            Assert.NotEmpty(s_reconstructedCodes);
            Assert.NotEmpty(s_transportOnlyCodes);
            Assert.NotEmpty(s_userMessageWhitelist);

            // 三個桶各釘一個代表成員：整桶被清空或搬走時，上面的 NotEmpty 擋不到，這裡擋得到。
            Assert.Contains(s_reconstructedCodes, pair => pair.Code == JsonRpcErrorCode.UserMessage);
            Assert.Contains(JsonRpcErrorCode.InternalError, s_transportOnlyCodes);
            Assert.Contains(typeof(ArgumentException), s_userMessageWhitelist);
        }

        [Fact]
        [DisplayName("登錄表必須把衍生型別排在基底型別之前（否則後者會吃掉前者）")]
        public void ErrorContract_DeclaresDerivedTypesBeforeTheirBaseTypes()
        {
            var rows = JsonRpcErrorContract.Rows;
            var shadowed = new List<string>();

            for (int i = 0; i < rows.Count; i++)
            {
                for (int j = i + 1; j < rows.Count; j++)
                {
                    // 比對是 IsInstanceOfType（可指派），所以排在前面的基底型別會攔下後面的衍生型別，
                    // 讓那一列永遠match不到。這不是風格問題，是那一列直接失效。
                    if (rows[i].ExceptionType != rows[j].ExceptionType
                        && rows[i].ExceptionType.IsAssignableFrom(rows[j].ExceptionType))
                    {
                        shadowed.Add($"{rows[j].ExceptionType.Name}(第 {j} 列) 被 {rows[i].ExceptionType.Name}(第 {i} 列) 遮蔽");
                    }
                }
            }

            Assert.True(shadowed.Count == 0,
                $"登錄表順序錯誤，下列各列永遠不會被match到：{string.Join("；", shadowed)}");
        }

        [Fact]
        [DisplayName("每個可重建的錯誤碼在登錄表中只能有一個重建型別")]
        public void ErrorContract_DeclaresExactlyOneRebuildPerCode()
        {
            var duplicated = JsonRpcErrorContract.Rows
                .Where(row => row.CanRebuild)
                .GroupBy(row => row.Code)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.True(duplicated.Count == 0,
                $"這些錯誤碼宣告了多個重建型別，呼叫端會拿到先宣告的那個：{string.Join(", ", duplicated)}");
        }

        [Fact]
        [DisplayName("登錄表可重建的碼必須與本測試宣告的規格完全一致")]
        public void ErrorContract_RebuildableCodes_MatchDeclaredSpecification()
        {
            // 本測試的 s_reconstructedCodes 是**規格**，刻意獨立於實作手寫一份：
            // 測試若改讀受測程式自己的清單，就只是拿實作驗證實作，什麼也證明不了。
            var expected = s_reconstructedCodes
                .Select(pair => (pair.Code, pair.ExceptionType))
                .OrderBy(pair => (int)pair.Code)
                .ToList();
            var actual = JsonRpcErrorContract.Rows
                .Where(row => row.CanRebuild)
                .Select(row => (row.Code, row.ExceptionType))
                .OrderBy(pair => (int)pair.Code)
                .ToList();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ReconstructedCodes))]
        [DisplayName("伺服端應把宣告的例外型別映成宣告的錯誤碼，且原樣保留訊息")]
        public void MapException_DeclaredExceptionType_ReturnsDeclaredCode(
            JsonRpcErrorCode expectedCode, Type exceptionType)
        {
            const string message = "contract probe message";
            var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

            var (code, mappedMessage) = JsonRpcExecutor.MapException(exception);

            Assert.Equal(expectedCode, code);
            Assert.Equal(message, mappedMessage);
        }

        [Theory]
        [MemberData(nameof(ReconstructedCodes))]
        [DisplayName("呼叫端應把宣告的錯誤碼重建回宣告的例外型別，且訊息不加前綴")]
        public async Task FinalizeResponse_DeclaredCode_RebuildsDeclaredExceptionType(
            JsonRpcErrorCode code, Type expectedExceptionType)
        {
            const string message = "contract probe message";

            var exception = await Record.ExceptionAsync(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(code, message));

            Assert.NotNull(exception);
            Assert.IsType(expectedExceptionType, exception);
            Assert.Equal(message, exception.Message);
        }

        [Theory]
        [MemberData(nameof(TransportOnlyCodes))]
        [DisplayName("呼叫端對刻意不重建的錯誤碼應落到通用分支並保留碼與原訊息")]
        public async Task FinalizeResponse_TransportOnlyCode_FallsBackToGenericBranch(JsonRpcErrorCode code)
        {
            const string message = "transport level failure";

            var exception = await Record.ExceptionAsync(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(code, message));

            var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("API error", invalidOperation.Message);
            Assert.Contains(((int)code).ToString(CultureInfo.InvariantCulture), invalidOperation.Message);
            Assert.Contains(message, invalidOperation.Message);
        }

        [Theory]
        [MemberData(nameof(UserMessageWhitelist))]
        [DisplayName("白名單 BCL 例外應與 UserMessageException 一同收斂為 UserMessage code（多對一是刻意的）")]
        public void MapException_WhitelistedBclException_CollapsesToUserMessage(Type exceptionType)
        {
            const string message = "whitelisted bcl message";
            var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

            var (code, _) = JsonRpcExecutor.MapException(exception);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
        }

        [Fact]
        [DisplayName("多對一不可逆：白名單 BCL 例外經 wire 一律重建為 UserMessageException")]
        public async Task FinalizeResponse_UserMessageCode_AlwaysRebuildsUserMessageException()
        {
            // 伺服端丟的是 InvalidOperationException，回到呼叫端只剩一個整數，
            // 能還原的就是這個整數認得的型別。這是刻意的取捨，不是缺陷。
            var (code, message) = JsonRpcExecutor.MapException(new InvalidOperationException("state is wrong"));

            var exception = await Record.ExceptionAsync(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(code, message));

            Assert.IsType<UserMessageException>(exception);
            Assert.Equal("state is wrong", exception.Message);
        }
    }
}
