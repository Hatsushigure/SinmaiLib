using System.Text.Json.Serialization;
using ChimeLib.NET.COMM.DTO;

namespace ChimeLib.NET.COMM;

[JsonSerializable(typeof(GetUserDataResponse))]
[JsonSerializable(typeof(GetUserDataRequest))]
internal partial class CCommJsonContext : JsonSerializerContext;
