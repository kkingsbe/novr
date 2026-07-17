# NOVR — VR UI/UX Improvement Findings

Date: 2026-07-16
Branch: hud-improvements @ 8c5d197
Source: static code analysis of `NOVR/VrUi/`, `NOVR/VrCamera/`, `NOVR/Patches/`, `NOVR/ModConfiguration.cs`

Foundation (what's already good): true world-space canvases rendered by dedicated stereo overlay cameras, collimated HUD at ~1 km (correct vergence for primary flight symbology), depth-tested pitch ladder occluded by cockpit geometry, per-aircraft seat position memory, OneEuro/EMA filtering on controller rays.

---

## P0 — Hit every session

### 1. Menus don't follow the user
All UI anchors are fixed world positions; the native menu's `CaptureMenuAnchor()` is commented out (`NativeVrUiRoot.cs:408`) and an unused `AnchorResetAfterHiddenSeconds` constant suggests planned-but-dropped re-anchoring. Recentering recalibrates *tracking* (2–3 s countdown) so the fixed menu lands in front. Turn around physically and every menu is behind you.
**Direction:** capture head yaw when a menu opens and place it in front of the user; re-anchor after N seconds hidden; gentle lazy-yaw follow for pause menu.

### 2. Pointing dies when the desktop window loses focus
`VrUiCursor.Update` early-outs on `!Application.isFocused` (`VrUiCursor.cs:194-199`). VR runtimes routinely leave the desktop window unfocused → cursor and all clicking stop. Likely the #1 source of "VR UI randomly doesn't work".
**Direction:** bypass the focus gate; raw Input System device reads work unfocused.

### 3. Drags are brittle (no pointer capture)
Hand-rolled pointer pipeline: if the ray slips off the Selectable or canvas plane mid-drag, the press is silently abandoned (`VrUiCursor.cs:336-346, 431-437`). The stuck fuel-slider bug (commit `fe473ea`) is this exact problem.
**Direction:** capture `pointerDrag` on `pointerDown` and keep firing drag events until release, like `PointerInputModule` does.

### 4. Recenter is fragmented and incomplete
F9 = translation only, no yaw (`Core.cs:110-114`, `NOVRHeadsetData.cs:24-34`). Yaw reset only via pause menu / native UI. No **vertical** seat trim — only forward/right — so per-aircraft height memory is impossible.
**Direction:** one unified recenter (translation + yaw, configurable), add Up offset to per-aircraft save, allow controller-button binding.

---

## P1 — Interaction completeness

### 5. No scroll, no double-click
`scrollDelta` is never assigned anywhere (standard input modules are disabled → ScrollRects are drag-only). `clickCount` hardwired to 1 (`VrUiCursor.cs:424`).
**Direction:** map right-thumbstick Y to scroll delta (thumbsticks currently have zero bindings); track click timing for double-click.

### 6. Trigger-only controller input, zero haptics
No A/B/X/Y, grip, or stick-click UI bindings (`VrControllerInput.cs:210-213` reads trigger only). No haptic impulse is ever sent.
**Direction:** haptic ticks on hover/press; A/B button → back/Escape.

### 7. Cursor polish
Angular dead-zone disabled (`VrUiCursor.cs:643-653`); cursor position unsmoothed (snaps to plane hit; only color/scale animate); laser end color ~black from double-`/255` (`VrControllerLaser.cs:36`).
**Direction:** re-enable dead zone, smooth cursor position, distance-scaled reticle, fix laser gradient.

### 8. HUD adjustments missing
Collimation fixed at 1000 m; off-screen marker edge-pin assumes hardcoded 50°×50° viewport regardless of headset FOV (`VrHudProjectionHelper.cs:8-9`); only HUD setting that exists is minimap opacity. `CockpitStableMode` description promises horizon-stable HUD but the mechanism only re-parents a smoothing reference (`NOUIManager.cs:40-53`).
**Direction:** HUD scale + opacity sliders in the in-VR settings panel; derive edge-pin ellipse from actual FOV; fix or relabel CockpitStableMode.

---

## P2 — Comfort suite (currently zero)

### 9. No head-position clamping in cockpit
Nothing stops leaning through canopy/glareshield (`NOVRPoseDriver` applies raw 6DOF pose).
**Direction:** soft spherical clamp around calibrated seat position with fade/pushback at the edge.

### 10. No comfort options for high-vection moments
External/orbit view is game-driven with raw 1:1 head pose on top; `CameraOrbitState` is entirely unpatched, so leaning physically detaches you from the orbit pivot. Eject/death transitions are abrupt head-locked quads at 1 m.
**Direction:** vignette/tunnel option for external views and eject; comfort fade on death/blackout; snap turn for selection/map screens.

---

## P3 — Bigger swings

### 11. VR keyboard for text input
Native multiplayer/workshop panels create real `InputField`s (search, passwords) with no input path — typing blind on a physical keyboard in a headset.
**Direction:** short-term `TouchScreenKeyboard.Open` (SteamVR overlay keyboard); long-term minimal in-world keyboard.

### 12. Controller-based flight input
VR controllers do UI only; flying requires gamepad/HOTAS/keyboard. The dead `Uuvr.XInput` proxy was clearly the intended bridge.
**Direction:** VTOL VR-style virtual stick (grip + hand position) as the long-term goal; thumbstick-Y throttle as a first step.

### 13. Per-panel layout + auto-registration
Aircraft/loadout/weapon selectors only get base world-space conversion (layout customization commented out). Interaction layer is opt-in per canvas — every runtime-spawned panel (e.g. TMP dropdown, `TMP_DropdownPatch.cs`) renders but is unclickable until someone writes a registration patch.
**Direction:** auto-register any world-space canvas on enable; finish per-panel layout passes.

---

## Top three (highest impact / well-scoped)
1. **#2** focus-independent pointing — biggest reliability win (`VrUiCursor.cs`)
2. **#1** menu auto-anchoring on open — biggest daily annoyance (`NativeVrUiRoot.cs`)
3. **#3** pointer-capture drags — interaction layer feels broken without it (`VrUiCursor.cs`)
