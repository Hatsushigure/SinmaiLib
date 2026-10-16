using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
[UseStaticMapper(typeof(MapEncountNpcMapper))]
internal static partial class UserExtendMapper
{
    [MapProperty(
        nameof(Manager.UserDatas.UserExtend.ExtendContendBit),
        nameof(UserExtend.ExtendContentBit)
    )]
    [MapValue(nameof(UserExtend.IsGotoCodeRead), false)]
    [MapValue(nameof(UserExtend.IsPhotoAgree), false)]
    public static partial UserExtend ToVO(this Manager.UserDatas.UserExtend source);

    [MapProperty(
        nameof(UserExtend.ExtendContentBit),
        nameof(Manager.UserDatas.UserExtend.ExtendContendBit)
    )]
    [MapperIgnoreSource(nameof(UserExtend.IsPhotoAgree))]
    [MapperIgnoreSource(nameof(UserExtend.IsGotoCodeRead))]
    public static partial Manager.UserDatas.UserExtend ToModel(this UserExtend source);

    extension(Manager.UserDatas.UserExtendContentBit self)
    {
        private static Manager.UserDatas.UserExtendContentBit FromBitmap(ulong source)
        {
            var userExtendContentBit = new Manager.UserDatas.UserExtendContentBit();
            for (
                var currentBit = DB.ExtendContentBitID.Begin;
                currentBit < DB.ExtendContentBitID.End;
                currentBit++
            )
            {
                var mask = 1UL << (int)currentBit;
                if ((source & mask) != 0)
                {
                    userExtendContentBit.SetFlag(currentBit, flag: true);
                }
            }
            return userExtendContentBit;
        }

        private ulong ToBitmap()
        {
            var result = 0UL;
            for (
                var currentBit = DB.ExtendContentBitID.PhotoAgree;
                currentBit < DB.ExtendContentBitID.End;
                currentBit++
            )
            {
                if (self.IsFlagOn(currentBit))
                {
                    result |= 1UL << (int)currentBit;
                }
            }
            return result;
        }
    }
}
