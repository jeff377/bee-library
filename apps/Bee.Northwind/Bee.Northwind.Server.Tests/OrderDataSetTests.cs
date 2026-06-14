using System.ComponentModel;
using System.Data;
using Bee.Base.Exceptions;
using Bee.Northwind.Server.BusinessObjects;

namespace Bee.Northwind.Server.Tests;

/// <summary>
/// Unit tests for the in-memory order DataSet logic in <see cref="OrderDataSet"/>, exercised
/// against hand-built DataSets (no database).
/// </summary>
public class OrderDataSetTests
{
    private sealed record Line(Guid Product, int Quantity, decimal Price, decimal Discount);

    private static DataSet BuildOrder(Guid? customer, params Line[] lines)
    {
        var ds = new DataSet();

        var master = new DataTable(OrderDataSet.MasterTable);
        master.Columns.Add("sys_rowid", typeof(Guid));
        master.Columns.Add("sys_id", typeof(string));
        master.Columns.Add("customer_rowid", typeof(Guid));
        master.Columns.Add("status", typeof(string));
        master.Columns.Add("total_amount", typeof(decimal));
        var mrow = master.NewRow();
        mrow["sys_rowid"] = Guid.NewGuid();
        if (customer.HasValue) { mrow["customer_rowid"] = customer.Value; }
        mrow["status"] = OrderRules.StatusDraft;
        master.Rows.Add(mrow);
        ds.Tables.Add(master);

        var detail = new DataTable(OrderDataSet.DetailTable);
        detail.Columns.Add("sys_rowid", typeof(Guid));
        detail.Columns.Add("product_rowid", typeof(Guid));
        detail.Columns.Add("quantity", typeof(int));
        detail.Columns.Add("unit_price", typeof(decimal));
        detail.Columns.Add("discount", typeof(decimal));
        detail.Columns.Add("amount", typeof(decimal));
        foreach (var line in lines)
        {
            var r = detail.NewRow();
            r["sys_rowid"] = Guid.NewGuid();
            r["product_rowid"] = line.Product;
            r["quantity"] = line.Quantity;
            r["unit_price"] = line.Price;
            r["discount"] = line.Discount;
            detail.Rows.Add(r);
        }
        ds.Tables.Add(detail);

        return ds;
    }

    private static Line ValidLine(int qty = 5, decimal price = 10m, decimal discount = 0m)
        => new(Guid.NewGuid(), qty, price, discount);

    [Fact]
    [DisplayName("Validate 未選客戶時拋出可讀訊息")]
    public void Validate_NoCustomer_Throws()
    {
        var ds = BuildOrder(customer: null, ValidLine());
        var ex = Assert.Throws<UserMessageException>(() => OrderDataSet.Validate(ds));
        Assert.Contains("customer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("Validate 無明細時拋出可讀訊息")]
    public void Validate_NoDetails_Throws()
    {
        var ds = BuildOrder(Guid.NewGuid());
        var ex = Assert.Throws<UserMessageException>(() => OrderDataSet.Validate(ds));
        Assert.Contains("at least one", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("Validate 明細未選商品時拋出可讀訊息")]
    public void Validate_DetailWithoutProduct_Throws()
    {
        var ds = BuildOrder(Guid.NewGuid(), new Line(Guid.Empty, 5, 10m, 0m));
        var ex = Assert.Throws<UserMessageException>(() => OrderDataSet.Validate(ds));
        Assert.Contains("product", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("Validate 數量非正時拋出可讀訊息")]
    public void Validate_NonPositiveQuantity_Throws()
    {
        var ds = BuildOrder(Guid.NewGuid(), ValidLine(qty: 0));
        var ex = Assert.Throws<UserMessageException>(() => OrderDataSet.Validate(ds));
        Assert.Contains("quantity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("Validate 合法訂單不拋例外")]
    public void Validate_ValidOrder_DoesNotThrow()
    {
        var ds = BuildOrder(Guid.NewGuid(), ValidLine(), ValidLine());
        Assert.Null(Record.Exception(() => OrderDataSet.Validate(ds)));
    }

    [Fact]
    [DisplayName("ComputeAmounts 計算每列金額並彙總主表總額")]
    public void ComputeAmounts_WritesLineAndTotal()
    {
        var ds = BuildOrder(Guid.NewGuid(),
            new Line(Guid.NewGuid(), 10, 18m, 0m),    // 180
            new Line(Guid.NewGuid(), 40, 97m, 0m),    // 3880
            new Line(Guid.NewGuid(), 10, 100m, 0.10m)); // 900

        var total = OrderDataSet.ComputeAmounts(ds);

        Assert.Equal(4960m, total);
        var details = ds.Tables[OrderDataSet.DetailTable]!.Rows;
        Assert.Equal(180m, details[0]["amount"]);
        Assert.Equal(3880m, details[1]["amount"]);
        Assert.Equal(900m, details[2]["amount"]);
        Assert.Equal(4960m, ds.Tables[OrderDataSet.MasterTable]!.Rows[0]["total_amount"]);
    }
}
