using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Base.UnitTests
{
    public class AssemblyLoaderExtraTests
    {
        [Fact]
        [DisplayName("GetType 應支援分離的組件名稱與型別名稱參數")]
        public void GetType_WithSeparateAssemblyAndTypeName_ReturnsType()
        {
            var type = AssemblyLoader.GetType("Bee.Base.dll", "Bee.Base.Attributes.TreeNodeAttribute");
            Assert.Equal(typeof(TreeNodeAttribute), type);
        }

        [Fact]
        [DisplayName("CreateInstance 應支援分離的組件名稱與型別名稱參數")]
        public void CreateInstance_SeparateAssemblyAndTypeName_ReturnsInstance()
        {
            var instance = AssemblyLoader.CreateInstance("Bee.Base.dll", "Bee.Base.Attributes.TreeNodeAttribute");
            Assert.IsType<TreeNodeAttribute>(instance);
        }

        [Fact]
        [DisplayName("CreateInstance 分離參數應支援建構子引數")]
        public void CreateInstance_SeparateParamsWithCtorArgs_UsesMatchingConstructor()
        {
            var instance = AssemblyLoader.CreateInstance(
                "Bee.Base.dll", "Bee.Base.Attributes.TreeNodeAttribute", "msg", true);
            var result = Assert.IsType<TreeNodeAttribute>(instance);
            Assert.Equal("msg", result.DisplayFormat);
            Assert.True(result.CollectionFolder);
        }
    }
}
