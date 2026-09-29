namespace Net.VO.Mai2;

public static class UserPreviewResponseVOExtensions
{
    extension(UserPreviewResponseVO preview)
    {
        internal bool IsNewUser() =>
            string.IsNullOrEmpty(preview.UserName) || string.IsNullOrEmpty(preview.LastPlayDate);

        internal bool IsInheritUser() => preview.IsInherit;
    }
}
