# ExtendedPlayerCount

Raises the Gatekeeper co-op lobby size beyond the vanilla cap of **4 players**.

## Features

- Set your lobby size anywhere from **1 to 16 players**
- Default cap is **6 players**
- Fully configurable via the BepInEx config file
- No game files modified — safe to remove at any time

## Requirements

- BepInEx IL2CPP 6.0.0-be.725 or newer

> ⚠️ **All players in the session must have this mod installed and set to the same MaxPlayers value.**

## Installation

1. Install BepInEx into your Gatekeeper folder
2. Drop `ExtendedPlayerCount.dll` into:
   `BepInEx/plugins/ExtendedPlayerCount/`
3. Launch the game once to generate the config file

## Configuration

Edit `BepInEx/config/com.yourname.extendedplayercount.cfg`:

```
[General]
MaxPlayers = 6
```

Change the number to anything between 1 and 16 and restart the game.

## Source Code

Built with BepInEx and Harmony. Patches `Discord.LobbyTransaction.SetCapacity` to override the lobby member cap at runtime.
