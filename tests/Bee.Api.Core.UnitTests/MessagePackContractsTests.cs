using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 Bee.Definition 合約型別的 MessagePack 序列化與反序列化行為。
    /// </summary>
    public sealed class MessagePackContractsTests
    {
        /// <summary>
        /// 單一測試：對 GetPackageRequest/Response 進行 round-trip 並比對內容（涵蓋泛型與非泛型兩個多載）。
        /// </summary>
        [Fact]
        [DisplayName("合約型別 MessagePack 序列化與反序列化 round-trip 應成功")]
        public void ContractTypes_RoundTrip_Succeeds()
        {
            // ===== 1) GetPackageRequest → Serialize/Deserialize（非泛型重載） =====
            var getArgs = new GetPackageRequest
            {
                AppId = "Client",
                ComponentId = "Main",
                Version = "1.2.4",
                Platform = "Win-x64",
                Channel = "Stable",
                FileId = ""
            };

            // 本段刻意呼叫 non-generic overload，驗證其行為
#pragma warning disable CA2263 // Prefer generic overload when type is known
            var getArgsBytes = MessagePackCodec.Serialize(getArgs, typeof(GetPackageRequest));
            Assert.NotNull(getArgsBytes);
            Assert.NotEmpty(getArgsBytes);

            var getArgs2Obj = MessagePackCodec.Deserialize(getArgsBytes, typeof(GetPackageRequest));
#pragma warning restore CA2263
            var getArgs2 = Assert.IsType<GetPackageRequest>(getArgs2Obj);
            Assert.Equal(getArgs.AppId, getArgs2.AppId);
            Assert.Equal(getArgs.Version, getArgs2.Version);
            Assert.Equal(getArgs.Platform, getArgs2.Platform);
            Assert.Equal(getArgs.Channel, getArgs2.Channel);
            Assert.Equal(getArgs.FileId, getArgs2.FileId);

            // ===== 2) GetPackageResponse → Serialize/Deserialize（泛型） =====
            var bytes = new byte[] { 1, 2, 3, 4, 5 }; // 模擬小檔案內容
            var getResult = new GetPackageResponse
            {
                FileName = "client-main-win-x64-1.2.4.zip",
                Content = bytes,
                FileSize = bytes.LongLength,
                Sha256 = "ABCDEF0123456789",
                PackageUrl = "" // Delivery=Api 時通常為空
            };

            var getResultBytes = MessagePackCodec.Serialize(getResult);
            Assert.NotNull(getResultBytes);
            Assert.NotEmpty(getResultBytes);

            var getResult2 = MessagePackCodec.Deserialize<GetPackageResponse>(getResultBytes);
            Assert.NotNull(getResult2);
            Assert.Equal(getResult.FileName, getResult2.FileName);
            Assert.Equal(getResult.FileSize, getResult2.FileSize);
            Assert.Equal(getResult.Sha256, getResult2.Sha256);
            Assert.Equal(getResult.PackageUrl, getResult2.PackageUrl);
            Assert.Equal(getResult.Content, getResult2.Content); // xUnit 對 byte[] 會做序列等值比對
        }
    }
}
