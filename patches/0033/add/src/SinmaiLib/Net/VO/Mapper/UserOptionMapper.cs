using DB;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper]
internal static partial class UserOptionMapper
{
    [MapValue(nameof(UserOption.DispBar), OptionDispbarlineID.Begin)]
    [MapValue(nameof(UserOption.SortMusic), SortMusicID.Begin)]
    [MapValue(nameof(UserOption.SortTab), SortTabID.Begin)]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserOption.BarDisp))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserOption.GetNoteSpeed))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserOption.GetTouchSpeed))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserOption.GetSlideSpeed))]
    public static partial UserOption ToVO(this Manager.UserDatas.UserOption source);
}
