using System.Collections.Generic;
using Manager.UserDatas;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserIntimateMapper
{
    [MapProperty(nameof(UserPartnerCloseness.Closeness), nameof(UserIntimate.IntimateLevel))]
    public static partial UserIntimate ToVO(this UserPartnerCloseness source);

    public static partial UserIntimate[] ToVOArray(this List<UserPartnerCloseness> source);
}
