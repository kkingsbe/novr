using System;
using System.Reflection;
using System.Text.RegularExpressions;
using NOVR.VrCamera;
using NOVR.VrTogglers;
using NOVR.VrUi;
using NOVR.VrUi.Native;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace NOVR;

public class Core : MonoBehaviour
{
    private static bool _isApplicationQuitting;
    private static readonly Regex NonAlphanumRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);
    
    private NOVRHeadsetData? _headsetData;
    private NOUIManager? _vrUi;
    private VrTogglerManager? _vrTogglerManager;
    
    private Aircraft _aircraft;
    private Aircraft _oldAircraft;

    public static string CurrentAircraftId { get; private set; }

    public static void Create()
    {
        new GameObject("NOVR").AddComponent<Core>();
    }

    private void Awake()
    {
        Application.quitting -= HandleApplicationQuitting;
        Application.quitting += HandleApplicationQuitting;
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<VrCameraManager>();
        gameObject.AddComponent<APIBus>();
if (NOVRPlugin.LogSource != null)
            NOVRPlugin.LogSource.LogMessage($"[Core Awake] New Core instance created. name={name}");
        EnsureNativeMenuEnvironmentAssetCache();
    }

    private static void HandleApplicationQuitting()
    {
        _isApplicationQuitting = true;
    }

    private void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (NOVRPlugin.LogSource != null)
            NOVRPlugin.LogSource.LogMessage("[Core OnDestroy] NOVR has been destroyed. This shouldn't have happened. Recreating...");
        if (_isApplicationQuitting) return;

        Debug.Log("NOVR has been destroyed. This shouldn't have happened. Recreating...");

        Create();
        OpenXrControllerProfileBootstrap.ClearInjectedProfiles();
    }

    private void Start()
    {
        EnsureNativeMenuEnvironmentAssetCache();

        _headsetData = NOVRBehaviour.Create<NOVRHeadsetData>(transform);
        _vrUi = NOVRBehaviour.Create<NOUIManager>(transform);

        if (ModConfiguration.Instance.LogXrStartupDiagnostics.Value &&
            gameObject.GetComponent<XrStartupDiagnosticsBehaviour>() == null)
        {
            gameObject.AddComponent<XrStartupDiagnosticsBehaviour>();
        }
        
        _vrTogglerManager = new VrTogglerManager();

        if (ModConfiguration.Instance.EnableExperimentalSteamVrControllerProfiles.Value)
        {
            OpenXrControllerProfileBootstrap.EnsureRequired();
        }

        ModConfiguration.Instance.EnableExperimentalSteamVrControllerProfiles.SettingChanged += (_, _) =>
        {
            Debug.Log("[NOVR] EnableExperimentalSteamVrControllerProfiles changed. Restart required to take effect.");
        };
    }



    private void Update()
    {
        EnsureNativeMenuEnvironmentAssetCache();

        if (ModConfiguration.Instance.RecenterShortcut.Value != KeyCode.None &&
            Input.GetKeyDown(ModConfiguration.Instance.RecenterShortcut.Value))
        {
            NOVRHeadsetData.RecenterCockpitSeat();
        }
    }

    private void EnsureNativeMenuEnvironmentAssetCache()
    {
        if (NativeMenuEnvironmentAssetCache.Instance != null ||
            gameObject.GetComponent<NativeMenuEnvironmentAssetCache>() != null)
        {
            return;
        }

        gameObject.AddComponent<NativeMenuEnvironmentAssetCache>();
    }
    private void FixedUpdate()
    {
        _oldAircraft = _aircraft;
        GameManager.GetLocalAircraft(out _aircraft);
        if (_aircraft != _oldAircraft)
        {
            CurrentAircraftId = ResolveAircraftId(_aircraft);
            if (ModConfiguration.Instance.TryGetSavedOffset(CurrentAircraftId, out var f, out var r))
            {
                ModConfiguration.Instance.CockpitHeadForwardOffset.Value = f;
                ModConfiguration.Instance.CockpitHeadRightOffset.Value = r;
            }
            NOVRHeadsetData.CalibrateTranslation();
        }
        CameraStateManager.enableMouseLook = false;
    }

    private static string ResolveAircraftId(Aircraft aircraft)
    {
        if (aircraft == null || aircraft.definition == null) return null;
        return NonAlphanumRegex.Replace(aircraft.definition.name, "_");
    }

}
