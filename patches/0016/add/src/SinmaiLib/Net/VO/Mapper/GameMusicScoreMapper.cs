using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;
using MusicScore = Manager.MusicScore;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class GameMusicScoreMapper
{
    [MapProperty(nameof(MusicScore.MusicID), nameof(GameMusicScoreRequestVO.MusicId))]
    [MapperIgnoreSource(nameof(MusicScore.EventID))]
    [MapperIgnoreSource(nameof(MusicScore.IsDownloaded))]
    [MapperIgnoreSource(nameof(MusicScore.ScoreData))]
    public static partial GameMusicScoreRequestVO ToRequestVO(MusicScore source);
}