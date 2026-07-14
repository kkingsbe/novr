using HarmonyLib;
using NOVR.PatchHelper;
using UnityEngine;

namespace NOVR.HarmonyPatches;

internal static class StayInCockpitOnEjectPatch
{
    [PatchPrefix(typeof(CameraStateManager), "SetFollowingUnit")]
    private static bool SetFollowingUnitPrefix(Unit unit)
    {
        var config = ModConfiguration.Instance;
        if (config == null || !config.StayInCockpitOnEject.Value)
        {
            return true;
        }

        if (unit is not PilotDismounted pilot)
        {
            return true;
        }

        if (!UnitRegistry.TryGetUnit<Aircraft>(pilot.parentUnit, out var aircraft) || aircraft == null)
        {
            return true;
        }

        if (!aircraft.LocalSim)
        {
            return true;
        }

        var cam = SceneSingleton<CameraStateManager>.i;
        if (cam == null)
        {
            return true;
        }

        var traverse = Traverse.Create(cam);
        traverse.Field("previousFollowingUnit").SetValue(cam.followingUnit);
        traverse.Field("followingUnit").SetValue(aircraft);
        traverse.Field("followingRB").SetValue(aircraft.rb);

        cam.SwitchState(cam.cockpitState);

        Debug.Log("[NOVR] StayInCockpitOnEject: redirected eject camera back to cockpit.");
        return false;
    }
}
