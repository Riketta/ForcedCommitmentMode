using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ForcedCommitmentMode
{
    /// <summary>Storyteller and scripted incidents alike funnel through
    /// IncidentWorker.TryExecute; filtering by incident category def keeps this
    /// future-proof for DLC and mods instead of hardcoding incident defs.</summary>
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class Patch_IncidentWorker_TryExecute
    {
        public static void Postfix(IncidentWorker __instance, bool __result)
        {
            if (!__result || !ForcedCommitmentModeMod.Active)
            {
                return;
            }
            ForcedCommitmentModeSettings settings = ForcedCommitmentModeMod.Settings;
            IncidentCategoryDef category = __instance.def.category;
            if (category == null)
            {
                return;
            }
            if (category == IncidentCategoryDefOf.ThreatBig)
            {
                if (settings.incidentsThreatBig)
                {
                    CommitmentSaveUtility.RequestSave("big threat", __instance.def.defName);
                }
            }
            else if (category == IncidentCategoryDefOf.ThreatSmall)
            {
                if (settings.incidentsThreatSmall)
                {
                    CommitmentSaveUtility.RequestSave("small threat", __instance.def.defName);
                }
            }
            else if (category == IncidentCategoryDefOf.DeepDrillInfestation)
            {
                if (settings.incidentsInfestation)
                {
                    CommitmentSaveUtility.RequestSave("infestation", __instance.def.defName);
                }
            }
            else if (category == IncidentCategoryDefOf.DiseaseHuman)
            {
                if (settings.incidentsDisease)
                {
                    CommitmentSaveUtility.RequestSave("disease", __instance.def.defName);
                }
            }
        }
    }

    /// <summary>Single choke point where a pawn transitions to the downed state; every
    /// damage, hediff and coma path ends here. Fires for downed colony animals too,
    /// which PawnUtility.ShouldSendNotificationAbout already narrows to player pawns.</summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_Pawn_HealthTracker_MakeDowned
    {
        public static void Postfix(Pawn ___pawn)
        {
            if (!ForcedCommitmentModeMod.Active || !ForcedCommitmentModeMod.Settings.pawnDowned)
            {
                return;
            }
            if (CommitmentSaveUtility.PawnMattersToPlayer(___pawn))
            {
                CommitmentSaveUtility.RequestSave("pawn downed", ___pawn.LabelShortCap);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Pawn_Kill
    {
        public static void Postfix(Pawn __instance)
        {
            if (!ForcedCommitmentModeMod.Active || !ForcedCommitmentModeMod.Settings.pawnKilled)
            {
                return;
            }
            if (CommitmentSaveUtility.PawnMattersToPlayer(__instance))
            {
                CommitmentSaveUtility.RequestSave("pawn killed", __instance.LabelShortCap);
            }
        }
    }

    /// <summary>MentalStateHandler.TryStartMentalState is the single choke point every
    /// break passes through - including break workers that override MentalBreakWorker
    /// without calling base. Break intensity is resolved through a def-built map, so
    /// new vanilla, DLC and modded breaks are classified without hardcoding.</summary>
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_MentalStateHandler_TryStartMentalState
    {
        private static readonly Dictionary<MentalStateDef, MentalBreakIntensity> intensityByState = new Dictionary<MentalStateDef, MentalBreakIntensity>();

        private static bool mapReady;

        private static void EnsureIntensityMap()
        {
            if (mapReady)
            {
                return;
            }
            mapReady = true;
            foreach (MentalBreakDef breakDef in DefDatabase<MentalBreakDef>.AllDefsListForReading)
            {
                if (breakDef.mentalState == null)
                {
                    continue;
                }
                if (intensityByState.TryGetValue(breakDef.mentalState, out MentalBreakIntensity existing))
                {
                    if (breakDef.intensity > existing)
                    {
                        intensityByState[breakDef.mentalState] = breakDef.intensity;
                    }
                }
                else
                {
                    intensityByState[breakDef.mentalState] = breakDef.intensity;
                }
            }
        }

        public static void Postfix(MentalStateDef stateDef, bool __result, Pawn ___pawn)
        {
            if (!__result || !ForcedCommitmentModeMod.Active)
            {
                return;
            }
            EnsureIntensityMap();
            if (!intensityByState.TryGetValue(stateDef, out MentalBreakIntensity intensity))
            {
                return;
            }
            ForcedCommitmentModeSettings settings = ForcedCommitmentModeMod.Settings;
            string label;
            bool enabled;
            switch (intensity)
            {
                case MentalBreakIntensity.Minor:
                    label = "minor break";
                    enabled = settings.mentalBreakMinor;
                    break;
                case MentalBreakIntensity.Major:
                    label = "major break";
                    enabled = settings.mentalBreakMajor;
                    break;
                case MentalBreakIntensity.Extreme:
                    label = "extreme break";
                    enabled = settings.mentalBreakExtreme;
                    break;
                default:
                    return;
            }
            if (!enabled)
            {
                return;
            }
            if (CommitmentSaveUtility.PawnMattersToPlayer(___pawn))
            {
                CommitmentSaveUtility.RequestSave("mental break", ___pawn.LabelShortCap + " (" + label + ")");
            }
        }
    }

    /// <summary>Patching the overload with out parameters catches every prison break:
    /// the simple overload delegates to this one, and interaction workers (for example
    /// the spark jailbreak) call it directly. The out types must be built at runtime in
    /// TargetMethod - by-ref types are not valid attribute arguments.</summary>
    [HarmonyPatch]
    public static class Patch_PrisonBreakUtility_StartPrisonBreak
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PrisonBreakUtility), "StartPrisonBreak", new[]
            {
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(LetterDef).MakeByRefType(),
                typeof(List<Pawn>).MakeByRefType()
            });
        }

        public static void Postfix(Pawn initiator)
        {
            if (!ForcedCommitmentModeMod.Active || !ForcedCommitmentModeMod.Settings.prisonBreak)
            {
                return;
            }
            CommitmentSaveUtility.RequestSave("prison break", initiator.LabelShortCap);
        }
    }

    /// <summary>Every ThingWithComps flows through this Destroy override exactly once,
    /// so all buildings pass through here regardless of their exact class. Only
    /// violent destruction counts: deconstruction, cancellation and quest cleanup are
    /// deliberate player actions that must not force a save.</summary>
    [HarmonyPatch(typeof(ThingWithComps), "Destroy")]
    public static class Patch_ThingWithComps_Destroy
    {
        public static void Postfix(ThingWithComps __instance, DestroyMode mode)
        {
            if (mode != DestroyMode.KillFinalize && mode != DestroyMode.KillFinalizeLeavingsOnly)
            {
                return;
            }
            if (!(__instance is Building building))
            {
                return;
            }
            if (!ForcedCommitmentModeMod.Active || !ForcedCommitmentModeMod.Settings.buildingDestroyed)
            {
                return;
            }
            if (building.Faction == Faction.OfPlayer)
            {
                CommitmentSaveUtility.RequestSave("building destroyed", building.LabelCap);
            }
        }
    }

    /// <summary>Fires only appear from gameplay events (arson, zzzt, lightning,
    /// incendiaries), never during map load; respawningAfterLoad is false on every
    /// gameplay spawn. Rate limited via FireTriggerAllowed to survive firestorms.</summary>
    [HarmonyPatch(typeof(Fire), "SpawnSetup")]
    public static class Patch_Fire_SpawnSetup
    {
        public static void Postfix(Fire __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad || __instance.Map == null)
            {
                return;
            }
            if (!ForcedCommitmentModeMod.Active || !ForcedCommitmentModeMod.Settings.fireStarted)
            {
                return;
            }
            if (!CommitmentSaveUtility.FireTriggerAllowed)
            {
                CommitmentSaveUtility.LogDebug("fire trigger rate limited.");
                return;
            }
            CommitmentSaveUtility.NotifyFireTriggered();
            CommitmentSaveUtility.RequestSave("fire started", "size " + __instance.fireSize.ToString("0.##"));
        }
    }
}
