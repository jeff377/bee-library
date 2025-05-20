using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連線設定驗證器。
    /// </summary>
    public class TApiConnectValidator
    {
        /// <summary>
        /// 驗證輸入服務端點，傳回對應的連線方式。
        /// </summary>
        /// <param name="endpoint">驗證的服務端點，遠端連線為網址，近端連線為本地路徑。</param>
        /// <param name="allowGenerateSettings">近端連結是否允許自動生成設定檔，包含 System.Settings.xml 及 Database.Settings.xml 設定檔。</param>
        public EConnectType Validate(string endpoint, bool allowGenerateSettings = false)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new TException("輸入資料不能為空字串");

            if (FileFunc.IsLocalPath(endpoint))  // 輸入資料為本地路徑，驗證近端連線設定
            {
                // 驗證近端連線設定
                ValidateLocal(endpoint, allowGenerateSettings);
                // 回傳連線方式為近端連線
                return EConnectType.Local;
            }
            else if (HttpFunc.IsUrl(endpoint))    // 輸入資料為網址，驗證遠端連線設定
            {
                // 驗證遠端連線設定
                ValidateRemote(endpoint);
                // 回傳連線方式為遠端連線
                return EConnectType.Remote;
            }
            else
            {
                throw new TException("無法識別的連線類型，請輸入有效的服務端點或本地路徑");
            }
        }

        /// <summary>
        /// 驗證近端連線設定。
        /// </summary>
        /// <param name="definePath">定義路徑。</param>
        /// <param name="allowGenerateSettings">近端連結是否允許自動生成設定檔，包含 System.Settings.xml 及 Database.Settings.xml 設定檔。</param>
        private void ValidateLocal(string definePath, bool allowGenerateSettings)
        {
            // 驗證程式是否支援近端連線
            if (!FrontendInfo.SupportedConnectTypes.HasFlag(ESupportedConnectTypes.Local))
                throw new TException("不支援近端連線");
            if (StrFunc.IsEmpty(definePath))
                throw new TException("未指定定義路徑");

            if (allowGenerateSettings) // 設定檔不存在允許自動生成，用於工具程式
            {
                // 驗證是否存在 SystemSettings.xml 設定檔，不存在則建立
                ValidateSystemSettings(definePath);
                // 驗證是否存在 DatabaseSettings.xml 設定檔，不存在則建立
                ValidateDatabaseSettings(definePath);
            }
            else // 要求設定檔一定要存在，用於一般應用程式
            {
                if (!FileFunc.DirectoryExists(definePath))
                    throw new TException("定義路徑不存在");
                // 驗證指定路徑下是否包含 SystemSettings.xml 檔案
                string filePath = FileFunc.PathCombine(definePath, "SystemSettings.xml");
                if (!FileFunc.FileExists(filePath))
                    throw new TException("定義路徑下無 SystemSettings.xml 檔案");
            }
        }

        /// <summary>
        /// 驗證定義路徑是否存在 SystemSettings.xml 設定檔，不存在則建立。
        /// </summary>
        /// <param name="definePath">定義路徑。</param>
        private void ValidateSystemSettings(string definePath)
        {
            // 判斷是否有 SystemSettings.xml 檔案，不存在則建立
            string filePath = FileFunc.PathCombine(definePath, "SystemSettings.xml");
            if (!FileFunc.FileExists(filePath))
            {
                var settings = new TSystemSettings();
                settings.SetObjectFilePath(filePath);
                settings.Save();
            }
        }

        /// <summary>
        /// 驗證定義路徑是否存在 DatabaseSettings.xml 設定檔，不存在則建立。
        /// </summary>
        /// <param name="definePath">定義路徑。</param>
        private void ValidateDatabaseSettings(string definePath)
        {
            // 判斷是否有 DatabaseSettings.xml 檔案，不存在則建立
            string filePath = FileFunc.PathCombine(definePath, "DatabaseSettings.xml");
            if (!FileFunc.FileExists(filePath))
            {
                var settings = new TDatabaseSettings();
                var item = new TDatabaseItem()
                {
                    ID = "default",
                    DisplayName = "預設資料庫"
                };
                settings.Items.Add(item);
                settings.SetObjectFilePath(filePath);
                settings.Save();
            }
        }

        /// <summary>
        /// 驗證遠端連線設定。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        private void ValidateRemote(string endpoint)
        {
            // 驗證程式是否支援遠端連線
            if (!FrontendInfo.SupportedConnectTypes.HasFlag(ESupportedConnectTypes.Remote))
                throw new TException("不支援遠端連線");
            if (StrFunc.IsEmpty(endpoint))
                throw new TException("未指定服務端點");
            // 使用遠端連線，執行 Ping 方法
            var args = new TPingArgs()
            {
                ClientName = "Connector",
                TraceId = "001"
            };
            var connector = new TSystemConnector(endpoint, Guid.Empty);
            var result = connector.Execute<TPingResult>("Ping", args, false);
            if (result.Status != "ok")
                throw new TException($"Ping 方法執行失敗，錯誤訊息：{result.Status}");
        }
    }
}
