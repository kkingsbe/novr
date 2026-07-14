using HarmonyLib;
using UnityEngine.XR.OpenXR;

namespace NOVR.Patches.VR;

[HarmonyPatch]
public static class OpenXRInteractionProfilePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(OpenXRLoaderBase), "RequestOpenXRFeatures")]
    private static void EnsureControllerProfiles()
    {
        OpenXrControllerProfileBootstrap.EnsureRequired();
    }
}
