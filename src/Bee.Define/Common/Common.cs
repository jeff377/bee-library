using System;

namespace Bee.Define
{
    #region 常數

    /// <summary>
    /// 定義後端常用 Provider 型別的預設名稱常數。
    /// 可用於 SystemSettings.xml 設定檔中的型別指定，或作為預設 fallback 使用。
    /// </summary>
    public static class DefaultProviderTypes
    {
        /// <summary>
        /// 預設的 API 加密金鑰提供者型別。
        /// </summary>
        public const string ApiEncryptionKeyProvider = "Bee.Business.StaticApiEncryptionKeyProvider, Bee.Business";
        /// <summary>
        /// 預設的業務邏輯物件提供者型別，用於動態建立 BusinessObject。
        /// </summary>
        public const string BusinessObjectProvider = "Bee.Business.BusinessObjectProvider, Bee.Business";
        /// <summary>
        /// 預設的快取資料來源提供者型別。
        /// </summary>
        public const string CacheDataSourceProvider = "Bee.Business.CacheDataSourceProvider, Bee.Business";
        /// <summary>
        /// 預設的資料儲存物件提供者型別，用於動態建立 Repository。
        /// </summary>
        public const string RepositoryProvider = "Bee.Db.RepositoryProvider, Bee.Db";
        /// <summary>
        /// 預設的定義資料提供者型別。
        /// </summary>
        public const string DefineProvider = "Bee.Define.FileDefineProvider, Bee.Define";
        /// <summary>
        /// 預設的 AccessToken 驗證提供者，用於驗證 AccessToken 的有效性。
        /// </summary>
        public const string AccessTokenValidationProvider = "Bee.Business.AccessTokenValidationProvider, Bee.Business";
    }

    /// <summary>
    /// SystemObject 的 Action 常數。
    /// </summary>
    public class SystemActions
    {
        /// <summary>
        /// Ping 方法，測試 API 服務是否可用，此方法不啟用資料編碼。
        /// </summary>
        public const string Ping = "Ping";
        /// <summary>
        /// 取得通用參數及環境設置，此方法不啟用資料編碼。
        /// </summary>
        public const string GetCommonConfiguration = "GetCommonConfiguration";
        /// <summary>
        /// 執行登入操作，此方法不啟用加密。
        /// </summary>
        public const string Login = "Login";
        /// <summary>
        /// 建立連線，此方法不啟用資料編碼。
        /// </summary>
        public const string CreateSession = "CreateSession";
        /// <summary>
        /// 取得定義資料。
        /// </summary>
        public const string GetDefine = "GetDefine";
        /// <summary>
        /// 取得定義資料（僅限本機）。
        /// </summary>
        public const string GetLocalDefine = "GetLocalDefine";
        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        public const string SaveDefine = "SaveDefine";
        /// <summary>
        /// 儲存定義資料（僅限本機）。
        /// </summary>
        public const string SaveLocalDefine = "SaveLocalDefine";
        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        public const string ExecFunc = "ExecFunc";
        /// <summary>
        /// 執行自訂方法，匿名存取。
        /// </summary>
        public const string ExecFuncAnonymous = "ExecFuncAnonymous";
        /// <summary>
        /// 執行自訂方法，僅限近端呼叫。
        /// </summary>
        public const string ExecFuncLocal = "ExecFuncLocal";
    }

    /// <summary>
    /// 機制使用的 FuncID 常數。
    /// </summary>
    public class SysFuncIDs
    {
        /// <summary>
        /// Hello 測試方法。
        /// </summary>
        public const string Hello = "Hello";
        /// <summary>
        /// 升級資料表結構。
        /// </summary>
        public const string UpgradeTableSchema = "UpgradeTableSchema";
        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        public const string TestConnection = "TestConnection";
    }

    /// <summary>
    /// 系統使用程式代碼。
    /// </summary>
    public class SysProgIds
    {
        /// <summary>
        /// 系統層級業務邏輯物件。
        /// </summary>
        public const string System = "System";
    }

    /// <summary>
    /// 系統欄位名稱常數。
    /// </summary>
    public class SysFields
    {
        /// <summary>
        /// 列序號，自動遞增。
        /// </summary>
        public const string No = "sys_no";
        /// <summary>
        /// 列識別。
        /// </summary>
        public const string RowId = "sys_rowid";
        /// <summary>
        /// 主檔列識別。
        /// </summary>
        public const string MasterRowId = "sys_master_rowid";
        /// <summary>
        /// 編號。
        /// </summary>
        public const string Id = "sys_id";
        /// <summary>
        /// 名稱。
        /// </summary>
        public const string Name = "sys_name";
        /// <summary>
        /// 寫入時間。
        /// </summary>
        public const string InsertTime = "sys_insert_time";
        /// <summary>
        /// 更新時間。
        /// </summary>
        public const string UpdateTime = "sys_update_time";
        /// <summary>
        /// 資料生效時間（該筆資料從何時開始有效）。
        /// </summary>
        public const string ValidTime = "sys_valid_time";
        /// <summary>
        /// 資料失效時間（該筆資料從何時開始無效，若 NULL 表示仍有效）。
        /// </summary>
        public const string InvalidTime = "sys_invalid_time";
    }

    #endregion

    #region 列舉型別

    /// <summary>
    /// 日誌事件的類型。
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// 一般訊息，表示系統正常執行的資訊。
        /// </summary>
        Information,
        /// <summary>
        /// 警告訊息，表示可能的異常狀況但系統仍可繼續執行。
        /// </summary>
        Warning,
        /// <summary>
        /// 錯誤訊息，表示系統執行時發生異常或失敗。
        /// </summary>
        Error
    }

    /// <summary>
    /// Logging level for abnormal SQL executions.
    /// </summary>
    public enum DbAccessAnomalyLogLevel
    {
        /// <summary>
        /// Disable abnormal logging completely.
        /// </summary>
        None = 0,
        /// <summary>
        /// Log only errors and exceptions.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Log errors, exceptions, and abnormal cases (slow queries, large updates, large result sets).
        /// </summary>
        Warning = 2
    }

    /// <summary>
    /// 主金鑰來源類型。
    /// </summary>
    public enum MasterKeySourceType
    {
        /// <summary>
        /// 從檔案載入主金鑰。
        /// </summary>
        File,
        /// <summary>
        /// 從環境變數載入主金鑰。
        /// </summary>
        Environment
    }

    /// <summary>
    /// 初始化選項。
    /// </summary>
    [Flags]
    public enum InitializeOptions
    {
        /// <summary>
        /// 後端初始化。
        /// </summary>
        Backend = 1,
        /// <summary>
        /// 前端初始化。
        /// </summary>
        Frontend = 2,
        /// <summary>
        /// 網站初始化。
        /// </summary>
        Website = 4,
        /// <summary>
        /// 背景服務初始化。
        /// </summary>
        Background = 8
    }

    /// <summary>
    /// 應用程式類型。
    /// </summary>
    public enum ApplicationType
    {
        /// <summary>
        /// 網站應用程式
        /// </summary>
        Website,
        /// <summary>
        /// Windows 桌面應用程式。
        /// </summary>
        Windows,
        /// <summary>
        /// 背景服務應用程式。
        /// </summary>
        BackgroundService
    }

    /// <summary>
    /// API 存取保護等級。
    /// </summary>
    public enum ApiProtectionLevel
    {
        /// <summary>
        /// 一般開放：允許任何呼叫（不強制編碼，開放給第三方）
        /// </summary>
        Public = 0,
        /// <summary>
        /// 需要編碼：允許遠端呼叫，但必須進行編碼（序列化與壓縮）
        /// </summary>
        Encoded = 1,
        /// <summary>
        /// 需要加密：允許遠端呼叫，必須進行編碼與加密（序列化、壓縮與加密）
        /// </summary>
        Encrypted = 2,
        /// <summary>
        /// 僅限近端呼叫（不驗證編碼，適用於工具程式、背景服務）
        /// </summary>
        LocalOnly = 3
    }

    /// <summary>
    /// API 存取授權需求。
    /// </summary>
    public enum ApiAccessRequirement
    {
        /// <summary>
        /// 不需登入（Anonymous Access）
        /// </summary>
        Anonymous = 0,
        /// <summary>
        /// 需要登入（需驗證 AccessToken）
        /// </summary>
        Authenticated = 1
    }

    /// <summary>
    /// 定義資料類型。
    /// </summary>
    public enum DefineType
    {
        /// <summary>
        /// 系統設定。
        /// </summary>
        SystemSettings,
        /// <summary>
        /// 資料庫設定。
        /// </summary>
        DatabaseSettings,
        /// <summary>
        /// 資料庫結構設定。
        /// </summary>
        DbSchemaSettings,
        /// <summary>
        /// 程式清單。
        /// </summary>
        ProgramSettings,
        /// <summary>
        /// 資料表結構。
        /// </summary>
        DbTable,
        /// <summary>
        /// 表單定義。
        /// </summary>
        FormDefine,
        /// <summary>
        /// 表單版面配置。
        /// </summary>
        FormLayout
    }

    /// <summary>
    /// 資料庫類型。
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// SQL Server。
        /// </summary>
        SQLServer,
        /// <summary>
        /// MySQL。
        /// </summary>
        MySQL,
        /// <summary>
        /// SQLite。
        /// </summary>
        SQLite,
        /// <summary>
        /// Oracle。
        /// </summary>
        Oracle
    }

    /// <summary>
    /// 資料庫結構升級動作。
    /// </summary>
    public enum DbUpgradeAction
    {
        /// <summary>
        /// 結構一致無需升級。
        /// </summary>
        None,
        /// <summary>
        /// 新增。
        /// </summary>
        New,
        /// <summary>
        /// 異動。
        /// </summary>
        Upgrade
    }

    /// <summary>
    /// 欄位類型。
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// 實際存在於資料庫表中的欄位。
        /// </summary>
        DbField,
        /// <summary>
        /// 通過 JOIN 操作從其他表取得的關連欄位。
        /// </summary>
        LinkField,
        /// <summary>
        /// 使用計算或表達式生成的虛擬欄位。
        /// </summary>
        VirtualField
    }

    /// <summary>
    /// 排序方式。
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// 遞增排序。
        /// </summary>
        Asc,
        /// <summary>
        /// 遞減排序。
        /// </summary>
        Desc
    }

    /// <summary>
    /// 結合運算子。
    /// </summary>
    public enum CombineOperator
    {
        /// <summary>
        /// 而且。
        /// </summary>
        And,
        /// <summary>
        /// 或者。
        /// </summary>
        Or
    }

    /// <summary>
    /// 比較運算子。
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// 等於。
        /// </summary>
        Equal,
        /// <summary>
        /// 不等於。
        /// </summary>
        NotEqual,
        /// <summary>
        /// 小於。
        /// </summary>
        Less,
        /// <summary>
        /// 小於等於。
        /// </summary>
        LessOrEqual,
        /// <summary>
        /// 大於。
        /// </summary>
        Greater,
        /// <summary>
        /// 大於等於。
        /// </summary>
        GreaterOrEqual,
        /// <summary>
        /// 包含。
        /// </summary>
        Like,
        /// <summary>
        /// 區間。
        /// </summary>
        Between,
        /// <summary>
        /// 包含。 
        /// </summary>
        In
    }

    #endregion

    #region Layout 相關列舉型別

    /// <summary>
    /// 單筆資料表單模式。
    /// </summary>
    public enum SingleFormMode
    {
        /// <summary>
        /// 檢視。
        /// </summary>
        View,
        /// <summary>
        /// 新增。
        /// </summary>
        Add,
        /// <summary>
        /// 編輯。
        /// </summary>
        Edit
    }

    /// <summary>
    /// 控制項類型。
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// 文字框。
        /// </summary>
        TextEdit,
        /// <summary>
        /// 按鈕文字框。
        /// </summary>
        ButtonEdit,
        /// <summary>
        /// 日期輸入框。
        /// </summary>
        DateEdit,
        /// <summary>
        /// 年月輸入框。
        /// </summary>
        YearMonthEdit,
        /// <summary>
        /// 下拉清單。
        /// </summary>
        DropDownEdit,
        /// <summary>
        /// 備註輸入框。
        /// </summary>
        MemoEdit,
        /// <summary>
        /// 核取框。
        /// </summary>
        CheckEdit
    }

    /// <summary>
    /// 表格欄位的控制項類型。
    /// </summary>
    public enum ColumnControlType
    {
        /// <summary>
        /// 文字框。
        /// </summary>
        TextEdit,
        /// <summary>
        /// 按鈕文字框。
        /// </summary>
        ButtonEdit,
        /// <summary>
        /// 日期輸入框。
        /// </summary>
        DateEdit,
        /// <summary>
        /// 年月輸入框。
        /// </summary>
        YearMonthEdit,
        /// <summary>
        /// 下拉清單。
        /// </summary>
        DropDownEdit,
        /// <summary>
        /// 核取框。
        /// </summary>
        CheckEdit
    }

    /// <summary>
    /// Grid 控制項允許執行的動作。
    /// </summary>
    [Flags]
    public enum GridControlAllowActions
    {
        /// <summary>
        /// 無。
        /// </summary>
        None = 0,
        /// <summary>
        /// 新增。
        /// </summary>
        Add = 1,
        /// <summary>
        /// 修改。
        /// </summary>
        Edit = 2,
        /// <summary>
        /// 刪除。
        /// </summary>
        Delete = 4,
        /// <summary>
        /// 全部。
        /// </summary>
        All = Add | Edit | Delete
    }

    #endregion
}
