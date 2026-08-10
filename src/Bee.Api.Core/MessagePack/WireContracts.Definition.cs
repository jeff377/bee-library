using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the definition-layer wire contracts.
        /// </summary>
        private static void AddDefinitionTypes(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Definition.Collections.ListItem>()
                .Member(nameof(Bee.Definition.Collections.ListItem.Value), static x => x.Value, static (x, v) => x.Value = v)
                .Member(nameof(Bee.Definition.Collections.ListItem.Text), static x => x.Text, static (x, v) => x.Text = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Collections.Property>()
                .Member(nameof(Bee.Definition.Collections.Property.Name), static x => x.Name, static (x, v) => x.Name = v)
                .Member(nameof(Bee.Definition.Collections.Property.Value), static x => x.Value, static (x, v) => x.Value = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Identity.CompanyInfo>()
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.CompanyId), static x => x.CompanyId, static (x, v) => x.CompanyId = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.CompanyName), static x => x.CompanyName, static (x, v) => x.CompanyName = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.CompanyDatabaseId), static x => x.CompanyDatabaseId, static (x, v) => x.CompanyDatabaseId = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.CustomizeId), static x => x.CustomizeId, static (x, v) => x.CustomizeId = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.NumberFormats), static x => x.NumberFormats, static (x, v) => x.NumberFormats = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.DefaultCurrency), static x => x.DefaultCurrency, static (x, v) => x.DefaultCurrency = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.CashRounding), static x => x.CashRounding, static (x, v) => x.CashRounding = v)
                .Member(nameof(Bee.Definition.Identity.CompanyInfo.AllowedCurrencies), static x => x.AllowedCurrencies, static (x, v) => x.AllowedCurrencies = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Organization.DepartmentTree>()
                .Member(nameof(Bee.Definition.Organization.DepartmentTree.CompanyId), static x => x.CompanyId, static (x, v) => x.CompanyId = v)
                .Member(nameof(Bee.Definition.Organization.DepartmentTree.Roots), static x => x.Roots, static (x, v) => x.Roots = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Paging.PagingInfo>()
                .Member(nameof(Bee.Definition.Paging.PagingInfo.Page), static x => x.Page, static (x, v) => x.Page = v)
                .Member(nameof(Bee.Definition.Paging.PagingInfo.PageSize), static x => x.PageSize, static (x, v) => x.PageSize = v)
                .Member(nameof(Bee.Definition.Paging.PagingInfo.TotalCount), static x => x.TotalCount, static (x, v) => x.TotalCount = v)
                .Member(nameof(Bee.Definition.Paging.PagingInfo.HasMore), static x => x.HasMore, static (x, v) => x.HasMore = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Paging.PagingOptions>()
                .Member(nameof(Bee.Definition.Paging.PagingOptions.Page), static x => x.Page, static (x, v) => x.Page = v)
                .Member(nameof(Bee.Definition.Paging.PagingOptions.PageSize), static x => x.PageSize, static (x, v) => x.PageSize = v)
                .Member(nameof(Bee.Definition.Paging.PagingOptions.IncludeTotalCount), static x => x.IncludeTotalCount, static (x, v) => x.IncludeTotalCount = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Security.ApiKeySummary>()
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.SysId), static x => x.SysId, static (x, v) => x.SysId = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.SysName), static x => x.SysName, static (x, v) => x.SysName = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.KeyType), static x => x.KeyType, static (x, v) => x.KeyType = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.Contact), static x => x.Contact, static (x, v) => x.Contact = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.Enabled), static x => x.Enabled, static (x, v) => x.Enabled = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.ExpiredAt), static x => x.ExpiredAt, static (x, v) => x.ExpiredAt = v)
                .Member(nameof(Bee.Definition.Security.ApiKeySummary.IssuedAt), static x => x.IssuedAt, static (x, v) => x.IssuedAt = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Settings.CurrencyItem>()
                .Member(nameof(Bee.Definition.Settings.CurrencyItem.Code), static x => x.Code, static (x, v) => x.Code = v)
                .Member(nameof(Bee.Definition.Settings.CurrencyItem.Numeric), static x => x.Numeric, static (x, v) => x.Numeric = v)
                .Member(nameof(Bee.Definition.Settings.CurrencyItem.Rounding), static x => x.Rounding, static (x, v) => x.Rounding = v)
                .Member(nameof(Bee.Definition.Settings.CurrencyItem.Symbol), static x => x.Symbol, static (x, v) => x.Symbol = v)
                .Member(nameof(Bee.Definition.Settings.CurrencyItem.Name), static x => x.Name, static (x, v) => x.Name = v)
                .Build());
            list.Add(WireContract.For<Bee.Definition.Settings.UnitItem>()
                .Member(nameof(Bee.Definition.Settings.UnitItem.Code), static x => x.Code, static (x, v) => x.Code = v)
                .Member(nameof(Bee.Definition.Settings.UnitItem.Decimals), static x => x.Decimals, static (x, v) => x.Decimals = v)
                .Member(nameof(Bee.Definition.Settings.UnitItem.Dimension), static x => x.Dimension, static (x, v) => x.Dimension = v)
                .Member(nameof(Bee.Definition.Settings.UnitItem.Name), static x => x.Name, static (x, v) => x.Name = v)
                .Build());

            list.Add(new KeyCollectionBaseFormatter<Bee.Definition.Collections.ListItemCollection, Bee.Definition.Collections.ListItem>());
            list.Add(new KeyCollectionBaseFormatter<Bee.Definition.Collections.PropertyCollection, Bee.Definition.Collections.Property>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.CompanyAllowedCurrencies, Bee.Definition.AllowedCurrencyItem>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.CompanyCashRounding, Bee.Definition.CashRoundingItem>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.CompanyNumberFormats, Bee.Definition.NumberFormatItem>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.Filters.FilterNodeCollection, Bee.Definition.Filters.FilterNode>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.Organization.DepartmentNodeCollection, Bee.Definition.Organization.DepartmentNode>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.Settings.CurrencySettings, Bee.Definition.Settings.CurrencyItem>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.Settings.UnitSettings, Bee.Definition.Settings.UnitItem>());
            list.Add(new CollectionBaseFormatter<Bee.Definition.Sorting.SortFieldCollection, Bee.Definition.Sorting.SortField>());
        }
    }
}
