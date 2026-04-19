using System.ComponentModel;
using System.Text.Json;
using Bee.Api.Core.System;
using Bee.Business.System;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiOutputConverter 的 Convert 與 ConvertResultValue 測試。
    /// </summary>
    public class ApiOutputConverterTests
    {
        [Fact]
        [DisplayName("Convert 於 null 應回傳 null")]
        public void Convert_Null_ReturnsNull()
        {
            var result = ApiOutputConverter.Convert(null!);
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Convert 於 BO Result 應轉成對應 API Response")]
        public void Convert_BoResult_ReturnsApiResponse()
        {
            var boResult = new PingResult
            {
                Status = "ok",
                Version = "9.9.9",
                TraceId = "TRACE-1"
            };

            var converted = ApiOutputConverter.Convert(boResult);

            var response = Assert.IsType<PingResponse>(converted);
            Assert.Equal("ok", response.Status);
            Assert.Equal("9.9.9", response.Version);
            Assert.Equal("TRACE-1", response.TraceId);
        }

        [Fact]
        [DisplayName("Convert 於名稱無 Result 後綴之物件應回傳原物件")]
        public void Convert_NonResultSuffix_ReturnsOriginal()
        {
            var input = "hello";
            var result = ApiOutputConverter.Convert(input);
            Assert.Same(input, result);
        }

        [Fact]
        [DisplayName("ConvertResultValue 於 value 已為目標型別時應直接回傳")]
        public void ConvertResultValue_DirectType_ReturnsSame()
        {
            var original = new PingResponse { Status = "x" };
            var result = ApiOutputConverter.ConvertResultValue<PingResponse>(original);

            Assert.Same(original, result);
        }

        [Fact]
        [DisplayName("ConvertResultValue 於 JsonElement 應反序列化為目標型別")]
        public void ConvertResultValue_JsonElement_Deserializes()
        {
            var json = """{"status":"ok","traceId":"T1"}""";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement.Clone();

            var result = ApiOutputConverter.ConvertResultValue<PingResponse>(element);

            Assert.NotNull(result);
            Assert.Equal("ok", result!.Status);
            Assert.Equal("T1", result.TraceId);
        }

        [Fact]
        [DisplayName("ConvertResultValue 於相容引用型別應強制轉型")]
        public void ConvertResultValue_CastablePath_ReturnsCast()
        {
            object value = "hello";
            var result = ApiOutputConverter.ConvertResultValue<string>(value);
            Assert.Equal("hello", result);
        }
    }
}
