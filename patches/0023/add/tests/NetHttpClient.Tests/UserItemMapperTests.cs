using System.Text.Json;
using System.Text.Json.Nodes;
using Net.VO;
using Net.VO.Mai2;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserItemMapperTests
{
    public static TheoryData<ItemKind, int, bool> ItemCases => new()
    {
        { ItemKind.Plate, 1, true },
        { ItemKind.Title, 0, false },
        { ItemKind.Icon, 0, true },
        { ItemKind.Partner, 1, false },
        { ItemKind.Present, 42, true },
        { ItemKind.Frame, 1, true },
        { ItemKind.Ticket, 99, true },
        { ItemKind.IntimateItem, 9999, true },
    };

    [Theory]
    [MemberData(nameof(ItemCases))]
    public void WireToModelAndBack_PreservesItemData(ItemKind kind, int stock, bool isValid)
    {
        var json = $$"""
            {"itemKind":{{(int)kind}},"itemId":10001,"stock":{{stock}},"isValid":{{(isValid ? "true" : "false")}}}
            """;
        var downloaded = JsonSerializer.Deserialize(json, JsonContext.Default.UserItem)!;
        var item = downloaded.ToModel();

        Assert.Equal(10001, item.ItemId);
        Assert.Equal(stock, item.Stock);
        Assert.Equal(isValid, item.IsValid);
        var uploaded = JsonSerializer.Serialize(item.ToVO(kind), JsonContext.Default.UserItem);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(uploaded)));
    }

    [Fact]
    public void ModelToWire_UsesSuppliedKindAndKeepsSnapshotsIndependent()
    {
        var downloaded = new UserItem
        {
            ItemKind = ItemKind.Present,
            ItemId = 42,
            Stock = 2,
            IsValid = true,
        };
        var item = downloaded.ToModel();
        item.Stock = 0;
        item.IsValid = false;
        var uploaded = item.ToVO(ItemKind.Ticket);
        item.Stock = 99;

        Assert.Equal(ItemKind.Ticket, uploaded.ItemKind);
        Assert.Equal(42, uploaded.ItemId);
        Assert.Equal(0, uploaded.Stock);
        Assert.False(uploaded.IsValid);
        Assert.Equal(2, downloaded.Stock);
        Assert.True(downloaded.IsValid);
        Assert.Equal(ItemKind.Present, downloaded.ItemKind);
    }

    [Fact]
    public void ArrayConversions_PreserveOrderDuplicatesAndZeroStockItems()
    {
        UserItem[] downloaded =
        [
            new() { ItemKind = ItemKind.Music, ItemId = 42, Stock = 0, IsValid = false },
            new() { ItemKind = ItemKind.Music, ItemId = 7, Stock = 1, IsValid = true },
            new() { ItemKind = ItemKind.Music, ItemId = 42, Stock = 2, IsValid = true },
        ];

        var items = downloaded.ToModelList();

        Assert.Equal(new[] { 42, 7, 42 }, downloaded.ToIdList());
        Assert.Equal(new[] { 42, 7, 42 }, items.Select(item => item.ItemId));
        Assert.Equal(new[] { 0, 1, 2 }, items.Select(item => item.Stock));
        Assert.Equal(new[] { false, true, true }, items.Select(item => item.IsValid));
        items[0].Stock = 9;
        Assert.Equal(0, downloaded[0].Stock);
        Assert.Empty(Array.Empty<UserItem>().ToModelList());
        Assert.Empty(Array.Empty<UserItem>().ToIdList());
    }
}
