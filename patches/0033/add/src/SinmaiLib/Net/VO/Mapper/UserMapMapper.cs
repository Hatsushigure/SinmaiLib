using System.Collections.Generic;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserMapMapper
{
    [MapperIgnoreSource(nameof(UserMapData.CharaColorKey))]
    [MapperIgnoreSource(nameof(UserMapData.MapColor))]
    [MapperIgnoreSource(nameof(UserMapData.Name))]
    [MapperIgnoreSource(nameof(UserMapData.IsEvent))]
    [MapperIgnoreSource(nameof(UserMapData.IsInfinity))]
    [MapperIgnoreSource(nameof(UserMapData.ReleaseIds))]
    [MapProperty(nameof(UserMapData.Id), nameof(UserMap.MapId))]
    [MapProperty(
        nameof(UserMapData.IsFinishedOpening),
        nameof(UserMap.UnlockFlag),
        Use = nameof(ToUnlockFlag)
    )]
    public static partial UserMap ToVO(this UserMapData source);

    public static partial UserMap[] ToVOArray(this List<UserMapData> source);

    [UserMapping(Default = false)]
    private static int ToUnlockFlag(bool isFinishedOpening) => isFinishedOpening ? 0 : 1;
}
