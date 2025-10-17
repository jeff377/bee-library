using Bee.Base;
using Bee.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server ��Ʈw�إ� Select �R�O���ͪ����O�C
    /// </summary>
    public class SqlSelectCommandBuilder
    {
        private readonly FormDefine _formDefine;

        /// <summary>
        /// �غc�禡�C
        /// </summary>
        /// <param name="formDefine">���w�q�C</param>
        public SqlSelectCommandBuilder(FormDefine formDefine)
        {
            _formDefine = formDefine;
        }

        /// <summary>
        /// �إ� Select �y�k�� DbCommandSpec�C
        /// </summary>
        /// <param name="tableName">��ƪ�W�١C</param>
        /// <param name="selectFields">�n���o����춰�X�r��A�H�r�I���j���W�١A�Ŧr���ܨ��o�Ҧ����C</param>
        public DbCommandSpec Build(string tableName, string selectFields)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));

            var formTable = _formDefine.Tables[tableName];
            if (formTable == null)
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");

            var dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName) ? formTable.DbTableName : formTable.TableName;
            var selectContext = GetSelectContext(formTable);
            var selectFieldNames = GetSelectFields(formTable, selectFields);
            var joins = new TableJoinCollection();

            var sb = new StringBuilder();
            var selectParts = new List<string>();
            foreach (var fieldName in selectFieldNames)
            {
                var field = formTable.Fields.GetOrDefault(fieldName);
                if (field == null)
                    throw new InvalidOperationException($"Field '{fieldName}' does not exist in table '{formTable.TableName}'.");
                if (field.Type == FieldType.DbField)
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");

                    AddTableJoin(selectContext, joins, mapping.TableJoin);

                    //if (!joins.Contains(mapping.TableJoin.Key))
                    //    joins.Add(mapping.TableJoin);
                }
            }

            sb.AppendLine("SELECT");
            sb.AppendLine(string.Join(",\n", selectParts));
            sb.AppendLine($"FROM {QuoteIdentifier(dbTableName)} A");

            var joinList = joins.OrderBy(j => j.RightAlias);
            foreach (var join in joinList)
            {
                var joinKeyword = join.JoinType.ToString().ToUpperInvariant() + " JOIN";
                sb.AppendLine($"{joinKeyword} {QuoteIdentifier(join.RightTable)} {join.RightAlias} ON {join.LeftAlias}.{QuoteIdentifier(join.LeftField)} = {join.RightAlias}.{QuoteIdentifier(join.RightField)}");
            }

            string sql = sb.ToString();
            return new DbCommandSpec(DbCommandKind.DataTable, sql);
        }

        /// <summary>
        /// �N��ƪ� Join ���Y�[�J���X�C
        /// ����]���� Join ���Y�y���L�a���j�C
        /// </summary>
        /// <param name="context">�y�z Select �d�߮ɩһݪ����ӷ��P Join ���Y���X�C</param>
        /// <param name="joins">��ƪ� Join ���Y���X�C</param>
        /// <param name="join">�n�[�J��ƪ� Join ���Y�C</param>
        /// <param name="visited">�w���j�L�� Join Key ���X�A�Ω󨾤������ѷӳy���L�a���j�C�I�s�ݥi���ǡA�w�]�۰ʫإߡC</param>
        private void AddTableJoin(SelectContext context, TableJoinCollection joins, TableJoin join, HashSet<string> visited = null)
        {
            if (visited == null)
                visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (visited.Contains(join.Key)) { return; }
            visited.Add(join.Key);

            if (joins.Contains(join.Key)) { return; }

            joins.Add(join);
            if (join.LeftAlias == "A") { return; }

            // �p�G LeftAlias ���� A�A��ܥ����D�D��A�n�[�J������ JOIN ���Y
            var srcJoin = context.Joins.FindRightAlias(join.LeftAlias);
            if (srcJoin != null)
                AddTableJoin(context, joins, srcJoin, visited);
        }

        /// <summary>
        /// �̾ڸ�Ʈw�����A�^�ǾA���ѧO�r�����榡�C
        /// </summary>
        /// <param name="identifier">�ѧO�r�W�١C</param>
        /// <returns>����᪺�ѧO�r�C</returns>
        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(DatabaseType.SQLServer, identifier);
        }

        /// <summary>
        /// ���o Select ����춰�X�C
        /// </summary>
        /// <param name="formTable">����ƪ�C</param>
        /// <param name="selectFields">�n���o����춰�X�r��A�H�r�I���j���W�١A�Ŧr���ܨ��o�Ҧ����</param>
        private StringHashSet GetSelectFields(FormTable formTable, string selectFields)
        {
            var set = new StringHashSet();
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // ���o�Ҧ����
                foreach (var field in formTable.Fields)
                {
                    set.Add(field.FieldName);
                }
            }
            else
            {
                // �u�����w���
                set.Add(selectFields, ",");
            }
            return set;
        }

        /// <summary>
        /// ���o Select �d�߮ɩһݪ����ӷ��P Join ���Y���X�C
        /// </summary>
        /// <param name="formTable">����ƪ�C</param>
        private SelectContext GetSelectContext(FormTable formTable)
        {
            var builder = new SelectContextBuilder(formTable);
            return builder.Build();
        }


    }
}