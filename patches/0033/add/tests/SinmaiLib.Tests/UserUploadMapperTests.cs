using DB;
using Manager;
using Manager.UserDatas;
using SinmaiLib.Net.VO.Mapper;
using Xunit;

namespace SinmaiLib.Tests;

public sealed class UserUploadMapperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(-1)]
    public void MusicDetail_PreservesAllScoreFieldsAndSuppliedDifficulty(int difficulty)
    {
        var source = new UserScore(100123)
        {
            PlayCount = uint.MaxValue,
            Achievement = 1005000,
            ComboStatus = PlayComboflagID.AllPerfectPlus,
            SyncStatus = PlaySyncflagID.SyncHi,
            Deluxscore = 9876,
            ScoreRank = MusicClearrankID.Rank_A,
            ExtNum1 = 42,
        };

        var result = source.ToVO((MusicDifficultyID)difficulty);

        Assert.Equal(source.Id, result.MusicId);
        Assert.Equal((MusicDifficultyID)difficulty, result.Level);
        Assert.Equal(source.PlayCount, result.PlayCount);
        Assert.Equal(source.Achievement, result.Achievement);
        Assert.Equal(source.ComboStatus, result.ComboStatus);
        Assert.Equal(source.SyncStatus, result.SyncStatus);
        Assert.Equal(source.Deluxscore, result.DeluxscoreMax);
        Assert.Equal(source.ScoreRank, result.ScoreRank);
        Assert.Equal(source.ExtNum1, result.ExtNum1);
    }

    [Fact]
    public void FriendSeasonRanking_PreservesEveryFieldAndDuplicateEntries()
    {
        var source = new UserFriendSeasonRanking(7, 12345, 2, true, "player", "2026/10/23");
        List<UserFriendSeasonRanking> entries = [source, source];

        var result = entries.ToVOArray();

        Assert.Equal(2, result.Length);
        Assert.All(result, item =>
        {
            Assert.Equal(source.SeasonId, item.SeasonId);
            Assert.Equal(source.Point, item.Point);
            Assert.Equal(source.Rank, item.Rank);
            Assert.Equal(source.RewardGet, item.RewardGet);
            Assert.Equal(source.UserName, item.UserName);
            Assert.Equal(source.RecordDate, item.RecordDate);
        });
    }

    [Fact]
    public void Intimate_MapsClosenessToIntimateLevelAndPreservesOrder()
    {
        List<UserPartnerCloseness> source =
        [
            new() { PartnerId = 9, Closeness = 123, IntimateCountRewarded = 45 },
            new() { PartnerId = 1, Closeness = 456, IntimateCountRewarded = 67 },
        ];

        var result = source.ToVOArray();

        Assert.Equal(new[] { 9, 1 }, result.Select(item => item.PartnerId));
        Assert.Equal(new[] { 123, 456 }, result.Select(item => item.IntimateLevel));
        Assert.Equal(new[] { 45, 67 }, result.Select(item => item.IntimateCountRewarded));
    }

    [Fact]
    public void LoginBonus_MapsIdAndIgnoresDisplayName()
    {
        var source = new global::UserLoginBonus(17, uint.MaxValue, true, false) { Name = "display" };

        var result = source.ToVO();

        Assert.Equal(source.ID, result.BonusId);
        Assert.Equal(source.Point, result.Point);
        Assert.Equal(source.IsCurrent, result.IsCurrent);
        Assert.Equal(source.IsComplete, result.IsComplete);
    }

    [Fact]
    public void CollectionMappers_EmptyModelsProduceEmptyArrays()
    {
        Assert.Empty(new List<UserFriendSeasonRanking>().ToVOArray());
        Assert.Empty(new List<UserPartnerCloseness>().ToVOArray());
        Assert.Empty(new List<global::UserLoginBonus>().ToVOArray());
    }
}
