using System.Text.Json.Serialization;

namespace ChimeLib.NET.COMM.DTO;

internal sealed record GetUserDataResponse(
    [property: JsonPropertyName("errorID")] int ErrorId,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("userID")] int UserId,
    [property: JsonPropertyName("token")] string Token
);
