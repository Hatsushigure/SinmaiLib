using System.Text.Json;
using MAI2.Util;
using Manager;
using Net.Packet;
using Net.Packet.Mai2;
using Net.VO.Mai2;
using SinmaiLib.Tests.Support;
using Xunit;

namespace SinmaiLib.Tests;

public sealed partial class PacketTests
{
    [Fact]
    public async Task UserDownload_ScoreRankingRequestsEachRankingWithStableApiAndOrder()
    {
        int[] rankingIds = [101, 202];
        var rankings = Singleton<ScoreRankingManager>.Instance.ScoreRankings;
        rankings.Clear();
        foreach (var rankingId in rankingIds)
            rankings[rankingId] = new ScoreRankingForMusicSeq();

        try
        {
            await using var server = await LoopbackServer.StartAsync(
                JsonResponse(ScoreRankingResponse(rankingIds[0])),
                JsonResponse(ScoreRankingResponse(rankingIds[1])));
            _manager.BaseUri = new Uri(server.Url).GetLeftPart(UriPartial.Authority) + "/";
            var results = new List<UserScoreRanking[]>();
            var errors = new List<PacketStatus>();
            using var lease = new DownloadPacketLease(new PacketGetUserScoreRanking(
                DownloadUserId, results.Add, errors.Add));

            for (var index = 0; index < rankingIds.Length; index++)
            {
                var request = await server.ReceiveAsync();
                using var json = JsonDocument.Parse(WirePayload.Decode(request.Body));
                Assert.Equal("GetUserScoreRankingApiMaimaiChn", lease.Packet.Query.Api);
                Assert.Equal(DownloadUserId, lease.Packet.Query.UserId);
                Assert.Equal(DownloadUserId, json.RootElement.GetProperty("userId").GetUInt64());
                Assert.Equal(rankingIds[index],
                    json.RootElement.GetProperty("competitionId").GetInt32());
                Assert.Empty(results);
                Assert.Empty(errors);

                await lease.WaitForRequestAsync();
                Assert.Equal(index < rankingIds.Length - 1 ? PacketState.Process : PacketState.Done,
                    lease.Packet.Proc());
            }

            Assert.Equal(rankingIds,
                Assert.Single(results).Select(ranking => ranking.TournamentId));
            Assert.Empty(errors);
        }
        finally
        {
            rankings.Clear();
        }
    }

    private static string ScoreRankingResponse(int tournamentId) =>
        "{\"userId\":" + DownloadUserId
        + ",\"userScoreRanking\":{\"tournamentId\":" + tournamentId
        + ",\"totalScore\":123,\"ranking\":4,\"rankingDate\":\"2026-10-20\"}}";
}
