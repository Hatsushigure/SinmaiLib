using System.Collections.Generic;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserGetPointMapper
{
    public static partial UserGetPoint ToVO(this Manager.UserDatas.UserGetPoint source);

    public static partial UserGetPoint[] ToVOArray(
        this List<Manager.UserDatas.UserGetPoint> source
    );
}