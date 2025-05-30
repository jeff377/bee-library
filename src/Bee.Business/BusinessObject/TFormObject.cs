using System;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 表單層級業務邏輯物件。
    /// </summary>
    public class TFormObject : TBusinessObject, IFormObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public TFormObject(Guid accessToken, string progID) : base(accessToken)
        {
            this.ProgID = progID;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgID { get; private set; }
    }
}
