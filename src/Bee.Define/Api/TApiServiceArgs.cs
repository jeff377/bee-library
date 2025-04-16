using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 呼叫 API 服務傳入引數。 
    /// </summary>
    [Serializable]
    public class TApiServiceArgs : IObjectSerializeBase
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TApiServiceArgs()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">傳入資料。</param>
        public TApiServiceArgs(string progID, string action, object value)
        {
            ProgID = progID;
            Action = action;
            Value = value;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        [DefaultValue("")]
        public string ProgID { get; set; } = string.Empty;

        /// <summary>
        /// 執行動作。
        /// </summary>
        [DefaultValue("")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 傳入資料。
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 資料是否加密。
        /// </summary>
        [DefaultValue(false)]
        public bool Encrypted { get; set; } = false;

        /// <summary>
        /// 資料進行加密。
        /// </summary>
        public void Encrypt()
        {
            // 已加密離開
            if (this.Encrypted) { return; }

            byte[] bytes = SerializeFunc.ObjectToBinary(Value);  // 序列化
            var encryption = SysFunc.CreateApiServiceEncryption();
            Value = encryption.Encrypt(bytes);  // 加密
            Encrypted = true;
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        public void Decrypt()
        {
            // 未加密則離開
            if (!this.Encrypted) { return; }

            var encryption = SysFunc.CreateApiServiceEncryption();
            byte[] bytes = encryption.Decrypt(Value as byte[]);  // 解密
            Value = SerializeFunc.BinaryToObject(bytes);  // 反序列化
            Encrypted = false;
        }
    }
}
