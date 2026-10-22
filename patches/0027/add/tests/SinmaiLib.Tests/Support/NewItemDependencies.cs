// Runtime-only dependencies of PacketGetUserNewItem. Packet tests use the
// overload accepting playlogs directly, so game-side playlog export is excluded.
namespace Manager
{
    internal sealed class UserData
    {
        public TestUserDetail Detail => throw new NotSupportedException();

        public Net.VO.Mai2.UserPlaylog ExportUserPlaylog(int index, int trackNo) =>
            throw new NotSupportedException();
    }

    internal sealed class TestUserDetail
    {
        public ulong UserId { get; set; }
    }
}

namespace MAI2System
{
    internal sealed class SystemConfig
    {
        public TestSystemConfig config { get; } = new();
    }

    internal sealed class TestSystemConfig
    {
        public TestRomVersion romVersionInfo { get; } = new();
    }

    internal sealed class TestRomVersion
    {
        public TestVersionNo versionNo { get; } = new();
    }

    internal sealed class TestVersionNo
    {
        public uint versionCode => 15600;
    }
}
