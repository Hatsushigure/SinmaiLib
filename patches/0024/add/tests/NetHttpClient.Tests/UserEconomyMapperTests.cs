using System.Text.Json;
using System.Text.Json.Nodes;
using Manager.UserDatas;
using Net.VO;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserEconomyMapperTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(9007199254740993L)]
    [InlineData(long.MaxValue)]
    public void GetPointUpload_PreservesLongPointsAndWireNames(long point)
    {
        var model = new UserGetPoint { GetKind = 3, Point = point };
        var uploaded = model.ToVO();
        model.Point = 1;

        var json = JsonSerializer.Serialize(uploaded, JsonContext.Default.UserGetPoint);
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse($$"""{"getKind":3,"point":{{point}}}"""), JsonNode.Parse(json)));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(9007199254740993L)]
    [InlineData(long.MaxValue)]
    public void TradeItemUpload_PreservesLongValuesAndWireNames(long value)
    {
        var model = new UserTradeItem { ShopItemId = value, Point = value, TradeCount = int.MaxValue };
        var uploaded = model.ToVO();
        model.ShopItemId = 1;
        model.Point = 1;
        model.TradeCount = 1;

        var json = JsonSerializer.Serialize(uploaded, JsonContext.Default.UserTradeItem);
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse($$"""{"shopItemId":{{value}},"point":{{value}},"tradeCount":2147483647}"""),
            JsonNode.Parse(json)));
    }

    [Fact]
    public void UploadLists_PreserveOrderAndDoNotTrackLaterPurchases()
    {
        var points = new List<UserGetPoint>
        {
            new() { GetKind = 3, Point = 17 },
            new() { GetKind = 1, Point = 29 },
        };
        var trades = new List<UserTradeItem>
        {
            new() { ShopItemId = 42, Point = 200, TradeCount = 2 },
            new() { ShopItemId = 11, Point = 100, TradeCount = 1 },
        };

        var uploadedPoints = points.ToVOArray();
        var uploadedTrades = trades.ToVOArray();
        points[0].Point++;
        trades[0].Point += 100;
        trades[0].TradeCount++;
        points.Clear();
        trades.Clear();

        Assert.Collection(uploadedPoints,
            item =>
            {
                Assert.Equal(3, item.GetKind);
                Assert.Equal(17L, item.Point);
            },
            item =>
            {
                Assert.Equal(1, item.GetKind);
                Assert.Equal(29L, item.Point);
            });
        Assert.Collection(uploadedTrades,
            item =>
            {
                Assert.Equal(42L, item.ShopItemId);
                Assert.Equal(200L, item.Point);
                Assert.Equal(2, item.TradeCount);
            },
            item =>
            {
                Assert.Equal(11L, item.ShopItemId);
                Assert.Equal(100L, item.Point);
                Assert.Equal(1, item.TradeCount);
            });
    }

    [Fact]
    public void UploadLists_EmptyModelsProduceEmptyArrays()
    {
        Assert.Empty(new List<UserGetPoint>().ToVOArray());
        Assert.Empty(new List<UserTradeItem>().ToVOArray());
    }
}
