using System.Text.Json;
using System.Text.Json.Nodes;
using Manager;
using Net.VO;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserUdemaeMapperTests
{
    [Theory]
    [InlineData(0, 0, UdemaeID.Class_B5, UdemaeID.Class_B5, 0)]
    [InlineData(10512, 20999, UdemaeID.Class_B4, UdemaeID.Class_B3, 512)]
    [InlineData(10999, 20999, UdemaeID.Class_B4, UdemaeID.Class_B3, 999)]
    [InlineData(11000, 21000, UdemaeID.Class_B4, UdemaeID.Class_B3, 999)]
    [InlineData(11001, 21000, UdemaeID.Class_B4, UdemaeID.Class_B3, 0)]
    [InlineData(41000, 41000, UdemaeID.Class_B1, UdemaeID.Class_B1, 1000)]
    [InlineData(260512, 300100, UdemaeID.Class_LEGEND, UdemaeID.Class_LEGEND, 512)]
    [InlineData(int.MaxValue, int.MaxValue, UdemaeID.Class_LEGEND, UdemaeID.Class_LEGEND, 0)]
    public void WireToModelAndBack_PreservesCountersAndRebuildsClassData(
        int classValue,
        int maxClassValue,
        UdemaeID classId,
        UdemaeID maxClassId,
        int classPoint
    )
    {
        var json = CreateWireData(classValue, maxClassValue);
        var downloaded = JsonSerializer.Deserialize(json, JsonContext.Default.UserUdemae)!;
        var model = downloaded.ToModel();

        Assert.Equal(classValue, model.ClassValue);
        Assert.Equal(maxClassValue, model.MaxClassValue);
        Assert.Equal(classId, model.ClassID);
        Assert.Equal(maxClassId, model.MaxClassID);
        Assert.Equal(classPoint, model.ClassPoint);
        Assert.Equal(uint.MaxValue, model.TotalWinNum);
        Assert.Equal(11u, model.TotalLoseNum);
        Assert.Equal(12u, model.MaxWinNum);
        Assert.Equal(13u, model.MaxLoseNum);
        Assert.Equal(14u, model.WinNum);
        Assert.Equal(15u, model.LoseNum);
        Assert.Equal(16u, model.NpcTotalWinNum);
        Assert.Equal(17u, model.NpcTotalLoseNum);
        Assert.Equal(18u, model.NpcMaxWinNum);
        Assert.Equal(19u, model.NpcMaxLoseNum);
        Assert.Equal(20u, model.NpcWinNum);
        Assert.Equal(21u, model.NpcLoseNum);

#pragma warning disable CS0612 // Legacy values must not be copied in either direction.
        Assert.Equal(0, model.UdemaeValue);
        Assert.Equal(0, model.MaxUdemaeValue);
        model.UdemaeValue = 123;
        model.MaxUdemaeValue = 456;
#pragma warning restore CS0612
        var uploaded = JsonSerializer.SerializeToNode(model.ToVO(), JsonContext.Default.UserUdemae);
        var expected = JsonNode.Parse(json)!;
        expected["rate"] = 0;
        expected["maxRate"] = 0;
        Assert.True(JsonNode.DeepEquals(expected, uploaded));
    }

    [Fact]
    public void WireToModel_LegacyRatesAreOptional()
    {
        var json = JsonNode.Parse(CreateWireData(10512, 20999))!.AsObject();
        json.Remove("rate");
        json.Remove("maxRate");

        var model = JsonSerializer.Deserialize(json.ToJsonString(), JsonContext.Default.UserUdemae)!.ToModel();

        Assert.Equal(UdemaeID.Class_B4, model.ClassID);
        Assert.Equal(UdemaeID.Class_B3, model.MaxClassID);
        Assert.Equal(512, model.ClassPoint);
        Assert.Equal(uint.MaxValue, model.TotalWinNum);
    }

    private static string CreateWireData(int classValue, int maxClassValue) => $$"""
        {"rate":99,"maxRate":199,"classValue":{{classValue}},"maxClassValue":{{maxClassValue}},
         "totalWinNum":4294967295,"totalLoseNum":11,"maxWinNum":12,"maxLoseNum":13,
         "winNum":14,"loseNum":15,"npcTotalWinNum":16,"npcTotalLoseNum":17,
         "npcMaxWinNum":18,"npcMaxLoseNum":19,"npcWinNum":20,"npcLoseNum":21}
        """;
}
