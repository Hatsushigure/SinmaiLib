namespace Net.VO.Mai2;

internal static class RequestApiExtensions
{
    extension(RequestAPI self)
    {
        public static RequestAPI Default =>
            new()
            {
                Count = 0,
                Size = 0,
                Msec = 0,
                Retry = 0,
            };

        public void Clear()
        {
            var defaults = RequestAPI.Default;
            self.Count = defaults.Count;
            self.Size = defaults.Size;
            self.Msec = defaults.Msec;
            self.Retry = defaults.Retry;
        }
    }
}
