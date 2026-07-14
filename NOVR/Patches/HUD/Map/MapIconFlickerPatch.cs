using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.Patches.HUD.Map;

internal static class MapIconFlickerPatch
{
    [HarmonyPatch(typeof(global::UnitMapIcon), "UpdateIcon")]
    private static class UnitMapIconUpdateIconPatch
    {
        [HarmonyPostfix]
        private static void Postfix(global::UnitMapIcon __instance)
        {
            if (__instance != null && __instance.iconImage != null)
                __instance.iconImage.enabled = __instance.isActiveAndEnabled
                    && __instance.iconImage.sprite != null;
        }
    }

    [HarmonyPatch(typeof(global::AirbaseMapIcon), "UpdateIcon")]
    private static class AirbaseMapIconUpdateIconPatch
    {
        [HarmonyPostfix]
        private static void Postfix(global::AirbaseMapIcon __instance)
        {
            if (__instance != null && __instance.iconImage != null)
                __instance.iconImage.enabled = __instance.isActiveAndEnabled
                    && __instance.iconImage.sprite != null;
        }
    }
}
