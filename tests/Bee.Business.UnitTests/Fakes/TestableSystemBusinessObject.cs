using Bee.Business.System;

namespace Bee.Business.UnitTests.Fakes
{
    /// <summary>
    /// 測試用 <see cref="SystemBusinessObject"/> 子類，允許自訂 <c>AuthenticateUser</c> 的回傳行為，
    /// 讓 Login 流程可在單元測試中走完成功或失敗分支。
    /// </summary>
    public class TestableSystemBusinessObject : SystemBusinessObject
    {
        private readonly Func<LoginArgs, (bool Authenticated, string UserName)> _authenticator;

        public TestableSystemBusinessObject(
            Guid accessToken,
            Func<LoginArgs, (bool Authenticated, string UserName)> authenticator,
            bool isLocalCall = true)
            : base(accessToken, isLocalCall)
        {
            _authenticator = authenticator;
        }

        protected override bool AuthenticateUser(LoginArgs args, out string userName)
        {
            var (authenticated, name) = _authenticator(args);
            userName = name;
            return authenticated;
        }
    }
}
