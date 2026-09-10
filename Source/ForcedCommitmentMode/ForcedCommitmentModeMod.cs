using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ForcedCommitmentMode
{
    public class ForcedCommitmentModeSettings : ModSettings
    {
        // Incident category triggers.
        public bool incidentsThreatBig = true;
        public bool incidentsThreatSmall = true;
        public bool incidentsInfestation = true;
        public bool incidentsDisease = true;

        // Direct state-change triggers.
        public bool pawnDowned = true;
        public bool pawnKilled = true;
        public bool mentalBreakMinor = true;
        public bool mentalBreakMajor = true;
        public bool mentalBreakExtreme = true;
        public bool prisonBreak = true;
        public bool buildingDestroyed = false;
        public bool fireStarted = false;

        // Scope.
        public bool activeInNonCommitment = false;

        // Anti-cheat extras.
        public bool saveOnExit = true;

        public bool debugLogging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref incidentsThreatBig, "incidentsThreatBig", true);
            Scribe_Values.Look(ref incidentsThreatSmall, "incidentsThreatSmall", true);
            Scribe_Values.Look(ref incidentsInfestation, "incidentsInfestation", true);
            Scribe_Values.Look(ref incidentsDisease, "incidentsDisease", true);
            Scribe_Values.Look(ref pawnDowned, "pawnDowned", true);
            Scribe_Values.Look(ref pawnKilled, "pawnKilled", true);
            Scribe_Values.Look(ref mentalBreakMinor, "mentalBreakMinor", true);
            Scribe_Values.Look(ref mentalBreakMajor, "mentalBreakMajor", true);
            Scribe_Values.Look(ref mentalBreakExtreme, "mentalBreakExtreme", true);
            Scribe_Values.Look(ref prisonBreak, "prisonBreak", true);
            Scribe_Values.Look(ref buildingDestroyed, "buildingDestroyed", false);
            Scribe_Values.Look(ref fireStarted, "fireStarted", false);
            Scribe_Values.Look(ref activeInNonCommitment, "activeInNonCommitment", false);
            Scribe_Values.Look(ref saveOnExit, "saveOnExit", true);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
        }
    }

    public class ForcedCommitmentModeMod : Mod
    {
        public const string PackageId = "Riketta.ForcedCommitmentMode";

        public static ForcedCommitmentModeSettings Settings;

        private static bool quitHookSubscribed;

        // The per-mod settings dialog (Dialog_ModSettings) does not scroll and small
        // resolutions clip the lower rows, so the listing is wrapped in a scrollview.
        // The view height only becomes known after a frame of drawing, hence the
        // previous-frame height.
        private Vector2 scrollPosition;

        private float lastContentHeight;

        public ForcedCommitmentModeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ForcedCommitmentModeSettings>();
            SubscribeQuitHookOnce();

            // Patch each class separately: a game update that renames one target must
            // degrade to "that trigger stops working", never break the rest.
            Harmony harmony = new Harmony(PackageId);
            PatchSafe(harmony, typeof(Patch_IncidentWorker_TryExecute));
            PatchSafe(harmony, typeof(Patch_Pawn_HealthTracker_MakeDowned));
            PatchSafe(harmony, typeof(Patch_Pawn_Kill));
            PatchSafe(harmony, typeof(Patch_MentalStateHandler_TryStartMentalState));
            PatchSafe(harmony, typeof(Patch_PrisonBreakUtility_StartPrisonBreak));
            PatchSafe(harmony, typeof(Patch_ThingWithComps_Destroy));
            PatchSafe(harmony, typeof(Patch_Fire_SpawnSetup));
            PatchSafe(harmony, typeof(Patch_DebugWindowsOpener_DrawButtons));
            PatchSafe(harmony, typeof(Patch_DebugWindowsOpener_DevToolStarterOnGUI));
            PatchSafe(harmony, typeof(Patch_Game_InitNewGame));
            PatchSafe(harmony, typeof(Patch_SavedGameLoaderNow_LoadGameFromSaveFileNow));

            CommitmentSaveUtility.LogDebug("loaded (debugLogging=" + Settings.debugLogging.ToString().ToLowerInvariant() + ").");
        }

        private static void PatchSafe(Harmony harmony, Type patchClass)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                CommitmentSaveUtility.LogDebug("applied " + patchClass.Name + ".");
            }
            catch (Exception e)
            {
                Log.Error("[ForcedCommitmentMode] Patch " + patchClass.Name + " could not be applied (game update?). " + e.Message);
            }
        }

        /// <summary>ALT+F4 and window kills never run game code, but Unity raises
        /// Application.quitting for any process exit that is not a hard crash. The
        /// callback is raised synchronously on the main thread between frames and
        /// blocks teardown until it returns, so the save below is safe to run there:
        /// nothing can interleave with it (vanilla saves share the same thread) and
        /// the process cannot die mid-write. SafeSaver additionally writes to a
        /// temp file and keeps the previous generation in commitment mode.</summary>
        private static void SubscribeQuitHookOnce()
        {
            if (quitHookSubscribed)
            {
                return;
            }
            quitHookSubscribed = true;
            Application.quitting += CommitmentSaveUtility.OnApplicationQuitting;
        }

        public override string SettingsCategory()
        {
            return "ForcedCommitmentMode.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Mathf.Max(lastContentHeight, inRect.height));
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard list = new Listing_Standard(viewRect, () => scrollPosition);
            list.Begin(viewRect);
            list.Label("ForcedCommitmentMode.Note".Translate());
            list.Gap(8f);
            list.CheckboxLabeled("ForcedCommitmentMode.NonCommitment".Translate(), ref Settings.activeInNonCommitment, "ForcedCommitmentMode.NonCommitmentTip".Translate());
            list.Gap(12f);

            Text.Font = GameFont.Medium;
            list.Label("ForcedCommitmentMode.TriggersHeader".Translate());
            Text.Font = GameFont.Small;
            list.Gap(4f);

            list.CheckboxLabeled("ForcedCommitmentMode.IncidentsThreatBig".Translate(), ref Settings.incidentsThreatBig, "ForcedCommitmentMode.IncidentsThreatBigTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.IncidentsThreatSmall".Translate(), ref Settings.incidentsThreatSmall, "ForcedCommitmentMode.IncidentsThreatSmallTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.IncidentsInfestation".Translate(), ref Settings.incidentsInfestation, "ForcedCommitmentMode.IncidentsInfestationTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.IncidentsDisease".Translate(), ref Settings.incidentsDisease, "ForcedCommitmentMode.IncidentsDiseaseTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.PawnDowned".Translate(), ref Settings.pawnDowned, "ForcedCommitmentMode.PawnDownedTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.PawnKilled".Translate(), ref Settings.pawnKilled, "ForcedCommitmentMode.PawnKilledTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.PrisonBreak".Translate(), ref Settings.prisonBreak, "ForcedCommitmentMode.PrisonBreakTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.MentalBreakMinor".Translate(), ref Settings.mentalBreakMinor, "ForcedCommitmentMode.MentalBreakMinorTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.MentalBreakMajor".Translate(), ref Settings.mentalBreakMajor, "ForcedCommitmentMode.MentalBreakMajorTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.MentalBreakExtreme".Translate(), ref Settings.mentalBreakExtreme, "ForcedCommitmentMode.MentalBreakExtremeTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.BuildingDestroyed".Translate(), ref Settings.buildingDestroyed, "ForcedCommitmentMode.BuildingDestroyedTip".Translate());
            list.CheckboxLabeled("ForcedCommitmentMode.FireStarted".Translate(), ref Settings.fireStarted, "ForcedCommitmentMode.FireStartedTip".Translate());
            list.Gap(12f);

            list.CheckboxLabeled("ForcedCommitmentMode.SaveOnExit".Translate(), ref Settings.saveOnExit, "ForcedCommitmentMode.SaveOnExitTip".Translate());
            list.Gap(12f);

            list.CheckboxLabeled("ForcedCommitmentMode.DebugLogging".Translate(), ref Settings.debugLogging, "ForcedCommitmentMode.DebugLoggingTip".Translate());
            list.End();
            lastContentHeight = list.CurHeight;
            Widgets.EndScrollView();
        }
    }
}
