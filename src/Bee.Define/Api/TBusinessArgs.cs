using System;

namespace Bee.Define
{
    /// <summary>
    /// 商業邏輯物件方法傳入引數基底類別。
    /// </summary>
    [Serializable]
    public abstract class TBusinessArgs
    {
        private TParameterCollection _parameters = null;

        /// <summary>
        /// 傳入參數集合。
        /// </summary>
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
