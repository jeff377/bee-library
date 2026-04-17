using Bee.Business;
using Bee.Business.BusinessObjects;

namespace Bee.Business.UnitTests.Fakes
{
    /// <summary>
    /// 測試用的 <see cref="BusinessObject"/> 子類，可驗證 <see cref="BusinessObject.ExecFunc"/>
    /// 與 <see cref="BusinessObject.ExecFuncAnonymous"/> 的派發是否正確呼叫 DoExecFunc* 覆寫方法。
    /// </summary>
    public class TestableBusinessObject : BusinessObject
    {
        public TestableBusinessObject(Guid accessToken, bool isLocalCall = true)
            : base(accessToken, isLocalCall)
        {
        }

        public int ExecFuncCallCount { get; private set; }
        public int ExecFuncAnonymousCallCount { get; private set; }
        public ExecFuncArgs? LastArgs { get; private set; }

        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            ExecFuncCallCount++;
            LastArgs = args;
            result.Parameters.Add("Marker", "DoExecFunc");
        }

        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            ExecFuncAnonymousCallCount++;
            LastArgs = args;
            result.Parameters.Add("Marker", "DoExecFuncAnonymous");
        }
    }

    /// <summary>
    /// 不覆寫任何 DoExecFunc* 的測試類別，驗證基底空實作不拋例外。
    /// </summary>
    public class BareBusinessObject : BusinessObject
    {
        public BareBusinessObject(Guid accessToken, bool isLocalCall = true)
            : base(accessToken, isLocalCall)
        {
        }
    }
}
