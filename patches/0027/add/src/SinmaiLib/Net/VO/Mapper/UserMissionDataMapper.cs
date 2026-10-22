using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserMissionDataMapper
{
    [MapperIgnoreSource(nameof(UserMissionResponseVO.UserId))]
    public static partial UserMissionResponseData ToResponseDataVO(this UserMissionResponseVO source);
}
