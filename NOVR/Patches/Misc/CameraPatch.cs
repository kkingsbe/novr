using NOVR.PatchHelper;
using UnityEngine;

namespace NOVR.Patches.Misc;

public class CameraPatch
{
    [PatchPrefix(typeof(Camera), "set_fieldOfView")]
    private static bool PreventChangingFov()
    {
        return false;
    }

    [PatchPrefix(typeof(Camera), "set_projectionMatrix")]
    private static bool PreventSettingProjectionMatrix()
    {
        return false;
    }

    [PatchPrefix(typeof(Camera), "SetStereoProjectionMatrix")]
    private static bool PreventSettingStereoProjectionMatrix()
    {
        return false;
    }

    [PatchPrefix(typeof(Camera), "ResetProjectionMatrix")]
    private static bool PreventResettingProjectionMatrix()
    {
        return false;
    }

    [PatchPrefix(typeof(Camera), "set_aspect")]
    private static bool PreventSettingAspect()
    {
        return false;
    }
}
