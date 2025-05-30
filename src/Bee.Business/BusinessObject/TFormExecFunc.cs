using System;
using System.Collections.Generic;
using System.Text;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 表單層級業務邏輯物件提供的自訂方法。
    /// </summary>
    internal class TFormExecFunc
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TFormExecFunc(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// Hello 測試方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        public void Hello(TExecFuncArgs args, TExecFuncResult result)
        {
            result.Parameters.Add("Hello", "Hello FormObject");
        }
    }
}
