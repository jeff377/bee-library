using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Bee.Api.Core;
using Bee.Cache;
using Bee.Db;
using Bee.Define;


namespace JsonRpcSampleAspNet
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // 從組態載入 DefinePath
            BackendInfo.DefinePath = ConfigurationManager.AppSettings["DefinePath"]
                ?? throw new InvalidOperationException("DefinePath 未設定");
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);

            // 系統設定初始化
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作
            ApiServiceOptions.Initialize(settings.CommonConfiguration.ApiPayloadOptions);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}