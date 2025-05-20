using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bee.Define;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 表單基底類別。
    /// </summary>
    public class TForm : Form
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TForm()
        {
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Tahoma", 9);
            if (UIInfo.AppIcon != null)
            {
                Icon = UIInfo.AppIcon;
            }
        }

        #endregion

        /// <summary>
        /// 表單參數集合，執行階段傳遞參數使用。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TParameterCollection? Parameters { get; set; } = null;

        /// <summary>
        /// 判斷表單是否已載入完成。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// 覆寫 OnLoad 方法。
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            IsLoaded = true;
        }
    }
}
