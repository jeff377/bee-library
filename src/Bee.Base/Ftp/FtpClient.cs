using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;

namespace Bee.Base
{
    /// <summary>
    /// FTP 服務用戶端，提供檔案的列舉、上傳、下載和刪除功能，並支援 FTPS（FTP over SSL）。
    /// </summary>
    public class FtpClient
    {
        private string _Endpoint = string.Empty;
        private string _Username = string.Empty;
        private string _Password = string.Empty;
        private bool _UseSsl = true;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="endpoint">FTP 伺服器端點。</param>
        /// <param name="username">登入帳號。</param>
        /// <param name="password">登入密碼。</param>
        public FtpClient(string endpoint, string username, string password)
        {
            _Endpoint = endpoint.EndsWith("/") ? endpoint : endpoint + "/";
            _Username = username;
            _Password = password;
            _UseSsl = true; // 預設使用 SSL
        }

        /// <summary>
        /// 指定是否使用 SSL 加密。
        /// </summary>
        public bool UseSsl
        {
            get { return _UseSsl; }
            set { _UseSsl = value; }
        }

        /// <summary>
        /// FTP 伺服器端點。
        /// </summary>
        public string Endpoint
        {
            get { return _Endpoint; }
        }

        /// <summary>
        /// 登入帳號。
        /// </summary>
        public string Username
        {
            get { return _Username; }
        }

        /// <summary>
        /// 登入密碼。
        /// </summary>
        public string Password
        {
            get { return _Password; }
        }

        /// <summary>
        /// 測試與 FTP/FTPS 伺服器的連線是否正常。
        /// </summary>
        /// <returns>如果連線正常則返回 true，否則返回 false。</returns>
        public ConnectionTestResult TestConnection()
        {
            FtpWebRequest oRequest;
            FtpWebResponse oResponse;

            try
            {
                oRequest = CreateFtpWebRequest(Endpoint, WebRequestMethods.Ftp.ListDirectory);
                using (oResponse = (FtpWebResponse)oRequest.GetResponse())
                {
                    return new ConnectionTestResult(true, oResponse.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult(false, ex.Message);
            }
        }

        /// <summary>
        /// 列舉伺服器上的檔案和目錄。
        /// </summary>
        /// <param name="remotePath">遠端目錄路徑。</param>
        /// <returns>返回檔案和目錄名稱的清單。</returns>
        public System.Collections.Generic.List<FtpItem> ListFiles(string remotePath)
        {
            FtpWebRequest oRequest;
            FtpWebResponse oResponse;
            StreamReader oReader;
            System.Collections.Generic.List<FtpItem> oItems;
            FtpItem oItem;
            string sLine;

            oItems = new System.Collections.Generic.List<FtpItem>();
            oRequest = CreateFtpWebRequest(Endpoint + remotePath, WebRequestMethods.Ftp.ListDirectoryDetails);
            using (oResponse = (FtpWebResponse)oRequest.GetResponse())
            {
                using (oReader = new StreamReader(oResponse.GetResponseStream()))
                {
                    while ((sLine = oReader.ReadLine()) != null)
                    {
                        oItem = ParseFtpItem(sLine);
                        if (oItem != null)
                            oItems.Add(oItem);
                    }
                }
            }
            return oItems;
        }

        /// <summary>
        /// 解析 FTP 詳細列表的行，並返回 FtpItem。
        /// </summary>
        /// <param name="line">FTP 列表的單行資訊</param>
        /// <returns>對應的 FtpItem，或 null 表示解析失敗。</returns>
        private FtpItem ParseFtpItem(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            // 分割行的不同部分
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return null;

            // 解析日期與時間
            var lastModified = DateTime.ParseExact($"{parts[0]} {parts[1]}", "MM-dd-yy hh:mmtt", CultureInfo.InvariantCulture);

            // 確定是否為目錄
            var isDirectory = parts[2] == "<DIR>";

            // 解析大小（目錄大小為 0）
            var size = isDirectory ? 0 : long.Parse(parts[2]);

            // 提取名稱（名稱是最後部分，可能包含空格，因此需跳過前三部分）
            var nameIndex = 3;
            var name = string.Join(" ", parts, nameIndex, parts.Length - nameIndex);

            return new FtpItem
            {
                Name = name,
                Type = isDirectory ? EFtpItemType.Directory : EFtpItemType.File,
                Size = size,
                LastModified = lastModified
            };
        }



        /// <summary>
        /// 上傳檔案到伺服器。
        /// </summary>
        /// <param name="remotePath">遠端檔案路徑</param>
        /// <param name="localFilePath">本地檔案路徑</param>
        /// <returns>如果上傳成功則返回 true，否則返回 false。</returns>
        public bool UploadFile(string remotePath, string localFilePath)
        {
            try
            {
                var request = CreateFtpWebRequest(Endpoint + remotePath, WebRequestMethods.Ftp.UploadFile);
                using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                using (var requestStream = request.GetRequestStream())
                {
                    fileStream.CopyTo(requestStream);
                }
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Upload completed: {response.StatusDescription}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 從伺服器下載檔案。
        /// </summary>
        /// <param name="remotePath">遠端檔案路徑</param>
        /// <param name="localFilePath">本地檔案儲存路徑</param>
        /// <returns>如果下載成功則返回 true，否則返回 false。</returns>
        public bool DownloadFile(string remotePath, string localFilePath)
        {
            try
            {
                var request = CreateFtpWebRequest(Endpoint + remotePath, WebRequestMethods.Ftp.DownloadFile);
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                {
                    responseStream.CopyTo(fileStream);
                }
                Console.WriteLine($"Download completed.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 刪除伺服器上的檔案。
        /// </summary>
        /// <param name="remotePath">遠端檔案路徑</param>
        /// <returns>如果刪除成功則返回 true，否則返回 false。</returns>
        public bool DeleteFile(string remotePath)
        {
            try
            {
                var request = CreateFtpWebRequest(Endpoint + remotePath, WebRequestMethods.Ftp.DeleteFile);
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Delete completed: {response.StatusDescription}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 建立 FtpWebRequest。
        /// </summary>
        /// <param name="uri">FTP 伺服器的 URI</param>
        /// <param name="method">FTP 請求方法</param>
        /// <returns>返回建立的 FtpWebRequest 實例。</returns>
        private FtpWebRequest CreateFtpWebRequest(string uri, string method)
        {
            if (uri.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri.Replace("ftps://", "ftp://");
                UseSsl = true;
            }
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Credentials = new NetworkCredential(Username, _Password);
            request.EnableSsl = UseSsl; // 使用屬性來決定是否啟用 SSL
            request.UseBinary = true;
            request.KeepAlive = false; // 避免持續開啟連線
            return request;
        }
    }

}

