using System.ComponentModel;
using Bee.Business.Form;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 補強 <see cref="DefaultFormBoTypeResolver"/> 的測試覆蓋率。
    /// 此 resolver 為最小化實作，任何 progId 皆回傳 <see cref="FormBusinessObject"/>。
    /// </summary>
    public class DefaultFormBoTypeResolverTests
    {
        [Fact]
        [DisplayName("DefaultFormBoTypeResolver.Resolve 任何 progId 皆應回傳 FormBusinessObject 型別")]
        public void Resolve_AnyProgId_ReturnsFormBusinessObjectType()
        {
            var resolver = new DefaultFormBoTypeResolver();

            Assert.Equal(typeof(FormBusinessObject), resolver.Resolve("AnyProgId"));
        }

        [Fact]
        [DisplayName("DefaultFormBoTypeResolver.Resolve 空字串 progId 應回傳 FormBusinessObject 型別")]
        public void Resolve_EmptyProgId_ReturnsFormBusinessObjectType()
        {
            var resolver = new DefaultFormBoTypeResolver();

            Assert.Equal(typeof(FormBusinessObject), resolver.Resolve(string.Empty));
        }

        [Fact]
        [DisplayName("IFormBoTypeResolver 預設 Resolve(customizeId, progId) 委派 Resolve(progId) 應回傳 FormBusinessObject 型別")]
        public void Resolve_WithCustomizeId_DefaultMethodDelegatesToBaseResolve()
        {
            IFormBoTypeResolver resolver = new DefaultFormBoTypeResolver();

            Type result = resolver.Resolve("acme", "AnyProgId");

            Assert.Equal(typeof(FormBusinessObject), result);
        }
    }
}
