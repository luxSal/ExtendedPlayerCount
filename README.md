# ExtendedPlayerCount

Raises the Gatekeeper co-op lobby size beyond the vanilla cap of **4 players**.

## Features

- Set your lobby size anywhere from **1 to 16 players**
- Default cap is **8 players**
- Fully configurable via the BepInEx config file
- No game files modified — safe to remove at any time

## Requirements

- BepInEx IL2CPP 6.0.0-be.725 or newer

> ⚠️ **All players in the session must have this mod installed and set to the same MaxPlayers value.**

## Installation

### With Thunderstore Mod Manager (recommended)
1. Install [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
2. Search for **ExtendedPlayerCount** in the Gatekeeper section
3. Click **Install**
4. Launch Gatekeeper through the mod manager

### Manual
1. Install [BepInEx IL2CPP](https://builds.bepinex.dev/projects/bepinex_be/725/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.725+e1974e2.zip) into your Gatekeeper folder
2. Drop `ExtendedPlayerCount.dll` into:
   `BepInEx/plugins/ExtendedPlayerCount/`
3. Launch the game once to generate the config file

## Configuration

Edit `BepInEx/config/com.yourname.extendedplayercount.cfg`:

```
[General]
MaxPlayers = 8
```

Change the number to anything between 1 and 16 and restart the game.

## Source Code

Built with BepInEx and Harmony. Patches `Discord.LobbyTransaction.SetCapacity` to override the lobby member cap at runtime.
