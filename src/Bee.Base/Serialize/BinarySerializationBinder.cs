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
            // 驗證參數型別是否合法
            if (!ValidateType(typeName))
                throw new InvalidOperationException($"Type name '{typeName}' is not allowed.");
            // 傳回序列化型別
            var type = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
            return type;
        }

        /// <summary>
        /// 允許 JSON-RPC 傳遞資料的型別命名空間清單。
        /// 僅允許這些命名空間中的型別進行反序列化，以確保安全性。
        /// 注意：Bee.Base 與 Bee.Define 為系統內建的預設命名空間，無需額外指定。
        /// </summary>
        private static List<string> AllowedTypeNamespaces { get; set; } = new List<string> {
            "System.Collections", "System.Globalization", "System.Data"
        };

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
            // 通用允許型別驗證
            if (SysInfo.IsTypeNameAllowed(typeName)) { return true; }

            // 二進位序列化專用，允許命名空間驗證
            foreach (var ns in AllowedTypeNamespaces)
            {
                if (typeName.StartsWith(ns + "."))
                    return true;
            }
            // 二進位序列化專用，允許允許型別驗證
            return AllowedTypes.Contains(typeName);
        }
    }
}
