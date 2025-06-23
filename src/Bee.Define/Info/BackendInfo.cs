using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端資訊，記錄伺服端在運行期間的參數及環境設置。
    /// </summary>
    public static class BackendInfo
    {
        private static IBusinessObjectProvider _businessObjectProvider = null;
        private static IRepositoryProvider _repositoryProvider = null;
        private static IDefineProvider _defineProvider = null;
        private static ICacheDataSourceProvider _cacheDataSourceProvider = null;

        /// <summary>
        /// 定義資料路徑。
        /// </summary>
        public static string DefinePath { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public static EDatabaseType DatabaseType { get; set; } = EDatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫編號。
        /// </summary>
        public static string DatabaseID { get; set; } = string.Empty;

        /// <summary>
        /// 業務邏輯物件提供者，定義所有 BusinessObject 的取得方式。
        /// </summary>
        public static IBusinessObjectProvider BusinessObjectProvider
        {
            get
            {
                if (_businessObjectProvider == null)
                    _businessObjectProvider = BaseFunc.CreateInstance("Bee.Business.TBusinessObjectProvider") as IBusinessObjectProvider;
                return _businessObjectProvider;
            }
            set { _businessObjectProvider = value; }
        }

        /// <summary>
        /// 資料儲存物件提供者，定義所有 Repository 的取得方式。
        /// </summary>
        public static IRepositoryProvider RepositoryProvider
        {
            get
            {
                if (_repositoryProvider == null)
                    _repositoryProvider = BaseFunc.CreateInstance("Bee.Db.TRepositoryProvider") as IRepositoryProvider;
                return _repositoryProvider;
            }
            set { _repositoryProvider = value; }
        }

        /// <summary>
        /// 定義資料提供者。
        /// </summary>
        public static IDefineProvider DefineProvider
        {
            get
            {
                if (_defineProvider == null)
                    _defineProvider = BaseFunc.CreateInstance("Bee.Define.TFileDefineProvider") as IDefineProvider;
                return _defineProvider;
            }
            set { _defineProvider = value; }
        }

        /// <summary>
        /// 快取資料來源提供者。
        /// </summary>
        public static ICacheDataSourceProvider CacheDataSourceProvider
        {
            get
            {
                if (_cacheDataSourceProvider == null)
                    _cacheDataSourceProvider = BaseFunc.CreateInstance("Bee.Business.TCacheDataSourceProvider") as ICacheDataSourceProvider;
                return _cacheDataSourceProvider;
            }
            set { _cacheDataSourceProvider = value; }
        }
    }
}
