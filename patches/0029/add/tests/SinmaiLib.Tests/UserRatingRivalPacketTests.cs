using System.Text.Json;
using System.Text.Json.Nodes;
using Net;
using Net.Packet;
using Net.Packet.Mai2;
using Net.VO.Mai2;
using SinmaiLib.Tests.Support;
using Xunit;
using ErrorCounter = Manager.BackupNetworkInformationRecord.ErrorKind;

namespace SinmaiLib.Tests;

public sealed partial class PacketTests
{
    [Theory]
    [InlineData("Rating")]
    [InlineData("RecommendRateMusic")]
    [InlineData("RecommendSelectionMusic")]
    [InlineData("Region")]
    public async Task UserDownload_RatingRecommendationsAndRegionAllowEmptyLists(string kind)
    {
        var response = JsonNode.Parse(DownloadResponse(kind))!;
        if (kind == "Rating")
        {
            foreach (var field in new[] { "ratingList", "newRatingList", "nextRatingList", "nextNewRatingList" })
                response["userRating"]![field] = new JsonArray();
        }
        else
        {
            response[kind == "Region" ? "userRegionList" : $"user{kind}IdList"] = new JsonArray();
            if (kind == "Region")
                response["length"] = 0;
        }
        await using var server = await LoopbackServer.StartAsync(JsonResponse(response.ToJsonString()));
        var results = new List<object>();
        using var lease = CreateDownloadPacket(server, kind, results.Add);

        await lease.WaitForRequestAsync();
        Assert.Equal(PacketState.Done, lease.Packet.Proc());
        var result = Assert.Single(results);
        if (kind == "Rating")
        {
            var rating = Assert.IsType<UserRating>(result);
            Assert.Empty(rating.RatingList);
            Assert.Empty(rating.NewRatingList);
            Assert.Empty(rating.NextRatingList);
            Assert.Empty(rating.NextNewRatingList);
            Assert.Equal(12345, rating.Rating);
        }
        else
            Assert.Empty(Assert.IsAssignableFrom<Array>(result));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UserDownload_RivalRequestsPreserveIdsAndOrderWithoutPartialResults(bool invalidLastResponse)
    {
        // Duplicate rival IDs must produce duplicate results in the requested order.
        var ids = new[] { ulong.MaxValue, DownloadUserId + 1, ulong.MaxValue };
        var responses = ids.Select(id =>
        {
            var response = JsonNode.Parse(DownloadResponse("RivalData"))!;
            response["userRivalData"]!["rivalId"] = id;
            return response;
        }).ToArray();
        if (invalidLastResponse)
            responses[^1]["userRivalData"] = null;
        await using var server = await LoopbackServer.StartAsync(
            responses.Select(response => JsonResponse(response.ToJsonString())).ToArray());
        _manager.BaseUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority) + "/";
        var results = new List<UserRivalData[]>();
        var errors = new List<PacketStatus>();
        using var lease = new DownloadPacketLease(new PacketGetUserRivalData(DownloadUserId,
            ids.Select((id, index) => new UserFavoriteItem { OrderId = (ulong)index, Id = id }).ToArray(),
            results.Add, errors.Add));

        for (var index = 0; index < ids.Length; index++)
        {
            var request = await server.ReceiveAsync();
            using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
            Assert.Equal(DownloadUserId, lease.Packet.Query.UserId);
            Assert.Equal(DownloadUserId, json.RootElement.GetProperty("userId").GetUInt64());
            Assert.Equal(ids[index], json.RootElement.GetProperty("rivalId").GetUInt64());
            Assert.Empty(results);
            Assert.Empty(errors);
            await lease.WaitForRequestAsync();
            var expectedState = index < ids.Length - 1 ? PacketState.Process
                : invalidLastResponse ? PacketState.Error : PacketState.Done;
            Assert.Equal(expectedState, lease.Packet.Proc());
        }

        if (invalidLastResponse)
        {
            Assert.Empty(results);
            Assert.Equal(PacketStatus.ErrorDecodeResponse, Assert.Single(errors));
        }
        else
        {
            Assert.Equal(ids, Assert.Single(results).Select(rival => rival.RivalId));
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task UserDownload_RivalRetryBudgetInvokesTimeoutWithoutDeliveringData()
    {
        await using var server = await LoopbackServer.StartAsync(JsonResponse(DownloadResponse("RivalData")));
        var results = new List<object>();
        var errors = new List<PacketStatus>();
        using var lease = CreateDownloadPacket(server, "RivalData", results.Add, errors.Add);
        await lease.WaitForRequestAsync();
        for (var count = 0; count < NetConfig.RetryCountToErrorWithSamePacket; count++)
            _counters.AddErrorNum(ErrorCounter.Packet_Retry);

        lease.Packet.Proc();

        Assert.Empty(results);
        Assert.Equal(PacketStatus.ErrorTimeout, Assert.Single(errors));
        Assert.Equal(1, _counters.GetErrorNum(ErrorCounter.UserDataDownloadError));
    }

    [Fact]
    public void UserDownload_EmptyRivalIdsCompleteImmediately()
    {
        var results = new List<UserRivalData[]>();
        var errors = new List<PacketStatus>();

        _ = new PacketGetUserRivalData(DownloadUserId, [], results.Add, errors.Add);

        Assert.Empty(Assert.Single(results));
        Assert.Empty(errors);
    }
}
