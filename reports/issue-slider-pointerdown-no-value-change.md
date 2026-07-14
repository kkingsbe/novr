# Unity UI Slider: `OnPointerDown` and `OnDrag` are dispatched but `Slider.value` does not change

## Context

I'm working on **NOVR**, a VR mod (BepInEx + Harmony) for the Unity game *Nuclear Option*. The mod replaces the game's flat UI with one that works in VR. We ray-cast from a virtual controller pose through registered Canvases, find the hit UI element, and synthesize `PointerEventData` ourselves rather than going through Unity's `StandaloneInputModule`.

Everything works for buttons, dropdowns, toggles, scroll views, and direct cursor manipulation — except **Sliders**. The user can see the cursor over the fuel slider in the airbase pre-flight menu, pull the trigger on the track, grab the handle, and drag — and the slider's `value` does not move (stays at the default `1f` set by `Refresh()`).

## Environment

- Unity 2022.3.62, IL2CPP=false (Mono).
- `UnityEngine.UI.Slider` (standard UGUI, no custom subclass).
- The Slider is a child of `GameplayUICanvas` (render-mode `WorldSpace`, plane in 3D at world Z=3 in front of the player camera).
- The Slider's GameObject hierarchy is:
  ```
  FuelSelector
    └─ FuelSlider [Slider, RectTransform, Image]
         ├─ Background [Image]
         ├─ Fill Area
         │    └─ Fill [Image]
         └─ Handle Slide Area
              └─ Handle [Image]
  ```
  `Slider.interactable = true`. Default `value = 1`. `minValue = 0`, `maxValue = 1`, `wholeNumbers = false`, `direction = LeftToRight`.

- `Screen.width = 2880` (display target; `Camera.pixelWidth` for the UI camera used in `WorldToScreenPoint` is the same).
- The game is paused/loaded enough that the bridge is up and `EventSystem.current` is non-null.

## Symptoms (from BepInEx diagnostic logs)

We instrumented `VrUiCursor.FirePointerEvents` to log every relevant frame. While the user pulled the trigger on the fuel slider track, dragged the handle, and clicked on the fill, the log contained (abbreviated):

```
[VrUiCursorDiag] raycast hit 'Background' -> root='FuelSlider' (canvas=GameplayUICanvas, isLeftDown=False)
[VrUiCursorDiag] pointerDown on 'FuelSlider' (root) — hit 'Fill',
                  screenPos=(2592.95, 616.97), slider.value=1 interactable=True
[VrUiCursorDiag]   AFTER pointerDown, slider.value=1
[VrUiCursorDiag] raycast hit 'Fill' -> root='FuelSlider' (canvas=GameplayUICanvas, isLeftDown=True)
[VrUiCursorDiag] drag on 'FuelSlider' delta=(-4.80, -2.49) pos=(2676.21, 593.05) slider.value=1
[VrUiCursorDiag] pointerDown on 'FuelSlider' (root) — hit 'Background',
                  screenPos=(2717.43, 617.73), slider.value=1 interactable=True
[VrUiCursorDiag]   AFTER pointerDown, slider.value=1
[VrUiCursorDiag] raycast hit 'Background' -> root='FuelSlider' (canvas=GameplayUICanvas, isLeftDown=True)
[VrUiCursorDiag] drag on 'FuelSlider' delta=(-2.05, -0.75) pos=(2665.25, 596.83) slider.value=1
```

Key observations:

1. **The raycast reaches the slider** — the graphic raycaster returns the Slider's `Background` / `Fill` / `Handle` GameObject correctly.
2. **The event "root" resolves to `FuelSlider`** — `ExecuteEvents.ExecuteHierarchy(FuelSlider, ped, ExecuteEvents.pointerDownHandler)` is called, which should reach the `Slider` component (which implements `IPointerDownHandler` and `IDragHandler`).
3. **`pointerDown` is dispatched** — the diagnostic logs right before and after `ExecuteHierarchy` show we are calling it with a non-null `ped` whose `button = Left`, `position` matches the raycast screen point, and `pressPosition` is set.
4. **`drag` is dispatched** — when the user holds the trigger and moves, drag events fire with non-zero `ped.delta` (e.g. `(-4.80, -2.49)`).
5. **`Slider.value` does not change** — both before and after every `pointerDown`, `slider.value` reads `1`. Drag events don't change it either.

In other words: from `Slider`'s point of view, the `OnPointerDown`/`OnDrag` calls happen (we know because the diagnostic code itself is running, and nothing throws), but `Slider` itself does not mutate its `value`.

## Code path

`VrUiCursor.FirePointerEvents(Vector2 screenPoint, bool isLeftDown)` synthesizes and dispatches events. The relevant excerpts:

```csharp
private void FirePointerEvents(Vector2 screenPoint, bool isLeftDown)
{
    var es = EventSystem.current;
    if (es == null) { ... return; }
    if (_activeCanvas == null || !_hasActiveCanvas) { ... return; }

    var raycaster = _activeCanvas.GetComponent<GraphicRaycaster>();
    if (raycaster == null) { ... return; }

    var ped = _pointerEventData ??= new PointerEventData(es);

    ped.delta    = screenPoint - ped.position;       // edited from Vector2.zero
    ped.position = screenPoint;
    ped.button   = PointerEventData.InputButton.Left;
    if (_pointerPress == null && !_wasLeftDown && !isLeftDown)
        ped.pressPosition = screenPoint;             // edited from unconditional set

    var results = new List<RaycastResult>();
    raycaster.Raycast(ped, results);

    GameObject? current = null;
    if (results.Count > 0)
        current = GetEventRoot(results[0].gameObject);   // -> 'FuelSlider' (after fix)
    ped.pointerCurrentRaycast = results[0];

    // ... hover/enter/exit handling ...

    if (isLeftDown)
    {
        if (!_wasLeftDown)
        {
            _pointerPress   = current;
            ped.pressPosition = screenPoint;
            ped.pointerPress  = current;
            ped.clickTime    = Time.unscaledTime;
            ped.clickCount   = 1;
            if (current != null)
                ExecuteEvents.ExecuteHierarchy(current, ped, ExecuteEvents.pointerDownHandler);
        }
        else if (_pointerPress == current)
        {
            ExecuteEvents.ExecuteHierarchy(_pointerPress, ped, ExecuteEvents.dragHandler);
        }
    }
    else if (_wasLeftDown) { /* pointerUp / pointerClick */ _pointerPress = null; ped.pressPosition = default; }

    _wasLeftDown = isLeftDown;
}
```

`GetEventRoot` (after the fix):

```csharp
private static GameObject? GetEventRoot(GameObject? obj)
{
    if (obj == null) return null;
    Transform t = obj.transform;
    Transform? selectableRoot = null;
    while (t != null)
    {
        if (t.GetComponent<IPointerClickHandler>() != null) return t.gameObject;
        if (selectableRoot == null && t.GetComponent<Selectable>() != null) selectableRoot = t;
        t = t.parent;
    }
    return selectableRoot != null ? selectableRoot.gameObject : obj;
}
```

The screen point comes from:

```csharp
public Vector2 GetScreenPoint()
{
    var camera = UiCamera;
    if (_cursor == null || camera == null) { _isOffscreen = true; return Vector2.zero; }

    Vector3 screenPoint = camera.WorldToScreenPoint(_cursor.transform.position);

    if (screenPoint.z <= 0f) { _isOffscreen = true; return Vector2.zero; }

    _isOffscreen = false;
    return new Vector2(
        Mathf.Clamp(screenPoint.x, 0f, camera.pixelWidth),
        Mathf.Clamp(screenPoint.y, 0f, camera.pixelHeight));
}
```

The cursor's world position is set by:

```csharp
if (VrCanvasHitTester.RaycastCanvasPlanes(probeRay, out var hit))
{
    _cursor.transform.position = hit.WorldPoint;
    var rt = hit.Canvas.GetComponent<RectTransform>();
    _cursor.transform.rotation = Quaternion.LookRotation(rt.forward, rt.up);
}
```

where `VrCanvasHitTester.RaycastCanvasPlanes` projects the controller ray onto the canvas's infinite plane and verifies a `GraphicRaycaster` actually finds a graphic at the projected local UV.

## Things we have already ruled out

- `pointerDown` is **not** being sent to the wrong object — the diagnostic confirms `root='FuelSlider'` and the log shows it is reached via `ExecuteHierarchy`.
- `Slider` is **not** non-interactable — log shows `interactable=True`.
- `Slider` is **not** silently disabled — log shows it's active and reachable.
- The cursor **does** land on different parts of the slider (`Background`, `Fill`, `Handle`) on different presses — so the raycast is reading the cursor position correctly.
- This is not a "missing listener" problem: even setting `Slider.value` requires no listener, and a listener wouldn't keep `value` pinned at `1` anyway.
- Mouse mode and controller mode behave identically (mouse mode goes through the same `FirePointerEvents` path).
- Pressing on the *Fill* (the colored bar) also does nothing — `Slider.OnPointerDown`'s `MayDrag` returns false for fills in some configurations but the *Background* (track) is supposed to always be draggable.

## Things we have NOT yet ruled out / suspects

- The `ped` we pass has `button = Left`, but is `ped.pressEventCamera` set? `Slider.OnPointerDown` uses `eventData.pressEventCamera` (via `UpdateDrag`) to convert the screen position into the slider's local RectTransform coords. We never set `pressEventCamera` on our synthesized `PointerEventData`. **Unity's standard `StandaloneInputModule` sets this to the EventCamera that processed the raycast.**
- `ped.position` is in *physical* screen pixels (e.g. `(2592, 616)` for a 2880×... display), but `GraphicRaycaster` and `Slider.OnPointerDown` interpret `eventData.position` in the canvas's *reference resolution* / *render-target* pixel space if the canvas is a ScreenSpaceOverlay, or in the canvas camera's pixel space if WorldSpace. The fuel slider's canvas is `WorldSpace` with `worldCamera = NOVR Main Camera`. If `ped.position` is in physical pixels but the canvas's `pixelRect` is in reference pixels, the conversion to the slider's local RectTransform could land at a position outside the slider's range — causing `MayDrag` / value math to clamp the result to the existing `value`.
- `Slider.OnPointerDown` sets `eventData.useDragThreshold = false` on its own, but if our `OnPointerDown` ordering fires something first that resets it, drag-thresholding could suppress the click. (Probably a red herring — we don't set it.)
- The mouse-position-based screen point calculation might be in a coordinate space the GraphicRaycaster does not recognize for this particular canvas — e.g. `Camera.pixelWidth` returns the wrong number if `Camera.targetTexture` is set, but the GraphicRaycaster uses the canvas's own RectTransform rect.
- The SelectionMenu canvas may be a *nested* canvas under a root whose pixel rect differs. We only set `_activeCanvas = hit.Canvas` (the deepest hit), but `GraphicRaycaster.Raycast` uses the root canvas's settings.

## Minimal failing scenario

A `UnityEngine.UI.Slider` (interactable, value=1, min=0, max=1) is placed in a `WorldSpace` Canvas whose `worldCamera` is the player's Main Camera. A custom input pipeline synthesizes `PointerEventData` like this:

```csharp
var ped = new PointerEventData(EventSystem.current)
{
    position    = camera.WorldToScreenPoint(cursorWorldPos),
    pressPosition = screenPoint,
    button      = PointerEventData.InputButton.Left,
    delta       = screenPoint - ped.position,
};
var results = new List<RaycastResult>();
canvas.GetComponent<GraphicRaycaster>().Raycast(ped, results);   // returns Slider's child
ExecuteEvents.ExecuteHierarchy(sliderGO, ped, ExecuteEvents.pointerDownHandler);
```

After the call, `slider.value` is unchanged.

`Slider.OnPointerDown` is supposed to update `value` based on `eventData.position` converted into the slider's RectTransform local space. The conversion path inside Slider is roughly:

```csharp
Vector2 localPos;
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    sliderRectTransform, eventData.position, eventData.pressEventCamera, out localPos);
```

If `eventData.pressEventCamera` is `null` (which it is in our case), `ScreenPointToLocalPointInRectangle` returns false / falls back, and the resulting local position may be at `(0, 0)` of the slider RectTransform — which corresponds to the *start* of the slider — and at `value=1` the start position is *already* where the handle sits, so the setter sees "new value equals old value" and (depending on implementation) may not even call the onValueChanged event. This is consistent with the symptom that `value` stays exactly at `1`.

So the leading hypothesis is: **`ped.pressEventCamera` is not being set**, and Slider's coordinate conversion is silently no-op'ing.

## Questions for review

1. Does Unity's `Slider.OnPointerDown` require `eventData.pressEventCamera` to be non-null to compute the new value?
2. If so, what is the correct value for a custom (non-StandaloneInputModule) input pipeline that drives a `WorldSpace` Canvas?
3. Are there other `PointerEventData` fields (e.g. `enterEventCamera`, `pointerCurrentRaycast.module`) that Unity's UGUI components consult?
4. Is there a known-good idiom for "synthesize PointerEventData from scratch and dispatch it to UGUI components on a WorldSpace Canvas" that we should be following instead?
5. Could the screen position units be the actual issue — i.e. Unity expects the position in canvas-rect pixels rather than camera-pixel pixels for WorldSpace canvases?
6. Could `GraphicRaycaster.Raycast` returning a non-null result but with an empty/missing `screenPosition` in the `RaycastResult` be silently failing the downstream `Slider.OnPointerDown` conversion?

## Files

- `NOVR/VrUi/VrUiCursor.cs` — owns `FirePointerEvents`, `GetScreenPoint`, `GetEventRoot` (with our recent edits).
- `NOVR/VrUi/VrCanvasHitTester.cs` — owns the ray-cast that produces the cursor world position.
- `NOVR/NOVRPlugin.cs` — owns `NOVRPlugin.LogSource` (the logger we use for diagnostics).