using System.Text.Json;
using System.Text.Json.Nodes;
using DB;
using Net.VO;
using Net.VO.Mapper;
using Xunit;
using Model = Manager.UserDatas.UserExtend;

namespace NetHttpClient.Tests;

public sealed class UserExtendMapperTests
{
    public static IEnumerable<object[]> ContentBitmaps =>
        Enumerable.Range(0, 1 << (int)ExtendContentBitID.End)
            .Select(bitmap => new object[] { (ulong)bitmap });

    [Theory]
    [MemberData(nameof(ContentBitmaps))]
    public void WireToModelAndBack_PreservesSettingsListsAndEveryContentBit(ulong bitmap)
    {
        var json = $$"""
            {"selectMusicId":12345,"selectDifficultyId":4,"categoryIndex":10,
             "musicIndex":17,"extraFlag":3,"selectScoreType":1,"extendContentBit":{{bitmap}},
             "isPhotoAgree":false,"isGotoCodeRead":false,"selectResultDetails":true,
             "selectResultScoreViewType":2,"sortCategorySetting":3,"sortMusicSetting":8,
             "playStatusSetting":5,"selectedCardList":[6,4,2],
             "encountMapNpcList":[{"npcId":42,"musicId":123},{"npcId":51,"musicId":456}]}
            """;
        var downloaded = JsonSerializer.Deserialize(json, JsonContext.Default.UserExtend)!;
        var model = downloaded.ToModel();

        Assert.Equal(12345, model.SelectMusicId);
        Assert.Equal(4, model.SelectDifficultyId);
        Assert.Equal(10, model.CategoryIndex);
        Assert.Equal(17, model.MusicIndex);
        Assert.Equal(3, model.ExtraFlag);
        Assert.Equal(1, model.SelectScoreType);
        Assert.True(model.SelectResultDetails);
        Assert.Equal(2, model.SelectResultScoreViewType);
        Assert.Equal(SortTabID.Version, model.SortCategorySetting);
        Assert.Equal(SortMusicID.DxScore, model.SortMusicSetting);
        Assert.Equal(PlaystatusTabID.DxScore, model.PlayStatusSetting);
        Assert.Equal(new[] { 6, 4, 2 }, model.SelectedCardList);
        Assert.Equal(new[] { (42, 123), (51, 456) },
            model.EncountMapNpcList.Select(npc => (npc.NpcId, npc.MusicId)));
        for (var bit = ExtendContentBitID.Begin; bit < ExtendContentBitID.End; bit++)
            Assert.Equal((bitmap & (1UL << (int)bit)) != 0, model.ExtendContendBit.IsFlagOn(bit));

        var uploaded = JsonSerializer.Serialize(model.ToVO(), JsonContext.Default.UserExtend);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(uploaded)));
    }

    [Fact]
    public void Mapping_CopiesMutableListsAndBitsToKeepSnapshotsIndependent()
    {
        var original = new Model();
        original.SelectedCardList.Add(4);
        original.AddEncountMapNpc(42, 123);
        original.ExtendContendBit.SetFlag(ExtendContentBitID.CharacterSortDescending, true);
        var snapshot = original.ToVO();
        var model = snapshot.ToModel();

        model.SelectedCardList[0] = 6;
        model.EncountMapNpcList[0].MusicId = 456;
        model.ExtendContendBit.Clear();
        var updated = model.ToVO();
        original.SelectedCardList.Clear();
        original.EncountMapNpcList[0].MusicId = 789;
        original.ExtendContendBit.Clear();

        Assert.Equal(new[] { 4 }, snapshot.SelectedCardList);
        Assert.Equal(123, snapshot.EncountMapNpcList[0].MusicId);
        Assert.Equal(16UL, snapshot.ExtendContentBit);
        Assert.Equal(new[] { 6 }, updated.SelectedCardList);
        Assert.Equal(456, updated.EncountMapNpcList[0].MusicId);
        Assert.Equal(0UL, updated.ExtendContentBit);
    }

    [Fact]
    public void WireToModel_IgnoresLegacyBooleansAndUnknownContentBits()
    {
        var downloaded = new Model().ToVO() with
        {
            IsPhotoAgree = true,
            IsGotoCodeRead = true,
            ExtendContentBit = 1UL << 63,
        };

        var uploaded = downloaded.ToModel().ToVO();

        Assert.Equal(0UL, uploaded.ExtendContentBit);
        Assert.False(uploaded.IsPhotoAgree);
        Assert.False(uploaded.IsGotoCodeRead);
        Assert.Empty(uploaded.SelectedCardList);
        Assert.Empty(uploaded.EncountMapNpcList);
    }

    [Fact]
    public void Initialize_RestoresConstructorDefaultsAfterPopulatedMapping()
    {
        var defaults = JsonSerializer.Serialize(new Model().ToVO(), JsonContext.Default.UserExtend);
        var source = new Model().ToVO() with
        {
            SelectMusicId = 12345,
            SelectDifficultyId = 4,
            CategoryIndex = 10,
            MusicIndex = 17,
            ExtraFlag = 3,
            SelectScoreType = 0,
            ExtendContentBit = 31,
            SelectResultDetails = true,
            SelectResultScoreViewType = 2,
            SortCategorySetting = SortTabID.Version,
            SortMusicSetting = SortMusicID.DxScore,
            PlayStatusSetting = PlaystatusTabID.DxScore,
            SelectedCardList = [4],
            EncountMapNpcList = [new() { NpcId = 42, MusicId = 123 }],
        };
        var model = source.ToModel();

        model.Initialize();

        Assert.Equal(1, model.CategoryIndex);
        Assert.Equal(1, model.SelectScoreType);
        Assert.Equal(defaults, JsonSerializer.Serialize(model.ToVO(), JsonContext.Default.UserExtend));
    }
}
