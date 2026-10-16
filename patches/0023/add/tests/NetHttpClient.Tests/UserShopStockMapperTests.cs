using System.Text.Json;
using System.Text.Json.Nodes;
using Net.VO;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserShopStockMapperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void WireToModelAndBack_PreservesShopStock(int tradeCount)
    {
        var json = $$"""{"shopItemId":42,"tradeCount":{{tradeCount}}}""";
        var stock = JsonSerializer.Deserialize(json, JsonContext.Default.UserShopStock)!.ToModel();

        Assert.Equal(42, stock.ShopItemId);
        Assert.Equal(tradeCount, stock.TradeCount);
        var uploaded = JsonSerializer.Serialize(stock.ToVO(), JsonContext.Default.UserShopStock);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(uploaded)));
    }

    [Fact]
    public void ModelToWire_ExportsPurchaseWithoutChangingDownloadedSnapshot()
    {
        var downloaded = new Net.VO.Mai2.UserShopStock { ShopItemId = 42, TradeCount = 7 };
        var stock = downloaded.ToModel();
        stock.TradeCount++;
        var uploaded = stock.ToVO();
        stock.TradeCount++;

        Assert.Equal(42, uploaded.ShopItemId);
        Assert.Equal(8, uploaded.TradeCount);
        Assert.Equal(7, downloaded.TradeCount);
    }
}
