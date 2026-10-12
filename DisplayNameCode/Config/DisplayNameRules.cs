using System.Buffers;
using System.Text;

namespace DisplayName.DisplayNameCode.Config;

internal static class DisplayNameRules
{
    public const int MaxLength = 32;
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var result = new StringBuilder();
        for (int offset = 0; offset < value.Length;)
        {
            OperationStatus status = Rune.DecodeFromUtf16(value.AsSpan(offset), out Rune rune, out int consumed);
            offset += Math.Max(1, consumed);
            if (status != OperationStatus.Done) continue;
            if (Rune.IsControl(rune)) continue;
            if (result.Length + rune.Utf16SequenceLength > MaxLength) break;
            result.Append(rune.ToString());
        }
        return result.ToString();
    }
}
