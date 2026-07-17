using System;
using HarmonyLib;
using UnityEngine;

namespace NOVR.VrCamera;

internal static class TurretVrCameraPatch
{
    private static readonly AccessTools.FieldRef<Turret, bool> ManualRef = AccessTools.FieldRefAccess<Turret, bool>("manual");
    private static readonly AccessTools.FieldRef<Turret, Unit> TargetRef = AccessTools.FieldRefAccess<Turret, Unit>("target");
    private static readonly AccessTools.FieldRef<Turret, Aircraft> AircraftRef = AccessTools.FieldRefAccess<Turret, Aircraft>("aircraft");
    private static readonly AccessTools.FieldRef<Turret, Unit> AttachedUnitRef = AccessTools.FieldRefAccess<Turret, Unit>("attachedUnit");
    private static readonly AccessTools.FieldRef<Turret, float> LastVectorSentRef = AccessTools.FieldRefAccess<Turret, float>("lastVectorSent");
    private static readonly AccessTools.FieldRef<Turret, WeaponStation> CurrentWeaponStationRef = AccessTools.FieldRefAccess<Turret, WeaponStation>("currentWeaponStation");
    private static readonly AccessTools.FieldRef<Turret, Vector3> ManualVectorRef = AccessTools.FieldRefAccess<Turret, Vector3>("manualVector");
    private static readonly Func<Turret, Vector3, object> AimTurretMethod = AccessTools.MethodDelegate<Func<Turret, Vector3, object>>(AccessTools.Method(typeof(Turret), "AimTurret"));

    [HarmonyPatch(typeof(Turret), "FixedUpdate")]
    private static class FixedUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Turret __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            if (!ManualRef(__instance) ||
                TargetRef(__instance) != null)
            {
                return true;
            }

            var aircraft = AircraftRef(__instance);
            var attachedUnit = AttachedUnitRef(__instance);
            if (aircraft == null ||
                attachedUnit == null ||
                !aircraft.LocalSim ||
                SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.cockpitState)
            {
                return true;
            }

            var vrCamera = APIBus.MainCamera;
            if (vrCamera == null)
            {
                return true;
            }

            __instance.SetVector(vrCamera.transform.forward);

            var lastVectorSent = LastVectorSentRef(__instance);
            if (Time.timeSinceLevelLoad - lastVectorSent > 0.20000000298023224)
            {
                var currentWeaponStation = CurrentWeaponStationRef(__instance);
                var manualVector = ManualVectorRef(__instance);
                aircraft.SetTurretVector(currentWeaponStation.Number, manualVector);
                LastVectorSentRef(__instance) = Time.timeSinceLevelLoad;
            }

            AimTurretMethod(__instance, ManualVectorRef(__instance));
            return false;
        }
    }
}
