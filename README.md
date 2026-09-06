# Forced Commitment Mode

A RimWorld 1.6 mod that enforces a true commitment mode playstyle: the game autosaves the
instant anything risky happens, so no outcome - a raid, a downed colonist, a burning base -
can ever be reloaded away. Closing or crashing the game can no longer dodge a save either.

The mod is only active while the loaded save is a commitment mode save. In any other save,
or on the main menu, it does nothing.

## Autosave triggers

Every trigger is optional and togglable in the mod settings; all are enabled by default.
Triggers coalesce: if several fire in the same moment (a raid downs two colonists and
kills a third), they share one save instead of stacking several.

- **Big threats** - incidents of the vanilla `ThreatBig` category: raids, sieges, mech
  clusters, ambushes and everything else filed under ThreatBig, vanilla, DLC and modded.
- **Small threats** - incidents of the `ThreatSmall` category: mad animals, blight, toxic
  fallout and everything else filed under ThreatSmall.
- **Infestations** - deep drill infestations hatching under your miners
  (`DeepDrillInfestation` category).
- **Disease outbreaks** - incidents of the `DiseaseHuman` category.
- **Pawn downed** - a pawn the colony cares about is downed: colonists, prisoners, slaves,
  guests, quest pawns and colony animals. Enemies and wild animals are ignored.
- **Pawn killed** - the same set of pawns, at the moment they die.
- **Prison break** - prisoners start a break.
- **Mental break** - colonist breaks, split into minor, major and extreme toggles.
- **Colony building destroyed** - a player-faction building destroyed by damage, fire or
  collapse. Deconstructing it yourself does not count.
- **Fire started** - a fire appears on one of your maps. Fires spread by creating more
  fires, so this trigger is rate limited to one save per ten seconds.
- **Save on exit** - flushes the save when the game process closes directly (ALT+F4,
  killing the window). Quitting through the main menu already saves and is detected.

## How it works

- Each trigger is a small Harmony postfix on the exact moment the risky thing happens:
  `IncidentWorker.TryExecute` for incidents (filtered by category def, not by hardcoded
  incident names), `Pawn_HealthTracker.MakeDowned`, `Pawn.Kill`,
  `MentalBreakWorker.TryStart`, `PrisonBreakUtility.StartPrisonBreak`,
  `ThingWithComps.Destroy` and `Fire.SpawnSetup`.
- The save itself is the vanilla commitment autosave (`Autosaver.DoAutosave`), queued
  exactly like a vanilla autosave: in commitment mode it overwrites the single permadeath
  save file, so reloading always returns you to the moment the risk appeared.
- Only the state matters, not the source: quest-triggered raids, DLC events and modded
  incidents are saved the same as storyteller raids as long as they use the vanilla
  incident categories.
- ALT+F4 and window kills never run game code, so the mod additionally hooks Unity's
  application-quit event and writes the save there - the last moment before teardown.
  A menu quit (which already saves) is detected and not duplicated.

## Debug tools

While a commitment mode save is running, the dev toolbar is reduced to the log window
button: the god mode toggle, debug actions, tweak values, view settings, the debug output
menu, the inspector and the dev palette are hidden, and their hotkeys are swallowed (the
log window hotkey keeps working). God mode is also switched off when a commitment save
starts - the flag is app-wide and would otherwise leak in from a previous session.

Requires development mode to be enabled in the game options to have any effect; the mod
never turns development mode on by itself.

## Compatibility

- Requires the [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) mod.
- RimWorld 1.6.
- Pure Harmony - no def changes, nothing saved to the game state, safe to add and remove
  at any time. The commitment save file itself is vanilla.

## Mod settings

- **Enable Forced Commitment Mode** - master toggle; the mod is inactive in non-commitment
  saves regardless.
- One toggle per autosave trigger, as listed above (all default to on).
- **Debug logging** - logs every trigger evaluation, coalesced trigger and save to the
  game log:

```
[ForcedCommitmentMode] saving (big threat: RaidEnemy).
[ForcedCommitmentMode] coalesced trigger into the pending save (pawn killed: Marianne).
[ForcedCommitmentMode] save finished.
```

## Known limitations

- **Only vanilla incident categories are recognized.** A modded incident that uses a
  custom incident category is not a trigger; downed/killed/break triggers still cover its
  consequences.
- **Rate-limited fire trigger.** During a firestorm, fires that spawn within ten seconds
  of the last fire save do not trigger their own save; the save that started the chain
  already contains the cause.
- **Saves are skipped while vanilla disables saving** (gravship cutscenes, tile picking),
  exactly like the vanilla autosaver. The next trigger or the periodic autosave catches up.
- **Events cannot be undone, by design.** An accidental wrong decision - banning a pawn,
  drafting into a doom - is committed as soon as anything risky happens around it.

## Technical notes

- Each Harmony patch is applied in its own try/catch: if a game update renames a patch
  target, that trigger is skipped with an error in the log while the rest keeps working.
- Triggers run once per event, not per tick; the only frequently invoked patch is the
  fire spawn check, which is a handful of field reads plus a rate limit check.
- Save coalescing uses the pending-save flag only, never a time window, so a trigger that
  fires right after a save still saves again - commitment integrity beats save count.
- The exit save runs synchronously inside Unity's quit callback; the process stays alive
  until the save is written.

## Build from source

Requires the .NET SDK. Build the Release configuration for the dll you ship - a plain
`dotnet build` defaults to Debug:

```
cd Source/ForcedCommitmentMode
dotnet build -c Release -p:RimWorldDir="C:\Path\To\RimWorld"
```

The output lands in `Assemblies/ForcedCommitmentMode.dll`; the whole `ForcedCommitmentMode`
folder can be copied or symlinked into the game's `Mods` directory.
