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
        /// 建立系統層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static ISystemBusinessObject CreateSystemObject(Guid accessToken)
        {
            // 若預載入系統業務邏輯物件，則直接回傳
            if (BackendInfo.SystemObject != null) { return BackendInfo.SystemObject; }

            string typeName = BackendInfo.SystemTypeName;
            return BaseFunc.CreateInstance(typeName, new object[] { accessToken }) as ISystemBusinessObject;
        }

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public static IFormBusinessObject CreateBusinessObject(Guid accessToken, string progId)
        {
            string typeName = BackendInfo.FormTypeName;
            return BaseFunc.CreateInstance(typeName, new object[] { accessToken, progId }) as IFormBusinessObject;
        }

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
