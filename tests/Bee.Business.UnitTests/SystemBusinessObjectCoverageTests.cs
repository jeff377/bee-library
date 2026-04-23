using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 覆蓋率補強測試：
    /// 涵蓋 DoExecFunc 委派路徑與 SaveDefineCore 內部流程。
    /// </summary>
    [Collection("Initialize")]
    public class SystemBusinessObjectCoverageTests
    {
        private static readonly string[] s_departmentKeys = { "Department" };

        [Fact]
        [DisplayName("ExecFunc 呼叫不存在的方法應拋 MissingMethodException（覆蓋 DoExecFunc 委派路徑）")]
        public void ExecFunc_UnknownMethod_ThrowsMissingMethodException()
        {
            var bo = new SystemBusinessObject(Guid.Empty);
            var args = new ExecFuncArgs("NoSuchMethodInHandler");

            Assert.Throws<MissingMethodException>(() => bo.ExecFunc(args));
        }

        [Fact]
        [DisplayName("SaveDefine 非敏感型別傳入空 XML 應拋 InvalidOperationException（SaveDefineCore null 路徑）")]
        public void SaveDefine_NonSensitiveType_EmptyXml_ThrowsInvalidOperation()
        {
            // Empty xml → SerializeFunc.XmlToObject returns null → InvalidOperationException in SaveDefineCore
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: false);
            var args = new SaveDefineArgs { DefineType = DefineType.FormSchema, Xml = string.Empty };

            Assert.Throws<InvalidOperationException>(() => bo.SaveDefine(args));
        }

        [Fact]
        [DisplayName("SaveDefine 本地呼叫傳入有效 XML 應成功完成存檔（SaveDefineCore 成功路徑）")]
        public void SaveDefine_LocalCall_ValidXml_Succeeds()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);

            // Retrieve valid FormSchema XML via GetDefine
            var getResult = bo.GetDefine(new GetDefineArgs
            {
                DefineType = DefineType.FormSchema,
                Keys = s_departmentKeys
            });

            // Round-trip save: write back the same content
            var result = bo.SaveDefine(new SaveDefineArgs
            {
                DefineType = DefineType.FormSchema,
                Xml = getResult.Xml,
                Keys = s_departmentKeys
            });

            Assert.NotNull(result);
        }
    }
}
