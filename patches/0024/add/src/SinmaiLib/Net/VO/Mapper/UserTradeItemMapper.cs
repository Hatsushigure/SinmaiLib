using System.Collections.Generic;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserTradeItemMapper
{
    public static partial UserTradeItem ToVO(this Manager.UserDatas.UserTradeItem source);

    public static partial UserTradeItem[] ToVOArray(
        this List<Manager.UserDatas.UserTradeItem> source
    );
}
