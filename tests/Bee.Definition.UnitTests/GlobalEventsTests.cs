using System.ComponentModel;
using Bee.Definition;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// GlobalEvents 測試。
    /// </summary>
    public class GlobalEventsTests
    {
        [Fact]
        [DisplayName("RaiseDatabaseSettingsChanged 應觸發已訂閱的 handler")]
        public void RaiseDatabaseSettingsChanged_InvokesSubscribedHandler()
        {
            var invoked = 0;
            EventHandler handler = (sender, e) => invoked++;
            GlobalEvents.DatabaseSettingsChanged += handler;
            try
            {
                GlobalEvents.RaiseDatabaseSettingsChanged();

                Assert.Equal(1, invoked);
            }
            finally
            {
                GlobalEvents.DatabaseSettingsChanged -= handler;
            }
        }

        [Fact]
        [DisplayName("RaiseDatabaseSettingsChanged 應以 sender=null、args=Empty 呼叫 handler")]
        public void RaiseDatabaseSettingsChanged_PassesNullSenderAndEmptyArgs()
        {
            object? capturedSender = new object();
            EventArgs? capturedArgs = null;
            EventHandler handler = (sender, e) =>
            {
                capturedSender = sender;
                capturedArgs = e;
            };
            GlobalEvents.DatabaseSettingsChanged += handler;
            try
            {
                GlobalEvents.RaiseDatabaseSettingsChanged();

                Assert.Null(capturedSender);
                Assert.Same(EventArgs.Empty, capturedArgs);
            }
            finally
            {
                GlobalEvents.DatabaseSettingsChanged -= handler;
            }
        }

        [Fact]
        [DisplayName("取消訂閱後 handler 不應被呼叫")]
        public void Unsubscribe_HandlerNotInvoked()
        {
            var invoked = 0;
            EventHandler handler = (sender, e) => invoked++;

            GlobalEvents.DatabaseSettingsChanged += handler;
            GlobalEvents.DatabaseSettingsChanged -= handler;

            GlobalEvents.RaiseDatabaseSettingsChanged();

            Assert.Equal(0, invoked);
        }
    }
}
