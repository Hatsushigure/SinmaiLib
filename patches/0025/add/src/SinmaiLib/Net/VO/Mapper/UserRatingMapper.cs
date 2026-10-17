using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
[UseStaticMapper(typeof(UserRateMapper))]
[UseStaticMapper(typeof(UserUdemaeMapper))]
internal static partial class UserRatingMapper
{
    public static partial UserRating ToVO(this Manager.UserDatas.UserRating source);

    public static partial Manager.UserDatas.UserRating ToModel(this UserRating source);
}
