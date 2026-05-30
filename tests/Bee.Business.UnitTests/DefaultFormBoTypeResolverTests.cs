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
            IFormBoTypeResolver resolver = new DefaultFormBoTypeResolver();

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
        [DisplayName("IFormBoTypeResolver.Resolve(customizeId, progId) 預設方法應委派至 Resolve(progId)，customizeId 被忽略")]
        public void Resolve_WithCustomizeIdAndProgId_DefaultDelegatesToBaseResolve()
        {
            IFormBoTypeResolver resolver = new DefaultFormBoTypeResolver();

            var result = resolver.Resolve("acme", "AnyProgId");

            Assert.Equal(typeof(FormBusinessObject), result);
        }
    }
}
