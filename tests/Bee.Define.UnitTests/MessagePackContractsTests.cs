using Bee.Contracts;

namespace Bee.Define.Tests
{
    /// <summary>
    /// 驗證 Bee.Define 合約型別的 MessagePack 序列化與反序列化行為。
    /// </summary>
    public sealed class MessagePackContractsTests
    {
        /// <summary>
        /// 單一測試：對 CheckPackageUpdateArgs/Result 與 GetPackageArgs/Result 進行 round-trip 並比對內容。
        /// </summary>
        [Fact]
        public void ContractTypes_Roundtrip_With_MessagePackHelper_Succeeds()
        {
            // ===== 1) CheckPackageUpdateArgs → Serialize/Deserialize（泛型） =====
            var checkArgs = new CheckPackageUpdateArgs
            {
                Queries = new List<PackageUpdateQuery>
                {
                    new PackageUpdateQuery
                    {
                        AppId = "Client",
                        ComponentId = "Main",
                        CurrentVersion = "1.2.3",
                        Platform = "Win-x64",
                        Channel = "Stable"
                    },
                    new PackageUpdateQuery
                    {
                        AppId = "SettingsEditor",
                        ComponentId = "Main",
                        CurrentVersion = "",
                        Platform = "Win-x64",
                        Channel = "Stable"
                    }
                }
            };

            var checkArgsBytes = MessagePackHelper.Serialize(checkArgs);
            Assert.NotNull(checkArgsBytes);
            Assert.NotEmpty(checkArgsBytes);

            var checkArgs2 = MessagePackHelper.Deserialize<CheckPackageUpdateArgs>(checkArgsBytes);
            Assert.NotNull(checkArgs2);
            Assert.Equal(checkArgs.Queries.Count, checkArgs2.Queries.Count);
            Assert.Equal(checkArgs.Queries[0].AppId, checkArgs2.Queries[0].AppId);
            Assert.Equal(checkArgs.Queries[0].CurrentVersion, checkArgs2.Queries[0].CurrentVersion);
            Assert.Equal(checkArgs.Queries[1].AppId, checkArgs2.Queries[1].AppId);

            // ===== 2) CheckPackageUpdateResult → Serialize/Deserialize（泛型） =====
            var checkResult = new CheckPackageUpdateResult
            {
                Updates = new List<PackageUpdateInfo>
                {
                    new PackageUpdateInfo
                    {
                        AppId = "Client",
                        ComponentId = "Main",
                        UpdateAvailable = true,
                        LatestVersion = "1.2.4",
                        Mandatory = false,
                        PackageSize = 12345678,
                        Sha256 = "ABCDEF0123456789",
                        Delivery = PackageDelivery.Url,
                        PackageUrl = "https://cdn.example.com/client-1.2.4.zip",
                        ReleaseNotes = "Minor fixes"
                    }
                }
            };

            var checkResultBytes = MessagePackHelper.Serialize(checkResult);
            Assert.NotNull(checkResultBytes);
            Assert.NotEmpty(checkResultBytes);

            var checkResult2 = MessagePackHelper.Deserialize<CheckPackageUpdateResult>(checkResultBytes);
            Assert.NotNull(checkResult2);
            Assert.Single(checkResult2.Updates);
            Assert.Equal(checkResult.Updates[0].AppId, checkResult2.Updates[0].AppId);
            Assert.Equal(checkResult.Updates[0].LatestVersion, checkResult2.Updates[0].LatestVersion);
            Assert.Equal(checkResult.Updates[0].Delivery, checkResult2.Updates[0].Delivery);

            // ===== 3) GetPackageArgs → Serialize/Deserialize（非泛型重載） =====
            var getArgs = new GetPackageArgs
            {
                AppId = "Client",
                ComponentId = "Main",
                Version = "1.2.4",
                Platform = "Win-x64",
                Channel = "Stable",
                FileId = ""
            };

            var getArgsBytes = MessagePackHelper.Serialize(getArgs, typeof(GetPackageArgs));
            Assert.NotNull(getArgsBytes);
            Assert.NotEmpty(getArgsBytes);

            var getArgs2Obj = MessagePackHelper.Deserialize(getArgsBytes, typeof(GetPackageArgs));
            var getArgs2 = Assert.IsType<GetPackageArgs>(getArgs2Obj);
            Assert.Equal(getArgs.AppId, getArgs2.AppId);
            Assert.Equal(getArgs.Version, getArgs2.Version);
            Assert.Equal(getArgs.Platform, getArgs2.Platform);
            Assert.Equal(getArgs.Channel, getArgs2.Channel);
            Assert.Equal(getArgs.FileId, getArgs2.FileId);

            // ===== 4) GetPackageResult → Serialize/Deserialize（泛型） =====
            var bytes = new byte[] { 1, 2, 3, 4, 5 }; // 模擬小檔案內容
            var getResult = new GetPackageResult
            {
                FileName = "client-main-win-x64-1.2.4.zip",
                Content = bytes,
                FileSize = bytes.LongLength,
                Sha256 = "ABCDEF0123456789",
                PackageUrl = "" // Delivery=Api 時通常為空
            };

            var getResultBytes = MessagePackHelper.Serialize(getResult);
            Assert.NotNull(getResultBytes);
            Assert.NotEmpty(getResultBytes);

            var getResult2 = MessagePackHelper.Deserialize<GetPackageResult>(getResultBytes);
            Assert.NotNull(getResult2);
            Assert.Equal(getResult.FileName, getResult2.FileName);
            Assert.Equal(getResult.FileSize, getResult2.FileSize);
            Assert.Equal(getResult.Sha256, getResult2.Sha256);
            Assert.Equal(getResult.PackageUrl, getResult2.PackageUrl);
            Assert.Equal(getResult.Content, getResult2.Content); // xUnit 對 byte[] 會做序列等值比對
        }
    }
}
