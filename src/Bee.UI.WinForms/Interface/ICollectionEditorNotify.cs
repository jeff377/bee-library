using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 集合屬性編輯器通知。
    /// </summary>
    public interface ICollectionEditorNotify
    {
        /// <summary>
        /// 集合屬性值變更通知。
        /// </summary>
        /// <param name="value">集合屬性值。</param>
        void OnCollectionEditValueChanged(object value);
    }
}
