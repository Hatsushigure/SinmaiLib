using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper(IgnoreObsoleteMembersStrategy = IgnoreObsoleteMembersStrategy.Both)]
internal static partial class UserUdemaeMapper
{
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserUdemae.ClassID))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserUdemae.MaxClassID))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserUdemae.ClassPoint))]
    public static partial UserUdemae ToVO(this Manager.UserDatas.UserUdemae source);

    public static Manager.UserDatas.UserUdemae ToModel(this UserUdemae source)
    {
        var target = source.ToModelImpl();
        target.SetClassValue(source.ClassValue);
        target.SetMaxClassValue(source.MaxClassValue);
        return target;
    }

    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserUdemae.ClassID))] // Will be set later
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserUdemae.MaxClassID))] // Will be set later
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserUdemae.ClassPoint))] // Will be set later
    private static partial Manager.UserDatas.UserUdemae ToModelImpl(this UserUdemae source);
}
