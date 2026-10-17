using System.Collections.Generic;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserRivalMapper
{
    public static partial UserRivalData ToVO(this Manager.UserDatas.UserRival source);
    
    public static partial Manager.UserDatas.UserRival ToModel(this UserRivalData source);
    
    public static partial List< Manager.UserDatas.UserRival> ToModelList(this UserRivalData[] source);
}