using System.Text.Json;
using System.Text.Json.Nodes;
using Net.VO;
using Net.VO.Mapper;
using Xunit;

namespace NetHttpClient.Tests;

public sealed class UserCourseMapperTests
{
    [Theory]
    [InlineData(false, 0u, "")]
    [InlineData(true, 17u, "2026-01-01 12:34:56")]
    public void WireToModelAndBack_PreservesCourseData(
        bool isLastClear,
        uint totalRestlife,
        string clearDate
    )
    {
        var json = $$"""
            {"courseId":42,"isLastClear":{{(isLastClear ? "true" : "false")}},
             "totalRestlife":{{totalRestlife}},"totalAchievement":4012345,"totalDeluxscore":8123,
             "playCount":4294967295,"clearDate":"{{clearDate}}","lastPlayDate":"2026-02-02 01:02:03",
             "bestAchievement":4020123,"bestAchievementDate":"2026-01-03 04:05:06",
             "bestDeluxscore":9000,"bestDeluxscoreDate":"2026-01-04 07:08:09"}
            """;
        var course = JsonSerializer.Deserialize(json, JsonContext.Default.UserCourse)!.ToModel();

        Assert.Equal(42, course.CourseId);
        Assert.Equal(isLastClear, course.IsLastClear);
        Assert.Equal(totalRestlife, course.TotalRestlife);
        Assert.Equal(4012345u, course.TotalAchievement);
        Assert.Equal(8123u, course.TotalDeluxscore);
        Assert.Equal(uint.MaxValue, course.PlayCount);
        Assert.Equal(clearDate, course.ClearDate);
        Assert.Equal("2026-02-02 01:02:03", course.LastPlayDate);
        Assert.Equal(4020123u, course.BestAchievement);
        Assert.Equal("2026-01-03 04:05:06", course.BestAchievementDate);
        Assert.Equal(9000u, course.BestDeluxscore);
        Assert.Equal("2026-01-04 07:08:09", course.BestDeluxscoreDate);

        var uploaded = JsonSerializer.Serialize(course.ToVO(), JsonContext.Default.UserCourse);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(uploaded)));
    }

    [Fact]
    public void ModelToWire_ExportsUpdatedCourseWithoutChangingDownloadedSnapshot()
    {
        var downloaded = new Net.VO.Mai2.UserCourse
        {
            CourseId = 42,
            IsLastClear = false,
            TotalRestlife = 0,
            TotalAchievement = 100,
            TotalDeluxscore = 200,
            PlayCount = 3,
            ClearDate = "",
            LastPlayDate = "2026-01-01 01:02:03",
            BestAchievement = 300,
            BestAchievementDate = "2026-01-02 04:05:06",
            BestDeluxscore = 400,
            BestDeluxscoreDate = "2026-01-03 07:08:09",
        };
        var course = downloaded.ToModel();
        const string playedAt = "2026-02-01 12:34:56";
        course.IsLastClear = true;
        course.TotalRestlife = 50;
        course.TotalAchievement = 500;
        course.TotalDeluxscore = 600;
        course.PlayCount++;
        course.ClearDate = playedAt;
        course.LastPlayDate = playedAt;
        course.BestAchievement = 700;
        course.BestAchievementDate = playedAt;
        course.BestDeluxscore = 800;
        course.BestDeluxscoreDate = playedAt;

        var uploaded = JsonSerializer.SerializeToNode(course.ToVO(), JsonContext.Default.UserCourse)!;

        Assert.Equal(12, uploaded.AsObject().Count);
        Assert.Equal(42, uploaded["courseId"]!.GetValue<int>());
        Assert.True(uploaded["isLastClear"]!.GetValue<bool>());
        Assert.Equal(50u, uploaded["totalRestlife"]!.GetValue<uint>());
        Assert.Equal(500u, uploaded["totalAchievement"]!.GetValue<uint>());
        Assert.Equal(600u, uploaded["totalDeluxscore"]!.GetValue<uint>());
        Assert.Equal(4u, uploaded["playCount"]!.GetValue<uint>());
        Assert.Equal(playedAt, uploaded["clearDate"]!.GetValue<string>());
        Assert.Equal(playedAt, uploaded["lastPlayDate"]!.GetValue<string>());
        Assert.Equal(700u, uploaded["bestAchievement"]!.GetValue<uint>());
        Assert.Equal(playedAt, uploaded["bestAchievementDate"]!.GetValue<string>());
        Assert.Equal(800u, uploaded["bestDeluxscore"]!.GetValue<uint>());
        Assert.Equal(playedAt, uploaded["bestDeluxscoreDate"]!.GetValue<string>());
        Assert.False(downloaded.IsLastClear);
        Assert.Equal(3u, downloaded.PlayCount);
        Assert.Equal("", downloaded.ClearDate);
    }
}
