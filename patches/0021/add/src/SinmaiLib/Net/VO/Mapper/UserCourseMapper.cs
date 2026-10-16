using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserCourseMapper
{
    public static partial UserCourse ToVO(this Manager.UserDatas.UserCourse source);
    
    public static partial Manager.UserDatas.UserCourse ToModel(this UserCourse source);
}
