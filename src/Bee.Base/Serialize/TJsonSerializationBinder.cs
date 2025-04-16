using System;
using Newtonsoft.Json.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// JSON 序列化驗證型別合法性。
    /// </summary>
    internal class TJsonSerializationBinder : ISerializationBinder
    {
        private static readonly DefaultSerializationBinder Binder = new DefaultSerializationBinder();
        private static readonly bool IsNetCore = !System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");

        /// <summary>
        /// 控制序列化對象與類型的綁定。
        /// </summary>
        /// <param name="serializedType">序列化對象型別。</param>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            Binder.BindToName(serializedType, out assemblyName, out typeName);
        }

        /// <summary>
        /// 控制序列化對象與類型的綁定。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        public Type BindToType(string assemblyName, string typeName)
        {
            // .NET Framework 4.8 → .NET 8
            if (IsNetCore && assemblyName == "mscorlib")
            {
                assemblyName = "System.Private.CoreLib";
            }
            // .NET 8 → .NET Framework 4.8
            else if (!IsNetCore && assemblyName == "System.Private.CoreLib")
            {
                assemblyName = "mscorlib";
            }

            if (!ValidateType(typeName))
                throw new TException($"TypeName={typeName} not allowed");

            return Binder.BindToType(assemblyName, typeName);
        }

        /// <summary>
        /// 驗證序列化型別合法性。
        /// </summary>
        /// <param name="typeName">型別名稱。</param>
        private bool ValidateType(string typeName)
        {
            // 正向列舉允許的命名空間
            if (StrFunc.LeftWith(typeName, "Bee.Base.")) { return true; }
            if (StrFunc.LeftWith(typeName, "Bee.Define.")) { return true; }

            // 正向列舉允許型別
            string[] typeNames = new string[] { "System.Byte[]" };
            foreach (string name in typeNames)
            {
                if (typeName == name) { return true; }
            }
            return false;
        }
    }
}
