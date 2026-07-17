# NOVR — In-Flight Rendering & UX Deep Analysis

Date: 2026-07-16 · Branch: hud-improvements @ 8c5d197 (+ working-tree changes)
Method: static analysis of `NOVR/` source + ilspycmd decompilation of game classes
(`FlightHud`, `CombatHUD`, `CameraStateManager`, `CameraCockpitState`, `DynamicMap`,
`ThreatItem`, `HUDUnitMarker`, `HeadMountedDisplay`, `PlayerSettings`, `FastMath`,
`MFDScreen`, `MFDAppManager`, `FloatingOrigin`) from
`D:\SteamLibrary\steamapps\common\Nuclear Option\NuclearOption_Data\Managed\Assembly-CSharp.dll`.
Decompiled dumps kept in `.analysis-tmp/`. Geometry claims are code-derived, not in-headset observations.

---

## 1. URP camera stack — exact per-frame composition

**Rig.** The game's root `Main Camera` is disabled and replaced by a tracked child
`NOVR Main Camera` (`VrCameraManager.cs:97-142`): `trackedCamera.CopyFrom(rootCamera)`,
renderType=Base, `allowXRRendering=true`, the root camera's stack copied over
(`VrCameraManager.cs:112-123`), root disabled (`:134-135`). The game's overlay children
`cockpitRenderer` and `postProcessingRenderer` are reparented under the tracked camera
(`VrCameraManager.cs:17,192-202`). `CameraStateManager.mainCamera` is redirected to the
tracked camera (`CameraStateManagerMainCameraPatch.cs:11-15`).

**Resulting per-frame render order** (stack order, enforced every frame by
`NOUIManager.EnforceClippedCameraStackPosition`, `NOUIManager.cs:180-216`):

1. **Base: tracked main camera** — world, terrain, sky. Post-process volume lives on its
   GameObject (`CameraStateManager.cs:124,187-190`); `ExposureController.UpdateExposure()`
   runs every frame (`CameraStateManager.cs:381`); NVG light exists (`:36`).
2. **cockpitRenderer** (game overlay) — cockpit interior geometry (layers 3 `Cockpit`,
   14 `CockpitAndExternal`; near 0.01 / far 5 per `NOUIManager.cs:235` comment). FOV synced
   to main camera every frame by the game (`CameraCockpitState.cs:90,144`).
3. **VrClippedHudCamera** (depth 99, layer 29 `VrUiClippedHud`) — `clearFlags=Nothing`,
   `m_ClearDepth=false` set via reflection (`NOUIManager.cs:125-143`). Keeps cockpitRenderer's
   depth buffer: the pitch ladder at ~1000 m loses the depth test against cockpit geometry
   <5 m but beats the cleared far-plane value, so **cockpit occludes it and sky does not**
   (`NOUIManager.cs:235-241`). Must sit after cockpitRenderer and before
   postProcessingRenderer because the latter clears depth (`NOUIManager.cs:14-17,129-131`).
4. **postProcessingRenderer** (game overlay) — clears depth per `NOUIManager.cs:14-17`.
5. **VrCockpitHudCamera** (depth 100, layer 30 `VrUi`) — `clearFlags=Depth`
   (`NOUIManager.cs:103-123,218-226`), so it ignores all prior depth anyway; the game's
   HUD shader additionally renders `ZTest Always` (`PitchCompassBehavior.cs:253-255`).
6. **Stack-final URP post-processing** — applied once after the last overlay from the base
   camera's settings, so the whole composite **including both HUD layers** receives
   auto-exposure/tonemapping/whatever the game's profile contains (only auto-exposure and
   NVG are confirmed in code; bloom/DoF/TAA presence not verified).

**HUD camera properties** (`NOUIManager.cs:103-143,218-242`): `stereoTargetEye=Both`,
`targetTexture=null`, `allowHDR=false`, `allowMSAA=false`, near 0.01 / far 10000,
full-screen rect, FOV never synced (stays at default — masked in XR because the runtime
overrides per-eye projection). Both are `NOVRPoseDriver`-driven (`NOVRPoseDriver.cs:46-50`)
**with head-only pose** — parented under `Core` at identity, so world pose = tracking-space
head pose (`NOVRHeadsetData.cs:113-117`), *not* aircraft×head like the tracked main camera.

**Retinal alignment trick.** All symbology positioning converts a target's direction into
the *main* camera's local frame, then re-expresses that same local direction through the
HUD camera (`VrHudProjectionHelper.cs:13-27`). With identical stereo projections, retinal
positions match exactly regardless of the two cameras' different world orientations — this
is also why floating-origin translation (threshold 1024 m, `FloatingOrigin.cs:21`) does not
disturb HUD/world alignment: only rotation composition matters.

---

## 2. Geometry of every in-flight element

Canvas units = meters (world-space canvas, scale 1). Vergence: everything ≥850 m is
optical infinity (≈0.001 D; stereo disparity at 64 mm IPD ≈ 0.004° — sub-pixel, no stereo cue).

| Element | Camera / layer | World position / parent | Flat vs sphere vs head-locked | Apparent depth |
|---|---|---|---|---|
| FlightHud canvas root | HUD cam / 30 | `(0,0,1050)`, identity, forced every `Update` (`NOVRFlightHudBehavior.cs:35-39`) | **flat plane** | ∞ |
| HUDCenter subtree (weapon/countermeasure/power panels) | 30 | pinned `(0,0,1050)` (`NoVrHudBehavior.cs:7-11`); panels at local (330,290) and (−400,80), scale 0.6, backgrounds disabled (`NOVRFlightHudBehavior.cs:19-20,41-92`) | flat plane; ~17°R/15°U and ~21°L/4°U | ∞ |
| HMD cluster (Speed/Altitude/Bearing/Artificial Horizon) | 30 | `SmoothedForwardReference.forward × 850`, rot = smoothed ref (`NOVRHMDBehavior.cs:7-13`); local offsets (±110,150),(0,200),(0,150) → ±7–13° cluster | **head-following** (smoothed, k=10/s) | ∞ (850 m) |
| Pitch ladder (37 slices ×2 antipodal) | **clipped cam / 29** | root at world origin, rot = f(inverse cockpit rotation, world forward) every frame (`PitchCompassBehavior.cs:38-57`); cards at radius 1000 + index×0.02, scale 0.8 (`:98-121`) | sphere (1000 m), counter-rotates to aircraft attitude; **occluded by cockpit** | ∞, but with real occlusion cue |
| Boresight cross | 30 | gun dir × 1000, sphere-projected (`HUDBoresightStatePatch.cs:36-39`); rotation NOT set (line 34 commented out) — card stays canvas-plane-parallel | sphere | ∞ |
| Lead pipper / target pos / lead line | 30 | sphere 1000, billboarded to HUD cam (`HUDBoresightStatePatch.cs:89-100`); line drawn in canvas-local 2D, 14-unit gap, green/yellow by 15-unit proximity (`:102-123`) | sphere | ∞ |
| Velocity vector | 30 | sphere 1000 (`FlightHudPatch.cs:38-48`), only when speed >10 m/s, disabled when behind | sphere | ∞ |
| Target designator | 30 | `SlerpUnclamped(identity, smoothedRef.rot, 1.1f)`, pos = fwd×1000 (`NOVRTargetDesignatorBehavior.cs:9-15`) | head-following + 10% overshoot + lag | ∞ |
| CCIP pipper / line / CCRP bar | 30 | sphere 1000 billboarded (`HUDBombingStatePatch.cs:77-121`); line pipper→velocity vector with 22px/8px gaps via `ReferencePixelsToHudDistance` | sphere | ∞ |
| Turret crosshair | 30 | turret dir × 1000 (`HUDTurretCrosshairPatch.cs:25-31`), `LookRotation` billboard | sphere | ∞ |
| Unit markers (+ selected edge-pin + targetArrow) | 30 | sphere 1000, billboarded (`HUDUnitMarkerPatch.cs:41,69-70,98-99`); selected pins to 50°×50° ellipse (`:59-64`) | sphere | ∞ |
| Objective pointer/dot/sizeIndicator + text | 30 | sphere 1000; off-screen → ellipse-pinned + rotated pointer, sizeIndicator off (`ObjectiveOverlayPatch.cs:42-69`); size ∝ 35·range/distance (`:71-77`) | sphere | ∞ |
| Airbase marker/label, runway borders, glideslope | 30 | sphere 1000 or ellipse-pinned (`AirbaseOverlayPatch.cs:64-78`); 4 corner lines via `SetVerticalLine` (`:97-121`); glideslope line + aim point (`:145-155`) | sphere | ∞ |
| Threat notch indicator (line+label) | 30 | sphere 1000 (`ThreatItemPatch.cs:66-85`); two-segment vertical line, total height **4096 units** (≈±63° stripe), gap 72, label at offset 200−slot·25 (`:12-19,197-227`) | sphere; stripe deliberately over-tall for peripheral pickup | ∞ |
| ThreatList text rows ("Missile [ARH] …") | 30 | stays at prefab position on the flat 1050 plane (unpatched) | flat plane | ∞ |
| StatusDisplay (actions report feed) | 30 | world `(630,145,1000)` → ~32° right, 8° up (`NOVRStatusDisplayBehavior.cs:8-10`) | flat plane (offset) | ∞ (~1197 m) |
| Minimap (minimized DynamicMap) | 30 (nested canvas) | parented to `hudMapAnchor` inside FlightHud canvas (game `DynamicMap.cs:380-413`) | flat plane, lower corner | ∞ |
| Maximized map | 30 | reparented into GameplayUI (game `DynamicMap.cs:322-324`) → the 3 m menu canvas (`NOVRGameplayUIBehaviour.cs:8-12`, scale 0.003 → ~5.8 m wide) | flat panel at **3 m** | 3 m — real vergence demand |
| MFD screens | 30 | unchanged game prefab positions on the 1050 plane; only background Image disabled (`MFDScreenPatch.cs:8-12`) | flat plane | ∞ |
| Hurt overlay | 30 | new canvas, camera pos + fwd×1 m, 5×5 m quad (`GameplayUIHurtOverlayPatch.cs:18-35`; `NOVRHurtOverlayBehavior.cs:19-30`) | **head-locked at 1 m** (~136° coverage) | 1 m — strong vergence/infinity conflict |
| Blackout canvas | 30 | camera pos + fwd×1 m (`NOVRBlackoutCanvasBehavior.cs:29-34`) | head-locked at 1 m | 1 m |
| GameplayUI / MessageUI (pause etc.) | 30 | `(0,0,3)`, scale 0.003 (`NOVRGameplayUIBehaviour.cs:8-12`) | flat at 3 m, room-locked | 3 m |
| VR cursor | 30 | plane-hit point, scale 0.001, sortingOrder max (`VrUiCursor.cs:718-745`) | on target canvas | = canvas |
| Controller laser | 30 | LineRenderer controller→cursor, only 0.01–50 m (`VrControllerLaser.cs:90-103`) | — | near |

**Behind-culling:** sphere projection refuses `z<=0` (`VrHudProjectionHelper.cs:22-23`) →
behind-aircraft elements are disabled (markers, velocity vector, CCIP, borders) or
ellipse-pinned (objectives, airbases, selected marker).

---

## 3. The flatness problem, precisely

**Coplanar set (flat 1050 m plane):** weapon/countermeasure/power panels, ThreatList text,
MFD screens, minimap, StatusDisplay, and any FlightHud child not explicitly re-projected.
**Sphere set (1000 m):** all projected symbology. **Head-locked set:** HMD cluster (850 m,
smoothed), designator (1000 m, smoothed+overshoot), hurt/blackout (1 m). **Occluded set:**
pitch ladder only.

- **Zero apparent-depth variation between classes.** Flat plane (1050), sphere (1000),
  HMD (850) are all at vergence ≈ ∞; disparity is sub-pixel. There is **no vergence or
  stereo separation between threats vs nav vs flight params** — attention hierarchy must be
  carried by color/flash alone (and everything defaults to the same HUD green,
  `PlayerSettings.cs:78-82`).
- **The only real depth cue in the entire presentation** is cockpit-vs-ladder occlusion on
  layer 29. Conversely, layer 30 renders `ZTest Always` *over* canopy frame, A-pillars,
  mirrors, glareshield — occlusion says "symbology is 5 cm from your face", vergence says
  "infinity". That contradiction is the classic VR-HUD rivalry trigger, and it also lets a
  5 km target marker visually sit on top of the canopy bow when you look through it.
- **Head translation (lean):** sphere elements re-project per frame → angularly invariant
  to lean (no motion parallax); the flat plane shifts ≤0.016° for a 0.3 m lean at 1050 m —
  nothing in the HUD yields parallax depth cues. Everything reads as "painted at infinity".
- **Element scale vs lean:** constant angular size (world-space at ~1 km) — no legibility
  change with lean, but also no perspective reinforcement of depth.
- **Minor flatness defect:** the boresight card never gets billboarded
  (`HUDBoresightStatePatch.cs:34` commented out) so it stays parallel to the 1050 plane and
  foreshortens when off-axis.

---

## 4. Off-boresight behavior (30–90°+ off nose)

- **Disappears (hard-culled behind aircraft / projection fails):** unselected unit markers
  (`HUDUnitMarkerPatch.cs:88-93`), velocity vector (`FlightHudPatch.cs:40-44`), CCIP pipper
  (`HUDBombingStatePatch.cs:89-94`), CCRP bar (`:57-62`), runway borders/glideslope
  (`AirbaseOverlayPatch.cs:108-113,145-151`). Note: lead display keeps its **last** position
  when projection fails (`HUDBoresightStatePatch.cs:89-91` early return) — stale-pipper risk.
- **Follows the head (with lag):** HMD cluster + designator via `SmoothedForwardReference`,
  `Quaternion.Lerp(rot, cam.rot, dt×10)` (`NOUIManager.cs:13,93-99`). τ = 100 ms; steady-state
  lag during a constant head turn ≈ ω/10 → **~18° behind at 180°/s, ~9° at 90°/s**; half-life
  after stopping ≈ 65 ms, 95% settled in ~0.3 s. Designator additionally overshoots ×1.1
  (`NOVRTargetDesignatorBehavior.cs:13`): at rest it sits 10% *beyond* gaze (6° at 60° head
  angle) — roughly equal to the 5.7° selection cone (100 units at 1000 m,
  `CombatHUD.cs:475`), so off-axis designation is systematically biased outward.
- **Pins to viewport ellipse:** selected unit marker (+ arrow + distance text,
  `HUDUnitMarkerPatch.cs:59-71,117-147`), objectives (`ObjectiveOverlayPatch.cs:42-56`),
  airbases (`AirbaseOverlayPatch.cs:64-67`) — hardcoded **50°×50°** ellipse
  (`VrHudProjectionHelper.cs:8-11`), headset-FOV-independent (noted in prior report).
- **Peripheral-vision treatment:** exactly one — the notch indicator's 4096-unit
  (≈126°-tall) vertical stripe with lerped label slots (`ThreatItemPatch.cs:12-19,168-195`).
  Beyond ~90° off the threat bearing even that is frustum-culled with no edge cue.
- **Gaze/head-adaptive behavior:** only two — the HMD cluster head-follow (above) and the
  **gaze-slaved turret** (`TurretVrCameraPatch.cs:51`: manual turrets aim at
  `MainCamera.transform.forward`). No gaze-based declutter, no look-to-reveal, no
  edge-of-FOV arrows for unselected threats.
- **HMD readout hide-cone (game logic misfiring in VR):** `HeadMountedDisplay.Update`
  hides each HMD app when it comes within `hideDistance` of `GetHUDCenter().position`
  (decompiled `HeadMountedDisplay.cs:85-137`). NOVR pins HUDCenter at `(0,0,1050)`
  (`NoVrHudBehavior.cs:9`) and the apps ride the smoothed head at 850 m with ±110–200 m
  local offsets → app-to-HUDCenter distance ≈ 273–283 m when looking along the recentered
  forward axis, vs `hideDistance = 0.33 × 0.5 × (hmdWidth+hmdHeight)` ≈ 345–495
  (`PlayerSettings.cs:68,234-235`). **Result: speed/altitude/bearing/horizon self-hide
  inside a ~±25–30° cone around the recentered forward direction and only reappear when
  you look further off-axis** — the only head-following data cluster vanishes where you
  look most.

---

## 5. MFD + minimap in cockpit

- **Minimap:** a nested canvas on the flat 1050 m plane (game `DynamicMap.cs:386-392`
  reparents to `hudMapAnchor` on minimize). NOVR forces the map image fully opaque and the
  default material every frame (`DynamicMapRotationSafePatch.cs:120-135`), rotates it with
  aircraft yaw and focuses 4000 map-units ahead (`:87-91`), applies `HudMinimapOpacity`
  **only at Minimize() time** (`:45-56`) — live config changes don't re-apply until a
  minimize/maximize cycle. It is interactive: raycast targets forced on
  (`NOVRDynamicMapBehavior.cs:19-36`) and clicks routed through the VR cursor pipeline
  (`VrUiCursor.ForwardMapClickIfNeeded`, `VrUiCursor.cs:830-889`, radius config
  `MapClickMaxRadius` 0.0375). Legibility at 1050 m: icon/vector lines ~1 unit ≈ 0.06° —
  borderline; and since it's layer 30 it floats unoccluded over the physical instrument
  panel when you look down.
- **Maximized map:** moved into the GameplayUI 3 m canvas (game `DynamicMap.cs:322-324`;
  `NOVRGameplayUIBehaviour.cs:8-12`) — ~5.8 m wide at 3 m, fully interactive via laser
  (within the laser's 50 m cap) — this is the one well-formed near-field UI surface.
- **MFD screens:** uGUI `MFDScreen`s refreshed per frame by `MFDAppManager` (decompiled).
  NOVR's only adaptation is disabling the background Image (`MFDScreenPatch.cs:8-12`) —
  they remain at their 2D prefab positions on the 1050 m plane, ~30–40-unit text ≈ 1.6–2.2°
  (readable), dense RenderTexture content (radar/map pages) marginal, no contrast backing,
  no VR-specific interaction path (bezel buttons are not VR-adapted; selection is via game
  controls). Not sphere-projected, not occluded, not dimmable.

---

## 6. Visual treatment (scale, text, lines, color, contrast, brightness)

- **Scale baseline:** the HUD canvas is authored 1920×1080; at 1050 m its 1080-unit height
  spans ≈ 54° vertically (1920 units ≈ 85° wide). 1 canvas "pixel" ≈ 0.05° ≈ 3 arcmin
  ≈ ~1.2 px on a ~25 px/deg HMD. `ReferencePixelsToHudDistance` compensation
  (`VrHudProjectionHelper.cs:146-148`) is calibrated against desktop 1080p, **not** HMD
  angular resolution.
- **Text/icons:** HUD text 32–40 units (`overlayTextSize` / `hmdTextSize`,
  `PlayerSettings.cs:72-76`) ≈ 1.8–2.2° — comfortable. Icons ~30 units (`hmdIconSize`)
  ≈ 1.6° — fine.
- **Lines are the failure mode:** notch line width 1 unit (`ThreatItemPatch.cs:14`), lead
  line ~1 unit, runway border lines similar → subpixel-to-1 px at the eye → aliasing and
  shimmer, worsened by any stack-final TAA. Minimap icon/vector strokes have the same
  problem.
- **Color language:** default HUD green (0,255,0) (`PlayerSettings.cs:78-82`); boresight
  cross green/yellow by 15-unit lead-proximity rule (`HUDBoresightStatePatch.cs:110-112`);
  designator & velocity vector alpha-fade to zero within ~1.3° of the boresight cross
  (`:44-59`); notch green/yellow/red by threat state with a 10–20 Hz sine flash
  (`ThreatItemPatch.cs:87-92`); fresh unit markers lerp from yellow over 1 s and flash at
  20 Hz (`HUDUnitMarkerPatch.cs:100-111`).
- **Contrast backing deliberately removed:** panel images disabled
  (`NOVRFlightHudBehavior.cs:62-67,90-91`), MFD background Image disabled
  (`MFDScreenPatch.cs:8-12`), notch box backing disabled (`ThreatItemPatch.cs:199`) —
  all symbology floats bare against sky/terrain; green-on-bright-cloud washout is
  unmitigated (no outline, no drop shadow, no dynamic contrast element).
- **Brightness / night:** nothing in NOVR. The HUD is unlit uGUI drawn with
  `allowHDR=false` (`NOUIManager.cs:117`) into the camera stack, then the stack-final
  post-processing pass applies to it — confirmed auto-exposure in the game's volume
  (`CameraStateManager.cs:381`) → **HUD brightness is slaved to scene exposure**: bright
  sky dims the HUD; dark/NVG scenes overdrive it. No dimming control, no NVG-safe
  palette, no bloom guard. (Bloom/TAA/DoF presence in the game profile NOT verified —
  treat as risk, not fact.)

---

## 7. Frame dynamics (update order, smoothing, latency)

- **Every frame:** canvas pin to anchor + rescale (`NOVRFlightHudBehavior.cs:35-39`);
  HUDCenter pin to `(0,0,1050)` (`NoVrHudBehavior.cs`); HMD cluster + target designator
  head-follow; smoothed-reference update (`NOUIManager.cs:87-99`); velocity vector
  (`FlightHudPatch`); all unit markers via `CombatHUD.LateUpdate` (`CombatHUD.cs:667-707`)
  → NOVR prefixes; boresight/lead (`CombatHUD.cs:706` postfix); objectives, airbases,
  threat items; pitch-ladder root (`PitchCompassBehavior.cs:38-57`); status/gameplay UI
  behaviors. **Round-robin:** marker `UpdateVisibility` runs one marker per frame
  (`CombatHUD.cs:629-634`). **Event-driven:** behavior injection
  (`UIBehaviorPatcher.cs:92-157`), map minimize/maximize, weapon-station UI swaps.
- **Smoothing actually felt:** head reference k=10/s (τ ≈ 100 ms); designator overshoot
  1.1 hardcoded — the `Target Designator Overshoot` config entry default 1.2
  (`ModConfiguration.cs:42-46`) is **dead code**; threat gap lerp k=8/s (τ ≈ 125 ms,
  `ThreatItemPatch.cs:191-194`); controller ray OneEuro (minCutoff 1.2, β 0.15) plus
  position EMA 8/s (`VrControllerInput.cs:81-92,148-154`) ≈ 100–200 ms pointing lag.
- **Latency hygiene (good):** HUD camera pose is written in Update + LateUpdate +
  OnBeforeRender (`NOVRPoseDriver.cs:30-44`); symbology positions are computed same-frame
  from the same pose → no inter-frame swim for unsmoothed elements.
- **Broken / stale paths:**
  - Hit markers are never re-projected — the game writes raw `WorldToScreenPoint` pixel
    coordinates as world positions (`CombatHUD.cs:37-50`) → they land near (960, 540, 0)
    in world space → misplaced or invisible in VR.
  - Lead pipper freezes in place on projection failure instead of hiding
    (`HUDBoresightStatePatch.cs:89-91`).
  - NOVR's canvas pins rely on running *after* the game's `FlightHud.Update`, which still
    writes screen-space position/roll to the same transforms every frame
    (`FlightHud.cs:131-143`) — works by script-order luck, unguarded.

---

## 8. Critical assessment — UX weaknesses (evidence-ranked)

**W1. Occlusion model inverted vs. a real HUD.** Only the pitch ladder depth-tests (layer
29, `NOUIManager.cs:125-143`); everything else draws with `ZTest Always` over the canopy
frame, pillars, mirrors and glareshield (`PitchCompassBehavior.cs:253-255`,
`NOUIManager.cs:114`). Occlusion contradicts vergence (all symbology converges at ∞):
5 km-away markers paint over 0.5 m-away structure, producing chronic stereo rivalry. No
"combiner glass" discipline — a real HUD collimates only what the pilot's eye path
actually clears.

**W2. Total depth monotony.** Flight-critical symbology, tactical markers and the
instrument cluster all live at 850–1050 m: identical vergence demand, zero binocular
disparity, no parallax under head lean. The entire attention hierarchy is carried by
color and flashing alone inside one flat green plane at infinity — no depth layering to
separate "fly the jet" from "manage the fight" from "read the instruments".

**W3. The only head-following data hides where you look.** HMD speed/altitude/bearing/
horizon self-hide inside ~±25–30° of the recentered forward axis: the game's
`HeadMountedDisplay.Update` hides apps within `hideDistance`
(0.33 × 0.5 × (hmdWidth+hmdHeight) ≈ 345–495 m, `PlayerSettings.cs:68,234-235`) of
HUDCenter, which NOVR pins at `(0,0,1050)` (`NoVrHudBehavior.cs:9`); the apps ride the
smoothed head at 850 m → 273–283 m away when looking forward (decompiled
`HeadMountedDisplay.cs:85-137`). The one cluster designed to be glanceable vanishes on
the boresight.

**W4. Off-boresight / peripheral hard edges.** Behind-aircraft symbology is culled with
no cue (`VrHudProjectionHelper.cs:22-23`); only the selected target, objectives and
airbases pin to a hardcoded 50°×50° ellipse (`VrHudProjectionHelper.cs:8-11`) —
unselected threats get nothing beyond ~±25°; even the 4096-unit (≈126°-tall) notch
stripe dies past ~90° (`ThreatItemPatch.cs:12-19`). The head-following layer adds ~18°
of lag at a 180°/s head turn (`NOUIManager.cs:13,98`), and the designator rests 10%
beyond gaze (`NOVRTargetDesignatorBehavior.cs:13`). Gaze-adaptive behavior exists only in
the gaze-slaved turret (`TurretVrCameraPatch.cs:51`).

**W5. Pitch ladder is a narrow meridian strip, not an attitude sphere.** 5°-step tangent
cards along one great circle (+ antipode), azimuth derived from world north
(`planeReference = Vector3.forward`, `PitchCompassBehavior.cs:49`) → flying east or west
the ladder sits at the 9 or 3 o'clock position with no attitude reference ahead; band
width is only the source strip width. It is the only occluded element yet has the least
coverage.

**W6. Instrument cluster floats at infinity with no contrast or brightness management.**
Panels, minimap and MFDs are coplanar at 1050 m, backgrounds stripped (Section 6), no
dimming, and HUD brightness is slaved to scene auto-exposure (`CameraStateManager.cs:381`)
— the opposite of a cockpit instrument, which should be near, shaded and stably lit.

**W7. Subpixel line work at 1 km.** 1-unit strokes ≈ 3.4 arcmin ≈ ~1 HMD px
(`ThreatItemPatch.cs:14` and similar for lead lines, runway borders, map vectors); the
reference-pixel compensation targets desktop 1080p, not HMD angular resolution →
aliasing/shimmer on precisely the elements that encode dynamic state (lead line, notch
stripe, glideslope).

**W8. Dead and broken details erode trust.** Dead `Target Designator Overshoot` config
(`ModConfiguration.cs:42-46` vs hardcoded 1.1); hit markers never re-projected
(`CombatHUD.cs:37-50`); minimap opacity applies only at Minimize time
(`DynamicMapRotationSafePatch.cs:45-56`); laser capped at 50 m plus cursor ≈ 0.0035° at
the 1050 m plane → pointing at in-flight HUD canvases is unsupported
(`VrControllerLaser.cs:90-99`, `VrUiCursor.cs:45`); stale lead pipper on projection
failure; Update-order dependence vs `FlightHud.Update`.

### Not verified — flag for in-headset / profile checks
- Game post-processing profile contents (bloom / DoF / TAA) — only auto-exposure confirmed.
- Canopy glass depth-write behavior (does the canopy occlude layer-29 pitch ladder correctly?).
- MFDScreen legibility and stereo comfort in-headset — all geometry here is code-derived,
  no in-headset observation was performed.
