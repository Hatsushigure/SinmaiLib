using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
[UseStaticMapper(typeof(UserActMapper))]
internal static partial class UserActivityMapper
{
    public static partial UserActivity ToVO(this Manager.UserDatas.UserActivity source);

    public static partial Manager.UserDatas.UserActivity ToModel(this UserActivity source);
}
