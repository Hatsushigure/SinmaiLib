using System.IO.Compression;
using Net;

namespace NetHttpClient.Tests.Support;

internal static class WirePayload
{
    public static byte[] Encode(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
            zlib.Write(data);
        return CipherAes.Encrypt(output.ToArray());
    }

    public static byte[] Decode(byte[] data)
    {
        using var input = new MemoryStream(CipherAes.Decrypt(data));
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}
