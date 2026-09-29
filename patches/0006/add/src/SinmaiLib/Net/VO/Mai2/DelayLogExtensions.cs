using System.Linq;

namespace Net.VO.Mai2;

internal static class DelayLogExtensions
{
    extension(DelayLog self)
    {
        public static DelayLog Default =>
            new()
            {
                DlRequests = 0,
                DlSize = 0,
                DlRetry = 0,
                LoginMsec = 0,
                SaveMsec = 0,
                ReductionMusic = 0,
                ReductionItem = 0,
                Request = [.. Enumerable.Repeat(0, 26).Select(_ => RequestAPI.Default)],
            };

        public void Clear()
        {
            var defaults = DelayLog.Default;
            self.DlRequests = defaults.DlRequests;
            self.DlSize = defaults.DlSize;
            self.DlRetry = defaults.DlRetry;
            self.LoginMsec = defaults.LoginMsec;
            self.SaveMsec = defaults.SaveMsec;
            self.ReductionMusic = defaults.ReductionMusic;
            self.ReductionItem = defaults.ReductionItem;
            self.Request = defaults.Request;
        }
    }
}
