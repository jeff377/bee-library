using System.ComponentModel;
using System.IO;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Northwind.Server.Tests;

/// <summary>
/// Guards the <c>Define/ProgramSettings.xml</c> binding that routes the <c>Order</c> progId to
/// <see cref="Bee.Northwind.Server.BusinessObjects.OrderBO"/>. Deserializes the shipped file
/// through the same <see cref="XmlCodec"/> path the framework uses at runtime, so a malformed
/// XML shape or a renamed/moved BO type fails here rather than silently falling back to the
/// default <c>FormBusinessObject</c> in the running app.
/// </summary>
public class OrderBoBindingTests
{
    [Fact]
    [DisplayName("ProgramSettings.xml 將 Order 綁定到 OrderBO")]
    public void ProgramSettingsXml_BindsOrderToOrderBo()
    {
        // BaseDirectory is .../Bee.Northwind.Server.Tests/bin/Debug/net10.0; the Define folder
        // sits four levels up under the app root.
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Define", "ProgramSettings.xml"));

        var settings = XmlCodec.DeserializeFromFile<ProgramSettings>(path);
        Assert.NotNull(settings);

        var category = settings!.Categories!["common"];
        Assert.NotNull(category);

        var item = category!.Items!["Order"];
        Assert.NotNull(item);
        Assert.Equal("Bee.Northwind.Server.BusinessObjects.OrderBO, Bee.Northwind.Server", item!.BusinessObject);
    }
}
