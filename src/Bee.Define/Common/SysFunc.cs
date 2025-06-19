using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 系統函式庫。
    /// </summary>
    public static class SysFunc
    {
        /// <summary>
        /// 建立快取資料來源提供者。
        /// </summary>
        public static ICacheDataSourceProvider CreateCacheDataSourceProvider()
        {
            string sTypeName;

            sTypeName = "Bee.Business.TCacheDataSourceProvider";
            return BaseFunc.CreateInstance(sTypeName, new object[] { }) as ICacheDataSourceProvider;
        }
    }
}
