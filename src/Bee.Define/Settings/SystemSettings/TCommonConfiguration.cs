using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 通用參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("CommonConfiguration")]
    [Description("通用參數及環境設置。")]
    [TreeNode("通用")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TCommonConfiguration
    {
        /// <summary>
        /// 系統主版琥。
        /// </summary>
        [Description("系統版號。")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否為偵錯模式。
        /// </summary>
        [Description("是否為偵錯模式")]
        [DefaultValue(false)]
        public bool IsDebugMode { get; set; } = false;

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            SysInfo.Version = Version;
            SysInfo.IsDebugMode = IsDebugMode;
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "CommonConfiguration";
        }
    }
}
