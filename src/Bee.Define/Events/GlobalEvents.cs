using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Define
{
    /// <summary>
    /// 全域事件，用於跨專案通知。
    /// </summary>
    public static class GlobalEvents
    {
        /// <summary>
        /// 當資料庫設定變更時觸發。
        /// </summary>
        public static event EventHandler DatabaseSettingsChanged;

        /// <summary>
        /// 觸發資料庫設定變更事件。
        /// </summary>
        public static void RaiseDatabaseSettingsChanged()
        {
            DatabaseSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
