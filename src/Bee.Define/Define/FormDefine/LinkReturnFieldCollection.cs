using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 關連取回欄位集合。
    /// </summary>
    [Serializable]
    public class LinkReturnFieldCollection : CollectionBase<LinkReturnField>
    {
        /// <summary>
        /// 加入關連取回欄位。
        /// </summary>
        /// <param name="sourceField">來源欄位。</param>
        /// <param name="destinationField">目的欄位。</param>
        public LinkReturnField Add(string sourceField, string destinationField)
        {
            LinkReturnField oField;

            oField = new LinkReturnField(sourceField, destinationField);
            base.Add(oField);
            return oField;
        }

        /// <summary>
        /// 依目的欄位尋找成員。
        /// </summary>
        /// <param name="destinationField">目的欄位名稱。</param>
        public LinkReturnField FindByDestination(string destinationField)
        {
            foreach (LinkReturnField item in this)
            {
                if (StrFunc.IsEquals(item.DestinationField, destinationField))
                    return item;
            }
            return null;
        }
    }
}
