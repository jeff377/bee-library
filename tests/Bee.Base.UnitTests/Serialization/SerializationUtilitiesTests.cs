using System.Collections;
using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// Tests for <see cref="SerializationUtilities.IsSerializeEmpty"/> covering all
    /// serialization-state branches and value-emptiness paths.
    /// </summary>
    public class SerializationUtilitiesTests
    {
        private sealed class EmptySerializeObject : IObjectSerializeEmpty
        {
            public bool IsSerializeEmpty { get; set; }
        }

        private sealed class PureEnumerable : IEnumerable
        {
            private readonly object[] _items;
            public PureEnumerable(params object[] items) => _items = items;
            public IEnumerator GetEnumerator() => _items.GetEnumerator();
        }

        [Fact]
        [DisplayName("IsSerializeEmpty state 為 None 時永遠回傳 false")]
        public void IsSerializeEmpty_StateNone_ReturnsFalse()
        {
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.None, null!));
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.None, new List<int>()));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於 Serialize 狀態且 value 為 null 應回傳 true")]
        public void IsSerializeEmpty_SerializeAndNull_ReturnsTrue()
        {
            Assert.True(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, null!));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 應尊重 IObjectSerializeEmpty 回報的狀態")]
        public void IsSerializeEmpty_ObjectSerializeEmpty_ReflectsProperty()
        {
            var emptyObj = new EmptySerializeObject { IsSerializeEmpty = true };
            Assert.True(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, emptyObj));

            var notEmptyObj = new EmptySerializeObject { IsSerializeEmpty = false };
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, notEmptyObj));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於空 IList 應回傳 true,非空應回傳 false")]
        public void IsSerializeEmpty_IList_ReflectsEmptiness()
        {
            Assert.True(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, new List<int>()));
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, new List<int> { 1 }));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於 IEnumerable 應依可列舉性判斷 empty")]
        public void IsSerializeEmpty_IEnumerable_ReflectsEmptiness()
        {
            Assert.True(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, new PureEnumerable()));
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, new PureEnumerable(1, 2)));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 其他型別應走 default 回傳 false")]
        public void IsSerializeEmpty_DefaultBranch_ReturnsFalse()
        {
            // int / string 等 primitive 不符合任何 case → default → false
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, 123));
            Assert.False(SerializationUtilities.IsSerializeEmpty(SerializeState.Serialize, "abc"));
        }
    }
}
