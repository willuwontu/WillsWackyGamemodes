using HarmonyLib;
using WWGM.GameModeModifiers;

namespace WWGM.Patches
{
    [HarmonyPatch(typeof(HealthHandler))] 
    public static class HealthHandler_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("RPCA_Die_Phoenix")]
        static void OnPheonixDeath(HealthHandler __instance)
        {
            if (RespawnsPerRound.enabled)
            {
                RespawnsPerRound.RecordRemainingRespawns(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Revive")]
        static void OnRevive(HealthHandler __instance, bool isFullRevive)
        {
            if (RespawnsPerRound.enabled && isFullRevive)
            {
                RespawnsPerRound.UpdateRemainingRespawns(__instance);
            }
        }
    }
}