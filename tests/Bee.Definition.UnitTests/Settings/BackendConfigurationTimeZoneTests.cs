using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// <see cref="BackendConfiguration.DefaultTimeZone"/> 的預設值測試。
    /// </summary>
    /// <remarks>
    /// 這個預設值是刻意的相容性選擇，不是隨手挑的常數：st_user.time_zone 是本版新增的欄位，
    /// 既有資料列全為空，若預設為空（＝UTC）會讓所有既有部署在升級後時刻整體位移。
    /// 改動此值等同改變既有部署的顯示時間，屬 breaking change。
    /// </remarks>
    public class BackendConfigurationTimeZoneTests
    {
        [Fact]
        [DisplayName("DefaultTimeZone 預設應為 Asia/Taipei（升級相容）")]
        public void DefaultTimeZone_Default_IsAsiaTaipei()
        {
            var config = new BackendConfiguration();

            Assert.Equal("Asia/Taipei", config.DefaultTimeZone);
        }

        [Fact]
        [DisplayName("DefaultTimeZone 可設為空字串以採用 UTC")]
        public void DefaultTimeZone_CanBeClearedForUtc()
        {
            var config = new BackendConfiguration { DefaultTimeZone = string.Empty };

            // 空字串是合法設定值：轉換層（FrameworkClock / DateTimeZoneConverter /
            // PayloadZoneConverter）一律把空時區視為 UTC，不做轉換。
            Assert.Empty(config.DefaultTimeZone);
        }
    }
}
