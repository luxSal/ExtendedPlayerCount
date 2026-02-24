using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

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
        PatchSetCapacity(harmony);
        PatchMaxMembers(harmony);
        PatchLobbyBrowserLine(harmony);
        PatchSetupMenu(harmony);
        Plugin.Log.LogInfo("[ExtendedPlayerCount] Runtime patching complete.");
    }

    // ── 1. Discord.LobbyTransaction.SetCapacity ──────────────────────────────

    private static void PatchSetCapacity(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Discord.LobbyTransaction");
        if (type == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] Discord.LobbyTransaction not found — skipped."); return; }

        var method = AccessTools.Method(type, "SetCapacity");
        if (method == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] SetCapacity not found — skipped."); return; }

        harmony.Patch(method, prefix: new HarmonyMethod(typeof(LobbyPatches), nameof(SetCapacity_Prefix)));
        Plugin.Log.LogInfo($"[ExtendedPlayerCount] Patched Discord.LobbyTransaction.SetCapacity → {PlayerCountConfig.MaxPlayers}");
    }

    static void SetCapacity_Prefix(ref uint capacity)
    {
        capacity = (uint)PlayerCountConfig.MaxPlayers;
    }

    // ── 2. LobbyDataDto.get_MaxMembers ───────────────────────────────────────

    private static void PatchMaxMembers(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Gatekeeper.Infrastructure.Providers.Network.LobbyDataDto");
        if (type == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] LobbyDataDto not found — skipped."); return; }

        var method = AccessTools.PropertyGetter(type, "MaxMembers");
        if (method == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] LobbyDataDto.MaxMembers getter not found — skipped."); return; }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(LobbyPatches), nameof(MaxMembers_Postfix)));
        Plugin.Log.LogInfo($"[ExtendedPlayerCount] Patched LobbyDataDto.MaxMembers → {PlayerCountConfig.MaxPlayers}");
    }

    static void MaxMembers_Postfix(ref int __result)
    {
        if (__result < PlayerCountConfig.MaxPlayers)
            __result = PlayerCountConfig.MaxPlayers;
    }

    // ── 3. LobbyBrowserLine.Setup — fixes X/4 text in lobby browser ──────────

    private static void PatchLobbyBrowserLine(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Gatekeeper.MainMenuScripts.MainMenu.LobbyBrowser.LobbyBrowserLine");
        if (type == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] LobbyBrowserLine not found — skipped."); return; }

        var method = AccessTools.Method(type, "Setup");
        if (method == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] LobbyBrowserLine.Setup not found — skipped."); return; }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(LobbyPatches), nameof(LobbyBrowserLine_Setup_Postfix)));
        Plugin.Log.LogInfo("[ExtendedPlayerCount] Patched LobbyBrowserLine.Setup");
    }

    static void LobbyBrowserLine_Setup_Postfix(object __instance)
    {
        try
        {
            var playersTextField = AccessTools.Field(__instance.GetType(), "playersText");
            if (playersTextField == null) return;

            var playersText = playersTextField.GetValue(__instance);
            if (playersText == null) return;

            var textProp = AccessTools.Property(playersText.GetType(), "text");
            if (textProp == null) return;

            string current = textProp.GetValue(playersText) as string ?? "";

            if (current.Contains("/"))
            {
                var parts = current.Split('/');
                if (parts.Length == 2)
                {
                    string newText = $"{parts[0]}/{PlayerCountConfig.MaxPlayers}";
                    textProp.SetValue(playersText, newText);
                    Plugin.Log.LogDebug($"[ExtendedPlayerCount] LobbyBrowserLine: {current} → {newText}");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ExtendedPlayerCount] LobbyBrowserLine_Setup_Postfix non-fatal: {ex.Message}");
        }
    }

    // ── 4. CharactersMenuController.SetupMenu — fixes character select slots ──
    // PlayerPanels is a List<LobbyPlayerPanel> with 4 entries by default.
    // We patch SetupMenu to activate extra panels by calling SetPlayerMissing()
    // on each slot beyond index 3, making them show as empty invite slots.

    private static void PatchSetupMenu(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Gatekeeper.MainMenuScripts.MainMenu.CharacterSelectPanel.CharactersMenuController");
        if (type == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] CharactersMenuController not found — skipped."); return; }

        var method = AccessTools.Method(type, "SetupMenu");
        if (method == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] SetupMenu not found — skipped."); return; }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(LobbyPatches), nameof(SetupMenu_Postfix)));
        Plugin.Log.LogInfo("[ExtendedPlayerCount] Patched CharactersMenuController.SetupMenu");
    }

    // Track the controller instance we already patched so we don't add slots twice
    private static object _patchedInstance = null;
    private static int _patchedMaxPlayers = 0;

    static void SetupMenu_Postfix(object __instance)
    {
        try
        {
            // If same instance and same max players, slots already exist — skip
            if (ReferenceEquals(__instance, _patchedInstance) && _patchedMaxPlayers == PlayerCountConfig.MaxPlayers)
            {
                Plugin.Log.LogInfo("[ExtendedPlayerCount] SetupMenu called again on same instance — slots already added, skipping.");
                return;
            }

            var playerPanelsGetter = AccessTools.PropertyGetter(__instance.GetType(), "PlayerPanels");
            if (playerPanelsGetter == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] PlayerPanels getter not found."); return; }

            var playerPanels = playerPanelsGetter.Invoke(__instance, null);
            if (playerPanels == null) return;

            var countProp = AccessTools.Property(playerPanels.GetType(), "Count");
            int count = (int)countProp.GetValue(playerPanels);

            Plugin.Log.LogInfo($"[ExtendedPlayerCount] PlayerPanels count: {count}, MaxPlayers: {PlayerCountConfig.MaxPlayers}");

            if (count >= PlayerCountConfig.MaxPlayers)
            {
                Plugin.Log.LogInfo("[ExtendedPlayerCount] Correct panel count already exists.");
                _patchedInstance = __instance;
                _patchedMaxPlayers = PlayerCountConfig.MaxPlayers;
                return;
            }

            // Only add the difference needed (handles case where count > 4 from a previous run)
            int baseCount = Math.Min(count, 4); // never remove original 4 panels
            int target = PlayerCountConfig.MaxPlayers;

            var getItemMethod = AccessTools.Method(playerPanels.GetType(), "get_Item");
            var addMethod = AccessTools.Method(playerPanels.GetType(), "Add");

            // Get last ORIGINAL panel (index 3) as clone template
            var lastPanel = getItemMethod.Invoke(playerPanels, new object[] { baseCount - 1 });
            if (lastPanel == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] Last panel is null."); return; }

            var gameObjectProp = AccessTools.Property(lastPanel.GetType(), "gameObject");
            var lastGO = gameObjectProp?.GetValue(lastPanel);
            if (lastGO == null) { Plugin.Log.LogWarning("[ExtendedPlayerCount] Last panel GO is null."); return; }

            var transformProp = AccessTools.Property(lastGO.GetType(), "transform");
            var lastTransform = transformProp?.GetValue(lastGO);
            var parentProp = AccessTools.Property(lastTransform?.GetType(), "parent");
            var parentTransform = parentProp?.GetValue(lastTransform);

            var getRectMethod = AccessTools.Method(lastGO.GetType(), "GetComponent", new Type[0], new[] { AccessTools.TypeByName("UnityEngine.RectTransform") });
            var lastRect = getRectMethod?.Invoke(lastGO, null);
            var anchoredPosProp = lastRect != null ? AccessTools.Property(lastRect.GetType(), "anchoredPosition") : null;
            var sizeDeltaProp = lastRect != null ? AccessTools.Property(lastRect.GetType(), "sizeDelta") : null;

            float lastX = 0f, lastY = 0f, panelHeight = 80f;
            if (anchoredPosProp != null)
            {
                dynamic pos = anchoredPosProp.GetValue(lastRect);
                lastX = (float)pos.x; lastY = (float)pos.y;
            }
            if (sizeDeltaProp != null)
            {
                dynamic sz = sizeDeltaProp.GetValue(lastRect);
                float h = (float)sz.y;
                if (h > 0) panelHeight = h;
            }

            var instantiateMethod = typeof(UnityEngine.Object).GetMethods()
                .FirstOrDefault(m => m.Name == "Instantiate" && !m.IsGenericMethod &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType.Name == "Transform");

            var panelTypeName = "Gatekeeper.MainMenuScripts.MainMenu.CharacterSelectPanel.LobbyPlayerPanel";
            var getCompByNameMethod = AccessTools.Method(lastGO.GetType(), "GetComponent", new[] { typeof(string) });

            for (int i = count; i < target; i++)
            {
                try
                {
                    var newGO = instantiateMethod?.Invoke(null, new object[] { lastGO, parentTransform });
                    if (newGO == null) { Plugin.Log.LogWarning($"[ExtendedPlayerCount] Instantiate null for slot {i}."); continue; }

                    var nameProp = AccessTools.Property(newGO.GetType(), "name");
                    nameProp?.SetValue(newGO, $"LobbyPlayerPanel_{i}");

                    var newRect = getRectMethod?.Invoke(newGO, null);
                    if (newRect != null && anchoredPosProp != null)
                    {
                        float offset = (i - baseCount + 1) * -(panelHeight + 5f);
                        var vec2Type = AccessTools.TypeByName("UnityEngine.Vector2");
                        var newPos = Activator.CreateInstance(vec2Type, lastX, lastY + offset);
                        anchoredPosProp.SetValue(newRect, newPos);
                    }

                    var newPanel = getCompByNameMethod?.Invoke(newGO, new object[] { panelTypeName });
                    if (newPanel == null) { Plugin.Log.LogWarning($"[ExtendedPlayerCount] No LobbyPlayerPanel on slot {i}."); continue; }

                    AccessTools.Field(newPanel.GetType(), "index")?.SetValue(newPanel, i);
                    AccessTools.Method(newPanel.GetType(), "SetPlayerMissing")?.Invoke(newPanel, null);
                    addMethod.Invoke(playerPanels, new object[] { newPanel });

                    Plugin.Log.LogInfo($"[ExtendedPlayerCount] Created extra player slot {i}.");
                }
                catch (Exception slotEx)
                {
                    Plugin.Log.LogWarning($"[ExtendedPlayerCount] Slot {i} error: {slotEx.Message}");
                }
            }

            _patchedInstance = __instance;
            _patchedMaxPlayers = PlayerCountConfig.MaxPlayers;
            Plugin.Log.LogInfo("[ExtendedPlayerCount] SetupMenu_Postfix complete.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ExtendedPlayerCount] SetupMenu_Postfix non-fatal: {ex.Message}");
        }
    }
}

