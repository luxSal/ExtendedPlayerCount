# Changelog

## 1.2.0
- Refactored to use BepInEx.AssemblyPublicizer — removed all reflection-based field/method access
- Code is now clean, direct, and follows standard BepInEx/Unity modding practices
- All patches now use typed Harmony attributes instead of runtime string lookups

## 1.1.0
- Added UI slot cloning: character selection screen now shows the correct number of "INVITE A FRIEND" slots matching your configured player count
- Fixed duplicate slots appearing when creating multiple sessions in the same game session
- Network lobby cap, lobby browser text, and lobby data all continue to reflect the configured max

## 1.0.0
- Initial release
- Configurable player cap from 1–16 (default 6)
- Patches Discord lobby capacity at the network level
- Patches LobbyDataDto.MaxMembers
- Patches lobby browser X/4 display text
