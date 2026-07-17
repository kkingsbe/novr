using HarmonyLib;
using NOVR.PatchHelper;
using NOVR.VrUi.HarmonyPatches;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.Patches.HUD;

internal static class ObjectiveOverlayPatch
{
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Image> ObjectivePointerRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Image>("objectivePointer");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Image> ObjectiveDotRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Image>("objectiveDot");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Image> SizeIndicatorRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Image>("sizeIndicator");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Text> ObjectiveInfoRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Text>("objectiveInfo");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Transform> PointerTailRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Transform>("pointerTail");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, Color> BaseColorRef = AccessTools.FieldRefAccess<ObjectiveOverlay, Color>("baseColor");
    private static readonly AccessTools.FieldRef<ObjectiveOverlay, bool> HiddenRef = AccessTools.FieldRefAccess<ObjectiveOverlay, bool>("hidden");

    [PatchPrefix(typeof(ObjectiveOverlay), nameof(ObjectiveOverlay.UpdateOverlay))]
    private static bool UpdateOverlay(ObjectiveOverlay __instance, MissionPosition.PositionResult result)
    {
        var mainCamera = APIBus.MainCamera;
        var cockpitHudCamera = APIBus.CockpitHudCamera;
        if (mainCamera == null || cockpitHudCamera == null)
            return true;

        var objectivePointer = ObjectivePointerRef(__instance);
        var objectiveDot = ObjectiveDotRef(__instance);
        var sizeIndicator = SizeIndicatorRef(__instance);
        var objectiveInfo = ObjectiveInfoRef(__instance);
        var pointerTail = PointerTailRef(__instance);
        var baseColor = BaseColorRef(__instance);

        if (objectivePointer == null || objectiveDot == null || sizeIndicator == null || objectiveInfo == null || pointerTail == null)
            return true;

        HiddenRef(__instance) = false;
        objectivePointer.enabled = true;
        objectiveInfo.enabled = true;

        var worldPosition = result.Position.ToLocalPosition();
        var isOffScreen = VrHudProjectionHelper.PinToScreenEdge(worldPosition, out var hudPosition, out var arrowAngle);
        var angleToTarget = Vector3.Angle(mainCamera.transform.forward, result.Direction);

        objectivePointer.transform.position = hudPosition;
        objectiveDot.transform.position = hudPosition;

        if (isOffScreen)
        {
            sizeIndicator.enabled = false;
            objectivePointer.transform.localEulerAngles = new Vector3(0f, 0f, arrowAngle * Mathf.Rad2Deg - 90f);
        }
        else
        {
            sizeIndicator.enabled = true;
        }

        if (angleToTarget > 10f)
        {
            objectivePointer.enabled = true;
            objectiveDot.enabled = false;
            __instance.TextNoOverlap.SetTarget(pointerTail.position);
        }
        else
        {
            objectivePointer.enabled = false;
            objectiveDot.enabled = true;
            __instance.TextNoOverlap.SetTarget(objectiveDot.transform.position - Vector3.up * 25f);
        }

        var range = result.Range.GetValueOrDefault();
        var invDistance = 1f / (result.Distance != 0f ? result.Distance : 0.01f);
        sizeIndicator.transform.localScale = Vector3.one * (35f * range * invDistance);
        var sizeRangeFactor = range * 20f * invDistance - 0.5f;
        sizeIndicator.transform.localEulerAngles = Vector3.forward * sizeRangeFactor * 3f;
        sizeIndicator.color = baseColor * Mathf.Clamp01(sizeRangeFactor);
        sizeIndicator.transform.position = objectivePointer.transform.position;

        var label = result.Objective?.SavedObjective.DisplayName ?? "Waypoint";
        objectiveInfo.text = $"{label} {UnitConverter.DistanceReading(result.Distance)}";
        objectiveInfo.fontSize = (int)PlayerSettings.overlayTextSize;

        return false;
    }
}
