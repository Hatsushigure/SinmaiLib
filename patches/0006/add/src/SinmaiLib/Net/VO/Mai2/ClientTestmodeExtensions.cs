namespace Net.VO.Mai2;

internal static class ClientTestmodeExtensions
{
    extension(ClientTestmode self)
    {
        public static ClientTestmode Default =>
            new()
            {
                PlaceId = 0,
                ClientId = "",
                TrackSingle = 0,
                TrackMulti = 0,
                TrackEvent = 0,
                TotalMachine = 0,
                SatelliteId = 0,
                CameraPosition = 0,
            };
    }
}
