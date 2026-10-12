namespace DisplayName.DisplayNameCode.Multiplayer;

internal static class RemoteDisplayNames
{
    private static readonly Dictionary<ulong, string> Names = new();
    public static event Action<ulong>? Updated;

    public static void Set(ulong playerId, string displayName)
    {
        if (Names.TryGetValue(playerId, out string? previous) && previous == displayName) return;
        Names[playerId] = displayName;
        NotifyUpdated(playerId);
    }

    public static void NotifyUpdated(ulong playerId) => Updated?.Invoke(playerId);
    public static bool TryGet(ulong playerId, out string displayName) => Names.TryGetValue(playerId, out displayName!);

    public static void Retain(IReadOnlySet<ulong> participants)
    {
        foreach (ulong id in Names.Keys.Where(id => !participants.Contains(id)).ToArray())
        {
            Names.Remove(id);
            NotifyUpdated(id);
        }
    }

    public static void Clear()
    {
        ulong[] ids = Names.Keys.ToArray();
        Names.Clear();
        foreach (ulong id in ids) NotifyUpdated(id);
    }
}
