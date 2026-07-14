using System.Reflection;
using HarmonyLib;
using NOVR.PatchHelper;
using NOVR.VrUi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.Patches.UI;

internal static class TMP_DropdownPatch
{
    private static readonly FieldInfo DropdownField = AccessTools.Field(typeof(TMP_Dropdown), "m_Dropdown");
    private static readonly FieldInfo BlockerField = AccessTools.Field(typeof(TMP_Dropdown), "m_Blocker");

    // Push the full-screen blocker slightly behind the list plane so, where they overlap, the
    // list (and its items) always wins the closest-hit test and stays clickable. The blocker
    // still catches clicks outside the list, where the list has no graphic, to dismiss the
    // dropdown.
    private const float BlockerDepthBias = 0.01f;

    [PatchPostfix(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Show))]
    private static void Show(TMP_Dropdown __instance)
    {
        if (__instance == null || !IsInVrLayerHierarchy(__instance.transform))
            return;

        var dropdown = (GameObject)DropdownField.GetValue(__instance);
        if (dropdown == null)
            return;

        LayerHelper.SetLayerRecursive(dropdown.transform, LayerHelper.Layers.VrUi);

        var mask = dropdown.gameObject.GetComponentInChildren<Mask>();
        if (mask) mask.enabled = false;

        // The list and blocker are instantiated at runtime with their own nested Canvases. The VR
        // hit-tester only raycasts registered canvases, so register both (pointed at the cockpit HUD
        // camera it matches against). Without this the list renders but is unclickable, and clicks
        // outside never reach the blocker that dismisses the dropdown.
        var ownerCanvas = __instance.GetComponentInParent<Canvas>();
        RegisterCanvas(dropdown, ownerCanvas, 0f);

        var blocker = (GameObject)BlockerField.GetValue(__instance);
        RegisterCanvas(blocker, ownerCanvas, BlockerDepthBias);
    }

    [PatchPrefix(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Hide))]
    private static void Hide(TMP_Dropdown __instance)
    {
        if (__instance == null)
            return;

        // Read the fields before Hide() destroys the objects so we unregister the exact canvases.
        UnregisterCanvas((GameObject)DropdownField.GetValue(__instance));
        UnregisterCanvas((GameObject)BlockerField.GetValue(__instance));
    }

    private static void RegisterCanvas(GameObject go, Canvas ownerCanvas, float depthBias)
    {
        if (go == null) return;
        var canvas = GetDropdownCanvas(go);
        if (canvas == null) return;

        canvas.worldCamera = APIBus.CockpitHudCamera;

        // Snap onto the owning panel's plane so the cursor sits at the same depth as the rest of
        // the panel instead of jumping when it moves over the freshly-spawned list/blocker.
        if (ownerCanvas != null)
        {
            var t = canvas.transform;
            var local = ownerCanvas.transform.InverseTransformPoint(t.position);
            local.z = 0f;
            t.position = ownerCanvas.transform.TransformPoint(local);
        }

        // canvas.forward faces away from the viewer, so nudging along it pushes this plane behind
        // the list, keeping list items in front where the two overlap.
        if (depthBias != 0f)
            canvas.transform.position += canvas.transform.forward * depthBias;

        VrCanvasHitTester.Register(canvas);
    }

    private static void UnregisterCanvas(GameObject go)
    {
        if (go == null) return;
        var canvas = GetDropdownCanvas(go);
        if (canvas != null)
            VrCanvasHitTester.Unregister(canvas);
    }

    private static Canvas GetDropdownCanvas(GameObject go)
    {
        var canvas = go.GetComponent<Canvas>();
        return canvas != null ? canvas : go.GetComponentInChildren<Canvas>();
    }

    private static bool IsInVrLayerHierarchy(Transform transform)
    {
        var vrLayer = LayerHelper.Layers.VrUi;
        while (transform != null)
        {
            if (transform.gameObject.layer == (int)vrLayer)
                return true;

            transform = transform.parent;
        }

        return false;
    }
}