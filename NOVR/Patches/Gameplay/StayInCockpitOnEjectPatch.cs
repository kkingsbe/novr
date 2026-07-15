using System;
using System.Reflection;
using HarmonyLib;
using NOVR.PatchHelper;
using UnityEngine;

namespace NOVR.HarmonyPatches;

internal static class StayInCockpitOnEjectPatch
{
    private static PilotDismounted? _followedPilot;
    private static CameraPilotFollowState? _followState;

    private static readonly MethodInfo DisableHandlerMethod =
        AccessTools.Method(typeof(CameraStateManager), "Cam_OnFollowingUnitDisabled");

    [PatchPrefix(typeof(CameraStateManager), "SetFollowingUnit")]
    private static bool SetFollowingUnitPrefix(Unit unit)
    {
        var config = ModConfiguration.Instance;
        var cam = SceneSingleton<CameraStateManager>.i;

        if (config == null || !config.StayInCockpitOnEject.Value)
        {
            ResetFollowState(cam);
            return true;
        }

        if (unit is not PilotDismounted pilot)
        {
            ResetFollowState(cam);
            return true;
        }

        if (cam == null) return true;

        if (!UnitRegistry.TryGetUnit<Aircraft>(pilot.parentUnit, out var aircraft) || aircraft == null ||
            !aircraft.LocalSim)
        {
            ResetFollowState(cam);
            return true;
        }

        var target = GetPilotCameraPivot(pilot);
        if (target == null)
        {
            ResetFollowState(cam);
            return true;
        }

        // Replicate the original method's bookkeeping that we're about to skip.
        cam.currentState.LeaveState(cam);
        UnhookDisableHandler(cam, cam.followingUnit);
        cam.previousFollowingUnit = cam.followingUnit;

        _followedPilot = pilot;
        cam.followingUnit = pilot;
        cam.followingRB = pilot.rb;
        HookDisableHandler(cam, pilot);

        var followState = _followState ??= new CameraPilotFollowState();
        cam.SwitchState(followState);
        followState.AttachTo(cam, target);

        Debug.Log("[NOVR] StayInCockpitOnEject: attached eject camera to pilot.");
        return false;
    }

    [PatchPrefix(typeof(CameraStateManager), "Cam_OnFollowingUnitDisabled")]
    private static bool IgnoreStaleDisablesPrefix(Unit unit)
    {
        // While we're following a pilot, only react to that pilot being disabled.
        return _followedPilot == null || unit == _followedPilot;
    }

    [PatchPostfix(typeof(CameraStateManager), "LateUpdate")]
    private static void LateUpdatePostfix(CameraStateManager __instance)
    {
        if (_followedPilot == null || __instance.followingUnit != _followedPilot) return;
        _followState?.DriveToPilot(__instance, _followedPilot);
    }

    private static void HookDisableHandler(CameraStateManager cam, Unit unit)
    {
        if (DisableHandlerMethod == null) return;
        var handler = (Action<Unit>)Delegate.CreateDelegate(typeof(Action<Unit>), cam, DisableHandlerMethod);
        unit.onDisableUnit += handler;
    }

    private static void UnhookDisableHandler(CameraStateManager cam, Unit unit)
    {
        if (unit == null || DisableHandlerMethod == null) return;
        var handler = (Action<Unit>)Delegate.CreateDelegate(typeof(Action<Unit>), cam, DisableHandlerMethod);
        unit.onDisableUnit -= handler;
    }

    private static void ResetFollowState(CameraStateManager? cam)
    {
        if (_followedPilot != null)
        {
            UnhookDisableHandler(cam, _followedPilot);
        }
        _followedPilot = null;
    }

    private static Transform? GetPilotCameraPivot(PilotDismounted pilot)
    {
        return pilot != null ? pilot.transform.Find("cameraPivot") ?? pilot.transform : null;
    }

    private sealed class CameraPilotFollowState : CameraBaseState
    {
        public void AttachTo(CameraStateManager cam, Transform pivot)
        {
            cam.cameraPivot.SetParent(pivot);
            cam.cameraPivot.localPosition = Vector3.zero;
            cam.cameraPivot.localRotation = Quaternion.identity;
            cam.transform.SetParent(cam.cameraPivot);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }

        public void DriveToPilot(CameraStateManager cam, PilotDismounted pilot)
        {
            var target = GetPilotCameraPivot(pilot);
            if (target == null) return;

            cam.cameraPivot.SetPositionAndRotation(target.position, target.rotation);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.cameraVelocity = pilot.rb != null ? pilot.rb.velocity : Vector3.zero;
        }

        public override void EnterState(CameraStateManager cam)
        {
            if (_followedPilot != null)
            {
                var target = GetPilotCameraPivot(_followedPilot);
                if (target != null)
                {
                    AttachTo(cam, target);
                    return;
                }
            }

            // Pilot is gone; fall back to game default behavior.
            cam.cameraPivot.SetParent(null);
            cam.transform.SetParent(null, worldPositionStays: true);
        }

        public override void LeaveState(CameraStateManager cam)
        {
            cam.cameraPivot.SetParent(null);
            cam.transform.SetParent(null, worldPositionStays: true);
            if (_followedPilot != null)
            {
                UnhookDisableHandler(cam, _followedPilot);
                _followedPilot = null;
            }
        }

        public override void UpdateState(CameraStateManager cam)
        {
            if (_followedPilot != null)
            {
                DriveToPilot(cam, _followedPilot);
            }
        }

        public override void FixedUpdateState(CameraStateManager cam)
        {
        }
    }
}