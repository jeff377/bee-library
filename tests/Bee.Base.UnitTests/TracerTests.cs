using System.ComponentModel;
using Bee.Base.Tracing;

namespace Bee.Base.UnitTests
{
    [Collection("SysInfoStatic")]
    public class TracerTests : IDisposable
    {
        private sealed class CapturingWriter : ITraceWriter
        {
            public List<TraceEvent> Events { get; } = new();
            public void Write(TraceEvent evt) => Events.Add(evt);
        }

        private readonly ITraceListener? _originalListener;

        public TracerTests()
        {
            _originalListener = SysInfo.TraceListener;
            SysInfo.TraceListener = null;
        }

        public void Dispose()
        {
            SysInfo.TraceListener = _originalListener;
            GC.SuppressFinalize(this);
        }

        [Fact]
        [DisplayName("TraceListener 建構子傳入 null writer 應拋出 ArgumentNullException")]
        public void TraceListener_Ctor_NullWriter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TraceListener(null!));
        }

        [Fact]
        [DisplayName("Tracer.Enabled 預設為 false，設定 TraceListener 後為 true")]
        public void Enabled_ReflectsTraceListenerPresence()
        {
            Assert.False(Tracer.Enabled);

            SysInfo.TraceListener = new TraceListener(new CapturingWriter());

            Assert.True(Tracer.Enabled);
        }

        [Fact]
        [DisplayName("Tracer.Start 在未啟用時應回傳 null 且 Writer 不被呼叫")]
        public void Start_WhenDisabled_ReturnsNull()
        {
            var writer = new CapturingWriter();
            // Not attaching the listener to SysInfo.
            _ = new TraceListener(writer);

            var ctx = Tracer.Start(TraceLayers.UI, "detail", name: "op");

            Assert.Null(ctx);
            Assert.Empty(writer.Events);
        }

        [Fact]
        [DisplayName("Tracer.Start 啟用時應建立 TraceContext 並發送 Start 事件")]
        public void Start_WhenEnabled_CreatesContextAndEmitsStartEvent()
        {
            var writer = new CapturingWriter();
            SysInfo.TraceListener = new TraceListener(writer);

            var tag = new object();
            var ctx = Tracer.Start(TraceLayers.Business, "sql-detail", "sql", tag, "MyOp");

            Assert.NotNull(ctx);
            Assert.Equal(TraceLayers.Business, ctx!.Layer);
            Assert.Equal("MyOp", ctx.Name);
            Assert.Equal("sql-detail", ctx.Detail);
            Assert.Equal("sql", ctx.Category);
            Assert.Same(tag, ctx.Tag);
            Assert.True(ctx.Stopwatch.IsRunning);

            var evt = Assert.Single(writer.Events);
            Assert.Equal(TraceEventKind.Start, evt.Kind);
            Assert.Equal(TraceLayers.Business, evt.Layer);
            Assert.Equal("MyOp", evt.Name);
            Assert.Equal("sql-detail", evt.Detail);
            Assert.Equal("sql", evt.Category);
            Assert.Same(tag, evt.Tag);
        }

        [Fact]
        [DisplayName("Tracer.End 啟用時應發送 End 事件並帶入 DurationMs 與狀態")]
        public void End_WhenEnabled_EmitsEndEventWithDurationAndStatus()
        {
            var writer = new CapturingWriter();
            SysInfo.TraceListener = new TraceListener(writer);

            var ctx = Tracer.Start(TraceLayers.Data, name: "Query");
            Tracer.End(ctx, TraceStatus.Error, "override-detail");

            Assert.Equal(2, writer.Events.Count);
            var endEvt = writer.Events[1];
            Assert.Equal(TraceEventKind.End, endEvt.Kind);
            Assert.Equal(TraceStatus.Error, endEvt.Status);
            Assert.Equal("override-detail", endEvt.Detail);
            Assert.True(endEvt.DurationMs >= 0);
            Assert.False(ctx!.Stopwatch.IsRunning);
        }

        [Fact]
        [DisplayName("Tracer.End 當 ctx 為 null 或未啟用時應為 no-op")]
        public void End_WhenDisabledOrNullContext_DoesNothing()
        {
            var writer = new CapturingWriter();

            // Null context while enabled → no emit.
            SysInfo.TraceListener = new TraceListener(writer);
            Tracer.End(null);
            Assert.Empty(writer.Events);

            // Disabled → no emit even when ctx is non-null (ctx is obtained from the listener,
            // but since SysInfo.TraceListener is cleared, Tracer.End skips emission).
            var ctx = SysInfo.TraceListener!.TraceStart(TraceLayers.UI, name: "op");
            writer.Events.Clear();
            SysInfo.TraceListener = null;
            Tracer.End(ctx, TraceStatus.Ok);
            Assert.Empty(writer.Events);
        }

        [Fact]
        [DisplayName("Tracer.Write 啟用時應發送 Point 事件")]
        public void Write_WhenEnabled_EmitsPointEvent()
        {
            var writer = new CapturingWriter();
            SysInfo.TraceListener = new TraceListener(writer);

            var tag = new { Key = "value" };
            Tracer.Write(TraceLayers.ApiServer, "some-detail", TraceStatus.Cancelled, "cat", tag, "PointOp");

            var evt = Assert.Single(writer.Events);
            Assert.Equal(TraceEventKind.Point, evt.Kind);
            Assert.Equal(TraceLayers.ApiServer, evt.Layer);
            Assert.Equal("some-detail", evt.Detail);
            Assert.Equal(TraceStatus.Cancelled, evt.Status);
            Assert.Equal("cat", evt.Category);
            Assert.Same(tag, evt.Tag);
            Assert.Equal("PointOp", evt.Name);
            Assert.Equal(0, evt.DurationMs);
        }

        [Fact]
        [DisplayName("Tracer.Write 未啟用時 Writer 不應被呼叫")]
        public void Write_WhenDisabled_DoesNothing()
        {
            var writer = new CapturingWriter();
            // Not attaching to SysInfo.
            _ = new TraceListener(writer);

            Tracer.Write(TraceLayers.UI, "no-op");

            Assert.Empty(writer.Events);
        }

        [Fact]
        [DisplayName("TraceListener.TraceStart 應填入預設 Category 與空名稱保護")]
        public void TraceListener_TraceStart_AppliesDefaults()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            var ctx = listener.TraceStart(TraceLayers.UI, name: "");

            Assert.Equal(string.Empty, ctx.Name);
            Assert.Equal(string.Empty, ctx.Category);
            Assert.Null(ctx.Tag);
        }

        [Fact]
        [DisplayName("TraceListener.TraceEnd 當 ctx 為 null 應忽略")]
        public void TraceListener_TraceEnd_NullContext_Ignored()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            listener.TraceEnd(null!, TraceStatus.Ok);

            Assert.Empty(writer.Events);
        }

        [Fact]
        [DisplayName("TraceListener.TraceEnd 若未指定 detail 應沿用 ctx.Detail")]
        public void TraceListener_TraceEnd_NullDetail_FallsBackToContextDetail()
        {
            var writer = new CapturingWriter();
            var listener = new TraceListener(writer);

            var ctx = listener.TraceStart(TraceLayers.UI, "ctx-detail", name: "op");
            listener.TraceEnd(ctx, TraceStatus.Ok);

            Assert.Equal(2, writer.Events.Count);
            Assert.Equal("ctx-detail", writer.Events[1].Detail);
        }
    }
}
