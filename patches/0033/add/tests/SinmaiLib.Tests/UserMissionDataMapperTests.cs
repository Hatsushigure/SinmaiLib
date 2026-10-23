using Net.VO.Mapper;
using Xunit;
using ModelMission = Manager.UserDatas.UserMissionData;

namespace SinmaiLib.Tests;

public sealed class UserMissionDataMapperTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToVO_PreservesEveryWireFieldRegardlessOfIsActive(bool isActive)
    {
        var source = new ModelMission
        {
            IsActive = isActive,
            Type = 11,
            Difficulty = 22,
            TargetGenreId = 33,
            TargetGenreTableId = 44,
            ConditionGenreId = 55,
            ConditionGenreTableId = 66,
            ClearFlag = true,
        };

        var result = source.ToVO();

        Assert.Equal(11, result.Type);
        Assert.Equal(22, result.Difficulty);
        Assert.Equal(33, result.TargetGenreId);
        Assert.Equal(44, result.TargetGenreTableId);
        Assert.Equal(55, result.ConditionGenreId);
        Assert.Equal(66, result.ConditionGenreTableId);
        Assert.True(result.ClearFlag);
    }

    [Fact]
    public void ToVOArray_PreservesDictionaryEnumerationOrderAndDuplicateMissions()
    {
        var first = new ModelMission { Type = 1, Difficulty = 2 };
        var second = new ModelMission { Type = 3, Difficulty = 4, IsActive = true };
        Dictionary<int, List<ModelMission>> source = new()
        {
            [90] = [first, second],
            [10] = [],
            [30] = [first],
        };

        var result = source.ToVOArray();

        Assert.Equal(new[] { 1, 3, 1 }, result.Select(mission => mission.Type));
        Assert.Equal(new[] { 2, 4, 2 }, result.Select(mission => mission.Difficulty));
    }

    [Fact]
    public void ToVOArray_EmptyDictionaryProducesEmptyArray()
    {
        Dictionary<int, List<ModelMission>> source = [];

        Assert.Empty(source.ToVOArray());
    }
}
