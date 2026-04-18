using System;
using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using MessagePack;
using MessagePack.Resolvers;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// SafeMessagePackSerializerOptions 測試：
    /// 驗證 ThrowIfDeserializingTypeIsDisallowed 會依白名單擋下不合法型別，
    /// 並確認 Clone 以同類型回傳新的 instance。
    /// </summary>
    public class SafeMessagePackSerializerOptionsTests
    {
        private static SafeMessagePackSerializerOptions Create()
            => new SafeMessagePackSerializerOptions(StandardResolver.Instance);

        [Fact]
        [DisplayName("不在白名單中的型別應拋 InvalidOperationException")]
        public void ThrowIfDisallowed_TypeNotInWhitelist_Throws()
        {
            var options = Create();

            var ex = Assert.Throws<InvalidOperationException>(
                () => options.ThrowIfDeserializingTypeIsDisallowed(typeof(Random)));

            Assert.Contains("not in the allowed type whitelist", ex.Message);
            Assert.Contains("System.Random", ex.Message);
        }

        [Fact]
        [DisplayName("白名單中的 primitive 型別應不拋出例外")]
        public void ThrowIfDisallowed_AllowedPrimitive_DoesNotThrow()
        {
            var options = Create();

            // System.String 是 AllowedPrimitiveTypes 成員
            var ex = Record.Exception(() => options.ThrowIfDeserializingTypeIsDisallowed(typeof(string)));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("WithResolver 複製後型別應為 SafeMessagePackSerializerOptions")]
        public void Clone_ViaWithResolver_ReturnsSafeOptions()
        {
            var options = Create();

            // WithResolver 內部會呼叫 Clone 產生同型別的新 instance
            var cloned = options.WithResolver(ContractlessStandardResolver.Instance);

            Assert.IsType<SafeMessagePackSerializerOptions>(cloned);
            Assert.NotSame(options, cloned);
        }
    }
}
