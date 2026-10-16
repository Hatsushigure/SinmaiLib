using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserShopStockMapper
{
    public static partial UserShopStock ToVO(this Manager.UserDatas.UserShopStock source);

    public static partial Manager.UserDatas.UserShopStock ToModel(this UserShopStock source);
}
