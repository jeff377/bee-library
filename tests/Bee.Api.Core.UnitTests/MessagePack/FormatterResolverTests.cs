using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using Bee.Definition.Filters;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// FormatterResolver.GetFormatter 直接測試：
    /// 驗證 DataSet/DataTable 專屬 formatter、MessagePackCollectionBase 子類別的
    /// 動態 formatter 建立、非泛型型別的 StandardResolver fallback，
    /// 並確認同一型別重複取得為同一 cached instance。
    /// </summary>
    public class FormatterResolverTests
    {
        [Fact]
        [DisplayName("GetFormatter<DataSet> 應回傳 DataSetFormatter")]
        public void GetFormatter_DataSet_ReturnsDataSetFormatter()
        {
            var formatter = FormatterResolver.Instance.GetFormatter<DataSet>();

            Assert.NotNull(formatter);
            Assert.IsType<DataSetFormatter>(formatter);
        }

        [Fact]
        [DisplayName("GetFormatter<DataTable> 應回傳 DataTableFormatter")]
        public void GetFormatter_DataTable_ReturnsDataTableFormatter()
        {
            var formatter = FormatterResolver.Instance.GetFormatter<DataTable>();

            Assert.NotNull(formatter);
            Assert.IsType<DataTableFormatter>(formatter);
        }

        [Fact]
        [DisplayName("GetFormatter<MessagePackCollectionBase 子類> 應回傳 CollectionBaseFormatter")]
        public void GetFormatter_CollectionBaseSubclass_ReturnsCollectionBaseFormatter()
        {
            // FilterNodeCollection : MessagePackCollectionBase<FilterNode>
            var formatter = FormatterResolver.Instance.GetFormatter<FilterNodeCollection>();

            Assert.NotNull(formatter);
            var typeName = formatter!.GetType().Name;
            Assert.StartsWith("CollectionBaseFormatter", typeName);
        }

        [Fact]
        [DisplayName("GetFormatter<基本型別> 應透過 StandardResolver fallback 取得 formatter")]
        public void GetFormatter_PrimitiveType_UsesStandardResolverFallback()
        {
            // int 不是 DataSet/DataTable/CollectionBase → fallback
            var formatter = FormatterResolver.Instance.GetFormatter<int>();

            Assert.NotNull(formatter);
            Assert.IsType<IMessagePackFormatter<int>>(formatter, exactMatch: false);
        }

        [Fact]
        [DisplayName("GetFormatter 應快取同一型別的 formatter")]
        public void GetFormatter_SameType_ReturnsCachedInstance()
        {
            var first = FormatterResolver.Instance.GetFormatter<DataSet>();
            var second = FormatterResolver.Instance.GetFormatter<DataSet>();

            Assert.Same(first, second);
        }

        /// <summary>
        /// 泛型但非 MessagePackCollectionBase 子類（例如 Nullable&lt;int&gt;）
        /// 應走 StandardResolver fallback。
        /// </summary>
        [Fact]
        [DisplayName("GetFormatter<非 CollectionBase 的泛型型別> 應 fallback 至 StandardResolver")]
        public void GetFormatter_GenericNonCollectionBase_FallsBack()
        {
            var formatter = FormatterResolver.Instance.GetFormatter<int?>();

            Assert.NotNull(formatter);
        }
    }
}
