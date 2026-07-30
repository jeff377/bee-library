using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// <see cref="BackendConfiguration.DefaultLanguage"/> 的預設值測試。
    /// </summary>
    /// <remarks>
    /// 與 <see cref="BackendConfigurationTimeZoneTests"/> 同理，預設值是刻意的相容性選擇：
    /// SessionInfo.Culture 原本硬編為 zh-TW，既有部署實際上都跑在該語系；改為由使用者屬性
    /// 決定後，未設值者必須落回同一個值才不會在升級後整體換語言。
    /// </remarks>
    public class BackendConfigurationDefaultLanguageTests
    {
        [Fact]
        [DisplayName("DefaultLanguage 預設應為 zh-TW（升級相容）")]
        public void DefaultLanguage_Default_IsZhTw()
        {
            var config = new BackendConfiguration();

            Assert.Equal("zh-TW", config.DefaultLanguage);
        }

        [Fact]
        [DisplayName("DefaultLanguage 可設為空字串，交由語言服務自行決定預設")]
        public void DefaultLanguage_CanBeCleared()
        {
            var config = new BackendConfiguration { DefaultLanguage = string.Empty };

            Assert.Empty(config.DefaultLanguage);
        }
    }
}
