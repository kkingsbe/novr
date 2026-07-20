using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRHMDBehavior : UIRenderedCanvasBehavior
{
    private float _offset = 850;

    private Transform _speed;
    private Transform _altitude;
    private Transform _bearing;
    private Transform _artificialHorizon;

    private static readonly Vector3 SpeedLocal = new(-110f, 150f, 0f);
    private static readonly Vector3 AltitudeLocal = new(110f, 150f, 0f);
    private static readonly Vector3 BearingLocal = new(0f, 200f, 0f);
    private static readonly Vector3 ArtificialHorizonLocal = new(0f, 150f, 0f);

    public override void Awake()
    {
        base.Awake();
        _speed = FindChildRecursive(transform, "Speed");
        _altitude = FindChildRecursive(transform, "Altitude");
        _bearing = FindChildRecursive(transform, "Bearing");
        _artificialHorizon = FindChildRecursive(transform, "Artificial Horizon");
    }

    private void Update()
    {
        var uiCam = APIBus.CockpitHudReference;
        transform.position = uiCam.transform.forward * _offset;
        transform.rotation = uiCam.transform.rotation;

        if (_speed != null) _speed.localPosition = SpeedLocal;
        if (_altitude != null) _altitude.localPosition = AltitudeLocal;
        if (_bearing != null) _bearing.localPosition = BearingLocal;
        if (_artificialHorizon != null) _artificialHorizon.localPosition = ArtificialHorizonLocal;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            var nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}