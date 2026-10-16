using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserActMapper
{
    public static partial UserAct ToVO(this Manager.UserDatas.UserAct source);

    public static partial Manager.UserDatas.UserAct ToModel(this UserAct source);
}
