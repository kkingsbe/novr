using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRHurtOverlayBehavior : MonoBehaviour
{
    private Canvas? _canvas;
    private RectTransform? _rectTransform;
    private static readonly Vector2 HurtOverlaySize = new(5f, 5f);

    private void Awake()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) return;

        LayerHelper.SetLayerRecursive(transform, LayerHelper.GetVrUiLayer());
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = APIBus.CockpitHudCamera;

        _rectTransform = (RectTransform)transform;
        _rectTransform.sizeDelta = HurtOverlaySize;
    }

    private void Update()
    {
        var hudCam = APIBus.CockpitHudCamera;
        if (hudCam == null) return;

        var hudCamTransform = hudCam.transform;
        transform.rotation = hudCamTransform.rotation;
        transform.position = hudCamTransform.position + hudCamTransform.forward;
    }
}