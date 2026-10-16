using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserFriendBonusMapper
{
    [MapperIgnoreSource(nameof(UserFriendBonusResponseVO.UserId))]
    public static partial UserFriendBonus FromResponseVO(UserFriendBonusResponseVO source);
}
