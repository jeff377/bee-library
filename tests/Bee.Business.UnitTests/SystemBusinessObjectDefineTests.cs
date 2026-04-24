using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 與 <c>BackendInfo.DefineAccess</c> 整合的純邏輯測試（記憶體存取，不走 DB）。
    /// </summary>
    [Collection("Initialize")]
    public class SystemBusinessObjectDefineTests
    {
        [Fact]
        [DisplayName("GetCommonConfiguration 應回傳非空 XML")]
        public void GetCommonConfiguration_ReturnsNonEmptyXml()
        {
            var bo = new SystemBusinessObject(Guid.Empty);

            var result = bo.GetCommonConfiguration(new GetCommonConfigurationArgs());

            Assert.False(string.IsNullOrWhiteSpace(result.CommonConfiguration));
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 DatabaseSettings 應回傳 XML")]
        public void GetDefine_LocalCallDatabaseSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.DatabaseSettings };

            var result = bo.GetDefine(args);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 SystemSettings 應回傳 XML")]
        public void GetDefine_LocalCallSystemSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.SystemSettings };

            var result = bo.GetDefine(args);

            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("SaveDefine 本地呼叫 SystemSettings 應成功執行並回傳結果")]
        public void SaveDefine_LocalCall_SystemSettings_ReturnsResult()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);
            var xml = bo.GetDefine(new GetDefineArgs { DefineType = DefineType.SystemSettings }).Xml;

            var result = bo.SaveDefine(new SaveDefineArgs { DefineType = DefineType.SystemSettings, Xml = xml });

            Assert.NotNull(result);
        }
    }
}
