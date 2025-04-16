using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端資訊，記錄伺服端在運行期間的參數及環境設置。
    /// </summary>
    public static class BackendInfo
    {
        private static string _SystemTypeName = string.Empty;
        private static string _BusinessTypeName = string.Empty;
        private static IDefineProvider _DefineProvider = null;
        private static IBusinessObjectProvider _BusinessObjectProvider = null;

        /// <summary>
        /// 定義資料路徑。
        /// </summary>
        public static string DefinePath { get; set; } = string.Empty;

        /// <summary>
        /// 預載入系統商業邏輯物件，工具程式若發佈為獨立執行檔需預先載入。
        /// </summary>
        public static ISystemObject SystemObject { get; set; }

        /// <summary>
        /// 系統層級商業邏輯物件預設型別，由設定檔指定。
        /// </summary>
        public static string SystemTypeName
        {
            get => StrFunc.IsEmpty(_SystemTypeName) ? "Bee.Business.TSystemObject" : _SystemTypeName;
            set => _SystemTypeName = value;
        }

        /// <summary>
        /// 功能層級商業邏輯物件預設型別，由設定檔指定。
        /// </summary>
        public static string BusinessTypeName
        {
            get => StrFunc.IsEmpty(_BusinessTypeName) ? "Bee.Business.TBusinessObject" : _BusinessTypeName;
            set => _BusinessTypeName = value;
        }

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public static EDatabaseType DatabaseType { get; set; } = EDatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫編號。
        /// </summary>
        public static string DatabaseID { get; set; } = string.Empty;

        /// <summary>
        /// 定義資料提供者。
        /// </summary>
        public static IDefineProvider DefineProvider
        {
            get
            {
                if (_DefineProvider == null)
                    _DefineProvider = BaseFunc.CreateInstance("Bee.Define.TFileDefineProvider") as IDefineProvider;
                return _DefineProvider;
            }
            set { _DefineProvider = value; }
        }

        /// <summary>
        /// 商業邏輯物件提供者。
        /// </summary>
        public static IBusinessObjectProvider BusinessObjectProvider
        {
            get
            {
                if (_BusinessObjectProvider == null)
                    _BusinessObjectProvider = BaseFunc.CreateInstance("Bee.Cache.TBusinessObjectProvider") as IBusinessObjectProvider;
                return _BusinessObjectProvider;
            }
            set { _BusinessObjectProvider = value; }
        }
    }
}
