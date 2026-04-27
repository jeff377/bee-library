using System.ComponentModel;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests
{
    public class ApiErrorInfoTests
    {
        [Fact]
        [DisplayName("無參建構子應產生空訊息與未處理狀態")]
        public void DefaultCtor_InitializesEmpty()
        {
            var info = new ApiErrorInfo();
            Assert.Equal(string.Empty, info.Message);
            Assert.Equal(string.Empty, info.StackTrace);
            Assert.False(info.IsHandle);
            Assert.Equal(string.Empty, info.ToString());
        }

        [Fact]
        [DisplayName("由例外建構時應複製 Message，預設不包含 StackTrace")]
        public void FromException_CopiesMessage_OmitsStackTraceByDefault()
        {
            InvalidOperationException inner;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException e) { inner = e; }

            var info = new ApiErrorInfo(inner);

            Assert.Equal("boom", info.Message);
            Assert.Equal(string.Empty, info.StackTrace);
            Assert.Equal("boom", info.ToString());
        }

        [Fact]
        [DisplayName("由例外建構且允許 StackTrace 時應填入非空堆疊")]
        public void FromException_IncludeStackTrace_PopulatesStackTrace()
        {
            InvalidOperationException inner;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException e) { inner = e; }

            var info = new ApiErrorInfo(inner, includeStackTrace: true);

            Assert.False(string.IsNullOrEmpty(info.StackTrace));
        }
    }
}
