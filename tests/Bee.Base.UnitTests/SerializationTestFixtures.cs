using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Base class providing a temp directory and disposal pattern for serialization tests.
    /// </summary>
    public abstract class SerializationTestBase : IDisposable
    {
        protected readonly string _tempDir;

        protected SerializationTestBase()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "bee-base-serialize-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            catch (UnauthorizedAccessException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            GC.SuppressFinalize(this);
        }

        protected string TempPath(string fileName) => Path.Combine(_tempDir, fileName);
    }

    /// <summary>
    /// Test payload used to verify serialization round-trips and lifecycle callbacks.
    /// </summary>
    public class SerializationTestPayload : IObjectSerializeBase, IObjectSerialize, IObjectSerializeFile, IObjectSerializeProcess
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }

        private SerializeState _state = SerializeState.None;
        [XmlIgnore, JsonIgnore]
        public SerializeState SerializeState => _state;
        public void SetSerializeState(SerializeState serializeState) => _state = serializeState;

        private string _objectFilePath = string.Empty;
        [XmlIgnore, JsonIgnore]
        public string ObjectFilePath => _objectFilePath;
        public void SetObjectFilePath(string filePath) => _objectFilePath = filePath;

        [XmlIgnore, JsonIgnore]
        public List<string> Events { get; } = [];

        public void BeforeSerialize(SerializeFormat serializeFormat) => Events.Add($"Before:{serializeFormat}");
        public void AfterSerialize(SerializeFormat serializeFormat) => Events.Add($"After:{serializeFormat}");
        public void AfterDeserialize(SerializeFormat serializeFormat) => Events.Add($"AfterDeser:{serializeFormat}");
    }
}
