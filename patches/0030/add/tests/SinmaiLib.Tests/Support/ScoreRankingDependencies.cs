namespace Manager;

internal sealed class ScoreRankingManager : MAI2.Util.Singleton<ScoreRankingManager>
{
    public Dictionary<int, ScoreRankingForMusicSeq> ScoreRankings { get; } = [];
}

internal sealed class ScoreRankingForMusicSeq;
