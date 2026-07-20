using System.Reflection;
using HarmonyLib;
using NOVR.PatchHelper;
using UnityEngine;

namespace NOVR.Patches.HUD;

internal static class FlightHudPatch
{
    private const float HudDistance = 1000.0f;
    private const float VelocityVectorProjectionDistance = 1000.0f;
    private static readonly AccessTools.FieldRef<FlightHud, Transform> CockpitTransformField =
        AccessTools.FieldRefAccess<FlightHud, Transform>("cockpitTransform");
    private static readonly AccessTools.FieldRef<FlightHud, Rigidbody> CockpitRbField =
        AccessTools.FieldRefAccess<FlightHud, Rigidbody>("cockpitRB");


    [PatchPostfix(typeof(FlightHud), "Update")]
    private static void Update(FlightHud __instance)
    {
        var velocityVector = __instance.velocityVector;
        if (velocityVector == null)
            return;

        var cockpitTransform = CockpitTransformField(__instance);
        var cockpitRb = CockpitRbField(__instance);
        if (cockpitTransform == null || cockpitRb == null)
            return;

        var velocity = cockpitRb.velocity;
        velocityVector.gameObject.SetActive(velocity.magnitude > 10.0f);
        if (!velocityVector.gameObject.activeSelf)
            return;

        var mainCamera = APIBus.MainCamera;
        var cockpitHudCamera = APIBus.CockpitHudCamera;
        if (mainCamera == null || cockpitHudCamera == null)
            return;

        var velocityWorldPosition = cockpitTransform.position + velocity * VelocityVectorProjectionDistance;
        var mainCameraLocalPosition = mainCamera.transform.InverseTransformPoint(velocityWorldPosition);
        if (mainCameraLocalPosition.z <= 0.0f)
        {
            velocityVector.enabled = false;
            return;
        }

        velocityVector.enabled = true;
        velocityVector.transform.position = cockpitHudCamera.transform.TransformPoint(mainCameraLocalPosition).normalized * HudDistance;
        velocityVector.transform.forward = cockpitHudCamera.transform.forward;
    }
}
