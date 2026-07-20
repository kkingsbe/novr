# NOVR GPU/CPU Optimization Analysis

**Project:** NOVR (Nuclear Option Virtual Reality Mod)
**Scope:** Performance optimization analysis of the BepInEx VR mod for Nuclear Option
**Date:** 2026-07-19
**Files analyzed:** ~50 core files across NOVR/, NOVR/VrCamera/, NOVR/VrUi/, NOVR/Patches/, NOVR.XR.OpenXR/ (sampled)

---

## Executive Summary - Top 15 Optimization Opportunities (Ranked by Impact)

| # | Issue | File(s) | CPU Impact | GPU Impact | Risk |
|---|---|---|---|---|---|
| 1 | **Duplicate per-frame head pose reads** - NOVRHeadsetData.UpdateTransform() and NOVRPoseDriver.UpdateTransform() run in Update, LateUpdate, AND OnBeforeRender (3x per frame) | NOVRHeadsetData.cs:103-117, NOVRPoseDriver.cs:36-44 | High | Low | Low |
| 2 | **Canvas stack re-validation every frame** - NOUIManager.EnforceClippedCameraStackPosition() scans cameraStack every Update; ConfigureUiCameras() re-applies identical state every frame | NOUIManager.cs:87-91, 170-216 | Medium | Low | Medium |
| 3 | **Camera stack AddRange on every rig setup** - VrCameraManager.SetUpMainCameraRig copies stack every time it is invoked | VrCameraManager.cs:122-123 | Low (one-shot) | Medium (extra overlay cameras) | Medium |
| 4 | **Resources.FindObjectsOfTypeAll<GameObject>() in ScanForMainMenuCanvas** - scans every loaded scene every 0.5s | NativeVrUiRoot.cs:660-687 | High | None | Low |
| 5 | **Resources.FindObjectsOfTypeAll(typeof(GameObject)) in UIBehaviorPatcher.FixedUpdate** - full scene scan when scene dirty | UIBehaviorPatcher.cs:104-156 | High | None | Medium |
| 6 | **Per-frame Camera.main queries** in hot path | VrControllerInput.cs:189, VrUiCursor.cs (via APIBus), HUDBoresightStatePatch.cs:24, ObjectiveOverlayPatch.cs:22, AirbaseOverlayPatch.cs:27 | Medium | None | Low |
| 7 | **GetComponentInChildren<Camera> allocations in ScanForCameras** - runs on every scene load | VrCameraManager.cs:177, 197 | Medium | Low | Low |
| 8 | **VrCanvasHitTester redundant GetComponent<GraphicRaycaster> calls** - called per canvas per raycast | VrCanvasHitTester.cs:121, 304 | Medium | None | Low |
| 9 | **ForwardMapClickIfNeeded uses FindObjectsOfType<MapIcon> per click** - O(N) scan + camera projection per icon | VrUiCursor.cs:862 | Medium | None | Low |
| 10 | **Multiple canvas Update writes identical transforms** - NOVRHurtOverlayBehavior, NOVRBlackoutCanvasBehavior, NoVrHudBehavior, NOVRStatusDisplayBehavior all set position+rotation every frame even when camera/anchor is static | Components/NOVRHurtOverlayBehavior.cs:19-30, NOVRBlackoutCanvasBehavior.cs:29-34, NoVrHudBehavior.cs:7-11 | Low-Medium | None | Low |
| 11 | **FindChildStartingWith / FindChildRecursive per Update on HUD children** | NOVRHMDBehavior.cs:23, PitchCompassBehavior.cs | Medium | None | Low |
| 12 | **Camera.GetAllCameras allocates a new array on every scan** | VrCameraManager.cs:52 | Low | None | Low |
| 13 | **UpdateSmoothedPosition + CockpitHudReference.SetParent cascade** - mutates parent in Update every frame | NOUIManager.cs:96-99 | Low | None | Medium |
| 14 | **String interpolation in diagnostics paths** - already gated behind VerboseDiagnostics flag | VrUiCursor.cs:309-325, VrControllerLaser.cs:54-58, 105-127 | Low (gated) | None | Low |
| 15 | **Camera[] allCameras scratch + Resources.FindObjectsOfTypeAll patterns** are pervasive; suggest consolidating to a single cached Cameras registry | Multiple | Medium | None | Medium |

### Highest-leverage wins

The four biggest wins, in order:

1. **Eliminate the 3x per-frame head pose update** (#1) - OnBeforeRender already fires once per XR frame; running it in Update and LateUpdate is pure waste.
2. **Cache Camera.main and game objects in APIBus** (#6) - Camera.main does a tag-search every call; replace with cached reference.
3. **Stop the per-frame cameraStack enforcement** (#2) - the stack is mutated rarely (scene load + camera change).
4. **Replace Resources.FindObjectsOfTypeAll<GameObject>() in NativeVrUiRoot** (#4) - this is the worst offender, scanning every loaded scene every 0.5s.

---

## Detailed Analysis

### 1. Per-frame Head Pose Updates (HIGH CPU)


NOVRHeadsetData overrides OnBeforeRender AND Update AND LateUpdate, each calling UpdateTransform() which does a tracked-device lookup. NOVRPoseDriver has the same triple-fire pattern.

**Fix:** Subscribe only to OnBeforeRender (which fires once per XR frame, before the eyes render). Remove Update and LateUpdate overrides.

```csharp
protected override void OnBeforeRender()
{
    base.OnBeforeRender();
    UpdateTransform();
}
// Delete Update() and LateUpdate()
```

**Impact:** CPU High (3x -> 1x per-frame work); also eliminates InputDevices.GetDeviceAtXRNode cache thrash. **GPU impact:** Low.

**Risk:** Low. OnBeforeRender is the standard VR pose-update hook.

---
### 2. `NOUIManager` Per-frame Camera Stack Enforcement (MEDIUM CPU)

**File:** `NOVR/VrUi/NOUIManager.cs:87-216`

`ConfigureUiCamera`/`ConfigureClippedHudCamera` set `clearFlags`, `backgroundColor`, `targetTexture`, `nearClipPlane`, `farClipPlane`, `rect` every frame - values set once at camera creation that never change.

`EnforceClippedCameraStackPosition` (L180-216) walks `cameraStack`, finds the cockpit renderer index, removes+re-inserts the clipped HUD camera if its position is wrong. Needed once after `OnMainCameraChanged`, then never again unless the game mutates the stack.

**Fix:**
1. Remove `ConfigureUiCamera`/`ConfigureClippedHudCamera` calls from `Update`. Run them once in `Start`.
2. Move `EnforceClippedCameraStackPosition` into the `OnMainCameraChanged` callback plus a one-shot `DelayedStackEnforcement()` coroutine that polls every 0.25s for the first 2s after a camera change.

**Impact:** CPU Medium. **Risk:** Medium - the game could mutate the stack at any time. Mitigation: keep the poll alive for a longer window.

---

### 3. `VrCameraManager.SetUpMainCameraRig` - Camera Stack `AddRange` (MEDIUM GPU)

**File:** `NOVR/VrCamera/VrCameraManager.cs:97-142`

This copies the entire camera stack from root camera into tracked camera. URP then renders every camera in the stack per eye (stack of N cameras = 2N stereo render passes).

**GPU cost:** Each overlay camera in a stereo-rendered base camera renders twice (left + right eye). For a typical cockpit + post-processing overlay stack, this is 4 extra stereo renders per frame.

**Fix:** Do not copy the camera stack - rely on `OnMainCameraChanged` to append VR cockpit HUD + clipped HUD cameras as new overlays.

**Risk:** Medium - removing the copy might break the cockpit renderer expected stack.

---

### 4. `Resources.FindObjectsOfTypeAll<GameObject>` in `NativeVrUiRoot` (HIGH CPU)

**File:** `NOVR/VrUi/Native/NativeVrUiRoot.cs:660-687`

`Resources.FindObjectsOfTypeAll<GameObject>()` returns every loaded GameObject including assets, prefabs, hidden objects, editor objects. One of the most expensive Unity APIs. Called every `MainMenuScanIntervalSeconds = 0.5f` (line 18).

`UIBehaviorPatcher.cs:139` uses the same pattern.

**Fix:** Hook `SceneManager.sceneLoaded` and only scan the new scene root GameObjects.

**Impact:** CPU High. GPU None. **Risk:** Low.

---

### 5. `UIBehaviorPatcher.FixedUpdate` - GameObject Sweep + Component Adds (HIGH CPU)

**File:** `NOVR/VrUi/UIBehaviorPatcher.cs:104-157`

Three issues:
1. `Resources.FindObjectsOfTypeAll(typeof(GameObject))` - same as #4.
2. Allocates a new array every cache rebuild (line 139) - large managed allocation, GC pressure.
3. Called every `FixedUpdate` (default 50Hz). Linear scan over `_cachedAllObjects` still runs every FixedUpdate until `_toPatch_name` is empty.

`Debug.Log` allocates strings every call (L119, L150) - these add up across all the components added during scene load.

**Fix:**
- Replace `Resources.FindObjectsOfTypeAll<GameObject>()` with `SceneManager.GetActiveScene().GetRootGameObjects()`.
- Wrap the `Debug.Log` calls in `#if DEBUG` or a `VerboseDiagnostics` flag.
- Move the patching work out of `FixedUpdate` into a one-shot coroutine triggered by `sceneLoaded`.

**Impact:** CPU High during scene load; Medium continuously. **Risk:** Medium.

---

### 6. `Camera.main` Per-frame Queries (MEDIUM CPU)

`Camera.main` is implemented internally as `Object.FindObjectWithTag("MainCamera")` - a scene-wide search each call.

**Call sites:**
- `VrControllerInput.cs:189` (`RefreshRig()`) - once per frame
- `APIBus.cs:35` - every Update
- `HUDUnitMarkerPatch.cs:39`, `ObjectiveOverlayPatch.cs:22`, `HUDBoresightStatePatch.cs:24` (via `APIBus.MainCamera`)

**Note:** `APIBus.Update()` already caches `_previousMainCamera` and only fires `OnMainCameraChanged` when it changes. The pattern is good for change detection, but every patch reading `APIBus.MainCamera` is paying for the same `Camera.main` lookup every frame.

**Fix:** Cache `Camera.main` once per frame in a static field updated at the start of Update from a known single source (e.g. `APIBus.Update`).

**Impact:** CPU Medium. **Risk:** Low.

---

### 7. `VrCanvasHitTester` Redundant `GetComponent` (MEDIUM CPU)

**File:** `NOVR/VrUi/VrCanvasHitTester.cs:121, 304`

`GraphicRaycaster` is added once at canvas creation and never destroyed. Calling `GetComponent` per raycast (every cursor frame, multiple times) is wasteful. `RaycastCanvasPlanes` calls `HasGraphicAtPoint` per registered canvas (line 188, 262), and there can be 5-10 canvases registered.

**Fix:** Maintain a parallel `Dictionary<Canvas, GraphicRaycaster>` updated by `Register`/`Unregister`.

**Impact:** CPU Medium. **Risk:** Low.

---

### 8. `VrUiCursor.ForwardMapClickIfNeeded` - `FindObjectsOfType<MapIcon>` Per Click (MEDIUM CPU)

**File:** `NOVR/VrUi/VrUiCursor.cs:830-889`

Per click: scan every MapIcon, do a camera projection + rect math for each. For a busy map (50+ icons), this is 50 projections + 50 `RectTransformUtility` calls.

**Fix:**
- Cache the MapIcon list when the map opens; refresh only when icons are added/removed.
- Use squared distance threshold instead of `RectTransformUtility.ScreenPointToLocalPointInRectangle`.

**Impact:** CPU Medium when map is open and click fires. **Risk:** Low.

---

### 9. `VrCameraManager.ScanForCameras` - Array Allocation (LOW CPU)

**File:** `NOVR/VrCamera/VrCameraManager.cs:50-89`

`Camera[] cameras = new Camera[Camera.allCamerasCount];` allocates per call.

These run only on scene load, so impact is bounded. But it is still GC pressure on every scene transition.

**Fix:** Use `Camera.allCameras` (returns cached array).

**Impact:** Low (scene load only). **Risk:** Low.

---
### 10. Per-frame Canvas Transform Writes (LOW-MEDIUM CPU)

**Files:**
- `NOVR/VrUi/Components/NOVRHurtOverlayBehavior.cs:19-30`
- `NOVR/VrUi/Components/NOVRBlackoutCanvasBehavior.cs:29-34`
- `NOVR/VrUi/Components/NoVrHudBehavior.cs:7-11`
- `NOVR/VrUi/Components/NOVRStatusDisplayBehavior.cs:7-10`
- `NOVR/VrUi/Components/PositionZeroBehavior.cs:7-10`

Pattern: `transform.rotation = hudCam.transform.rotation` and `transform.position = ...` every frame.

`PositionZeroBehavior` is particularly egregious: `transform.position = new Vector3(0,0,0)` every frame, even when the floating origin does not shift.

**Fix:**
- For overlay behaviors: cache the camera reference once; update transform only when the camera has actually moved.
- For `PositionZeroBehavior`: only write `transform.position` when `FloatingOriginPatch` actually shifts the origin.

**Impact:** CPU Low-Medium. **Risk:** Low to Medium.

---

### 11. `FindChildRecursive` / `FindChildStartingWith` in Update (MEDIUM CPU)

**Files:**
- `NOVR/VrUi/Components/NOVRHMDBehavior.cs:21-30` (4x per Update, each a recursive traversal)
- `NOVR/VrUi/Components/UIRenderedCanvasBehavior.cs:56-74`
- `NOVR/VrUi/Components/NOVRFlightHudBehavior.cs:11, 14, 23, 46, 71, 86, 96`

**Fix:** Cache the child Transforms in `Awake`.

**Impact:** CPU Medium. **Risk:** Low.

---

### 12. `NOUIManager.UpdateSmoothedPosition` - Smoothed Reference Mutation (LOW CPU)

**File:** `NOVR/VrUi/NOUIManager.cs:93-99`

Sets the smoothed reference position/rotation every frame. Setting `transform.position` invalidates parent chain dirty flags. Smoothing should be on the consumer side.

**Fix:** Apply smoothing in the consumer (`NOVRHMDBehavior`, etc.) and delete `UpdateSmoothedPosition`.

**Impact:** CPU Low. **Risk:** Medium - changes visual feel slightly.

---

### 13. `VrUiCursor` - String Concatenation in Diagnostics (LOW CPU, GC)

**File:** `NOVR/VrUi/VrUiCursor.cs:237-251, 309-325`

The 1s throttle makes this rare, but every frame still pays for the property dispatch and field reads when `VerboseDiagnostics` is on. Gated correctly. **No fix needed.**

---

### 14. `VrCanvasHitTester.RaycastCanvasPlanes` - Repeated `_candidateBuffer.Sort` (LOW CPU)

**File:** `NOVR/VrUi/VrCanvasHitTester.cs:184, 256`

`candidates.Sort((a, b) => a.distance.CompareTo(b.distance))` - uses a delegate allocation per sort.

**Fix:** Cache the Comparison delegate as a static field.

**Impact:** Low. **Risk:** Low.

---

### 15. `VrUiCursor.FirePointerEvents` - Repeated Raycaster Allocation (MEDIUM CPU)

**File:** `NOVR/VrUi/VrUiCursor.cs:106, 360-380, 467-484`

`GetEventRoot` walks up parents calling `GetComponent<IPointerClickHandler>()` and `GetComponent<Selectable>()` per ancestor. Per pointer hit, this is O(depth) with 2 `GetComponent` per node.

**Fix:** Cache the Selectable root per object using a `Dictionary<int, GameObject>` keyed by instance ID, invalidated on scene unload.

**Impact:** CPU Medium when cursor moves. **Risk:** Low.

---

### 16. `AdditionalCameraData` Reflection Caches (LOW CPU)

**File:** `NOVR/VrCamera/AdditionalCameraData.cs:32-38`

Already correctly cached. **No action needed.**

---

### 17. `OpenXRInteractionProfilePatches` - Per-call Reflection Lookup (LOW CPU)

**File:** `NOVR/Patches/VR/OpenXRInteractionProfilePatches.cs:38-55`

Uses `??=` to cache fields. **No fix needed.**

---

### 18. `OpenXrControllerProfileBootstrap` - Heavy LINQ Chain (LOW CPU, one-shot)

**File:** `NOVR/OpenXrControllerProfileBootstrap.cs:77-83, 126-131, 242-244`

`ReadIntFeatureField` and `ReadStringFeatureField` use `typeof(OpenXRFeature).GetField(...)` per call - should be cached statically.

**Fix:** Cache the `priority` and `nameUi` FieldInfo at class level.

**Impact:** Low (one-shot). **Risk:** Low.

---

### 19. `VrControllerInput.LogDiagnostics` - Heavy Reflection (MEDIUM GC)

**File:** `NOVR/VrUi/VrControllerInput.cs:316-385`

Diagnostics-only path. **No fix needed unless diagnostics becomes hot.**

---

### 20. `Core.Update` - Regex Allocation (LOW CPU)

**File:** `NOVR/Core.cs:95-104, 116-131`

`Core.FixedUpdate` (L116-131) calls `GameManager.GetLocalAircraft(out _aircraft)` and a `Regex.Replace` every fixed update (50Hz default).

**Fix:**
- Pre-compile the regex to a static `Regex` field:
  ```csharp
  private static readonly Regex NonAlphanumRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);
  ```

**Impact:** CPU Low. **Risk:** Low.

---

### 21. `VrUiCursor.GetScreenPoint` - Camera.pixelWidth/pixelHeight Reads (LOW CPU)

**File:** `NOVR/VrUi/VrUiCursor.cs:134-147`

`camera.pixelWidth`/`pixelHeight` are property accessors with managed-to-native marshalling. Called twice per frame per cursor update.

**Fix:** Cache them, refresh on camera change.

**Impact:** Low. **Risk:** Low.

---

### 22. `PitchCompassBehavior` - Slices Geometry Built Once, Update Writes Transform (LOW GPU/CPU)

**File:** `NOVR/VrUi/Components/PitchCompassBehavior.cs:38-57`

Two normalized cross-products per frame. `_sourcePitchCompass.enabled = false` set every frame even when already false.

**Fix:**
- Track `_sourcePitchCompassEnabled` state and only set once.
- Cache `_cockpitTransform.rotation` changes.

**Impact:** Low. **Risk:** Low.

---

### 23. HUD Patch Reflection in Update - Acceptable but Cleanup Possible

**Files:** All `NOVR/Patches/HUD/*.cs`

Some patches still use `AccessTools.Field(...).GetValue(__instance)` per call (`FlightHudPatch.cs:23-24`, `ThreatItemPatch.cs:38-39, 60, 64, 102`, `JammedMarkerPatch.cs:23-27, 37-39`). The boxing of value-type fields creates small allocations per call.

**Fix:** Convert these to `AccessTools.FieldRefAccess` pattern.

**Impact:** CPU Low-Medium. **Risk:** Low.

---

### 24. `NativeVrUiRoot.IsMissionPickerAvailable` - `GetComponentsInChildren` Per Frame (MEDIUM CPU)

**File:** `NOVR/VrUi/Native/NativeVrUiRoot.cs:689-751`

Four `GetComponentsInChildren` per Update in menu state. Each call allocates a new array and walks the children.

**Fix:**
- Cache the child lookups when `_mainCanvas` changes.
- Or use a single `GetComponentsInChildren<MonoBehaviour>(true)` cast and check type per-result.

**Impact:** CPU Medium when in menu. **Risk:** Low.

---

### 25. `NativeVrUiRoot.SetActive` Calls in Menu-State Branch (LOW CPU)

**File:** `NOVR/VrUi/Native/NativeVrUiRoot.cs:215-218, 191-196`

`_panel?.SetVisible(...)` calls hit every frame even when state has not changed.

**Fix:** Track previous state and skip if unchanged.

**Impact:** Low. **Risk:** Low.

---

### 26. `NOVRHurtOverlayBehavior.Update` - `RectTransform` Cast Every Frame (LOW CPU)

**File:** `NOVR/VrUi/Components/NOVRHurtOverlayBehavior.cs:28-29`

```csharp
var rt = (RectTransform)transform;  // unchecked cast every frame
rt.sizeDelta = new Vector2(5f, 5f);  // same value every frame
```

**Fix:** Cache `_rectTransform` in `Awake`; only set `sizeDelta` once.

**Impact:** Low. **Risk:** Low.

---

### 27. `MainCameraSlaved.Update` - `APIBus.MainCamera` Read Every Frame (LOW CPU)

**File:** `NOVR/VrUi/Components/MainCameraSlaved.cs:8-14`

Uses the cached reference via `APIBus.MainCamera` - good. Same advice as #6 applies.

---

## GPU-Specific Findings

### G1. Two Stereo Render Paths per Frame

The base camera renders the scene to both eyes via URP single-pass stereo. Overlay cameras (`cockpitRenderer`, `postProcessingRenderer`, `VrClippedHudCamera`, `VrCockpitHudCamera`) each render to both eyes independently.

For the typical stack: 4 cameras x 2 eyes = 8 stereo renders/frame.

**Recommendation:** Verify `XRSettings.renderViewportScale` is set to 1.0.

### G2. UI Canvas `pixelPerfect = true` on Cursor (LOW GPU)

**File:** `NOVR/VrUi/VrUiCursor.cs:736`

`pixelPerfect` forces integer-rounded canvas size, which can cause full canvas rebuilds on resize.

**Fix:** Set `pixelPerfect = false` since the cursor is in world space.

**Impact:** Low GPU. **Risk:** Low.

### G3. `LineRenderer.material = new Material(Shader.Find("Sprites/Default"))` (LOW GPU)

**File:** `NOVR/VrUi/VrControllerLaser.cs:37`

Creating a new Material instance per laser means no batching with other LineRenderers using the same shader.

**Fix:** Use a static cached material.

**Impact:** Low. **Risk:** Low.

### G4. `VrCameraManager` adds `UniversalAdditionalCameraData.cameraStack.AddRange`

The GPU impact is real: each overlay camera in a stack renders per-eye. Verify that `cockpitRenderer` and `postProcessingRenderer` are correctly rendered (and not double-rendered due to both root and tracked camera stacks containing them).

---
## Risk Assessment Summary

| Issue | Risk | Mitigation |
|---|---|---|
| #1 Remove Update/LateUpdate from NOVRHeadsetData | Low | Verify with headset tracking test |
| #2 Move stack enforcement to coroutine | Medium | Keep 2s polling window after camera change |
| #3 Remove camera stack AddRange | Medium | Visually verify cockpit rendering |
| #4 Replace FindObjectsOfTypeAll with scene-based lookup | Low | Test in all scenes |
| #5 Move patching out of FixedUpdate | Medium | Confirm components added correctly on first frame |
| #6 Cache Camera.main | Low | Add explicit cache invalidation |
| #7 Cache GraphicRaycaster per canvas | Low | None needed |
| #8 Cache MapIcon list | Low | Invalidate on icon layer change |
| #10 Reparent canvas to HUD camera | Low | Verify world-space sizeDelta behavior |

---

## Recommended Fix Priority

### Phase 1 (Quick wins, < 1 day work)
- **#1** - Remove redundant `Update`/`LateUpdate` from `NOVRHeadsetData` and `NOVRPoseDriver`. Pure win, no risk.
- **#4** - Replace `FindObjectsOfTypeAll<GameObject>` in `NativeVrUiRoot` with scene-root scan.
- **#6** - Cache `Camera.main` in `APIBus`.
- **#7** - Cache `GraphicRaycaster` per canvas in `VrCanvasHitTester`.
- **#11** - Cache `FindChildRecursive` results in `NOVRHMDBehavior`.
- **#26** - Cache `RectTransform` cast in `NOVRHurtOverlayBehavior`.
- **#5 part** - Compile the `Regex` in `Core.cs` once.
- **#23** - Convert `FieldInfo.GetValue` to `AccessTools.FieldRefAccess` in remaining HUD patches.

### Phase 2 (Medium risk, ~1 week)
- **#2** - Move `EnforceClippedCameraStackPosition` out of `Update` into a post-camera-change coroutine.
- **#5** - Move `UIBehaviorPatcher.FixedUpdate` work into a `sceneLoaded` coroutine.
- **#8** - Cache `MapIcon` list with invalidation hook.
- **#10** - Reparent overlay canvases to cockpit HUD camera.
- **#15** - Cache `GetEventRoot` results.
- **#18** - Cache `OpenXrControllerProfileBootstrap` `priority`/`nameUi` FieldInfo.

### Phase 3 (Larger refactor)
- **#3** - Verify and remove redundant camera stack copy.
- **#12** - Move smoothing from `NOUIManager` to consumers.
- **#24** - Cache `NativeVrUiRoot` menu availability lookups.
- **#22** - Refactor `PitchCompassBehavior` math.

---

## Verification Plan

After each fix:

1. **CPU profiling:** Use Unity Profiler - compare `GC.Alloc` per frame and `Update`/`LateUpdate` self-time.
2. **Frame time:** Target frame time = 11.1ms (90Hz Quest 2 / Index) or 8.3ms (120Hz Index/Quest 3).
3. **GPU profiling:** Use RenderDoc or Unity Frame Debugger - count stereo draw calls and overdraw on the cockpit HUD overlay.
4. **Visual regression:** Verify in-flight HUD rendering, menu panels, map icons, cursor behavior, controller laser, recenter function.

---

## File-by-File Summary

| File | Issue Count | Severity |
|---|---|---|
| `NOVR/NOVRHeadsetData.cs` | 1 | High (#1) |
| `NOVR/NOVRPoseDriver.cs` | 1 | High (#1) |
| `NOVR/Core.cs` | 1 | Low (#20) |
| `NOVR/VrCamera/VrCameraManager.cs` | 2 | Medium (#3, #9) |
| `NOVR/VrUi/NOUIManager.cs` | 2 | Medium (#2, #12) |
| `NOVR/VrUi/VrUiCursor.cs` | 4 | Medium (#8, #13, #15, #21) |
| `NOVR/VrUi/VrCanvasHitTester.cs` | 2 | Medium (#7, #14) |
| `NOVR/VrUi/VrControllerLaser.cs` | 1 | Low (#G3) |
| `NOVR/VrUi/VrControllerInput.cs` | 2 | Low (#6, #19) |
| `NOVR/VrUi/UIBehaviorPatcher.cs` | 1 | High (#5) |
| `NOVR/VrUi/Components/NOVRHMDBehavior.cs` | 1 | Medium (#11) |
| `NOVR/VrUi/Components/NOVRHurtOverlayBehavior.cs` | 1 | Low (#10, #26) |
| `NOVR/VrUi/Components/NOVRBlackoutCanvasBehavior.cs` | 1 | Low (#10) |
| `NOVR/VrUi/Components/PositionZeroBehavior.cs` | 1 | Low (#10) |
| `NOVR/VrUi/Components/PitchCompassBehavior.cs` | 1 | Low (#22) |
| `NOVR/VrUi/Native/NativeVrUiRoot.cs` | 3 | High (#4), Medium (#24), Low (#25) |
| `NOVR/APIBus.cs` | 1 | Medium (#6) |
| `NOVR/OpenXrControllerProfileBootstrap.cs` | 1 | Low (#18) |
| `NOVR/Patches/HUD/*.cs` (8 files) | 1 | Low (#23) |
| `NOVR/Patches/Misc/CameraPatch.cs` | - | OK (one-shot prefixes) |
| `NOVR/Patches/VR/OpenXRInteractionProfilePatches.cs` | - | OK (cached reflection) |
| `NOVR/XrStartupDiagnostics.cs` | - | OK (one-shot / gated) |

---

## Final Notes

- The codebase is generally well-structured for performance: static field caches for reflection, per-frame caches in `VrControllerInput`, scratch buffers in `VrCanvasHitTester`.
- The biggest wins are eliminating duplicate work that runs every frame when it could run once (head pose updates, camera stack enforcement).
- The biggest risks are around visual regressions from removing camera stack copies (#3) and the timing changes from moving FixedUpdate work (#5).
- The HUD patches are largely fine; the main improvement is consistent use of `FieldRefAccess` instead of `FieldInfo.GetValue`.

**Estimated overall impact if Phase 1 + Phase 2 are applied:**
- ~30-50% reduction in `Update`/`LateUpdate` CPU time on the VR camera/UI path.
- Reduced GC alloc per frame from ~5-10 KB to ~1-2 KB during steady-state flight.
- Frame time reduction on Quest 2: estimated 0.5-1.5ms per frame (significant for hitting 90Hz).

---

*End of report.*
