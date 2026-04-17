using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class AssemblyLoaderTests
    {
        private const string BaseAssembly = "Bee.Base.dll";

        [Fact]
        [DisplayName("FindAssembly 應能從 AppDomain 找到已載入組件")]
        public void FindAssembly_AlreadyLoaded_ReturnsAssembly()
        {
            var assembly = AssemblyLoader.FindAssembly(BaseAssembly);
            Assert.NotNull(assembly);
            Assert.Equal("Bee.Base", assembly!.GetName().Name);
        }

        [Fact]
        [DisplayName("FindAssembly 重複呼叫應命中快取")]
        public void FindAssembly_RepeatedCalls_ReturnSameInstance()
        {
            var first = AssemblyLoader.FindAssembly(BaseAssembly);
            var second = AssemblyLoader.FindAssembly(BaseAssembly);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        [DisplayName("FindAssembly 於未知名稱應回傳 null")]
        public void FindAssembly_UnknownName_ReturnsNull()
        {
            Assert.Null(AssemblyLoader.FindAssembly("Does.Not.Exist.dll"));
        }

        [Fact]
        [DisplayName("IsAssemblyLoaded 應正確回報載入狀態")]
        public void IsAssemblyLoaded_ReflectsLoadState()
        {
            Assert.True(AssemblyLoader.IsAssemblyLoaded(BaseAssembly));
            Assert.False(AssemblyLoader.IsAssemblyLoaded("Does.Not.Exist.dll"));
        }

        [Fact]
        [DisplayName("LoadAssembly 對已載入組件應回傳快取實例")]
        public void LoadAssembly_AlreadyLoaded_ReturnsCached()
        {
            var first = AssemblyLoader.LoadAssembly(BaseAssembly);
            var second = AssemblyLoader.LoadAssembly(BaseAssembly);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        [DisplayName("GetType 應支援「類型, 組件」格式")]
        public void GetType_WithAssemblyQualifiedName_ReturnsType()
        {
            var type = AssemblyLoader.GetType("Bee.Base.ConnectionTestResult, Bee.Base");
            Assert.Equal(typeof(ConnectionTestResult), type);
        }

        [Fact]
        [DisplayName("GetType 應支援純型別名稱（由命名空間推斷組件）")]
        public void GetType_WithFullTypeName_ReturnsType()
        {
            var type = AssemblyLoader.GetType("Bee.Base.ConnectionTestResult");
            Assert.Equal(typeof(ConnectionTestResult), type);
        }

        [Fact]
        [DisplayName("CreateInstance 應建立指定型別的新物件")]
        public void CreateInstance_ReturnsInstance()
        {
            var instance = AssemblyLoader.CreateInstance("Bee.Base.ConnectionTestResult, Bee.Base");
            Assert.IsType<ConnectionTestResult>(instance);
        }

        [Fact]
        [DisplayName("CreateInstance 應支援建構子參數")]
        public void CreateInstance_WithConstructorArgs_UsesMatchingConstructor()
        {
            var instance = AssemblyLoader.CreateInstance(
                "Bee.Base.ConnectionTestResult, Bee.Base", true, "ok");
            var result = Assert.IsType<ConnectionTestResult>(instance);
            Assert.True(result.IsSuccess);
            Assert.Equal("ok", result.Message);
        }
    }
}
