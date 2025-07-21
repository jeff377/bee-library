using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// 記錄選項設定，用於控制各模組是否進行記錄。
    /// </summary>
    [Serializable]
    [Description("記錄選項設定，用於控制各模組是否進行記錄。")]
    [TreeNode("Logging")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class LogOptions
    {
        /// <summary>
        /// 控制 ApiConnector 模組的記錄選項。
        /// </summary>
        [Description("控制 ApiConnector 模組的記錄選項。")]
        public ApiConnectorLogOptions ApiConnector { get; set; } = new ApiConnectorLogOptions();

        /// <summary>
        /// 控制 DbAccess 模組的記錄選項。
        /// </summary>
        [Description("控制 DbAccess 模組的記錄選項。")]
        public DbAccessLogOptions DbAccess { get; set; } = new DbAccessLogOptions();
    }
}
