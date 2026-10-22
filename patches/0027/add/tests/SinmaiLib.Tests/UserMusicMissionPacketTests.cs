using System.Text.Json;
using System.Text.Json.Nodes;
using Net.Packet;
using Net.VO.Mai2;
using SinmaiLib.Tests.Support;
using Xunit;

namespace SinmaiLib.Tests;

public sealed partial class PacketTests
{
    [Fact]
    public async Task UserDownload_MusicFlattensGroupsAcrossPagesAndPreservesDuplicates()
    {
        var first = JsonNode.Parse(DownloadResponse("Music"))!;
        first["nextIndex"] = int.MaxValue - 1;
        var groups = first["userMusicList"]!.AsArray();
        var detail = groups[0]!["userMusicDetailList"]![0]!.DeepClone();
        detail["musicId"] = 22;
        groups[0]!["userMusicDetailList"]!.AsArray().Add(detail);
        groups.Add(new JsonObject { ["userMusicDetailList"] = new JsonArray() });
        groups.Add(groups[0]!.DeepClone());
        var empty = JsonNode.Parse(DownloadResponse("Music"))!;
        empty["nextIndex"] = int.MaxValue;
        empty["userMusicList"] = new JsonArray();
        await using var server = await LoopbackServer.StartAsync(
            JsonResponse(first.ToJsonString()), JsonResponse(empty.ToJsonString()),
            JsonResponse(DownloadResponse("Music")));
        var results = new List<object>();
        var errors = new List<PacketStatus>();
        using var lease = CreateDownloadPacket(server, "Music", results.Add, errors.Add);

        foreach (var cursor in new[] { 0, int.MaxValue - 1, int.MaxValue })
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            Assert.Equal(DownloadUserId, json.RootElement.GetProperty("userId").GetUInt64());
            Assert.Equal(cursor, json.RootElement.GetProperty("nextIndex").GetInt32());
            Assert.Equal(50, json.RootElement.GetProperty("maxCount").GetInt32());
            Assert.Empty(results);
            await lease.WaitForRequestAsync();
            Assert.Equal(cursor == int.MaxValue ? PacketState.Done : PacketState.Process, lease.Packet.Proc());
        }

        var music = Assert.IsType<UserMusicDetail[]>(Assert.Single(results));
        Assert.Equal(new[] { 11, 22, 11, 22, 11 }, music.Select(item => item.MusicId));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task UserDownload_MusicAllowsEmptyDetailList()
    {
        var response = JsonNode.Parse(DownloadResponse("Music"))!;
        response["userMusicList"]![0]!["userMusicDetailList"] = new JsonArray();
        await using var server = await LoopbackServer.StartAsync(JsonResponse(response.ToJsonString()));
        var results = new List<object>();
        using var lease = CreateDownloadPacket(server, "Music", results.Add);

        await lease.WaitForRequestAsync();
        Assert.Equal(PacketState.Done, lease.Packet.Proc());
        Assert.Empty(Assert.IsType<UserMusicDetail[]>(Assert.Single(results)));
    }

    [Fact]
    public async Task UserDownload_MissionAllowsEmptyMissionsAndPreservesWeeklyData()
    {
        var response = JsonNode.Parse(DownloadResponse("MissionData"))!;
        response["userMissionDataList"] = new JsonArray();
        await using var server = await LoopbackServer.StartAsync(JsonResponse(response.ToJsonString()));
        var results = new List<object>();
        using var lease = CreateDownloadPacket(server, "MissionData", results.Add);

        await lease.WaitForRequestAsync();
        Assert.Equal(PacketState.Done, lease.Packet.Proc());
        var mission = Assert.IsType<UserMissionResponseData>(Assert.Single(results));
        Assert.Empty(mission.UserMissionDataList);
        Assert.Equal("2026-10-19", mission.UserWeeklyData.LastLoginWeek);
        Assert.Equal("2026-10-12", mission.UserWeeklyData.BeforeLoginWeek);
        Assert.True(mission.UserWeeklyData.FriendBonusFlag);
    }
}
