using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Data;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <see cref="DateTimeZoneConverter"/> 測試：instant 欄位雙向轉換、日曆日欄位不動、
    /// 列狀態與兩個版本（Current / Original）皆保留、來源不被就地修改。
    /// </summary>
    /// <remarks>
    /// 期望值一律由 <see cref="TimeZoneInfo"/> 動態推導，不寫死偏移量——測試在開發機
    /// （Asia/Taipei）與 CI（UTC）下都必須成立。設計見 docs/adr/adr-032-datetime-timezone.md（D4）。
    /// </remarks>
    public class DateTimeZoneConverterTests
    {
        private const string Taipei = "Asia/Taipei";
        private static readonly DateTime Utc9Am = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        private static DateTime ExpectedInTaipei(DateTime utcValue)
            => DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(utcValue, DateTimeKind.Unspecified),
                    TimeZoneInfo.FindSystemTimeZoneById(Taipei)),
                DateTimeKind.Unspecified);

        private static DataTable BuildTable()
        {
            var table = new DataTable("orders");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.AddColumn("order_date", FieldDbType.Date);
            table.AddColumn("remark", FieldDbType.String);
            return table;
        }

        private static DataTable BuildTableWithRow()
        {
            var table = BuildTable();
            table.Rows.Add(Utc9Am, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), "a");
            table.AcceptChanges();
            return table;
        }

        [Fact]
        [DisplayName("DateTime 欄位由 UTC 轉為使用者時區")]
        public void UtcToUser_ShiftsInstantColumn()
        {
            var converted = DateTimeZoneConverter.UtcToUser(BuildTableWithRow(), Taipei);

            Assert.NotNull(converted);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)converted.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("Date 欄位絕不轉換——日曆日沒有可重新表達的瞬間")]
        public void UtcToUser_LeavesCalendarDayColumnUntouched()
        {
            var converted = DateTimeZoneConverter.UtcToUser(BuildTableWithRow(), Taipei);

            Assert.NotNull(converted);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0), (DateTime)converted.Rows[0]["order_date"]);
        }

        [Fact]
        [DisplayName("雙向轉換為反函數，round-trip 恆等")]
        public void UserToUtc_IsInverseOfUtcToUser()
        {
            var toUser = DateTimeZoneConverter.UtcToUser(BuildTableWithRow(), Taipei);
            var backToUtc = DateTimeZoneConverter.UserToUtc(toUser, Taipei);

            Assert.NotNull(backToUtc);
            Assert.Equal(Utc9Am, (DateTime)backToUtc.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("空白時區為 no-op，且原樣回傳同一參考")]
        public void BlankTimeZone_IsNoOp()
        {
            var source = BuildTableWithRow();

            var converted = DateTimeZoneConverter.UtcToUser(source, string.Empty);

            Assert.Same(source, converted);
        }

        [Fact]
        [DisplayName("轉換不得就地修改來源——in-process 下來源是呼叫端自己的物件")]
        public void Convert_DoesNotMutateSource()
        {
            var source = BuildTableWithRow();

            DateTimeZoneConverter.UtcToUser(source, Taipei);

            Assert.Equal(Utc9Am, (DateTime)source.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("Modified 列的 Current 與 Original 兩個版本都要轉換")]
        public void Convert_ModifiedRow_ConvertsBothVersions()
        {
            // 只轉 Current 會讓兩個版本落在不同時區，伺服端的並行檢查與稽核 DiffGram 都會失準。
            var table = BuildTableWithRow();
            var newUtc = new DateTime(2026, 1, 2, 15, 0, 0, DateTimeKind.Unspecified);
            table.Rows[0]["created_at"] = newUtc;
            Assert.Equal(DataRowState.Modified, table.Rows[0].RowState);

            var converted = DateTimeZoneConverter.UtcToUser(table, Taipei);

            Assert.NotNull(converted);
            var row = converted.Rows[0];
            Assert.Equal(DataRowState.Modified, row.RowState);
            Assert.Equal(ExpectedInTaipei(newUtc), (DateTime)row["created_at", DataRowVersion.Current]);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)row["created_at", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("Added 列維持 Added，且值已轉換")]
        public void Convert_AddedRow_KeepsState()
        {
            var table = BuildTable();
            table.Rows.Add(Utc9Am, new DateTime(2026, 1, 1), "a");
            Assert.Equal(DataRowState.Added, table.Rows[0].RowState);

            var converted = DateTimeZoneConverter.UtcToUser(table, Taipei);

            Assert.NotNull(converted);
            Assert.Equal(DataRowState.Added, converted.Rows[0].RowState);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)converted.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("Deleted 列維持 Deleted，其 Original 值亦已轉換（稽核會讀它）")]
        public void Convert_DeletedRow_KeepsStateAndConvertsOriginal()
        {
            var table = BuildTableWithRow();
            table.Rows[0].Delete();

            var converted = DateTimeZoneConverter.UtcToUser(table, Taipei);

            Assert.NotNull(converted);
            var row = converted.Rows[0];
            Assert.Equal(DataRowState.Deleted, row.RowState);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)row["created_at", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("Unchanged 列維持 Unchanged，不因轉換而變成 Modified")]
        public void Convert_UnchangedRow_StaysUnchanged()
        {
            var converted = DateTimeZoneConverter.UtcToUser(BuildTableWithRow(), Taipei);

            Assert.NotNull(converted);
            Assert.Equal(DataRowState.Unchanged, converted.Rows[0].RowState);
        }

        [Fact]
        [DisplayName("DataSet 內每一張表都會被轉換")]
        public void Convert_DataSet_ConvertsEveryTable()
        {
            using var dataSet = new DataSet("s");
            dataSet.Tables.Add(BuildTableWithRow());
            var detail = BuildTableWithRow();
            detail.TableName = "order_items";
            dataSet.Tables.Add(detail);

            var converted = DateTimeZoneConverter.UtcToUser(dataSet, Taipei);

            Assert.NotNull(converted);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)converted.Tables["orders"]!.Rows[0]["created_at"]);
            Assert.Equal(ExpectedInTaipei(Utc9Am), (DateTime)converted.Tables["order_items"]!.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("DBNull 儲存格不受影響")]
        public void Convert_NullCell_IsLeftAlone()
        {
            var table = BuildTable();
            table.Columns["created_at"]!.AllowDBNull = true;
            table.Rows.Add(DBNull.Value, new DateTime(2026, 1, 1), "a");
            table.AcceptChanges();

            var converted = DateTimeZoneConverter.UtcToUser(table, Taipei);

            Assert.NotNull(converted);
            Assert.Equal(DBNull.Value, converted.Rows[0]["created_at"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [DisplayName("filter 值：DateTime 轉換、DateOnly 不轉")]
        public void ConvertFilterValue_RespectsValueType(bool toUtc)
        {
            var day = new DateOnly(2026, 1, 1);

            var shifted = DateTimeZoneConverter.ConvertFilterValue(Utc9Am, Taipei, toUtc);
            var untouched = DateTimeZoneConverter.ConvertFilterValue(day, Taipei, toUtc);

            Assert.NotEqual(Utc9Am, shifted);
            Assert.Equal(day, untouched);
        }

        [Fact]
        [DisplayName("filter 值：非時間型別原樣回傳")]
        public void ConvertFilterValue_NonTemporalValue_IsReturnedAsIs()
        {
            Assert.Equal("open", DateTimeZoneConverter.ConvertFilterValue("open", Taipei, toUtc: true));
            Assert.Null(DateTimeZoneConverter.ConvertFilterValue(null, Taipei, toUtc: true));
        }

        [Fact]
        [DisplayName("無法解析的時區應擲例外，不得靜默略過轉換")]
        public void Convert_UnresolvableZone_Throws()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => DateTimeZoneConverter.UtcToUser(BuildTableWithRow(), "Not/AZone"));

            Assert.Contains("Not/AZone", exception.Message, StringComparison.Ordinal);
        }
    }
}
