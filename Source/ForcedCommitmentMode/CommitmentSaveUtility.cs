using System;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ForcedCommitmentMode
{
    /// <summary>Central save machinery: commitment mode detection, save coalescing and
    /// the pawn filter shared by all triggers.</summary>
    public static class CommitmentSaveUtility
    {
        public const string LogPrefix = "[ForcedCommitmentMode] ";

        /// <summary>Fires spread by spawning further fires; without a cooldown a burning
        /// base would chain saves back to back. Generous on purpose: commitment mode is
        /// about never being able to undo, not about a save per flame.</summary>
        private const float FireSaveCooldownSeconds = 60f;

        /// <summary>The menu quit path already saves right before exiting, as do the
        /// event saves and the periodic autosave; vanilla tracks the last save tick for
        /// all of them, so "nothing to save" is answered by CurrentGameStateIsValuable.</summary>
        private static bool saveQueued;

        private static float lastFireTriggerRealTime = -999f;

        /// <summary>A game is loaded and far enough along to save.</summary>
        private static bool InRunningGame
        {
            get
            {
                if (Current.ProgramState != ProgramState.Playing
                    || Current.Game == null
                    || Current.Game.Info == null
                    || Find.Autosaver == null)
                {
                    return false;
                }
                return true;
            }
        }

        public static bool CommitmentModeActive
        {
            get
            {
                if (!InRunningGame)
                {
                    return false;
                }
                return Current.Game.Info.permadeathMode;
            }
        }

        public static void LogDebug(string message)
        {
            if (ForcedCommitmentModeMod.Settings == null || ForcedCommitmentModeMod.Settings.debugLogging)
            {
                Log.Message(LogPrefix + message);
            }
        }

        /// <summary>True while the mod enforces in the currently loaded save: always in
        /// commitment mode saves, and in non-commitment saves when the optional setting
        /// is on. There is no off switch for commitment saves - turning the mod off
        /// means removing it from the mod list (which needs a game restart). The
        /// anti-cheat debug restrictions do NOT follow this flag; they stay tied to
        /// CommitmentModeActive, where reloading is not allowed anyway.</summary>
        public static bool ShouldEnforce
        {
            get
            {
                if (!InRunningGame)
                {
                    return false;
                }
                if (Current.Game.Info.permadeathMode)
                {
                    return true;
                }
                return ForcedCommitmentModeMod.Settings?.activeInNonCommitment ?? false;
            }
        }

        /// <summary>Queues one vanilla autosave. Concurrent triggers coalesce into the
        /// same queued save because it serializes the game state at execution time -
        /// which is always later than any event that queued it. Purely additive: the
        /// vanilla periodic autosave and its interval timer are never touched, so a
        /// crash during a long quiet stretch still loses at most one in-game day.</summary>
        public static void RequestSave(string trigger, string detail)
        {
            if (!ShouldEnforce)
            {
                return;
            }
            if (GameDataSaveLoader.SavingIsTemporarilyDisabled)
            {
                // Vanilla skips saving during gravship cutscenes and tile picking too;
                // the periodic commitment autosave covers the gap.
                LogDebug("skipped save while saving is temporarily disabled (" + trigger + ": " + detail + ").");
                return;
            }
            if (saveQueued)
            {
                LogDebug("coalesced trigger into the pending save (" + trigger + ": " + detail + ").");
                return;
            }
            saveQueued = true;
            LogDebug("saving (" + trigger + ": " + detail + ").");
            LongEventHandler.QueueLongEvent(PerformSave, "Autosaving", doAsynchronously: false, null);
        }

        private static void PerformSave()
        {
            saveQueued = false;
            if (!ShouldEnforce)
            {
                return;
            }
            try
            {
                Find.Autosaver.DoAutosave();
                LogDebug("save finished.");
            }
            catch (Exception e)
            {
                Log.Error(LogPrefix + "autosave failed: " + e);
            }
        }

        /// <summary>Fire triggers are rate limited to avoid save chains while a fire
        /// spreads; other triggers are rare enough to never need this.</summary>
        public static bool FireTriggerAllowed
        {
            get
            {
                return Time.realtimeSinceStartup - lastFireTriggerRealTime >= FireSaveCooldownSeconds;
            }
        }

        public static void NotifyFireTriggered()
        {
            lastFireTriggerRealTime = Time.realtimeSinceStartup;
        }

        /// <summary>Vanilla's own "does the player get messages about this pawn" filter:
        /// colonists, prisoners, slaves, guests, quest pawns and colony animals pass,
        /// while enemies, wild animals and world pawns are filtered out. Dead pawns are
        /// not excluded - the killed trigger calls this after the fact on purpose. The
        /// spawned/caravan requirement keeps off-screen world pawns (banished colonists
        /// keep the player faction) from triggering saves when they die out of sight.</summary>
        public static bool PawnMattersToPlayer(Pawn pawn)
        {
            if (pawn == null || (!pawn.Spawned && !pawn.IsCaravanMember()))
            {
                return false;
            }
            return PawnUtility.ShouldSendNotificationAbout(pawn);
        }

        public static void OnGameSessionStarted()
        {
            // Quitting to the menu runs LongEventHandler.ClearQueuedEvents(), which can
            // drop a queued save while saveQueued is still set; a stale flag would make
            // every later trigger coalesce into a save that never executes.
            saveQueued = false;
            if (!CommitmentModeActive)
            {
                return;
            }
            if (DebugSettings.godMode)
            {
                DebugSettings.godMode = false;
                Log.Message(LogPrefix + "god mode switched off (commitment mode save).");
            }
            CloseDebugWindows();
        }

        /// <summary>Debug windows live on the app-wide window stack and survive loading
        /// a different save: one opened in a non-commitment save would stay up (and keep
        /// working) inside a commitment save whose toolbar buttons are hidden. All
        /// Dialog_Debug tab menus (actions, settings, output) share one class, so a
        /// single TryRemove catches them all; the log window is the allowed exception.</summary>
        private static void CloseDebugWindows()
        {
            WindowStack windowStack = Find.WindowStack;
            if (windowStack == null)
            {
                return;
            }
            bool removed = windowStack.TryRemove(typeof(Dialog_Debug));
            removed |= windowStack.TryRemove(typeof(Dialog_DevPalette));
            removed |= windowStack.TryRemove(typeof(EditWindow_TweakValues));
            removed |= windowStack.TryRemove(typeof(EditWindow_DebugInspector));
            if (removed)
            {
                LogDebug("debug windows closed (commitment mode save).");
            }
        }

        public static void OnApplicationQuitting()
        {
            try
            {
                ForcedCommitmentModeSettings settings = ForcedCommitmentModeMod.Settings;
                if (settings == null || !settings.saveOnExit || !ShouldEnforce)
                {
                    return;
                }
                if (GameDataSaveLoader.SavingIsTemporarilyDisabled)
                {
                    LogDebug("exit save skipped while saving is temporarily disabled.");
                    return;
                }
                if (!GameDataSaveLoader.CurrentGameStateIsValuable)
                {
                    LogDebug("exit save skipped, the save file is already up to date.");
                    return;
                }
                LogDebug("exit save (process quitting).");
                // DoAutosave writes the single permadeath file in commitment mode and
                // the rotating Autosave-N slots in non-commitment saves.
                Find.Autosaver.DoAutosave();
                Log.Message(LogPrefix + "exit save finished.");
            }
            catch (Exception e)
            {
                Log.Warning(LogPrefix + "exit save failed: " + e.Message);
            }
        }
    }
}
