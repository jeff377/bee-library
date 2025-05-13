using System.Collections.Generic;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 可序列化的資料關聯，用於描述資料表之間的父子關係。
    /// </summary>
    [MessagePackObject]
    public class TSerializableDataRelation
    {
        /// <summary>
        /// 關聯名稱。
        /// </summary>
        [Key(0)]
        public string RelationName { get; set; }

        /// <summary>
        /// 父資料表名稱。
        /// </summary>
        [Key(1)]
        public string ParentTable { get; set; }

        /// <summary>
        /// 子資料表名稱。
        /// </summary>
        [Key(2)]
        public string ChildTable { get; set; }

        /// <summary>
        /// 父資料表的欄位名稱集合（對應關聯鍵）。
        /// </summary>
        [Key(3)]
        public List<string> ParentColumns { get; set; }

        /// <summary>
        /// 子資料表的欄位名稱集合（對應關聯鍵）。
        /// </summary>
        [Key(4)]
        public List<string> ChildColumns { get; set; }

        /// <summary>
        /// 建構函式，初始化欄位集合。
        /// </summary>
        public TSerializableDataRelation()
        {
            ParentColumns = new List<string>();
            ChildColumns = new List<string>();
        }
    }

}
