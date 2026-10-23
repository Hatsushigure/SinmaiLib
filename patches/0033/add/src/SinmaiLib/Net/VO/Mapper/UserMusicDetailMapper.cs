using Manager;
using Manager.UserDatas;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserMusicDetailMapper
{
    public static UserMusicDetail ToVO(this UserScore source, MusicDifficultyID difficulty) =>
        source.ToVOInternal() with
        {
            Level = difficulty,
        };

    [MapValue(nameof(UserMusicDetail.Level), MusicDifficultyID.Begin)]
    [MapProperty(nameof(UserScore.Deluxscore), nameof(UserMusicDetail.DeluxscoreMax))]
    [MapProperty(nameof(UserScore.Id), nameof(UserMusicDetail.MusicId))]
    private static partial UserMusicDetail ToVOInternal(this UserScore source);
}
