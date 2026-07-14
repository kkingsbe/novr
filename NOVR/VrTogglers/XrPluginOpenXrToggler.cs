using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace NOVR.VrTogglers;

public class XrPluginOpenXrToggler : XrPluginToggler
{
    protected override XRLoader CreateLoader()
    {
        return ScriptableObject.CreateInstance<OpenXRLoader>();
    }
}
