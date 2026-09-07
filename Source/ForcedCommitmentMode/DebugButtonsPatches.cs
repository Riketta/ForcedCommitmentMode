using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace ForcedCommitmentMode
{
    /// <summary>While a commitment mode save is played, the dev toolbar is reduced to
    /// the log window button: god mode, debug actions, tweak values, view settings,
    /// the debug output menu, the inspector and the dev palette are all hidden.
    /// Anti-cheat only: it stays tied to commitment mode even when the optional
    /// non-commitment autosave setting is on, where reloading is allowed anyway.</summary>
    [HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
    public static class Patch_DebugWindowsOpener_DrawButtons
    {
        private static readonly FieldInfo widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");

        private static readonly FieldInfo widgetRowFinalXField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRowFinalX");

        public static bool Prefix(DebugWindowsOpener __instance)
        {
            if (!CommitmentSaveUtility.CommitmentModeActive)
            {
                return true;
            }
            if (widgetRowField == null || widgetRowFinalXField == null)
            {
                // Renamed by a game update - drawing everything is safer than
                // throwing in OnGUI every frame.
                return true;
            }
            WidgetRow row = (WidgetRow)widgetRowField.GetValue(__instance);
            row.Init(0f, 0f);
            if (row.ButtonIcon(TexButton.ToggleLog, "Open the debug log."))
            {
                if (!Find.WindowStack.TryRemove(typeof(EditWindow_Log)))
                {
                    Find.WindowStack.Add(new EditWindow_Log());
                }
            }
            widgetRowFinalXField.SetValue(__instance, row.FinalX);
            return false;
        }
    }

    /// <summary>Hotkeys would open the hidden debug tools anyway, so they are consumed
    /// before vanilla sees them. The log window hotkey (Dev_ToggleDebugLog) is the one
    /// exception and keeps working.</summary>
    [HarmonyPatch(typeof(DebugWindowsOpener), "DevToolStarterOnGUI")]
    public static class Patch_DebugWindowsOpener_DevToolStarterOnGUI
    {
        public static bool Prefix()
        {
            if (!CommitmentSaveUtility.CommitmentModeActive)
            {
                return true;
            }
            // Vanilla re-opens the dev palette on session start when
            // Prefs.StartDevPaletteOn is set (queued from Game.FinalizeInit, which
            // runs after our session-start god mode reset) - close it again.
            if (DebugSettings.devPalette)
            {
                DebugSettings.devPalette = false;
                Find.WindowStack?.TryRemove(typeof(Dialog_DevPalette));
                CommitmentSaveUtility.LogDebug("dev palette closed (commitment mode save).");
            }
            if (!Prefs.DevMode || Event.current == null)
            {
                return true;
            }
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleDebugActionsMenu);
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleDebugLogMenu);
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleDebugSettingsMenu);
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleDevPalette);
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleDebugInspector);
            SwallowIfPressed(KeyBindingDefOf.Dev_ToggleGodMode);
            return true;
        }

        private static void SwallowIfPressed(KeyBindingDef key)
        {
            if (key != null && key.KeyDownEvent)
            {
                CommitmentSaveUtility.LogDebug("debug hotkey swallowed (" + key.defName + ").");
                Event.current.Use();
            }
        }
    }
}
