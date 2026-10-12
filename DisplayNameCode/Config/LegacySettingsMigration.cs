using System.IO;
using System.Text.Json;

namespace DisplayName.DisplayNameCode.Config;

internal static class LegacySettingsMigration
{
    public static string Resolve(bool hasNewSettings, string currentName, string legacyPath, Action<string> warn) =>
        hasNewSettings ? DisplayNameRules.Normalize(currentName) : Read(legacyPath, warn);

    public static string Read(string path, Action<string> warn)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("DisplayName", out JsonElement name) &&
                name.ValueKind == JsonValueKind.String)
                return DisplayNameRules.Normalize(name.GetString());
            warn("DisplayName: legacy settings have no valid DisplayName string; using an empty name.");
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) { return ""; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            warn($"DisplayName: could not read legacy settings ({e.GetType().Name}); using an empty name.");
        }
        return "";
    }
}
