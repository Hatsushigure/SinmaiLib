using System.Collections.Generic;
using System.Linq;
using Net.VO.Mai2;
using Riok.Mapperly.Abstractions;

namespace Net.VO.Mapper;

[Mapper]
internal static partial class UserItemMapper
{
    public static partial UserItem ToVO(this Manager.UserDatas.UserItem source, ItemKind itemKind);

    [MapperIgnoreSource(nameof(UserItem.ItemKind))]
    public static partial Manager.UserDatas.UserItem ToModel(this UserItem source);

    public static partial List<Manager.UserDatas.UserItem> ToModelList(this UserItem[] source);

    public static List<int> ToIdList(this UserItem[] source) => [.. source.Select(x => x.ItemId)];
}
