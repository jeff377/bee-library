using Bee.Define;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server ��Ʈw�إ� Select �R�O���ͪ����O�C
    /// </summary>
    internal class SqlSelectCommandBuilder
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
                throw new ArgumentException("tableName ���i����", nameof(tableName));

            // ���o FormTable
            var table = _formDefine.Tables[tableName];
            if (table == null)
                throw new InvalidOperationException($"�䤣����w����ƪ�: {tableName}");

            var dbTableName = !string.IsNullOrWhiteSpace(table.DbTableName) ? table.DbTableName : table.TableName;

            string fields;
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // ���o�Ҧ����
                fields = string.Join(", ", table.Fields.Select(f => $"[{f.FieldName}]"));
            }
            else
            {
                // �u�����w���
                var fieldNames = selectFields.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToArray();
                fields = string.Join(", ", fieldNames.Select(f => $"[{f}]"));
            }

            var sql = $"SELECT {fields} FROM [{dbTableName}]";
            return new DbCommandSpec(DbCommandKind.DataTable, sql, new Dictionary<string, object>());
        }
    }
}