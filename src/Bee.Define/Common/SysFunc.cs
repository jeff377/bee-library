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
        /// 建立系統層級商業邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static ISystemObject CreateSystemObject(Guid accessToken)
        {
            // 若預載入系統商業邏輯物件，則直接回傳
            if (BackendInfo.SystemObject != null) { return BackendInfo.SystemObject; }

            string typeName = BackendInfo.SystemTypeName;
            return BaseFunc.CreateInstance(typeName, new object[] { accessToken }) as ISystemObject;
        }

        /// <summary>
        /// 建立功能層級商業邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public static IBusinessObject CreateBusinessObject(Guid accessToken, string progID)
        {
            string typeName = BackendInfo.BusinessTypeName;
            return BaseFunc.CreateInstance(typeName, new object[] { accessToken, progID }) as IBusinessObject;
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

        /// <summary>
        /// 建立 API 服務傳輸資料加密物件。
        /// </summary>
        /// <returns></returns>
        public static IApiServiceEncryption CreateApiServiceEncryption()
        {
            return new TApiServiceEncryption();
        }


    }
}
