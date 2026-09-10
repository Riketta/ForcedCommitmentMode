# Steam Workshop description

Paste the text below into the Workshop item's description field when publishing
(the BBCode renders on Steam, but not in-game - `About/About.xml` carries its own
plain-text description).

```
[h3]Forced Commitment Mode[/h3]
The game autosaves the instant anything risky happens - a raid, a downed colonist, a mental break - so no outcome can ever be reloaded away. Closing or crashing the game can no longer dodge a save either.

Always active while the loaded save is a commitment mode save; in reload-anytime saves the mod does nothing unless the optional "also enforce autosaves in non-commitment saves" setting is turned on (event autosaves then write to the normal rotating Autosave slots; the anti-cheat debug restrictions stay commitment-only).

[h3]Autosave triggers (all optional; on by default except animal downed, animal killed, building destroyed and fire started)[/h3]
[list][*]Big threats - raids, sieges, mech clusters, ambushes and everything else the storyteller files under ThreatBig, including DLC and modded incidents.
[*]Small threats - mad animals, blight, toxic fallout and the rest of ThreatSmall.
[*]Infestations - deep drill infestations.
[*]Disease outbreaks.
[*]Pawn downed - colonists, prisoners, slaves, guests and quest pawns; enemies and wild animals are ignored.
[*]Animal downed - pets, trained animals and livestock - off by default.
[*]Pawn killed - the same pawns as pawn downed, at the moment they die.
[*]Animal killed - pets, trained animals and livestock - off by default.
[*]Prison break.
[*]Mental breaks - separate toggles for minor, major and extreme.
[*]Colony building destroyed by damage, fire or collapse (deconstruction does not count) - off by default.
[*]Fire started - rate limited to one save per minute while a fire spreads - off by default.
[*]Save on exit - writes the save when the process is closed directly (ALT+F4 protection).[/list]

[h3]Debug tools[/h3]
While a commitment mode save is running, the dev toolbar is reduced to the log window button: god mode, debug actions, tweak values, view settings, the debug output menu, the inspector and the dev palette are hidden, their hotkeys swallowed (the log hotkey keeps working). God mode is also switched off when a commitment save starts, so a stale flag cannot leak in. Requires development mode to be enabled in the game options; the mod never turns it on by itself.

[h3]Things to keep in mind[/h3]
[list][*]The save is the vanilla commitment autosave: it overwrites the single permadeath save file, exactly like the vanilla autosave does - at the moment the risk appears. The normal once-a-day autosave keeps running as well, so a crash during a quiet stretch still loses at most one in-game day.
[*]Several triggers in the same moment share one save; a trigger right after a save still saves again - commitment integrity beats save count.
[*]Modded incidents that use a custom incident category are not triggers, but their consequences (downed or killed pawns, destroyed buildings) still are.
[*]Saves are skipped while vanilla itself disables saving (gravship cutscenes, tile picking); the next trigger or the periodic autosave catches up.
[*]There is no on/off switch in the mod settings, by design - the mod is always on in commitment mode saves and inert everywhere else; to turn it off, remove it from the mod list (requires a game restart).[/list]

[h3]Compatibility[/h3]
Requires RimWorld 1.6 and [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url].
Pure Harmony mod: no def changes, nothing saved to the game state - safe to add and remove at any time.

Source code and details: [url]https://github.com/Riketta/ForcedCommitmentMode[/url]
```
