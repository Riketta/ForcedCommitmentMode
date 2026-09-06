using HarmonyLib;
using Verse;

namespace ForcedCommitmentMode
{
    /// <summary>God mode is a static app-wide flag: it survives quitting to the menu,
    /// loading another save and restarting the process. Whenever a commitment mode
    /// save is entered, it is forced off so a stale flag cannot leak into the run.</summary>
    [HarmonyPatch(typeof(Game), "InitNewGame")]
    public static class Patch_Game_InitNewGame
    {
        public static void Postfix()
        {
            CommitmentSaveUtility.OnGameSessionStarted();
        }
    }

    [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
    public static class Patch_SavedGameLoaderNow_LoadGameFromSaveFileNow
    {
        public static void Postfix()
        {
            CommitmentSaveUtility.OnGameSessionStarted();
        }
    }
}
