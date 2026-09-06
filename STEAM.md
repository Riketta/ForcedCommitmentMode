# Steam Workshop description

Paste the text below into the Workshop item's description field when publishing
(the BBCode renders on Steam, but not in-game - `About/About.xml` carries its own
plain-text description).

```
[h3]Forced Commitment Mode[/h3]
The game autosaves the instant anything risky happens - a raid, a downed colonist, a mental break, a fire - so no outcome can ever be reloaded away. Closing or crashing the game can no longer dodge a save either.

Only active while the loaded save is a commitment mode save; in any other save the mod does nothing.

[h3]Autosave triggers (all optional, all on by default)[/h3]
[list][*]Big threats - raids, sieges, mech clusters, ambushes and everything else the storyteller files under ThreatBig, including DLC and modded incidents.
[*]Small threats - mad animals, blight, toxic fallout and the rest of ThreatSmall.
[*]Infestations - deep drill infestations.
[*]Disease outbreaks.
[*]Pawn downed / pawn killed - colonists, prisoners, slaves, guests, quest pawns and colony animals; enemies and wild animals are ignored.
[*]Prison break.
[*]Mental breaks - separate toggles for minor, major and extreme.
[*]Colony building destroyed by damage, fire or collapse (deconstruction does not count).
[*]Fire started - rate limited to one save per ten seconds while a fire spreads.
[*]Save on exit - writes the save when the process is closed directly (ALT+F4 protection).[/list]

[h3]Debug tools[/h3]
While a commitment mode save is running, the dev toolbar is reduced to the log window button: god mode, debug actions, tweak values, view settings, the debug output menu, the inspector and the dev palette are hidden, their hotkeys swallowed (the log hotkey keeps working). God mode is also switched off when a commitment save starts, so a stale flag cannot leak in. Requires development mode to be enabled in the game options; the mod never turns it on by itself.

[h3]Things to keep in mind[/h3]
[list][*]The save is the vanilla commitment autosave: it overwrites the single permadeath save file, exactly like the vanilla autosave does - just at the moment the risk appears instead of once a day.
[*]Several triggers in the same moment share one save; a trigger right after a save still saves again - commitment integrity beats save count.
[*]Modded incidents that use a custom incident category are not triggers, but their consequences (downed or killed pawns, destroyed buildings) still are.
[*]Saves are skipped while vanilla itself disables saving (gravship cutscenes, tile picking); the next trigger or the periodic autosave catches up.[/list]

[h3]Compatibility[/h3]
Requires RimWorld 1.6 and [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url].
Pure Harmony mod: no def changes, nothing saved to the game state - safe to add and remove at any time.

Source code and details: [url]https://github.com/Riketta/ForcedCommitmentMode[/url]
```
