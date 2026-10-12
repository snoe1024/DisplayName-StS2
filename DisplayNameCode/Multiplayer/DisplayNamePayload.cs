using System.Text;
using DisplayName.DisplayNameCode.Config;

namespace DisplayName.DisplayNameCode.Multiplayer;

internal static class DisplayNamePayload
{
    public const int MaxBytes = DisplayNameRules.MaxLength * 4;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static byte[] Encode(string name) => Utf8.GetBytes(DisplayNameRules.Normalize(name));

    public static bool TryDecode(ReadOnlySpan<byte> payload, out string name)
    {
        name = "";
        if (payload.Length > MaxBytes) return false;
        try
        {
            string decoded = Utf8.GetString(payload);
            if (DisplayNameRules.Normalize(decoded) != decoded) return false;
            name = decoded;
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }
}
