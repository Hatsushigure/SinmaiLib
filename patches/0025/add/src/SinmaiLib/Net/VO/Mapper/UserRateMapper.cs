using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserRateMapper
{
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserRate.OldFlag))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserRate.ScoreRate))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserRate.SingleRate))]
    public static partial UserRate ToVO(this Manager.UserDatas.UserRate source);

    public static Manager.UserDatas.UserRate ToModel(this UserRate source)
        => new(source.MusicId, source.Level, source.Achievement, source.RomVersion);
}