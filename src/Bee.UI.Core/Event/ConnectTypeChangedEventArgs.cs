using System;
using System.Collections.Generic;
using System.Text;
using Bee.Define;

namespace Bee.UI.Core
{
    /// <summary>
    /// 連線方式異動事件參數。
    /// </summary>
    public class ConnectTypeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="connectType">服務連線方式。</param>
        /// <param name="endpoint">服務端點。</param>
        public ConnectTypeChangedEventArgs(ConnectType connectType, string endpoint)
        {
            ConnectType = connectType;
            Endpoint = endpoint;
        }

        /// <summary>
        /// 服務連線方式。
        /// </summary>
        public ConnectType ConnectType { get; }

        /// <summary>
        /// 服務端點。
        /// </summary>
        public string Endpoint { get; }
    }

}
