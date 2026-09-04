using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the system API wire contracts.
        /// </summary>
        private static void AddSystemMessages(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.CreateApiKeyRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.SysName), static x => x.SysName, static (x, v) => x.SysName = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.KeyType), static x => x.KeyType, static (x, v) => x.KeyType = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.Contact), static x => x.Contact, static (x, v) => x.Contact = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyRequest.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.CreateApiKeyResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyResponse.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateApiKeyResponse.ApiKey), static x => x.ApiKey, static (x, v) => x.ApiKey = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.CreateSessionRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionRequest.UserID), static x => x.UserID, static (x, v) => x.UserID = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionRequest.ExpiresIn), static x => x.ExpiresIn, static (x, v) => x.ExpiresIn = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionRequest.OneTime), static x => x.OneTime, static (x, v) => x.OneTime = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.CreateSessionResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionResponse.AccessToken), static x => x.AccessToken, static (x, v) => x.AccessToken = v)
                .Member(nameof(Bee.Api.Core.Messages.System.CreateSessionResponse.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.EnterCompanyRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.EnterCompanyRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.EnterCompanyRequest.CompanyId), static x => x.CompanyId, static (x, v) => x.CompanyId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.EnterCompanyResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.EnterCompanyResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.EnterCompanyResponse.Company), static x => x.Company, static (x, v) => x.Company = v)
                .Member(nameof(Bee.Api.Core.Messages.System.EnterCompanyResponse.Capabilities), static x => x.Capabilities, static (x, v) => x.Capabilities = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetCommonConfigurationRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetCommonConfigurationRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetCommonConfigurationResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetCommonConfigurationResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetCommonConfigurationResponse.CommonConfiguration), static x => x.CommonConfiguration, static (x, v) => x.CommonConfiguration = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetCustomizePluginSettingsRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetCustomizePluginSettingsRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetCustomizePluginSettingsRequest.CustomizeId), static x => x.CustomizeId, static (x, v) => x.CustomizeId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetCustomizePluginSettingsResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetCustomizePluginSettingsResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetCustomizePluginSettingsResponse.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetDefineRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetDefineRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetDefineRequest.DefineType), static x => x.DefineType, static (x, v) => x.DefineType = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetDefineRequest.Keys), static x => x.Keys, static (x, v) => x.Keys = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetDefineResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetDefineResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetDefineResponse.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetDepartmentTreeRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetDepartmentTreeRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetDepartmentTreeResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetDepartmentTreeResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetDepartmentTreeResponse.Tree), static x => x.Tree, static (x, v) => x.Tree = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetFormLayoutRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormLayoutRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormLayoutRequest.ProgId), static x => x.ProgId, static (x, v) => x.ProgId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormLayoutRequest.LayoutId), static x => x.LayoutId, static (x, v) => x.LayoutId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetFormLayoutResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormLayoutResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormLayoutResponse.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetFormSchemaRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormSchemaRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormSchemaRequest.ProgId), static x => x.ProgId, static (x, v) => x.ProgId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetFormSchemaResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormSchemaResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetFormSchemaResponse.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetLanguageRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetLanguageRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetLanguageRequest.Lang), static x => x.Lang, static (x, v) => x.Lang = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetLanguageRequest.Namespace), static x => x.Namespace, static (x, v) => x.Namespace = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetLanguageResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetLanguageResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetLanguageResponse.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetPackageRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.AppId), static x => x.AppId, static (x, v) => x.AppId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.ComponentId), static x => x.ComponentId, static (x, v) => x.ComponentId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.Version), static x => x.Version, static (x, v) => x.Version = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.Platform), static x => x.Platform, static (x, v) => x.Platform = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.Channel), static x => x.Channel, static (x, v) => x.Channel = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageRequest.FileId), static x => x.FileId, static (x, v) => x.FileId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.GetPackageResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.FileName), static x => x.FileName, static (x, v) => x.FileName = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.Content), static x => x.Content, static (x, v) => x.Content = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.FileSize), static x => x.FileSize, static (x, v) => x.FileSize = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.Sha256), static x => x.Sha256, static (x, v) => x.Sha256 = v)
                .Member(nameof(Bee.Api.Core.Messages.System.GetPackageResponse.PackageUrl), static x => x.PackageUrl, static (x, v) => x.PackageUrl = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LeaveCompanyRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.LeaveCompanyRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LeaveCompanyResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.LeaveCompanyResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.ListApiKeysRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.ListApiKeysRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.ListApiKeysResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.ListApiKeysResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.ListApiKeysResponse.ApiKeys), static x => x.ApiKeys, static (x, v) => x.ApiKeys = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LoginRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.LoginRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginRequest.Password), static x => x.Password, static (x, v) => x.Password = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginRequest.ClientPublicKey), static x => x.ClientPublicKey, static (x, v) => x.ClientPublicKey = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LoginResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.AccessToken), static x => x.AccessToken, static (x, v) => x.AccessToken = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.ApiEncryptionKey), static x => x.ApiEncryptionKey, static (x, v) => x.ApiEncryptionKey = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.UserName), static x => x.UserName, static (x, v) => x.UserName = v)
                .Member(nameof(Bee.Api.Core.Messages.System.LoginResponse.TimeZone), static x => x.TimeZone, static (x, v) => x.TimeZone = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LogoutRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.LogoutRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.LogoutResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.LogoutResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.PingRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.PingRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingRequest.ClientName), static x => x.ClientName, static (x, v) => x.ClientName = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingRequest.TraceId), static x => x.TraceId, static (x, v) => x.TraceId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.PingResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.Status), static x => x.Status, static (x, v) => x.Status = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.ServerTime), static x => x.ServerTime, static (x, v) => x.ServerTime = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.ApiKeyStatus), static x => x.ApiKeyStatus, static (x, v) => x.ApiKeyStatus = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.Version), static x => x.Version, static (x, v) => x.Version = v)
                .Member(nameof(Bee.Api.Core.Messages.System.PingResponse.TraceId), static x => x.TraceId, static (x, v) => x.TraceId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsRequest.CustomizeId), static x => x.CustomizeId, static (x, v) => x.CustomizeId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsRequest.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveCustomizePluginSettingsResponse.PluginCount), static x => x.PluginCount, static (x, v) => x.PluginCount = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SaveDefineRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.SaveDefineRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveDefineRequest.DefineType), static x => x.DefineType, static (x, v) => x.DefineType = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveDefineRequest.Xml), static x => x.Xml, static (x, v) => x.Xml = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SaveDefineRequest.Keys), static x => x.Keys, static (x, v) => x.Keys = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SaveDefineResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.SaveDefineResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetApiKeyEnabledRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledRequest.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledRequest.Enabled), static x => x.Enabled, static (x, v) => x.Enabled = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetApiKeyEnabledResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledResponse.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyEnabledResponse.Enabled), static x => x.Enabled, static (x, v) => x.Enabled = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetApiKeyExpiryRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryRequest.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryRequest.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetApiKeyExpiryResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryResponse.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetApiKeyExpiryResponse.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetDeploymentAdminRequest>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminRequest.IsDeploymentAdmin), static x => x.IsDeploymentAdmin, static (x, v) => x.IsDeploymentAdmin = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.System.SetDeploymentAdminResponse>()
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminResponse.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.System.SetDeploymentAdminResponse.IsDeploymentAdmin), static x => x.IsDeploymentAdmin, static (x, v) => x.IsDeploymentAdmin = v)
                .Build());
        }
    }
}
