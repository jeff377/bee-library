using System.ComponentModel;
using Bee.Northwind.Server.BusinessObjects;

namespace Bee.Northwind.Server.Tests;

/// <summary>Unit tests for the pure order business rules in <see cref="OrderRules"/>.</summary>
public class OrderRulesTests
{
    [Theory]
    [InlineData("Draft", "Draft", true)]
    [InlineData("Draft", "Confirmed", true)]
    [InlineData("Confirmed", "Shipped", true)]
    [InlineData("Shipped", "Shipped", true)]
    [InlineData("Confirmed", "Draft", false)]
    [InlineData("Shipped", "Confirmed", false)]
    [InlineData("Draft", "Shipped", false)]
    [InlineData("Draft", "Bogus", false)]
    [DisplayName("IsValidTransition 只允許不變或前進一步")]
    public void IsValidTransition_OnlyForwardByOneOrSame(string from, string to, bool expected)
    {
        Assert.Equal(expected, OrderRules.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData("Draft", false)]
    [InlineData("Confirmed", true)]
    [InlineData("Shipped", true)]
    [DisplayName("DetailsLocked 自 Confirmed 起鎖定明細")]
    public void DetailsLocked_LockedFromConfirmed(string status, bool expected)
    {
        Assert.Equal(expected, OrderRules.DetailsLocked(status));
    }

    [Theory]
    [InlineData(10, 18, 0, 180)]
    [InlineData(40, 97, 0, 3880)]
    [InlineData(10, 100, 0.10, 900)]
    [InlineData(0, 50, 0, 0)]
    [DisplayName("LineAmount = 數量 × 單價 × (1 - 折扣)")]
    public void LineAmount_AppliesDiscount(int qty, decimal price, decimal discount, decimal expected)
    {
        Assert.Equal(expected, OrderRules.LineAmount(qty, price, discount));
    }

    [Fact]
    [DisplayName("NextOrderNumber 無既有號碼時自 001 起算")]
    public void NextOrderNumber_FirstOfMonth_StartsAt001()
    {
        Assert.Equal("ORD-202606-001", OrderRules.NextOrderNumber("202606", null));
    }

    [Fact]
    [DisplayName("NextOrderNumber 遞增既有最大號碼")]
    public void NextOrderNumber_IncrementsExistingMax()
    {
        Assert.Equal("ORD-202606-008", OrderRules.NextOrderNumber("202606", "ORD-202606-007"));
    }

    [Fact]
    [DisplayName("NextOrderNumber 跨百遞增維持三位數")]
    public void NextOrderNumber_IncrementsPastNinetyNine()
    {
        Assert.Equal("ORD-202606-100", OrderRules.NextOrderNumber("202606", "ORD-202606-099"));
    }
}
