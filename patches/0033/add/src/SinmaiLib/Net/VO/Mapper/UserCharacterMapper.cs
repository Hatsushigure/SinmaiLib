using System.Collections.Generic;
using Manager.UserDatas;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserCharacterMapper
{
    [MapperIgnoreSource(nameof(UserChara.DisplayLevel))]
    [MapperIgnoreSource(nameof(UserChara.NextAwake))]
    [MapperIgnoreSource(nameof(UserChara.NextAwakePercent))]
    [MapperIgnoreSource(nameof(UserChara.Reincarnation))]
    [MapProperty(nameof(UserChara.Count), nameof(UserCharacter.UseCount))]
    [MapProperty(nameof(UserChara.Id), nameof(UserCharacter.CharacterId))]
    public static partial UserCharacter ToVO(this UserChara source);

    public static partial UserCharacter[] ToVOArray(this List<UserChara> source);
}
