using System.Collections.Generic;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserFriendSeasonRankingMapper
{
    public static partial UserFriendSeasonRanking ToVO(
        this Manager.UserDatas.UserFriendSeasonRanking source
    );

    public static partial UserFriendSeasonRanking[] ToVOArray(
        this List<Manager.UserDatas.UserFriendSeasonRanking> source
    );
}
