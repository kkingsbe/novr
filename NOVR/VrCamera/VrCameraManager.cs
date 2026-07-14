#if CPP
using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NOVR.VrCamera;

public class VrCameraManager: MonoBehaviour
{
    private const string NuclearOptionMainCameraName = "Main Camera";
    private const string NuclearOptionMenuCameraName = "Menu Camera";
    private const string VrCameraChildName = "NOVR Main Camera";
    private static readonly string[] TrackedChildNames = {"cockpitRenderer", "postProcessingRenderer"};

    public static HashSet<Camera> IgnoredCameras = new();
    
    private void Update() // Todo: Make me behave on events if possible
    {
        Camera[] cameras = new Camera[Camera.allCamerasCount];
        Camera.GetAllCameras(cameras);

        foreach (var camera in cameras)
        {
            var gameObject = camera.gameObject;
            if (gameObject.name is not (NuclearOptionMainCameraName or NuclearOptionMenuCameraName))
            {
                continue;
            }

            if (gameObject.name == NuclearOptionMainCameraName)
            {
                var existingTrackedCamera = GetTrackedMainCamera(gameObject);
                if (existingTrackedCamera != null)
                {
                    EnsureTrackedMainCameraRig(camera, existingTrackedCamera);
                    continue;
                }
            }

            if (IgnoredCameras.Contains(camera))
            {
                continue;
            }

            if (gameObject.name == NuclearOptionMainCameraName)
            {
                SetUpMainCameraRig(camera);
            }
            else
            {
                HandleChildCameras(camera);
                gameObject.AddComponent<VrCamera>();
                IgnoredCameras.Add(camera);
            }
        }
    }

    public static Camera? GetTrackedMainCamera(GameObject gameCameraRoot)
    {
        var trackedCameraTransform = gameCameraRoot.transform.Find(VrCameraChildName);
        return trackedCameraTransform != null ? trackedCameraTransform.GetComponent<Camera>() : null;
    }

    private void SetUpMainCameraRig(Camera rootCamera)
    {
        HandleChildCameras(rootCamera);

        var existingTrackedCamera = GetTrackedMainCamera(rootCamera.gameObject);
        if (existingTrackedCamera != null)
        {
            EnsureTrackedMainCameraRig(rootCamera, existingTrackedCamera);
            return;
        }

        var trackedCameraObject = new GameObject(VrCameraChildName);
        trackedCameraObject.transform.SetParent(rootCamera.transform, false);
        trackedCameraObject.tag = rootCamera.tag;

        var trackedCamera = trackedCameraObject.AddComponent<Camera>();
        trackedCamera.CopyFrom(rootCamera);
        var additionalCameraData = AdditionalCameraData.Create(trackedCamera);
        additionalCameraData?.SetRenderTypeBase();
        additionalCameraData?.SetAllowXrRendering(true);
        //additionalCameraData.GetCameraStack().AddRange(rootCamera.GetComponent<AdditionalCameraData>().GetCameraStack());

        var universalAdditionalCameraData = trackedCameraObject.GetComponent<UniversalAdditionalCameraData>();
        var rootUniversalAdditionalCameraData = rootCamera.GetComponent<UniversalAdditionalCameraData>();
        
        if (universalAdditionalCameraData != null && rootUniversalAdditionalCameraData != null)
            universalAdditionalCameraData.cameraStack.AddRange(rootUniversalAdditionalCameraData.cameraStack);
        
        
        var rootAudioListener = rootCamera.GetComponent<AudioListener>();
        if (rootAudioListener != null)
        {
            var trackedAudioListener = trackedCameraObject.AddComponent<AudioListener>();
            trackedAudioListener.enabled = rootAudioListener.enabled;
            rootAudioListener.enabled = false;
        }

        rootCamera.tag = "Untagged";
        rootCamera.enabled = false;
        ReparentTrackedChildren(rootCamera.transform, trackedCameraObject.transform);

        trackedCameraObject.AddComponent<VrCamera>();

        IgnoredCameras.Add(rootCamera);
        IgnoredCameras.Add(trackedCamera);
    }

    private static void EnsureTrackedMainCameraRig(Camera rootCamera, Camera trackedCamera)
    {
        if (rootCamera.enabled)
        {
            rootCamera.enabled = false;
        }

        if (rootCamera.CompareTag("MainCamera"))
        {
            rootCamera.tag = "Untagged";
        }

        if (!trackedCamera.enabled)
        {
            trackedCamera.enabled = true;
        }

        if (!trackedCamera.CompareTag("MainCamera"))
        {
            trackedCamera.tag = "MainCamera";
        }

        if (trackedCamera.GetComponent<VrCamera>() == null)
        {
            trackedCamera.gameObject.AddComponent<VrCamera>();
        }

        IgnoredCameras.Add(rootCamera);
        IgnoredCameras.Add(trackedCamera);
    }

    private void HandleChildCameras(Camera parentCamera)
    {
        foreach (var child in parentCamera.GetComponentsInChildren<Camera>())
        {
            if (child != parentCamera && !IgnoredCameras.Contains(child)) 
                child.gameObject.AddComponent<StereoCamera>();
        }
    }

    private void Awake()
    {
        Application.onBeforeRender += OnBeforeRenderPose;
    }

    private void OnDestroy()
    {
        Application.onBeforeRender -= OnBeforeRenderPose;
    }

    private void OnBeforeRenderPose()
    {
        Camera rigCamera = GetActiveTrackedCamera();
        if (rigCamera != null)
        {
            ApplyRigPose(rigCamera);
        }
    }

    private void ApplyRigPose(Camera camera)
    {
        // Rig pose logic extracted from Update
        // Currently VrCameraManager.Update() doesn't have explicit rig pose logic -
        // it discovers cameras and sets up the rig. The OnBeforeRenderPose is added
        // for future use as a hook point. For now, leave ApplyRigPose as a no-op
        // placeholder that subclasses/extensions can fill.
    }

    private static Camera? GetActiveTrackedCamera()
    {
        // Find the tracked main camera
        Camera[] cameras = new Camera[Camera.allCamerasCount];
        Camera.GetAllCameras(cameras);
        foreach (var camera in cameras)
        {
            if (camera != null && camera.CompareTag("MainCamera"))
            {
                return camera;
            }
        }
        return null;
    }

    private static void ReparentTrackedChildren(Transform rootCameraTransform, Transform trackedCameraTransform)
    {
        foreach (var childName in TrackedChildNames)
        {
            var child = rootCameraTransform.Find(childName);
            if (child != null)
            {
                child.SetParent(trackedCameraTransform, false);
            }
        }
    }
}
