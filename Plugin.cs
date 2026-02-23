using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ExtendedPlayerCount;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Gatekeeper.exe")]
public class Plugin : BasePlugin
{
    internal new static ManualLogSource Log { get; private set; } = null!;

    private BepInEx.Configuration.ConfigEntry<int> _maxPlayers = null!;

    public override void Load()
    {
        Log = base.Log;

        _maxPlayers = Config.Bind(
            section:      "General",
            key:          "MaxPlayers",
            defaultValue: 8,
            description:  "Maximum number of players allowed in a co-op lobby (vanilla = 4). " +
                          "Valid range: 1-16. Requires all players to have the mod installed."
        );

        int cap = Math.Clamp(_maxPlayers.Value, 1, 16);
        PlayerCountConfig.MaxPlayers = cap;

        Log.LogInfo($"[ExtendedPlayerCount] Loaded - MaxPlayers = {cap}");

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        LobbyPatches.ApplyAll(harmony);

        Log.LogInfo("[ExtendedPlayerCount] Harmony patches applied.");
    }
}
