namespace MAI2.Util
{
    internal static class SingletonStateMachine<TClass, TState> where TClass : new()
    {
        public static TClass Instance { get; } = new();
    }
}

namespace Manager
{
    internal sealed class AmManager
    {
        public enum EState { }

        public BackupData Backup { get; } = new();
    }

    internal sealed class BackupData
    {
        public BackupNetworkInformationRecord networkInformation { get; } = new();
    }

    internal sealed class BackupNetworkInformationRecord
    {
        public enum ErrorKind
        {
            Packet_Retry, UserDataDownloadError, E07, E08, E09, E14,
            PacketStatus_ErrorTimeout, PacketStatus_ErrorConnection, PacketStatus_ErrorInternal,
        }

        public enum Error2Kind
        {
            E00, E01, E02, E03, E04, E05, E06, E07,
            E08, E09, E10, E11, E12, E13, E14, E15,
        }

        private readonly Dictionary<ErrorKind, int> _errors = [];
        private readonly Dictionary<Error2Kind, int> _errors2 = [];

        public int GetErrorNum(ErrorKind kind) => _errors.GetValueOrDefault(kind);

        public int GetError2Num(Error2Kind kind) => _errors2.GetValueOrDefault(kind);

        public void AddErrorNum(ErrorKind kind) => _errors[kind] = GetErrorNum(kind) + 1;

        public void AddError2Num(Error2Kind kind) => _errors2[kind] = GetError2Num(kind) + 1;

        public void Clear()
        {
            _errors.Clear();
            _errors2.Clear();
        }
    }

    internal static class GameManager
    {
        public static int PingMsec => 10;
    }

    internal sealed class UserDataManager
    {
        public TestUserData UserData { get; } = new();

        public TestUserData GetUserDataByID(ulong userId) => UserData;
    }

    internal sealed class TestUserData
    {
        public TestNetDelayLog NetDelayLog { get; } = new();
    }

    internal sealed class TestNetDelayLog
    {
        public int DlRequests;
        public int DlRetry;
        public TestRequestLog[] Request { get; } = Enumerable.Range(0, (int)Net.VO.Mai2.ApiKind.ENUM_MAX)
            .Select(_ => new TestRequestLog()).ToArray();
    }

    internal sealed class TestRequestLog
    {
        public int Count;
        public int Size;
        public int Msec;
        public int Retry;
    }
}
