using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表結構比對。
    /// </summary>
    public class TTableSchemaComparer
    {
        private readonly TDbTable _DefineTable = null;
        private readonly TDbTable _RealTable = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="defineTable">定義的資料表結構。</param>
        /// <param name="realTable">實際的資料表結構。</param>
        public TTableSchemaComparer(TDbTable defineTable, TDbTable realTable)
        {
            _DefineTable = defineTable;
            _RealTable = realTable;
        }

        /// <summary>
        /// 定義的資料表結構。
        /// </summary>
        public TDbTable DefineTable
        {
            get { return _DefineTable; }
        }

        /// <summary>
        /// 實際的資料表結構。
        /// </summary>
        public TDbTable RealTable
        {
            get { return _RealTable; }
        }

        /// <summary>
        /// 執行比對，並傳回比對後產生的資料表結構。
        /// </summary>
        public TDbTable Compare()
        {
            TDbTable oCompareTable;

            // 建立定義的資料表結構複本，作為比對的回傳結果
            oCompareTable = this.DefineTable.Clone();
            // 無實體的資料表結構，直接回傳定義的資料表結構
            if (this.RealTable == null)
            {
                oCompareTable.UpgradeAction = EDbUpgradeAction.New;
                return oCompareTable;
            }
            // 比對欄位結構
            if (!CompareFields(oCompareTable))
                oCompareTable.UpgradeAction = EDbUpgradeAction.Upgrade;
            // 比對索引
            if (!CompareIndexes(oCompareTable))
                oCompareTable.UpgradeAction = EDbUpgradeAction.Upgrade;
            // 加入實體資料表的額外欄位
            if (oCompareTable.UpgradeAction != EDbUpgradeAction.None)
                AddExtensionFields(oCompareTable);
            return oCompareTable;
        }

        /// <summary>
        /// 比對欄位結構。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private bool CompareFields(TDbTable compareTable)
        {
            bool bCompare;

            bCompare = true;
            foreach (TDbField field in compareTable.Fields)
            {
                if (this.RealTable.Fields.Contains(field.FieldName))
                {
                    if (!field.Compare(this.RealTable.Fields[field.FieldName]))
                    {
                        // 已存在欄位，升級模式為異動
                        field.UpgradeAction = EDbUpgradeAction.Upgrade;
                        bCompare = false;
                    }
                }
                else
                {
                    // 不存在欄位，升級模式為新增
                    field.UpgradeAction = EDbUpgradeAction.New;
                    bCompare = false;
                }
            }
            return bCompare;
        }

        /// <summary>
        /// 比對索引。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private bool CompareIndexes(TDbTable compareTable)
        {
            // 有任一索引比對不符，則直接回傳 false
            foreach (TDbTableIndex index in compareTable.Indexes)
            {
                string name = StrFunc.Format(index.Name, compareTable.TableName);
                if (this.RealTable.Indexes.Contains(name))
                {
                    if (!index.Compare(this.RealTable.Indexes[name]))
                    {
                        // 已存在索引，升級模式為異動
                        index.UpgradeAction = EDbUpgradeAction.Upgrade;
                        return false;
                    }
                }
                else
                {
                    // 不存在欄位，升級模式為新增
                    index.UpgradeAction = EDbUpgradeAction.New;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 加入實體資料表的額外欄位。
        /// </summary>
        /// <param name="compareTable">比對回傳的資料表結構。</param>
        private void AddExtensionFields(TDbTable compareTable)
        {
            foreach (TDbField field in this.RealTable.Fields)
            {
                if (!compareTable.Fields.Contains(field.FieldName))
                    compareTable.Fields.Add(field.Clone());
            }
        }
    }
}
