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
    /// the debug output menu, the inspector and the dev palette are all hidden.</summary>
    [HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
    public static class Patch_DebugWindowsOpener_DrawButtons
    {
        private static readonly FieldInfo widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");

        private static readonly FieldInfo widgetRowFinalXField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRowFinalX");

        public static bool Prefix(DebugWindowsOpener __instance)
        {
            if (!CommitmentSaveUtility.ShouldEnforce)
            {
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
            if (!Prefs.DevMode || !CommitmentSaveUtility.ShouldEnforce)
            {
                return true;
            }
            if (Event.current == null)
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
