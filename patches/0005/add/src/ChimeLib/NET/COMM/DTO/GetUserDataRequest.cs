using System.Text.Json.Serialization;

namespace ChimeLib.NET.COMM.DTO;

internal sealed record GetUserDataRequest
{
    [JsonPropertyName("chipID")]
    public required string ChipId { get; init; }

    [JsonPropertyName("openGameID")]
    public required string OpenGameId { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("qrCode")]
    public required string QrCode { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("titlekey")]
    public required string TitleKey { get; init; }
}
