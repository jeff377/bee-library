using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// 二進位序列化驗證型別合法性。
    /// </summary>
    internal class BinarySerializationBinder : SerializationBinder
    {
        /// <summary>
        /// 控制序列化物件與類型的繫結。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type oType;

            // 驗證參數型別是否合法
            if (!ValidateType(typeName))
                throw new InvalidOperationException($"Type name '{typeName}' is not allowed.");
            // 傳回序列化型別
            oType = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
            return oType;
        }

        /// <summary>
        /// 允許型別集合。
        /// </summary>
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>
        {
                "System.Byte[]", "System.Guid", "System.CultureAwareComparer",
                "System.Version", "System.UnitySerializationHolder"
        };

        /// <summary>
        /// 驗證參數型別是否合法。
        /// </summary>
        /// <param name="typeName">型別名稱。</param>
        private bool ValidateType(string typeName)
        {
            // 正向列舉允許的命名空間
            if (typeName.StartsWith("Bee.Base.") ||
                typeName.StartsWith("Bee.Define.") ||
                typeName.StartsWith("System.Collections.") ||
                typeName.StartsWith("System.Globalization.") ||
                typeName.StartsWith("System.Data."))
            {
                return true;
            }

            // 正向列舉允許型別
            return AllowedTypes.Contains(typeName);
        }
    }
}
