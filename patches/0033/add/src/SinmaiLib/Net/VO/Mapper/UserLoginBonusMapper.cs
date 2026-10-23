using System.Collections.Generic;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserLoginBonusMapper
{
    [MapperIgnoreSource(nameof(UserLoginBonus.Name))]
    [MapProperty(nameof(UserLoginBonus.ID), nameof(global::Net.VO.Mai2.UserLoginBonus.BonusId))]
    public static partial global::Net.VO.Mai2.UserLoginBonus ToVO(this UserLoginBonus source);

    public static partial global::Net.VO.Mai2.UserLoginBonus[] ToVOArray(
        this List<UserLoginBonus> source
    );
}
