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
        /// <param name="filter">�L�o���� FilterNode�A�Y�� null �h���[ WHERE�C</param>
        public DbCommandSpec Build(string tableName, string selectFields, FilterNode filter = null)
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
                {
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                }
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");

                    // �N�ϥΪ���ƪ� Join ���Y�[�J���X
                    AddTableJoin(selectContext, joins, mapping.TableJoin);
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

            IReadOnlyDictionary<string, object> parameters = null;
            if (filter != null)
            {
                var remappedFilter = RemapFilterNodeFields(filter, selectContext);
                var whereBuilder = new SqlServerWhereBuilder();
                var whereResult = whereBuilder.Build(remappedFilter, true);
                if (!string.IsNullOrWhiteSpace(whereResult.WhereClause))
                {
                    sb.AppendLine(whereResult.WhereClause);
                    parameters = whereResult.Parameters;
                }
            }

            string sql = sb.ToString();
            if (parameters != null && parameters.Count > 0)
                return new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
            else
                return new DbCommandSpec(DbCommandKind.DataTable, sql);
        }

        /// <summary>
        /// �N�ϥΪ���ƪ� Join ���Y�[�J���X�C
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

        /// <summary>
        /// ���s�M�g�L�o�`�I�������W�٬� SQL �d�ߩһݪ��榡�]�[�W��ƪ�O�W�^�C
        /// </summary>
        /// <param name="node">�n���s�M�g���L�o�`�I�C</param>
        /// <param name="selectContext">�d�����ӷ��P Join ���Y���X�C</param>
        /// <returns>���s�M�g�᪺�L�o�`�I�C</returns>
        private FilterNode RemapFilterNodeFields(FilterNode node, SelectContext selectContext)
        {
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                var mapping = selectContext.FieldMappings.GetOrDefault(cond.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    // �������A�w�]�O�W A
                    fieldExpr = $"A.{QuoteIdentifier(cond.FieldName)}";
                }
                return new FilterCondition(fieldExpr, cond.Operator, cond.Value);
            }
            else if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                var newGroup = new FilterGroup(group.Operator);
                foreach (var child in group.Nodes)
                    newGroup.Nodes.Add(RemapFilterNodeFields(child, selectContext));
                return newGroup;
            }
            else
            {
                return node;
            }
        }


    }
}