using System.Data;
using Bee.Base;

namespace Bee.Define.UnitTests
{
    /// <summary>
    /// MessagePack 序列化測試。
    /// </summary>
    public class MessagePackTests
    {
        /// <summary>
        /// 靜態建構函式。
        /// </summary>
        static MessagePackTests()
        {
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        /// <summary>
        /// 測試 MessagePack 是否能正確序列化與反序列化 DataSet。
        /// </summary>
        [Fact(DisplayName = "DataSet 序列化")]
        public void DataSet_Serialize()
        {
            // 建立範例 DataSet 並加入兩個 DataTable
            var dataSet = new DataSet("TestDataSet");

            var table1 = new DataTable("Table1");
            table1.Columns.Add("Name", typeof(string));
            table1.Columns.Add("Age", typeof(int));
            table1.Rows.Add("Alice", 30);
            table1.Rows.Add("Bob", 40);

            var table2 = new DataTable("Table2");
            table2.Columns.Add("Product", typeof(string));
            table2.Columns.Add("Price", typeof(decimal));
            table2.Rows.Add("Pen", 1.5m);
            table2.Rows.Add("Notebook", 3.2m);

            dataSet.Tables.Add(table1);
            dataSet.Tables.Add(table2);

            // 使用 MessagePackHelper 進行序列化
            byte[] serialized = MessagePackHelper.Serialize(dataSet);

            // 反序列化回 DataSet
            var deserialized = MessagePackHelper.Deserialize<DataSet>(serialized);

            // 驗證資料是否正確
            Assert.Equal(2, deserialized.Tables.Count);

            var dt1 = deserialized.Tables["Table1"];
            Assert.NotNull(dt1);
            Assert.Equal(2, dt1.Rows.Count);
            Assert.Equal("Alice", dt1.Rows[0]["Name"]);
            Assert.Equal(30, dt1.Rows[0]["Age"]);
            Assert.Equal("Bob", dt1.Rows[1]["Name"]);
            Assert.Equal(40, dt1.Rows[1]["Age"]);

            var dt2 = deserialized.Tables["Table2"];
            Assert.NotNull(dt2);
            Assert.Equal(2, dt2.Rows.Count);
            Assert.Equal("Pen", dt2.Rows[0]["Product"]);
            Assert.Equal(1.5m, dt2.Rows[0]["Price"]);
            Assert.Equal("Notebook", dt2.Rows[1]["Product"]);
            Assert.Equal(3.2m, dt2.Rows[1]["Price"]);
        }

        /// <summary>
        /// 測試 MessagePack 是否能正確序列化與反序列化 DataTable。
        /// </summary>
        [Fact(DisplayName = "DataTable 序列化")]
        public void DataTable_Serialize()
        {
            // 建立範例 DataTable 並加入測試資料
            var table = new DataTable("TestTable");
            table.Columns.Add("Column1", typeof(string));
            table.Columns.Add("Column2", typeof(int));
            table.Rows.Add("Test1", 100);
            table.Rows.Add("Test2", 200);

            // 使用 MessagePackHelper 進行序列化
            byte[] serialized = MessagePackHelper.Serialize(table);
            // 反序列化回 DataTable
            var deserialized = MessagePackHelper.Deserialize<DataTable>(serialized);

            // 驗證資料是否正確
            Assert.Equal(2, deserialized.Rows.Count);
            Assert.Equal("Test1", deserialized.Rows[0]["Column1"]);
            Assert.Equal(100, deserialized.Rows[0]["Column2"]);
            Assert.Equal("Test2", deserialized.Rows[1]["Column1"]);
            Assert.Equal(200, deserialized.Rows[1]["Column2"]);
        }

        /// <summary>
        /// 測試 DbNull.Value 是否能正確轉換為 null，並確認轉換後資料能夠正確寫回資料庫。
        /// </summary>
        [Fact(DisplayName = "DataTable 序列化包含 DBNull 值")]
        public void DataTable_Serialize_DbNull()
        {
            // Arrange：建立含 DBNull 的 DataTable
            var dt = new DataTable("TestTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));

            var row = dt.NewRow();
            row["Id"] = 1;
            row["Name"] = DBNull.Value; // 模擬空值
            dt.Rows.Add(row);

            // Act：轉為序列化格式，再轉回 DataTable
            var serializable = TSerializableDataTable.FromDataTable(dt);
            var restored = TSerializableDataTable.ToDataTable(serializable);

            // Assert：確認還原後的值為 DBNull.Value
            Assert.Equal(1, restored.Rows[0]["Id"]);
            Assert.True(restored.Rows[0].IsNull("Name")); // 正確為 DBNull.Value
        }

        /// <summary>
        /// 測試 DataTable 在序列化後能否保留 RowState 狀態。
        /// </summary>
        [Fact(DisplayName = "DataTable 序列化保留 RowState 狀態")]
        public void DataTable_Serialize_RowState()
        {
            var table = new DataTable("SampleTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            // 新增第一筆
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["Name"] = "資料1";
            table.Rows.Add(row1);

            // 新增第二筆
            var row2 = table.NewRow();
            row2["Id"] = 2;
            row2["Name"] = "資料2";
            table.Rows.Add(row2);

            // 先 AcceptChanges，兩筆變成 Unchanged
            table.AcceptChanges();

            // 修改第一筆 (RowState -> Modified)
            table.Rows[0]["Name"] = "修改後資料1";

            // 刪除第二筆 (RowState -> Deleted)
            table.Rows[1].Delete();

            // 新增第三筆 (RowState -> Added)
            var row3 = table.NewRow();
            row3["Id"] = 3;
            row3["Name"] = "新增資料3";
            table.Rows.Add(row3);

            // Serialize & Deserialize
            var bytes = MessagePackHelper.Serialize(table);
            var restored = MessagePackHelper.Deserialize<DataTable>(bytes);

            if (!DataTableComparer.IsEqual(table, restored))
            {
                Assert.Fail("序列化還原後的 DataTable 與原始 DataTable 不相等");
            }
        }

        /// <summary>
        /// 測試 TListItemCollection 類別的序列化與反序列化。
        /// </summary>
        [Fact(DisplayName = "TListItemCollection 序列化")]
        public void TListItemCollection_Serialize()
        {
            // 建立原始物件
            var original = new TListItemCollection()
            {
                new TListItem("A001", "選項一"),
                new TListItem("A002", "選項二"),
                new TListItem("A003", "選項三")
            };

            // 序列化為位元組陣列
            var bytes = MessagePackHelper.Serialize(original);

            // 反序列化為物件
            var restored = MessagePackHelper.Deserialize<TListItemCollection>(bytes);

            // 驗證還原後的值與原值一致
            Assert.NotNull(restored);
            Assert.Equal(original.Count, restored.Count);

            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i].Value, restored[i].Value);
                Assert.Equal(original[i].Text, restored[i].Text);
            }
        }

        /// <summary>
        /// 測試 TFilterItemCollection 類別的序列化與反序列化。
        /// </summary>
        [Fact(DisplayName = "TFilterItemCollection 序列化")]
        public void TFilterItemCollection_Serialize()
        {
            // 建立集合並加入條件
            var original = new TFilterItemCollection();
            original.Add("Age", EComparisonOperator.GreaterOrEqual, "18");
            original.Add("Gender", EComparisonOperator.Equal, "Male");

            // 設定結合運算子，驗證欄位也能序列化
            original[0].Combine = ECombineOperator.And;
            original[1].Combine = ECombineOperator.Or;

            // 序列化集合
            byte[] bytes = MessagePackHelper.Serialize(original);

            // 反序列化集合
            var restored = MessagePackHelper.Deserialize<TFilterItemCollection>(bytes);

            // 驗證集合數量
            Assert.Equal(original.Count, restored.Count);

            // 驗證每筆內容
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i].FieldName, restored[i].FieldName);
                Assert.Equal(original[i].Comparison, restored[i].Comparison);
                Assert.Equal(original[i].Value, restored[i].Value);
                Assert.Equal(original[i].Combine, restored[i].Combine);
            }
        }

        /// <summary>
        /// 測試 TParameterCollection 支援多種型別的序列化與反序列化。
        /// </summary>
        [Fact(DisplayName = "TParameterCollection 多型別序列化")]
        public void TParameterCollection_Serialize()
        {
            // 建立原始物件，包含不同型別的參數
            var original = new TParameterCollection();
            original.Add("IntValue", 123);
            original.Add("StringValue", "測試字串");
            original.Add("BoolValue", true);
            original.Add("DateTimeValue", new DateTime(2025, 5, 16, 10, 30, 0));
            original.Add("DecimalValue", 123.45m);
            original.Add("DoubleValue", 9876.54321);
            original.Add("NullValue", null);

            // 序列化為位元組陣列
            var bytes = MessagePackHelper.Serialize(original);

            // 反序列化為物件
            var restored = MessagePackHelper.Deserialize<TParameterCollection>(bytes);

            // 驗證還原後的值與原值一致
            Assert.NotNull(restored);
            Assert.Equal(original.Count, restored.Count);

            foreach (var param in original)
            {
                Assert.True(restored.Contains(param.Name));

                var originalValue = param.Value;
                var restoredValue = restored[param.Name].Value;

                if (originalValue == null)
                {
                    Assert.Null(restoredValue);
                }
                else
                {
                    Assert.Equal(originalValue.GetType(), restoredValue.GetType());
                    Assert.Equal(originalValue, restoredValue);
                }
            }
        }

        /// <summary>
        /// 測試 TParameterCollection 加入 DataTable 可正常序列化。
        /// </summary>
        [Fact(DisplayName = "TParameterCollection 加入 DataTable 可正常序列化")]
        public void TParameterCollection_Serialize_DataTable()
        {
            // 建立測試用的 DataTable
            var table = new DataTable("TestTable");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, "Alice");
            table.Rows.Add(2, "Bob");

            // 建立參數集合，加入 DataTable 參數
            var parameters = new TParameterCollection();
            parameters.Add("Data", table);

            // 序列化
            var bytes = MessagePackHelper.Serialize(parameters);

            // 反序列化
            var restored = MessagePackHelper.Deserialize<TParameterCollection>(bytes);

            // 驗證
            Assert.NotNull(restored);
            Assert.True(restored.Contains("Data"));
            Assert.IsType<DataTable>(restored["Data"].Value);

            var restoredTable = (DataTable)restored["Data"].Value;
            Assert.Equal("TestTable", restoredTable.TableName);
            Assert.Equal(2, restoredTable.Rows.Count);
            Assert.Equal("Alice", restoredTable.Rows[0]["Name"]);
            Assert.Equal("Bob", restoredTable.Rows[1]["Name"]);
        }

        /// <summary>
        /// 測試 TPropertyCollection 可正確序列化與還原屬性集合資料。
        /// </summary>
        [Fact(DisplayName = "TPropertyCollection 序列化")]
        public void TPropertyCollection_Serialize()
        {
            // 建立屬性集合
            var properties = new TPropertyCollection();
            properties.Add("AppName", "BeeERP");
            properties.Add("Enabled", "true");
            properties.Add("RetryCount", "3");

            // 序列化
            var bytes = MessagePackHelper.Serialize(properties);

            // 反序列化
            var restored = MessagePackHelper.Deserialize<TPropertyCollection>(bytes);

            // 驗證內容是否正確還原
            Assert.NotNull(restored);
            Assert.Equal(3, restored.Count);
            Assert.Equal("BeeERP", restored.GetValue("AppName", "DefaultApp"));
            Assert.True(restored.GetValue("Enabled", false));
            Assert.Equal(3, restored.GetValue("RetryCount", 0));

            // 測試預設值（不存在的欄位）
            Assert.Equal("Default", restored.GetValue("NotExist", "Default"));
            Assert.False(restored.GetValue("NotExistBool", false));
            Assert.Equal(999, restored.GetValue("NotExistInt", 999));
        }

        /// <summary>
        /// 測試 TPingArgs 可正確序列化與還原資料。
        /// </summary>
        [Fact(DisplayName = "TPingArgs 序列化")]
        public void TPingArgs_Serialize()
        {
            // 建立 TPingArgs 並指定屬性與參數
            var args = new TPingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            args.Parameters.Add("Env", "UAT");
            args.Parameters.Add("Verbose", true);

            // 序列化
            var bytes = MessagePackHelper.Serialize(args);

            // 反序列化
            var restored = MessagePackHelper.Deserialize<TPingArgs>(bytes);

            // 驗證基本屬性
            Assert.NotNull(restored);
            Assert.Equal("TestClient", restored.ClientName);
            Assert.Equal(args.TraceId, restored.TraceId);

            // 驗證參數集合
            Assert.NotNull(restored.Parameters);
            Assert.Equal("UAT", restored.Parameters.GetValue<string>("Env"));
            Assert.True(restored.Parameters.GetValue<bool>("Verbose"));

            // 驗證不存在參數的預設值
            Assert.Equal("Unknown", restored.Parameters.GetValue<string>("UnknownKey", "Unknown"));
        }

        /// <summary>
        /// 測試 TPingResult 可正確序列化與還原資料。
        /// </summary>
        [Fact(DisplayName = "TPingResult 序列化")]
        public void TPingResult_Serialize()
        {
            // 建立 TPingResult 並指定屬性與參數
            var result = new TPingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            result.Parameters.Add("Region", "TW");
            result.Parameters.Add("Elapsed", 42);

            // 序列化
            var bytes = MessagePackHelper.Serialize(result);

            // 反序列化
            var restored = MessagePackHelper.Deserialize<TPingResult>(bytes);

            // 驗證基本屬性
            Assert.NotNull(restored);
            Assert.Equal("pong", restored.Status);
            Assert.Equal(result.ServerTime, restored.ServerTime);
            Assert.Equal("1.2.3", restored.Version);
            Assert.Equal(result.TraceId, restored.TraceId);

            // 驗證參數集合
            Assert.NotNull(restored.Parameters);
            Assert.Equal("TW", restored.Parameters.GetValue<string>("Region"));
            Assert.Equal(42, restored.Parameters.GetValue<int>("Elapsed"));
        }

        /// <summary>
        /// 測試 TExecFuncArgs 可正確序列化與還原資料。
        /// </summary>
        [Fact(DisplayName = "TExecFuncArgs 序列化")]
        public void TExecFuncArgs_Serialize()
        {
            // 建立 TExecFuncArgs 並指定屬性與參數
            var args = new TExecFuncArgs
            {
                FuncID = "CustomFunction123"
            };
            args.Parameters.Add("Key1", "Value1");
            args.Parameters.Add("Key2", 42);

            // 序列化
            var bytes = MessagePackHelper.Serialize(args);

            // 反序列化
            var restored = MessagePackHelper.Deserialize<TExecFuncArgs>(bytes);

            // 驗證基本屬性
            Assert.NotNull(restored);
            Assert.Equal("CustomFunction123", restored.FuncID);

            // 驗證參數集合
            Assert.NotNull(restored.Parameters);
            Assert.Equal("Value1", restored.Parameters.GetValue<string>("Key1"));
            Assert.Equal(42, restored.Parameters.GetValue<int>("Key2"));

            // 驗證不存在參數的預設值
            Assert.Equal("Default", restored.Parameters.GetValue<string>("UnknownKey", "Default"));
        }

        /// <summary>
        /// 測試 CreateSession 方法傳入引數及傳出結果的序列化。
        /// </summary>
        [Fact(DisplayName = "TCreateSessionArgs 序列化測試")]
        public void CreateSession_Serialize()
        {
            // Arrange: 建立 TCreateSessionArgs 實例並設定屬性
            var args = new TCreateSessionArgs
            {
                UserID = "TestUser",
                ExpiresIn = 7200,
                OneTime = true
            };

            // Act & Assert: 使用 TestMessagePackSerialization 測試
            TestFunc.TestMessagePackSerialization(args);

            // Arrange: 建立 TCreateSessionResult 實例並設定屬性
            var result = new TCreateSessionResult
            {
                AccessToken = Guid.NewGuid(),
                Expires = new DateTime(2025, 5, 16, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act & Assert: 使用 TestMessagePackSerialization 測試
            TestFunc.TestMessagePackSerialization(result);
        }

    }
}

