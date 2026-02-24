using HarmonyLib;
using System;
using Discord;
using UnityEngine;
using Il2CppSystem.Collections.Generic;
using Gatekeeper.Infrastructure.Providers.Network;
using Gatekeeper.MainMenuScripts.MainMenu.LobbyBrowser;
using Gatekeeper.MainMenuScripts.MainMenu.CharacterSelectPanel;

namespace ExtendedPlayerCount;

/// <summary>
/// Patches for extending player count in Gatekeeper.
///
/// Layer 1 - Network:  Discord.LobbyTransaction.SetCapacity
/// Layer 2 - Data:     LobbyDataDto.get_MaxMembers
/// Layer 3 - UI Text:  LobbyBrowserLine.Setup (lobby browser X/4 text)
/// Layer 4 - UI Slots: CharactersMenuController.SetupMenu (character select slots)
/// </summary>
public static class LobbyPatches
{
    public static void ApplyAll(Harmony harmony)
    {
        harmony.PatchAll(typeof(LobbyPatches));
        Plugin.Log.LogInfo("[ExtendedPlayerCount] All patches applied.");
    }

    // ── 1. Discord.LobbyTransaction.SetCapacity ──────────────────────────────

    [HarmonyPatch(typeof(LobbyTransaction), nameof(LobbyTransaction.SetCapacity))]
    [HarmonyPrefix]
    static void SetCapacity_Prefix(ref uint capacity)
    {
        capacity = (uint)PlayerCountConfig.MaxPlayers;
        Plugin.Log.LogInfo($"[ExtendedPlayerCount] SetCapacity → {capacity}");
    }

    // ── 2. LobbyDataDto.get_MaxMembers ───────────────────────────────────────

    [HarmonyPatch(typeof(LobbyDataDto), nameof(LobbyDataDto.MaxMembers), MethodType.Getter)]
    [HarmonyPostfix]
    static void MaxMembers_Postfix(ref int __result)
    {
        if (__result < PlayerCountConfig.MaxPlayers)
            __result = PlayerCountConfig.MaxPlayers;
    }

    // ── 3. LobbyBrowserLine.Setup — fixes X/4 text in lobby browser ──────────

    [HarmonyPatch(typeof(LobbyBrowserLine), nameof(LobbyBrowserLine.Setup))]
    [HarmonyPostfix]
    static void LobbyBrowserLine_Setup_Postfix(LobbyBrowserLine __instance)
    {
        try
        {
            string current = __instance.playersText.text;
            if (current.Contains("/"))
            {
                var parts = current.Split('/');
                if (parts.Length == 2)
                {
                    string newText = $"{parts[0]}/{PlayerCountConfig.MaxPlayers}";
                    __instance.playersText.text = newText;
                    Plugin.Log.LogDebug($"[ExtendedPlayerCount] LobbyBrowserLine: {current} → {newText}");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ExtendedPlayerCount] LobbyBrowserLine_Setup_Postfix: {ex.Message}");
        }
    }

    // ── 4. CharactersMenuController.SetupMenu — fixes character select slots ──

    private static CharactersMenuController? _patchedInstance = null;
    private static int _patchedMaxPlayers = 0;

    [HarmonyPatch(typeof(CharactersMenuController), nameof(CharactersMenuController.SetupMenu))]
    [HarmonyPostfix]
    static void SetupMenu_Postfix(CharactersMenuController __instance)
    {
        try
        {
            if (ReferenceEquals(__instance, _patchedInstance) && _patchedMaxPlayers == PlayerCountConfig.MaxPlayers)
            {
                Plugin.Log.LogInfo("[ExtendedPlayerCount] SetupMenu: slots already added, skipping.");
                return;
            }

            var panels = __instance.PlayerPanels;
            int count = panels.Count;

            Plugin.Log.LogInfo($"[ExtendedPlayerCount] PlayerPanels count: {count}, MaxPlayers: {PlayerCountConfig.MaxPlayers}");

            if (count >= PlayerCountConfig.MaxPlayers)
            {
                _patchedInstance = __instance;
                _patchedMaxPlayers = PlayerCountConfig.MaxPlayers;
                return;
            }

            int baseCount = Math.Min(count, 4);
            var lastPanel = panels[baseCount - 1];
            var lastGO = lastPanel.gameObject;
            var lastRect = lastGO.GetComponent<RectTransform>();
            var lastPos = lastRect != null ? lastRect.anchoredPosition : Vector2.zero;
            float panelHeight = lastRect != null && lastRect.sizeDelta.y > 0 ? lastRect.sizeDelta.y : 80f;
            var parent = lastGO.transform.parent;

            for (int i = count; i < PlayerCountConfig.MaxPlayers; i++)
            {
                var newGO = UnityEngine.Object.Instantiate(lastGO, parent);
                newGO.name = $"LobbyPlayerPanel_{i}";

                var newRect = newGO.GetComponent<RectTransform>();
                if (newRect != null)
                {
                    float offset = (i - baseCount + 1) * -(panelHeight + 5f);
                    newRect.anchoredPosition = new Vector2(lastPos.x, lastPos.y + offset);
                }

                var newPanel = newGO.GetComponent<LobbyPlayerPanel>();
                if (newPanel == null)
                {
                    Plugin.Log.LogWarning($"[ExtendedPlayerCount] No LobbyPlayerPanel on slot {i}.");
                    continue;
                }

                newPanel.index = i;
                newPanel.SetPlayerMissing();
                panels.Add(newPanel);

                Plugin.Log.LogInfo($"[ExtendedPlayerCount] Created extra player slot {i}.");
            }

            _patchedInstance = __instance;
            _patchedMaxPlayers = PlayerCountConfig.MaxPlayers;
            Plugin.Log.LogInfo("[ExtendedPlayerCount] SetupMenu_Postfix complete.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ExtendedPlayerCount] SetupMenu_Postfix: {ex.Message}");
        }
    }
}
