using UnityEngine;

namespace NOVR.VrCamera;

public class StereoCamera : MonoBehaviour
{
    public Camera? ParentCamera { get; private set; }
    public Camera? CameraInUse
    {
        get
        {
            return ParentCamera;
        }
    }

    protected void Awake()
    {
        ParentCamera = GetComponent<Camera>();
    }
}
