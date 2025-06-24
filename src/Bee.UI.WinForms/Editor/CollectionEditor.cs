using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 集合屬性編輯器。
    /// </summary>
    public class CollectionEditor : System.ComponentModel.Design.CollectionEditor
    {
        private IServiceProvider? _provider;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="Type">型別。</param>
        public CollectionEditor(Type Type) : base(Type)
        { }

        #endregion

        /// <summary>
        /// 覆寫。建立集合屬性編輯器表單。
        /// </summary>
        protected override CollectionForm CreateCollectionForm()
        {
            var form = base.CreateCollectionForm();
            var fieldInfo = form.GetType().GetField("propertyBrowser", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                var propertyGrid = fieldInfo.GetValue(form) as PropertyGrid;
                if (propertyGrid != null)
                {
                    propertyGrid.HelpVisible = true;
                }
            }
            return form;
        }

        /// <summary>
        /// 開啟集合編輯器引發事件。
        /// </summary>
        /// <param name="context">提供元件的內容資訊，例如其容器和屬性描述項。</param>
        /// <param name="provider">定義機制來擷取服務物件，也就是為其他物件提供自訂支援的物件。</param>
        /// <param name="value">編輯的物件。</param>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _provider = provider;
            return base.EditValue(context, provider, value);
        }

        /// <summary>
        /// 將編輯值的結果填入陣列中。
        /// </summary>
        /// <param name="editValue">編輯值。</param>
        /// <param name="value">陣列。</param>
        protected override object SetItems(object editValue, object[] value)
        {
            if (_provider is ICollectionEditorNotify notify)
            {
                notify.OnCollectionEditValueChanged(editValue);
            }
            return base.SetItems(editValue, value);
        }
    }
}
