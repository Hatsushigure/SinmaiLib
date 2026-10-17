using System;
using AMDaemon.Allnet;
using DB;
using MAI2System;
using Manager;
using Manager.UserDatas;
using Riok.Mapperly.Abstractions;
using UserDetail = Net.VO.Mai2.UserDetail;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserDetailMapper
{
    [MapperIgnoreObsoleteMembers(IgnoreObsoleteMembersStrategy.Target)]
    [MapValue(nameof(Manager.UserDatas.UserDetail.LastPlayCredit), 0)]
    [MapValue(nameof(Manager.UserDatas.UserDetail.LastPlayMode), 0)]
    [MapProperty(nameof(UserDetail.IconId), nameof(Manager.UserDatas.UserDetail.EquipIconID))]
    [MapProperty(nameof(UserDetail.PlateId), nameof(Manager.UserDatas.UserDetail.EquipPlateID))]
    [MapProperty(nameof(UserDetail.TitleId), nameof(Manager.UserDatas.UserDetail.EquipTitleID))]
    [MapProperty(nameof(UserDetail.PartnerId), nameof(Manager.UserDatas.UserDetail.EquipPartnerID))]
    [MapProperty(nameof(UserDetail.FrameId), nameof(Manager.UserDatas.UserDetail.EquipFrameID))]
    [MapProperty(nameof(UserDetail.PlayerRating), nameof(Manager.UserDatas.UserDetail.Rating))]
    [MapProperty(
        nameof(UserDetail.PlayerOldRating),
        nameof(Manager.UserDatas.UserDetail.OldRating)
    )]
    [MapProperty(
        nameof(UserDetail.PlayerNewRating),
        nameof(Manager.UserDatas.UserDetail.NewRating)
    )]
    [MapperIgnoreSource(nameof(UserDetail.TotalBasicDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalAdvancedDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalExpertDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalMasterDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalReMasterDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalBasicSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalAdvancedSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalExpertSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalMasterSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalReMasterSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalBasicAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.TotalAdvancedAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.TotalExpertAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.TotalMasterAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.TotalReMasterAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.LastPlayCredit))]
    [MapperIgnoreSource(nameof(UserDetail.LastPlayMode))]
    [MapperIgnoreSource(nameof(UserDetail.TotalDeluxscore))]
    [MapperIgnoreSource(nameof(UserDetail.TotalSync))]
    [MapperIgnoreSource(nameof(UserDetail.TotalAchievement))]
    [MapperIgnoreSource(nameof(UserDetail.DateTime))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalDeluxscore))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalSync))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalAchievement))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalDeluxScores))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalSyncs))]
    [MapperIgnoreTarget(nameof(Manager.UserDatas.UserDetail.TotalAchieves))]
    public static partial Manager.UserDatas.UserDetail ToModel(this UserDetail src, ulong userId);

    [MapperIgnoreObsoleteMembers(IgnoreObsoleteMembersStrategy.Source)]
    [MapValue(nameof(UserDetail.LastAllNetId), 0)]
    [MapValue(nameof(UserDetail.LastGameId), Use = nameof(GetGameId))]
    [MapValue(nameof(UserDetail.LastRomVersion), Use = nameof(GetRomVersion))]
    [MapValue(nameof(UserDetail.LastDataVersion), Use = nameof(GetDataVersion))]
    [MapValue(nameof(UserDetail.LastPlayDate), Use = nameof(GetNowDateString))]
    [MapValue(nameof(UserDetail.LastPlaceId), Use = nameof(GetPlaceId))]
    [MapValue(nameof(UserDetail.LastPlaceName), Use = nameof(GetPlaceName))]
    [MapValue(nameof(UserDetail.LastRegionId), Use = nameof(GetRegionId))]
    [MapValue(nameof(UserDetail.LastRegionName), Use = nameof(GetRegionName))]
    [MapValue(nameof(UserDetail.LastClientId), Use = nameof(GetClientId))]
    [MapValue(nameof(UserDetail.LastCountryCode), Use = nameof(GetCountryCode))]
    [MapValue(nameof(UserDetail.CompatibleCmVersion), Use = nameof(GetCardMakerVersion))]
    [MapValue(nameof(UserDetail.DateTime), Use = nameof(GetAuthTimestamp))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.EquipIconID), nameof(UserDetail.IconId))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.EquipPlateID), nameof(UserDetail.PlateId))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.EquipTitleID), nameof(UserDetail.TitleId))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.EquipPartnerID), nameof(UserDetail.PartnerId))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.EquipFrameID), nameof(UserDetail.FrameId))]
    [MapProperty(nameof(Manager.UserDatas.UserDetail.Rating), nameof(UserDetail.PlayerRating))]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.OldRating),
        nameof(UserDetail.PlayerOldRating)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.NewRating),
        nameof(UserDetail.PlayerNewRating)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.FirstPlayDate),
        nameof(UserDetail.FirstPlayDate),
        Use = nameof(GetFirstPlayDate)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.FirstGameId),
        nameof(UserDetail.FirstGameId),
        Use = nameof(GetFirstGameId)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.FirstRomVersion),
        nameof(UserDetail.FirstRomVersion),
        Use = nameof(GetFirstRomVersion)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.FirstDataVersion),
        nameof(UserDetail.FirstDataVersion),
        Use = nameof(GetFirstDataVersion)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.DailyBonusDate),
        nameof(UserDetail.DailyBonusDate),
        Use = nameof(GetValueOrToday)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.DailyCourseBonusDate),
        nameof(UserDetail.DailyCourseBonusDate),
        Use = nameof(GetValueOrToday)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.LastPairLoginDate),
        nameof(UserDetail.LastPairLoginDate),
        Use = nameof(GetValueOrToday)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.LastTrialPlayDate),
        nameof(UserDetail.LastTrialPlayDate),
        Use = nameof(GetValueOrToday)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalDeluxScores),
        nameof(UserDetail.TotalBasicDeluxscore),
        Use = nameof(GetTotalBasicDeluxscore)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalDeluxScores),
        nameof(UserDetail.TotalAdvancedDeluxscore),
        Use = nameof(GetTotalAdvancedDeluxscore)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalDeluxScores),
        nameof(UserDetail.TotalExpertDeluxscore),
        Use = nameof(GetTotalExpertDeluxscore)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalDeluxScores),
        nameof(UserDetail.TotalMasterDeluxscore),
        Use = nameof(GetTotalMasterDeluxscore)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalDeluxScores),
        nameof(UserDetail.TotalReMasterDeluxscore),
        Use = nameof(GetTotalReMasterDeluxscore)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalSyncs),
        nameof(UserDetail.TotalBasicSync),
        Use = nameof(GetTotalBasicSync)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalSyncs),
        nameof(UserDetail.TotalAdvancedSync),
        Use = nameof(GetTotalAdvancedSync)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalSyncs),
        nameof(UserDetail.TotalExpertSync),
        Use = nameof(GetTotalExpertSync)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalSyncs),
        nameof(UserDetail.TotalMasterSync),
        Use = nameof(GetTotalMasterSync)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalSyncs),
        nameof(UserDetail.TotalReMasterSync),
        Use = nameof(GetTotalReMasterSync)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalAchieves),
        nameof(UserDetail.TotalBasicAchievement),
        Use = nameof(GetTotalBasicAchievement)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalAchieves),
        nameof(UserDetail.TotalAdvancedAchievement),
        Use = nameof(GetTotalAdvancedAchievement)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalAchieves),
        nameof(UserDetail.TotalExpertAchievement),
        Use = nameof(GetTotalExpertAchievement)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalAchieves),
        nameof(UserDetail.TotalMasterAchievement),
        Use = nameof(GetTotalMasterAchievement)
    )]
    [MapProperty(
        nameof(Manager.UserDatas.UserDetail.TotalAchieves),
        nameof(UserDetail.TotalReMasterAchievement),
        Use = nameof(GetTotalReMasterAchievement)
    )]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastGameId))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastRomVersion))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastDataVersion))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastPlayDate))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastAllNetId))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastPlaceId))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastPlaceName))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastRegionId))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastRegionName))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastClientId))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.LastCountryCode))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.CompatibleCmVersion))]
    [MapperIgnoreSource(nameof(Manager.UserDatas.UserDetail.UserId))]
    public static partial UserDetail ToVO(this Manager.UserDatas.UserDetail source);

    extension(UserContentBit)
    {
        private static UserContentBit FromBitmap(ulong source)
        {
            var userContentBit = new UserContentBit();
            for (
                var currentBit = (int)ContentBitID.FirstPlay;
                currentBit < (int)ContentBitID.End;
                currentBit++
            )
            {
                var mask = 1UL << currentBit;
                if ((source & mask) != 0L)
                {
                    userContentBit.SetFlag((ContentBitID)currentBit, true);
                }
            }

            return userContentBit;
        }

        private static ulong ToBitmap(UserContentBit src)
        {
            var result = 0UL;
            for (
                var currentBit = (int)ContentBitID.FirstPlay;
                currentBit < (int)ContentBitID.End;
                currentBit++
            )
            {
                if (src.IsFlagOn((ContentBitID)currentBit))
                {
                    result |= 1UL << currentBit;
                }
            }
            return result;
        }
    }

    private static int[] MapIntArray(int[] source) => (int[])source.Clone();

    private static string GetGameId() => AMDaemon.System.GameId;

    private static string GetRomVersion() =>
        SystemConfig.Instance.config.romVersionInfo.versionNo.versionString;

    private static string GetDataVersion() =>
        SystemConfig.Instance.config.dataVersionInfo.versionNo.versionString;

    private static string GetNowDateString() => TimeManager.GetNowDateString();

    private static int GetPlaceId() => (int)Auth.LocationId;

    private static string GetPlaceName() => Auth.LocationName;

    private static int GetRegionId() => Auth.RegionCode;

    private static string GetRegionName() => Auth.RegionNames[0];

    private static string GetClientId() => AMDaemon.System.KeychipId.ShortValue;

    private static string GetCountryCode() => Auth.CountryCode;

    private static string GetCardMakerVersion() =>
        SystemConfig.Instance.config.cardMakerVersionInfo.versionNo.versionString;

    private static long GetAuthTimestamp() => new DateTimeOffset(Auth.AuthTime).ToUnixTimeSeconds();

    [UserMapping(Default = false)]
    private static string GetFirstPlayDate(string original) =>
        string.IsNullOrEmpty(original)
            ? TimeManager.GetDateString(TimeManager.PlayBaseTime)
            : original;

    [UserMapping(Default = false)]
    private static string GetFirstGameId(string original) =>
        string.IsNullOrEmpty(original) ? GetGameId() : original;

    [UserMapping(Default = false)]
    private static string GetFirstRomVersion(string original) =>
        string.IsNullOrEmpty(original) ? GetRomVersion() : original;

    [UserMapping(Default = false)]
    private static string GetFirstDataVersion(string original) =>
        string.IsNullOrEmpty(original) ? GetDataVersion() : original;

    [UserMapping(Default = false)]
    private static string GetValueOrToday(string original) =>
        string.IsNullOrEmpty(original) ? TimeManager.GetDateString(0) : original;

    [UserMapping(Default = false)]
    private static long GetTotalBasicDeluxscore(long[] source) => source[0];

    [UserMapping(Default = false)]
    private static long GetTotalAdvancedDeluxscore(long[] source) => source[1];

    [UserMapping(Default = false)]
    private static long GetTotalExpertDeluxscore(long[] source) => source[2];

    [UserMapping(Default = false)]
    private static long GetTotalMasterDeluxscore(long[] source) => source[3];

    [UserMapping(Default = false)]
    private static long GetTotalReMasterDeluxscore(long[] source) => source[4];

    [UserMapping(Default = false)]
    private static int GetTotalBasicSync(int[] source) => source[0];

    [UserMapping(Default = false)]
    private static int GetTotalAdvancedSync(int[] source) => source[1];

    [UserMapping(Default = false)]
    private static int GetTotalExpertSync(int[] source) => source[2];

    [UserMapping(Default = false)]
    private static int GetTotalMasterSync(int[] source) => source[3];

    [UserMapping(Default = false)]
    private static int GetTotalReMasterSync(int[] source) => source[4];

    [UserMapping(Default = false)]
    private static long GetTotalBasicAchievement(long[] source) => source[0];

    [UserMapping(Default = false)]
    private static long GetTotalAdvancedAchievement(long[] source) => source[1];

    [UserMapping(Default = false)]
    private static long GetTotalExpertAchievement(long[] source) => source[2];

    [UserMapping(Default = false)]
    private static long GetTotalMasterAchievement(long[] source) => source[3];

    [UserMapping(Default = false)]
    private static long GetTotalReMasterAchievement(long[] source) => source[4];
}
