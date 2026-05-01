using System.ComponentModel;
using System.Reflection;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ExceptionExtensions.Unwrap(Exception)"/> covering plain
    /// exceptions, <see cref="AggregateException"/>, <see cref="TargetInvocationException"/>,
    /// and recursive unwrap scenarios.
    /// </summary>
    public class ExceptionExtensionsTests
    {
        [Fact]
        [DisplayName("Unwrap 於單層 Exception 應回傳原例外")]
        public void Unwrap_PlainException_ReturnsSelf()
        {
            var ex = new InvalidOperationException("plain");
            Assert.Same(ex, ex.Unwrap());
        }

        [Fact]
        [DisplayName("Unwrap 於 AggregateException 應回傳第一個 inner exception")]
        public void Unwrap_AggregateException_ReturnsFirstInner()
        {
            var inner1 = new InvalidOperationException("inner1");
            var inner2 = new NotSupportedException("inner2");
            var agg = new AggregateException(inner1, inner2);

            var result = agg.Unwrap();

            Assert.Same(inner1, result);
        }

        [Fact]
        [DisplayName("Unwrap 於 TargetInvocationException 應回傳 inner exception")]
        public void Unwrap_TargetInvocation_ReturnsInner()
        {
            var inner = new InvalidOperationException("inner");
            var wrapper = new TargetInvocationException(inner);

            var result = wrapper.Unwrap();

            Assert.Same(inner, result);
        }

        [Fact]
        [DisplayName("Unwrap 應遞迴展開多層包裝")]
        public void Unwrap_NestedWrappers_UnwrapsFully()
        {
            var core = new InvalidOperationException("core");
            var target = new TargetInvocationException(core);
            var agg = new AggregateException(target);

            var result = agg.Unwrap();

            Assert.Same(core, result);
        }

        [Fact]
        [DisplayName("Unwrap 於 null 應拋出 ArgumentNullException")]
        public void Unwrap_NullException_Throws()
        {
            Exception? nullEx = null;
            Assert.Throws<ArgumentNullException>(() => nullEx!.Unwrap());
        }
    }
}
