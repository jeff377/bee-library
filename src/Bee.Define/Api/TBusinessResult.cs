using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 商業邏輯物件方法傳出結果基底類別。
    /// </summary>
    [Serializable]
    public abstract class TBusinessResult
    {
        private TParameterCollection _parameters = null;

        /// <summary>
        /// 傳出參數集合。
        /// </summary>
        [Key(0)]
        public TParameterCollection Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = new TParameterCollection();
                }
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }
    }
}
