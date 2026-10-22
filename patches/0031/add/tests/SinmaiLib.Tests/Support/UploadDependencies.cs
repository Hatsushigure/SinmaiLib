// Only game runtime metadata and photo export are substituted. Upload tests compile the real
// Packet, NetQuery, JSON context, VO types and HTTP client; they do not verify VOExtensions mapping.
namespace Manager
{
    internal static class TimeManager
    {
        public static string CurrentDate { get; set; } = "2026-10-21 12:00:00";

        public static string GetNowDateString() => CurrentDate;
    }

    internal sealed class NetDataManager
    {
        public static NetDataManager Instance { get; } = new();

        private readonly TestUploadLogin[] _logins = [new(), new()];

        public TestUploadLogin GetLoginVO(int index) => _logins[index];
    }

    internal sealed class TestUploadLogin
    {
        public ulong LoginId { get; set; } = 123456789012345;
    }
}

namespace Net.VO.Mai2
{
    internal static class UploadPhotoDependencies
    {
        extension(UserPhoto)
        {
            public static UserPhoto ExportPhotoData(ulong userId, ulong playlogId, int trackNo) =>
                new()
                {
                    OrderId = 0,
                    UserId = userId,
                    DivNumber = 0,
                    DivLength = 0,
                    DivData = "",
                    PlaceId = (int)AMDaemon.Allnet.Auth.LocationId,
                    ClientId = AMDaemon.System.KeychipId.ShortValue,
                    UploadDate = Manager.TimeManager.GetNowDateString(),
                    PlaylogId = playlogId,
                    TrackNo = trackNo,
                };
        }
    }
}

// PacketUploadUserPhoto's conditional diagnostic methods are compiled but never invoked.
namespace MAI2
{
    internal static class DateTime
    {
        public static global::System.DateTime Now => global::System.DateTime.Now;
    }
}

namespace MAI2System
{
    internal static class Path
    {
        public static string ErrorLogPath => global::System.IO.Path.GetTempPath();
    }
}
