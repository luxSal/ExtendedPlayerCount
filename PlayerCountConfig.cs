namespace ExtendedPlayerCount;

/// <summary>
/// Shared, static state that all Harmony patches read from.
/// Populated during Plugin.Load() so the value is available before any
/// in-game type is constructed.
/// </summary>
public static class PlayerCountConfig
{
    /// <summary>The effective maximum lobby size (clamped to 1–16).</summary>
    public static int MaxPlayers { get; internal set; } = 6;
}
