using System.Reflection;
using HarmonyLib;
using NOVR.PatchHelper;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.Patches.HUD;

internal static class JammedMarkerPatch
{
    private static readonly AccessTools.FieldRef<JammedMarker, GameObject> VectorLineField =
        AccessTools.FieldRefAccess<JammedMarker, GameObject>("vectorLine");
    private static readonly AccessTools.FieldRef<JammedMarker, Image> VectorLineImageField =
        AccessTools.FieldRefAccess<JammedMarker, Image>("vectorLineImage");
    private static readonly AccessTools.FieldRef<JammedMarker, Radar> RadarField =
        AccessTools.FieldRefAccess<JammedMarker, Radar>("radar");
    private static readonly AccessTools.FieldRef<JammedMarker, Unit> UnitField =
        AccessTools.FieldRefAccess<JammedMarker, Unit>("unit");
    private static readonly AccessTools.FieldRef<JammedMarker, Unit> JammedByField =
        AccessTools.FieldRefAccess<JammedMarker, Unit>("jammedBy");
    private static readonly AccessTools.FieldRef<JammedMarker, UnitMapIcon> MapIconField =
        AccessTools.FieldRefAccess<JammedMarker, UnitMapIcon>("mapIcon");
    private static readonly AccessTools.FieldRef<JammedMarker, UnitMapIcon> JammedByIconField =
        AccessTools.FieldRefAccess<JammedMarker, UnitMapIcon>("jammedByIcon");

    
    [PatchPrefix(typeof(JammedMarker), "Update")]
    private static bool Update(JammedMarker __instance)
    {
        var unit = UnitField(__instance);
        var mapIcon = MapIconField(__instance);
        var radar = RadarField(__instance);
        var jammedBy = JammedByField(__instance);
        if (unit == null || mapIcon == null || unit.disabled || radar == null || jammedBy == null || jammedBy.disabled || !radar.IsJammed())
            return true;

        var map = SceneSingleton<DynamicMap>.i;
        var iconLayer = map.iconLayer.transform;
        var jammedLocalPosition = iconLayer.InverseTransformPoint(mapIcon.transform.position);
            
        __instance.transform.localPosition = jammedLocalPosition;
        __instance.transform.localScale = Vector3.one / map.mapImage.transform.localScale.x;

        var vectorLineImage = VectorLineImageField(__instance);
        var vectorLine = VectorLineField(__instance);
        var jammedByIcon = JammedByIconField(__instance);
        if (jammedByIcon == null || vectorLineImage == null || vectorLine == null)
        {
            if (vectorLineImage != null)
                vectorLineImage.enabled = false;
            return false;
        }

        var jammerLocalPosition = iconLayer.InverseTransformPoint(jammedByIcon.transform.position);
        var localDelta = jammerLocalPosition - jammedLocalPosition;
        vectorLineImage.enabled = true;
        vectorLine.transform.localPosition = jammedLocalPosition;
        vectorLine.transform.localEulerAngles = new Vector3(
            0.0f,
            0.0f,
            -Mathf.Atan2(localDelta.x, localDelta.y) * Mathf.Rad2Deg);
        vectorLine.transform.localScale = new Vector3(1.0f, localDelta.magnitude, 1.0f);
        return false;
    }
}
