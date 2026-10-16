using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class MapEncountNpcMapper
{
    public static partial MapEncountNpc ToVO(this Manager.UserDatas.MapEncountNpc source);
    
    public static partial Manager.UserDatas.MapEncountNpc ToModel(this MapEncountNpc source);
}
