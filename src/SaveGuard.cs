using HarmonyLib;

namespace HuniepopEndless
{
    /// <summary>
    /// Hard block on every persistence write while an Endless run is active. The
    /// synthetic PlayerFile lives only in <c>Game.Persistence.playerData.files[3]</c>
    /// in memory; <c>PlayerData</c> is rebuilt from <c>_saveData</c> on the next
    /// <c>GamePersistence.Reset()</c>, so nothing we do here can reach the disk as
    /// long as these four writers stay no-ops.
    /// </summary>
    [HarmonyPatch]
    internal static class SaveGuard
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamePersistence), "Save")]
        private static bool BlockSave() => !EndlessRun.Active;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamePersistence), "SaveGame")]
        private static bool BlockSaveGame() => !EndlessRun.Active;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamePersistence), nameof(GamePersistence.Apply), typeof(int))]
        private static bool BlockApplyInt() => !EndlessRun.Active;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamePersistence), nameof(GamePersistence.Apply), typeof(bool))]
        private static bool BlockApplyBool() => !EndlessRun.Active;
    }
}
