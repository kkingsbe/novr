using HarmonyLib;

namespace NOVR.VrCamera;

public class CameraCockpitStatePatch
{
    [HarmonyPatch(typeof(CameraCockpitState), "UpdateState")]
    private static class UpdateStatePatch
    {
        private static readonly AccessTools.FieldRef<CameraCockpitState, float> PanViewRef = AccessTools.FieldRefAccess<CameraCockpitState, float>("panView");
        private static readonly AccessTools.FieldRef<CameraCockpitState, float> TiltViewRef = AccessTools.FieldRefAccess<CameraCockpitState, float>("tiltView");


        [HarmonyPostfix]
        private static void Postfix(CameraCockpitState __instance, CameraStateManager cam)
        {
            PanViewRef(__instance) = 0.0f;
            TiltViewRef(__instance) = 0.0f;
        }
    }
}