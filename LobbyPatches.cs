using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ExtendedPlayerCount;

/// <summary>
/// Gatekeeper uses the Discord GameSDK for lobbies.
/// The lobby max member count is set via Discord.LobbyTransaction.SetCapacity(uint capacity)
/// before being passed to Discord.LobbyManager.CreateLobby().
/// We patch SetCapacity to override the value with our configured cap.
/// </summary>
public static class LobbyPatches
{
    public static void ApplyAll(Harmony harmony)
    {
        PatchSetCapacity(harmony);
        PatchGetCapacity(harmony);
        Plugin.Log.LogInfo("[ExtendedPlayerCount] Runtime patching complete.");
    }

    // ── 1. LobbyTransaction.SetCapacity ──────────────────────────────────────
    // Called by the game when creating or updating a lobby to set the max
    // number of members.  We intercept it and replace the value with our cap.

    private static void PatchSetCapacity(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Discord.LobbyTransaction");
        if (type == null)
        {
            Plugin.Log.LogWarning("[ExtendedPlayerCount] Could not find Discord.LobbyTransaction — SetCapacity patch skipped.");
            return;
        }

        var method = AccessTools.Method(type, "SetCapacity");
        if (method == null)
        {
            Plugin.Log.LogWarning("[ExtendedPlayerCount] Could not find SetCapacity on Discord.LobbyTransaction — patch skipped.");
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(typeof(LobbyPatches), nameof(SetCapacity_Prefix)));
        Plugin.Log.LogInfo($"[ExtendedPlayerCount] Patched Discord.LobbyTransaction.SetCapacity — cap set to {PlayerCountConfig.MaxPlayers}");
    }

    static void SetCapacity_Prefix(ref uint capacity)
    {
        Plugin.Log.LogDebug($"[ExtendedPlayerCount] SetCapacity {capacity} → {PlayerCountConfig.MaxPlayers}");
        capacity = (uint)PlayerCountConfig.MaxPlayers;
    }

    // ── 2. LobbyManager.GetMemberCount / capacity checks ─────────────────────
    // Some games also check lobby capacity via GetCapacity on LobbyManager.
    // Patch it to always return our cap so full-lobby checks use the right value.

    private static void PatchGetCapacity(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Discord.LobbyManager");
        if (type == null)
        {
            Plugin.Log.LogWarning("[ExtendedPlayerCount] Could not find Discord.LobbyManager — GetCapacity patch skipped.");
            return;
        }

        // Try patching GetCapacity if it exists
        var getCapacity = AccessTools.Method(type, "GetCapacity");
        if (getCapacity != null)
        {
            harmony.Patch(getCapacity, postfix: new HarmonyMethod(typeof(LobbyPatches), nameof(GetCapacity_Postfix)));
            Plugin.Log.LogInfo("[ExtendedPlayerCount] Patched Discord.LobbyManager.GetCapacity");
        }
        else
        {
            Plugin.Log.LogDebug("[ExtendedPlayerCount] Discord.LobbyManager.GetCapacity not found — skipping (non-critical).");
        }
    }

    static void GetCapacity_Postfix(ref uint __result)
    {
        if (__result < (uint)PlayerCountConfig.MaxPlayers)
        {
            Plugin.Log.LogDebug($"[ExtendedPlayerCount] GetCapacity {__result} → {PlayerCountConfig.MaxPlayers}");
            __result = (uint)PlayerCountConfig.MaxPlayers;
        }
    }
}
