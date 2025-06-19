using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表關連產生器。
    /// </summary>
    internal class TTableJoinBuilder
    {
        private readonly IDbCommandHelper _Helper = null;
        private readonly TFormDefine _FormDefine = null;
        private readonly string _TableName = string.Empty;
        private readonly string _SelectFields = string.Empty;
        private string _ActiveTableAlias = string.Empty;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="helper">資料庫命令輔助類別。</param>
        /// <param name="formDefine">表單定義。</param>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">取回欄位的集合字串。</param>
        public TTableJoinBuilder(IDbCommandHelper helper, TFormDefine formDefine, string tableName, string selectFields)
        {
            _Helper = helper;
            _FormDefine = formDefine;
            _TableName = tableName;
            _SelectFields = selectFields;
        }

        /// <summary>
        /// 資料庫命令輔助類別。
        /// </summary>
        public IDbCommandHelper Helper
        {
            get { return _Helper; }
        }

        /// <summary>
        /// 表單定義。
        /// </summary>
        public TFormDefine FormDefine
        {
            get { return _FormDefine; }
        }

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        public string TableName
        {
            get { return _TableName; }
        }

        /// <summary>
        /// 取回欄位的集合字串。
        /// </summary>
        public string SelectFields
        {
            get { return _SelectFields; }
        }

        /// <summary>
        /// 目使使用的資料表別名。
        /// </summary>
        public string ActiveTableAlias
        {
            get { return _ActiveTableAlias; }
            set { _ActiveTableAlias = value; }
        }

        /// <summary>
        /// 建立資料表關連資訊提供者。
        /// </summary>
        public TTableJoinProvider Execute()
        {
            TTableJoinProvider oProvider;
            TFormTable oTable;
            TStringHashSet oUseFields, oUseTableFields;

            // 資料表別名初始值
            this.ActiveTableAlias = "A";
            // 使用的表單資料表
            oTable = this.FormDefine.Tables[this.TableName];
            // 取得所有使用的欄位集合
            oUseFields = GetUseFields(oTable);
            // 取得主要資料表使用欄位集合
            oUseTableFields = GetUseTableFields(oUseFields);
            // 建立資料表關連
            oProvider = new TTableJoinProvider(this.Helper);            
            BuildTableJoins(oProvider, oTable, oUseTableFields);




            return oProvider;
        }

        /// <summary>
        /// 取得使用的欄位集合。
        /// </summary>
        /// <param name="table">表單資料表。</param>
        private TStringHashSet GetUseFields(TFormTable table)
        {
            TStringHashSet oUseFields;

            // 取得資料表定義
            oUseFields = new TStringHashSet();
            if (StrFunc.IsEmpty(this.SelectFields))
            {
                // 包含所有欄位
                foreach (TFormField field in table.Fields)
                    oUseFields.Add(field.FieldName);
            }
            else
            {
                // 加入取回欄位
                oUseFields.Add(this.SelectFields, ",");
            }
            return oUseFields;
        }

        /// <summary>
        /// 取得主要資料表使用欄位集合。
        /// </summary>
        /// <param name="useFields">使用欄位集合。</param>
        private TStringHashSet GetUseTableFields(TStringHashSet useFields)
        {
            TStringHashSet oUseFields;

            oUseFields = new TStringHashSet();
            foreach (string fieldName in useFields)
            {
                // 有包含資料表別名的欄位為明細欄位，非主要資料表的欄位
                if (!StrFunc.Contains(fieldName, "."))
                    oUseFields.Add(fieldName);
            }
            return oUseFields;
        }

        /// <summary>
        /// 建立資料表關連。
        /// </summary>
        /// <param name="provider">資料表關連資訊提供者。</param>
        /// <param name="table">表單資料表。</param>
        /// <param name="useFields">使用到的欄位集合。</param>
        /// <param name="detailTableName">明細資料表名稱。</param>
        private void BuildTableJoins(TTableJoinProvider provider, TFormTable table, TStringHashSet useFields, string detailTableName = "")
        {
            TStringHashSet oReturnFields;
            string sLeftTableAlias;
            string sKey;

            foreach (TFormField field in table.Fields)
            {
                // 有設定 LinkProgId 的實際欄位
                if (field.Type == EFieldType.DbField && StrFunc.IsNotEmpty(field.LinkProgId))
                {
                    oReturnFields = GetReturnFields(table, field.FieldName, useFields);
                    if (oReturnFields.Count > 0)
                    {
                        sKey = StrFunc.Format("{0}.{1}.{2}", table.TableName, field.FieldName, field.LinkProgId);
                        sLeftTableAlias = StrFunc.IsEmpty(detailTableName) ? "A" : "DA";
                        // BuildTableJoin(sKey, provider, field, oReturnFields, sLeftTableAlias, detailTableName);
                    }
                }
            }
        }

        ///// <summary>
        ///// 建立資料表關連。
        ///// </summary>
        ///// <param name="key">關連鍵值。</param>
        ///// <param name="provider">資料表關連資訊提供者。</param>
        ///// <param name="field">設定關連程式代碼的表單欄位。</param>
        ///// <param name="returnFields">關連取回欄位集合。</param>
        ///// <param name="leftTableAlias">左側資料表別名。</param>
        ///// <param name="detailTableName">明細資料表名稱。</param>
        ///// <param name="destFieldName">目的欄位名稱。</param>
        //private void BuildTableJoin(string key, TTableJoinProvider provider, TFormField field, TStringHashSet returnFields, string leftTableAlias, string detailTableName, string destFieldName = "")
        //{
        //    TFormDefine oFormDefine;
        //    TFormTable oSourceTable;
        //    TFormField oSourceField;
        //    TFormField oLinkDefineField;
        //    TLinkReturnField oLinkReturnField;
        //    TTableJoin oTableJoin;
        //    TLinkFieldMapping oMapping;
        //    TStringHashSet oReturnFields;
        //    string sKey;

        //    // 取得關連程式定義
        //    oFormDefine = CacheFunc.GetFormDefine(field.LinkProgId);
        //    if (BaseFunc.IsNull(oFormDefine))
        //        throw new TException($"'{field.LinkProgId}' FormDefine not found");

        //    if (BaseFunc.IsNull(oFormDefine.MasterTable))
        //        throw new TException($"'{field.LinkProgId}' MasterTable not found");

        //    // 取得關連資料表定義
        //    oSourceTable = oFormDefine.MasterTable;

        //    foreach (string fieldName in returnFields)
        //    {
        //        oLinkReturnField = field.LinkReturnFields.FindByDestination(fieldName);
        //        if (BaseFunc.IsNull(oLinkReturnField))
        //            throw new TException($"'{field.FieldName}' FormField's LinkReturnFields not find DestinationField '{fieldName}'");

        //        oSourceField = oSourceTable.Fields[oLinkReturnField.SourceField];
        //        if (BaseFunc.IsNull(oSourceField))
        //            throw new TException("'{0}' DefineTable not find '{1}' DefineField", oSourceTable.TableName, oLinkReturnField.SourceField);

        //        if (oSourceField.Type == EFieldType.VirtualField)
        //            throw new TException("'{0}' DefineTable's '{1}' DefineField not allow VirtualField", oSourceTable.TableName, oSourceField.FieldName);

        //        oTableJoin = provider.TableJoins[key];
        //        if (BaseFunc.IsNull(oTableJoin))
        //        {
        //            // 建立資料表關連
        //            oTableJoin = new TTableJoin();
        //            oTableJoin.Key = key;
        //            oTableJoin.LeftTableAlias = leftTableAlias;
        //            oTableJoin.LeftField= field.GetLinkField().DbFieldName;
        //            oTableJoin.RightTable = oSourceTable.DbTableName;
        //            oTableJoin.RightTableAlias = GetNextTableAlias();
        //            oTableJoin.RightField = oSourceTable.Fields[field.GetLinkSourceFieldName()].DbFieldName;
        //            provider.TableJoins.Add(oTableJoin);
        //        }

        //        // 若來源欄位的欄位類型是 LinkField，則需往上階找關連來源
        //        if (oSourceField.Type == EFieldType.LinkField)
        //        {
        //            oLinkDefineField = oSourceField.GetLinkField();
        //            sKey = key + "." + oLinkDefineField.LinkProgId;
        //            oReturnFields = new TStringHashSet();
        //            oReturnFields.Add(oSourceField.DbFieldName);
        //            BuildTableJoin(sKey, provider, oLinkDefineField, oReturnFields, oTableJoin.RightTableAlias, detailTableName, fieldName);
        //        }
        //        else
        //        {
        //            // 記錄關連欄位對應
        //            oMapping = new TLinkFieldMapping();
        //            oMapping.FieldName = (StrFunc.StrIsNotEmpty(destFieldName)) ? destFieldName : fieldName;
        //            if (StrFunc.StrIsNotEmpty(detailTableName))
        //                oMapping.FieldName = StrFunc.StrFormat("{0}.{1}", detailTableName, oMapping.FieldName);
        //            oMapping.SourceTableAlias = oTableJoin.RightTableAlias;
        //            oMapping.SourceFieldName = oSourceField.DbFieldName;
        //            provider.Mappings.Add(oMapping);
        //        }
        //    }
        //}

        /// <summary>
        /// 取得關連取回欄位集合。
        /// </summary>
        /// <param name="table">表單資料表。</param>
        /// <param name="linkFieldName">關連來源欄位。</param>
        /// <param name="useFields">使用到的欄位集合。</param>
        private TStringHashSet GetReturnFields(TFormTable table, string linkFieldName, TStringHashSet useFields)
        {
            TStringHashSet oReturnFields;
            TFormField oField;

            oReturnFields = new TStringHashSet();
            foreach (string fieldName in useFields)
            {
                oField = table.Fields[fieldName];
                if (oField != null && oField.Type == EFieldType.LinkField && StrFunc.Equals(oField.LinkFieldName, linkFieldName))
                    oReturnFields.Add(fieldName);
            }
            return oReturnFields;
        }

        /// <summary>
        /// 取得下一個資料表別名。
        /// </summary>
        /// <param name="tableAlias">目前資料表別名。</param>
        private string GetNextTableAlias(string tableAlias)
        {
            string sTableAlias;
            string sBaseValues;

            sBaseValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            sTableAlias = StrFunc.GetNextID(tableAlias, sBaseValues);
            // 若資料表別名為關鍵字，則重取資料表別名
            if (StrFunc.IsEqualsOr(sTableAlias, "AS", "BY"))
                sTableAlias = StrFunc.GetNextID(sTableAlias, sBaseValues);
            return sTableAlias;
        }

        /// <summary>
        /// 取得下一個資料表別名。
        /// </summary>
        private string GetNextTableAlias()
        {
           this.ActiveTableAlias = GetNextTableAlias(this.ActiveTableAlias);
           return this.ActiveTableAlias;    
        }
    }
}
