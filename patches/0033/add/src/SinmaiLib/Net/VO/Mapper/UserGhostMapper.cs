using System.Collections.Generic;
using MAI2System;
using Manager;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace SinmaiLib.Net.VO.Mapper;

[Mapper(IgnoreObsoleteMembersStrategy = IgnoreObsoleteMembersStrategy.Target)]
internal static partial class UserGhostMapper
{
    [MapValue(nameof(UserGhost.Version), Use = nameof(GetCurrentVersion))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.Id))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.IsUsed))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.NeedSave))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.Rank))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.Type))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserGhost.Version))]
    [MapProperty(
        nameof(Manager.UserDatas.UserGhost.UnixTime),
        nameof(UserGhost.PlayDatetime),
        Use = nameof(GetTimeString)
    )]
    public static partial UserGhost ToVO(this Manager.UserDatas.UserGhost source);

    private static int GetCurrentVersion() =>
        (int)SystemConfig.Instance.config.dataVersionInfo.versionNo.versionCode;

    [UserMapping(Default = false)]
    private static string GetTimeString(long timestamp) => TimeManager.GetDateString(timestamp);

    [UserMapping(Default = true)]
    private static List<byte> CloneByteList(List<byte> source) => [.. source];
}
