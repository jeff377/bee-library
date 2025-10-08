using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 欄位對應集合。
    /// </summary>
    [Serializable]
    public class FieldMappingCollection : CollectionBase<FieldMapping>
    {
        /// <summary>
        /// 加入關連取回欄位。
        /// </summary>
        /// <param name="sourceField">來源欄位。</param>
        /// <param name="destinationField">目的欄位。</param>
        public FieldMapping Add(string sourceField, string destinationField)
        {
            var field = new FieldMapping(sourceField, destinationField);
            base.Add(field);
            return field;
        }

        /// <summary>
        /// 依目的欄位尋找成員。
        /// </summary>
        /// <param name="destinationField">目的欄位名稱。</param>
        public FieldMapping FindByDestination(string destinationField)
        {
            foreach (FieldMapping item in this)
            {
                if (StrFunc.IsEquals(item.DestinationField, destinationField))
                    return item;
            }
            return null;
        }
    }
}
