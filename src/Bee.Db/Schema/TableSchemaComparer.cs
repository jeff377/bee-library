using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表結構比對。
    /// </summary>
    public class TableSchemaComparer
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="defineTable">定義的資料表結構。</param>
        /// <param name="realTable">實際的資料表結構。</param>
        public TableSchemaComparer(DbTable defineTable, DbTable realTable)
        {
            DefineTable = defineTable;
            RealTable = realTable;
        }

        /// <summary>
        /// 定義的資料表結構。
        /// </summary>
        public DbTable DefineTable { get; } = null;    

        /// <summary>
        /// 實際的資料表結構。
        /// </summary>
        public DbTable RealTable { get; } = null;

        /// <summary>
        /// 執行比對，並傳回比對後產生的資料表結構。
        /// </summary>
        public DbTable Compare()
        {
            // 建立定義的資料表結構複本，作為比對的回傳結果
            var compareTable = this.DefineTable.Clone();
            // 無實體的資料表結構，直接回傳定義的資料表結構
            if (this.RealTable == null)
            {
                compareTable.UpgradeAction = DbUpgradeAction.New;
                return compareTable;
            }
            // 比對欄位結構
            if (!CompareFields(compareTable))
                compareTable.UpgradeAction = DbUpgradeAction.Upgrade;
            // 比對索引
            if (!CompareIndexes(compareTable))
                compareTable.UpgradeAction = DbUpgradeAction.Upgrade;
            // 加入實體資料表的額外欄位
            if (compareTable.UpgradeAction != DbUpgradeAction.None)
                AddExtensionFields(compareTable);
            return compareTable;
        }

        /// <summary>
        /// 比對欄位結構。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private bool CompareFields(DbTable compareTable)
        {
            bool isMatch = true;
            foreach (DbField field in compareTable.Fields)
            {
                if (this.RealTable.Fields.Contains(field.FieldName))
                {
                    if (!field.Compare(this.RealTable.Fields[field.FieldName]))
                    {
                        // 已存在欄位，升級模式為異動
                        field.UpgradeAction = DbUpgradeAction.Upgrade;
                        isMatch = false;
                    }
                }
                else
                {
                    // 不存在欄位，升級模式為新增
                    field.UpgradeAction = DbUpgradeAction.New;
                    isMatch = false;
                }
            }
            return isMatch;
        }

        /// <summary>
        /// 比對索引。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private bool CompareIndexes(DbTable compareTable)
        {
            // 有任一索引比對不符，則直接回傳 false
            foreach (DbTableIndex index in compareTable.Indexes)
            {
                string name = StrFunc.Format(index.Name, compareTable.TableName);
                if (this.RealTable.Indexes.Contains(name))
                {
                    if (!index.Compare(this.RealTable.Indexes[name]))
                    {
                        // 已存在索引，升級模式為異動
                        index.UpgradeAction = DbUpgradeAction.Upgrade;
                        return false;
                    }
                }
                else
                {
                    // 不存在欄位，升級模式為新增
                    index.UpgradeAction = DbUpgradeAction.New;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 加入實體資料表的額外欄位。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private void AddExtensionFields(DbTable compareTable)
        {
            foreach (DbField field in this.RealTable.Fields)
            {
                if (!compareTable.Fields.Contains(field.FieldName))
                    compareTable.Fields.Add(field.Clone());
            }
        }
    }
}
