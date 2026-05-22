using System.ComponentModel;
using Bee.Base.Tracing;

namespace Bee.Base.UnitTests
{
    public class TraceListenerTests
    {
        private sealed class CapturingWriter : ITraceWriter
        {
            public List<TraceEvent> Events { get; } = [];
            public void Write(TraceEvent evt) => Events.Add(evt);
        }

        [Fact]
        [DisplayName("TraceStart 應建立含正確屬性的 TraceContext")]
        public void TraceStart_WithFullParameters_ReturnsContextWithCorrectProperties()
        {
            var listener = new TraceListener(new CapturingWriter());

            var ctx = listener.TraceStart(TraceLayers.Business, "detail", name: "TestOp");

            Assert.Equal(TraceLayers.Business, ctx.Layer);
            Assert.Equal("TestOp", ctx.Name);
            Assert.Equal("detail", ctx.Detail);
            Assert.True(ctx.Stopwatch.IsRunning);
        }

        [Fact]
        [DisplayName("TraceStart 省略所有選用參數應建立含預設值的 Context")]
        public void TraceStart_WithOnlyRequiredLayer_CreatesContextWithDefaults()
        {
            var listener = new TraceListener(new CapturingWriter());

            var ctx = listener.TraceStart(TraceLayers.Data, name: "TestMethod");

            Assert.Equal(TraceLayers.Data, ctx.Layer);
            Assert.Equal(string.Empty, ctx.Category);
            Assert.Null(ctx.Tag);
            Assert.Equal(string.Empty, ctx.Detail);
        }

        [Fact]
        [DisplayName("TraceEnd 應停止計時器並發送 End 事件")]
        public void TraceEnd_AfterStart_StopsStopwatchAndEmitsEndEvent()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            var ctx = listener.TraceStart(TraceLayers.UI, name: "Op");
            listener.TraceEnd(ctx, TraceStatus.Ok);

            Assert.False(ctx.Stopwatch.IsRunning);
            Assert.Equal(2, writer.Events.Count);
            Assert.Equal(TraceEventKind.End, writer.Events[1].Kind);
        }

        [Fact]
        [DisplayName("TraceEnd 傳入 detail 應覆蓋 Context 原有的 Detail")]
        public void TraceEnd_WithExplicitDetail_OverridesContextDetail()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            var ctx = listener.TraceStart(TraceLayers.Data, "original-detail", name: "Op");
            listener.TraceEnd(ctx, TraceStatus.Error, "override-detail");

            Assert.Equal("override-detail", writer.Events[1].Detail);
            Assert.Equal(TraceStatus.Error, writer.Events[1].Status);
        }

        [Fact]
        [DisplayName("TraceWrite 應發送 Point 事件")]
        public void TraceWrite_WithDetailAndStatus_EmitsPointEvent()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            listener.TraceWrite(TraceLayers.ApiServer, "write-detail", TraceStatus.Cancelled, name: "WriteOp");

            var evt = Assert.Single(writer.Events);
            Assert.Equal(TraceEventKind.Point, evt.Kind);
            Assert.Equal(TraceLayers.ApiServer, evt.Layer);
            Assert.Equal("write-detail", evt.Detail);
            Assert.Equal(TraceStatus.Cancelled, evt.Status);
            Assert.Equal(0, evt.DurationMs);
        }

        [Fact]
        [DisplayName("TraceWrite 省略所有選用參數應發送預設狀態的 Point 事件")]
        public void TraceWrite_WithOnlyRequiredLayer_EmitsDefaultStatusEvent()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            listener.TraceWrite(TraceLayers.None, name: "TestMethod");

            var evt = Assert.Single(writer.Events);
            Assert.Equal(TraceEventKind.Point, evt.Kind);
            Assert.Equal(TraceStatus.Ok, evt.Status);
            Assert.Equal(string.Empty, evt.Detail);
            Assert.Equal(string.Empty, evt.Category);
            Assert.Null(evt.Tag);
        }
    }
}
