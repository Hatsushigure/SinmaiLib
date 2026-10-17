using System.Text.Json;
using System.Text.Json.Nodes;
using Net.VO;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserRivalMapperTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(9007199254740993UL)]
    [InlineData(ulong.MaxValue)]
    public void WireToModelAndBack_PreservesIdAndName(ulong rivalId)
    {
        var json = $$"""{"rivalId":{{rivalId}},"rivalName":"对手 Player"}""";
        var downloaded = JsonSerializer.Deserialize(json, JsonContext.Default.UserRivalData)!;
        var model = downloaded.ToModel();

        Assert.Equal(rivalId, model.RivalId);
        Assert.Equal("对手 Player", model.RivalName);
        var uploaded = JsonSerializer.SerializeToNode(model.ToVO(), JsonContext.Default.UserRivalData);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), uploaded));
    }

    [Fact]
    public void ListToModel_PreservesOrderDuplicatesAndIndependentNames()
    {
        var downloaded = new Net.VO.Mai2.UserRivalData { RivalId = 9007199254740993, RivalName = "Old" };
        var models = new[]
        {
            downloaded,
            new Net.VO.Mai2.UserRivalData { RivalId = 42, RivalName = "Other" },
            downloaded,
        }.ToModelList();

        Assert.Equal(new ulong[] { 9007199254740993, 42, 9007199254740993 }, models.Select(x => x.RivalId));
        models[0].RivalName = "Updated";
        var uploaded = models[0].ToVO();
        models[0].RivalName = "Updated again";

        Assert.Equal("Old", downloaded.RivalName);
        Assert.Equal("Old", models[2].RivalName);
        Assert.Equal("Updated", uploaded.RivalName);
    }

    [Fact]
    public void EmptyListToModel_ReturnsEmptyList() =>
        Assert.Empty(Array.Empty<Net.VO.Mai2.UserRivalData>().ToModelList());
}
