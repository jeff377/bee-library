using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫結構集合。
    /// </summary>
    [Serializable]
    [Description("資料庫結構集合。")]
    [TreeNode("資料庫結構", false)]
    public class DbSchemaCollection : KeyCollectionBase<DbSchema>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="settings">資料表清單。</param>
        public DbSchemaCollection(DbSchemaSettings settings) : base(settings)
        { }
    }
}
