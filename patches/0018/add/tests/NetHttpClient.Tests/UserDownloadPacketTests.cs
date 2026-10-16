using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Net;
using Net.Packet;
using Net.Packet.Mai2;
using Net.VO.Mai2;
using NetHttpClient.Tests.Support;
using Xunit;

namespace NetHttpClient.Tests;

public sealed partial class PacketTests
{
    private const ulong DownloadUserId = 9007199254740993;

    [Theory]
    [InlineData("Card")]
    [InlineData("Character")]
    [InlineData("Charge")]
    [InlineData("Course")]
    [InlineData("Data")]
    public async Task UserDownload_SerializesRequestAndDeliversTypedResponse(string kind)
    {
        await using var server = await LoopbackServer.StartAsync(JsonResponse(DownloadResponse(kind)));
        var results = new List<object>();
        var errors = new List<PacketStatus>();
        using var lease = CreateDownloadPacket(server, kind, results.Add, errors.Add);
        var packet = lease.Packet;
        var request = await server.ReceiveAsync();
        using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));

        Assert.Equal($"GetUser{kind}ApiMaimaiChn", packet.Query.Api);
        Assert.Equal(DownloadUserId, packet.Query.UserId);
        Assert.Equal(DownloadUserId, json.RootElement.GetProperty("userId").GetUInt64());
        Assert.Equal(kind == "Card" ? 3 : kind == "Course" ? 2 : 1,
            json.RootElement.EnumerateObject().Count());
        if (kind is "Card" or "Course")
            Assert.Equal(0, json.RootElement.GetProperty("nextIndex").GetInt64());
        if (kind == "Card")
            Assert.Equal(20, json.RootElement.GetProperty("maxCount").GetInt32());

        await lease.WaitForRequestAsync();
        Assert.Equal(PacketState.Done, packet.Proc());
        var result = Assert.Single(results);
        switch (kind)
        {
            case "Card":
                Assert.Equal(11, Assert.Single(Assert.IsType<UserCard[]>(result)).CardId);
                break;
            case "Character":
                Assert.Equal(11, Assert.Single(Assert.IsType<UserCharacter[]>(result)).CharacterId);
                break;
            case "Charge":
                Assert.Equal(11, Assert.Single(Assert.IsType<UserCharge[]>(result)).ChargeId);
                break;
            case "Course":
                Assert.Equal(11, Assert.Single(Assert.IsType<UserCourse[]>(result)).CourseId);
                break;
            case "Data":
                var (detail, banState) = Assert.IsType<(UserDetail, int)>(result);
                Assert.Equal("Test Player", detail.UserName);
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, detail.CharaSlot);
                Assert.Equal(1, banState);
                break;
        }
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Card")]
    [InlineData("Character")]
    [InlineData("Charge")]
    [InlineData("Course")]
    public async Task UserDownload_EmptyListCompletesWithEmptyArray(string kind)
    {
        var response = JsonNode.Parse(DownloadResponse(kind))!;
        response[$"user{kind}List"] = new JsonArray();
        await using var server = await LoopbackServer.StartAsync(JsonResponse(response.ToJsonString()));
        var results = new List<object>();
        using var lease = CreateDownloadPacket(server, kind, results.Add);

        await lease.WaitForRequestAsync();
        Assert.Equal(PacketState.Done, lease.Packet.Proc());
        Assert.Empty(Assert.IsAssignableFrom<Array>(Assert.Single(results)));
    }

    [Theory]
    [InlineData("Card", 7L)]
    [InlineData("Course", 10000000000L)]
    public async Task UserDownload_PaginatesThroughEmptyPageAndPreservesOrder(string kind, long cursor)
    {
        var first = JsonNode.Parse(DownloadResponse(kind))!;
        first["nextIndex"] = cursor;
        var empty = JsonNode.Parse(DownloadResponse(kind))!;
        empty["nextIndex"] = cursor + 1;
        empty[$"user{kind}List"] = new JsonArray();
        var last = JsonNode.Parse(DownloadResponse(kind))!;
        var idField = kind == "Card" ? "cardId" : "courseId";
        last[$"user{kind}List"]![0]![idField] = 22;
        await using var server = await LoopbackServer.StartAsync(
            JsonResponse(first.ToJsonString()), JsonResponse(empty.ToJsonString()), JsonResponse(last.ToJsonString()));
        var results = new List<object>();
        var errors = new List<PacketStatus>();
        using var lease = CreateDownloadPacket(server, kind, results.Add, errors.Add);

        foreach (var expectedCursor in new[] { 0, cursor, cursor + 1 })
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            Assert.Equal(DownloadUserId, json.RootElement.GetProperty("userId").GetUInt64());
            Assert.Equal(expectedCursor, json.RootElement.GetProperty("nextIndex").GetInt64());
            Assert.Empty(results);
            await lease.WaitForRequestAsync();
            Assert.Equal(expectedCursor == cursor + 1 ? PacketState.Done : PacketState.Process, lease.Packet.Proc());
        }

        var result = Assert.Single(results);
        var ids = kind == "Card"
            ? Assert.IsType<UserCard[]>(result).Select(card => card.CardId)
            : Assert.IsType<UserCourse[]>(result).Select(course => course.CourseId);
        Assert.Equal(new[] { 11, 22 }, ids);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Card", "userCardList")]
    [InlineData("Character", "userCharacterList")]
    [InlineData("Charge", "userChargeList")]
    [InlineData("Course", "userCourseList")]
    [InlineData("Data", "userData")]
    [InlineData("Data", "charaSlot")]
    public async Task UserDownload_RejectsNullAndMissingRequiredMembers(string kind, string member)
    {
        foreach (var omit in new[] { false, true })
        {
            var response = JsonNode.Parse(DownloadResponse(kind))!.AsObject();
            var target = member == "charaSlot" ? response["userData"]!.AsObject() : response;
            if (omit)
                target.Remove(member);
            else
                target[member] = null;
            await using var server = await LoopbackServer.StartAsync(JsonResponse(response.ToJsonString()));
            var results = new List<object>();
            var errors = new List<PacketStatus>();
            using var lease = CreateDownloadPacket(server, kind, results.Add, errors.Add);

            await lease.WaitForRequestAsync();
            Assert.Equal(PacketState.Error, lease.Packet.Proc());
            Assert.Equal(PacketStatus.ErrorDecodeResponse, Assert.Single(errors));
            Assert.Empty(results);
        }
    }

    [Theory]
    [InlineData("Card")]
    [InlineData("Character")]
    [InlineData("Charge")]
    [InlineData("Course")]
    [InlineData("Data")]
    public async Task UserDownload_HttpFailureInvokesErrorCallback(string kind)
    {
        await using var server = await LoopbackServer.StartAsync(new TestResponse(HttpStatusCode.InternalServerError));
        var results = new List<object>();
        var errors = new List<PacketStatus>();
        using var lease = CreateDownloadPacket(server, kind, results.Add, errors.Add);

        await Assert.ThrowsAsync<NetHttpException>(() => lease.WaitForRequestAsync());
        Assert.Equal(PacketState.Error, lease.Packet.Proc());
        Assert.Equal(PacketStatus.ErrorInternal, Assert.Single(errors));
        Assert.Empty(results);
    }

    private DownloadPacketLease CreateDownloadPacket(
        LoopbackServer server, string kind, Action<object> onDone, Action<PacketStatus>? onError = null)
    {
        _manager.BaseUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority) + "/";
        Packet packet = kind switch
        {
            "Card" => new PacketGetUserCard(DownloadUserId, data => onDone(data), onError),
            "Character" => new PacketGetUserCharacter(DownloadUserId, data => onDone(data), onError),
            "Charge" => new PacketGetUserCharge(DownloadUserId, data => onDone(data), onError),
            "Course" => new PacketGetUserCourse(DownloadUserId, data => onDone(data), onError),
            "Data" => new PacketGetUserData(DownloadUserId, (data, ban) => onDone((data, ban)), onError),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        return new DownloadPacketLease(packet);
    }

    private static TestResponse JsonResponse(string json) =>
        new(Body: WirePayload.Encode(Encoding.UTF8.GetBytes(json)));

    private static string DownloadResponse(string kind) => kind switch
    {
        "Card" => """
            {"userId":9007199254740993,"nextIndex":0,"userCardList":[
              {"cardId":11,"cardTypeId":2,"charaId":3,"mapId":4,"startDate":"2026-01-01","endDate":"2026-12-31"}]}
            """,
        "Character" => """
            {"userId":9007199254740993,"userCharacterList":[{"characterId":11,"level":2,"awakening":3,"useCount":4}]}
            """,
        "Charge" => """
            {"userId":9007199254740993,"length":1,"userChargeList":[
              {"chargeId":11,"stock":2,"purchaseDate":"2026-01-01","validDate":"2026-12-31"}]}
            """,
        "Course" => """
            {"userId":9007199254740993,"nextIndex":0,"userCourseList":[
              {"courseId":11,"isLastClear":true,"totalRestlife":2,"totalAchievement":3,"totalDeluxscore":4,
               "playCount":5,"clearDate":"2026-01-01","lastPlayDate":"2026-01-02","bestAchievement":6,
               "bestAchievementDate":"2026-01-01","bestDeluxscore":7,"bestDeluxscoreDate":"2026-01-01"}]}
            """,
        "Data" => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "UserDetailResponse.json")),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private sealed class DownloadPacketLease(Packet packet) : IDisposable
    {
        private static readonly FieldInfo RequestTaskField = typeof(Packet).GetField(
            "_requestTask", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ClearMethod = typeof(Packet).GetMethod(
            "Clear", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public Packet Packet { get; } = packet;

        public Task<NetHttpResponse> WaitForRequestAsync() =>
            ((Task<NetHttpResponse>)RequestTaskField.GetValue(Packet)!).WaitAsync(TimeSpan.FromSeconds(10));

        public void Dispose() => ClearMethod.Invoke(Packet, null);
    }
}
