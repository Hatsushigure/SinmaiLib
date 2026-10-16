using System.Text.Json;
using Net.VO;
using Net.VO.Mapper;
using SinmaiLib.Enums;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserActMapperTests
{
    [Theory]
    [InlineData(ActivityKind.Play, 1)]
    [InlineData(ActivityKind.Music, 2)]
    public void WireToModelAndBack_PreservesActivityDataAndNumericKind(
        ActivityKind kind,
        int wireKind
    )
    {
        // Exercise the production JSON context and mapper across both wire boundaries.
        var json = $$"""
            {"kind":{{wireKind}},"id":100001,"sortNumber":9007199254740993,
             "param1":11,"param2":22,"param3":33,"param4":44}
            """;
        var activity = JsonSerializer.Deserialize(json, JsonContext.Default.UserAct)!.ToModel();

        Assert.Equal(kind, activity.Kind);
        Assert.Equal(100001, activity.Id);
        Assert.Equal(9007199254740993L, activity.SortNumber);
        Assert.Equal(11, activity.Param1);
        Assert.Equal(22, activity.Param2);
        Assert.Equal(33, activity.Param3);
        Assert.Equal(44, activity.Param4);

        // Upload preparation changes Param2 for special music; the mapper must retain it.
        activity.Param2 = 10;
        using var uploaded = JsonDocument.Parse(
            JsonSerializer.Serialize(activity.ToVO(), JsonContext.Default.UserAct)
        );
        var root = uploaded.RootElement;
        Assert.Equal(7, root.EnumerateObject().Count());
        Assert.Equal(wireKind, root.GetProperty("kind").GetInt32());
        Assert.Equal(100001, root.GetProperty("id").GetInt32());
        Assert.Equal(9007199254740993L, root.GetProperty("sortNumber").GetInt64());
        Assert.Equal(11, root.GetProperty("param1").GetInt32());
        Assert.Equal(10, root.GetProperty("param2").GetInt32());
        Assert.Equal(33, root.GetProperty("param3").GetInt32());
        Assert.Equal(44, root.GetProperty("param4").GetInt32());
    }
}
