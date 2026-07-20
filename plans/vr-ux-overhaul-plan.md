# VR UI/UX Overhaul — Implementation Plan

Date: 2026-07-16
Basis: `reports/vr-hud-redesign-directions-2026-07-16.md` (sequencing steps 1–2 + first depth-band moves), `reports/vr-ui-ux-improvement-findings-2026-07-16.md`
Branch: `hud-improvements` (commit directly, no worktrees — batches are sequential, they share files)

## Build / validation contract

The sandbox lacks standard Windows env vars → restore fails with `NuGet.targets error: Value cannot be null (Parameter 'path1')` unless these are set. Validation command (compile-only; skips game deploy):

```bash
"/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe" -NoProfile -Command '$env:ProgramFiles="C:\Program Files"; ${env:ProgramFiles(x86)}="C:\Program Files (x86)"; $env:ProgramW6432="C:\Program Files"; $env:CommonProgramFiles="C:\Program Files\Common Files"; $env:ProgramData="C:\ProgramData"; & "C:\Program Files\dotnet\dotnet.exe" build NOVR/NOVR.csproj -c Debug -v minimal -p:GameDeployPath= 2>&1 | Select-Object -Last 15'
```

- Baseline: **0 errors, 42 pre-existing warnings**. Definition of done per batch: 0 errors, no *new* warnings.
- The game is running: any build without `-p:GameDeployPath=` fails with MSB3021/MSB3026/MSB3027 copy errors into `D:\SteamLibrary\...BepInEx` — expected, not a defect.

## Code conventions contract

- C# 10, file-scoped namespaces, nullable enabled — no new CS86xx warnings.
- Follow existing patch patterns: `[HarmonyPatch]`-style classes auto-loaded by `PatchLoader` (check existing files under `NOVR/Patches/` for the attribute style in use before writing a new patch).
- All new tunables go in `NOVR/ModConfiguration.cs` using the existing `Config.Bind` pattern, in the sections assigned below (no other batch touches another's section list, but the file is shared — read before editing, append entries, do not reorder existing ones).
- Diagnostic logging behind `ModConfiguration.VerboseDiagnostics` only.
- Behavior-changing features get config entries with **defaults that preserve current behavior**, except where noted.

## Batch 1 — Interaction layer (`NOVR/VrUi/`)

Owner files: `VrUiCursor.cs`, `VrControllerInput.cs`, `VrControllerLaser.cs`, `ModConfiguration.cs` (Input section entries), `Native/NativeVrUiSettingsPanel.cs` (toggles).

1. **Focus-independent pointing** — remove the `Application.isFocused` early-out for cursor positioning/event feeding (`VrUiCursor.cs:194-199`). Config `CursorRequiresWindowFocus` (bool, **default false**).
2. **Pointer-capture drags** — mimic `PointerInputModule`: once `pointerDown` lands, keep `pointerDrag` captured to the pressed handler until release, regardless of where the ray moves; do not abandon the press when the ray leaves the canvas plane mid-press (`VrUiCursor.cs:326-515`). No config — strict improvement.
3. **Thumbstick scroll** — poll both thumbsticks via Input System (`VrControllerInput`), map Y to `PointerEventData.scrollDelta` (dead zone ~0.2), dispatch `ExecuteEvents.scroll` to the hover hierarchy. Config `EnableThumbstickScroll` (bool, default true), `ScrollSpeed` (float, default 1.0).
4. **UI haptics** — small `UnityEngine.XR.InputDevice.SendHapticImpulse` on hover-enter and on press, to the dominant-hand device. Config `EnableUiHaptics` (bool, default true), `UiHapticStrength` (float 0–1, default 0.5).
5. **Double-click** — `clickCount=2` when second press ≤0.35 s on the same event root (currently hardwired 1 at `VrUiCursor.cs:424`).
6. **Laser color fix** — remove double `/255` (`VrControllerLaser.cs:36`); `LaserColor` is already normalized.
7. Settings panel: add toggles for haptics + thumbstick scroll (follow existing `NativeVrUiSettingsPanel` patterns).

## Batch 2 — HUD correctness (`NOVR/Patches/HUD/`, `NOVR/VrUi/Components/`)

Owner files: `ThreatItemPatch.cs`, `HUDUnitMarkerPatch.cs`, `HUDBoresightStatePatch.cs`, new `Patches/HUD/HitMarkerPatch.cs`, new `Patches/HUD/HeadMountedDisplayPatch.cs`, `NOVRTargetDesignatorBehavior.cs`, `DynamicMapRotationSafePatch.cs`, `NOVRFlightHudBehavior.cs`, `ModConfiguration.cs` (HUD section entries), `NativeVrUiSettingsPanel.cs` (HUD sliders).

1. **Blink-color math** — `Color.Lerp` t = `Mathf.Sin(t)*0.5f+0.5f` (was `+0.5f` extrapolating): `ThreatItemPatch.cs:90,92`, `HUDUnitMarkerPatch.cs:112`.
2. **Stale lead pipper** — on projection failure hide pipper+line instead of freezing last position (`HUDBoresightStatePatch.cs:89-100`).
3. **Boresight billboarding** — uncomment/fix rotation so the card faces the HUD camera (`HUDBoresightStatePatch.cs:34`).
4. **Hit markers** — new patch: game writes raw `WorldToScreenPoint` pixel coords as world positions (`CombatHUD.DisplayHit`, decompiled evidence in `.decomp-tmp/CombatHUD.cs:37-50`); re-project onto the 1000 m HUD sphere via `VrHudProjectionHelper` like other markers.
5. **HMD hide-cone fix** — the game's `HeadMountedDisplay.Update` hide logic (apps hide within `hideDistance` of pinned `HUDCenter`) wrongly hides speed/alt/bearing/horizon when looking forward (evidence: `.decomp-tmp/HeadMountedDisplay.cs:85-137`). Patch so the four HMD apps stay visible in VR (follow how other patches neutralize game logic; decompiled source available in `.decomp-tmp/`).
6. **Overshoot config** — `NOVRTargetDesignatorBehavior.cs:13` hardcodes 1.1f; wire the existing dead `TargetDesignatorOvershoot` config entry (default 1.2 per original design intent).
7. **Minimap opacity live-apply** — currently applied only at `Minimize()` (`DynamicMapRotationSafePatch.cs:45-56`); poll/apply each frame or on config change.
8. **HUD scale + opacity** — `HudScale` (0.75–1.5, default 1.0) applied to FlightHud canvas scale; `HudOpacity` (0.2–1.0, default 1.0) via CanvasGroup on the FlightHud canvas. Add sliders to the in-VR settings panel.
9. Static FieldInfo null-safety pass on the touched patch classes (guard + one-time warning log).

## Batch 3 — Comfort, menus, depth band (`NOVR/`, `NOVR/VrUi/Native/`, HUD patches)

Owner files: `NOVRHeadsetData.cs`, `Core.cs`, `NativeVrUiRoot.cs`, `NOVRGameplayUIBehaviour.cs`, `ThreatItemPatch.cs`, `HUDUnitMarkerPatch.cs`, `ModConfiguration.cs` (VR/Experimental entries), `NativeVrUiSettingsPanel.cs` (toggles).

1. **Vertical seat trim** — new `CockpitHeadUpOffset` (float, default 0) applied in `CalibrateTranslation` alongside forward/right; extend per-aircraft save/load with `{aircraftId}_Up` (`ModConfiguration.cs:163-175,211-253`, `Core.cs:147-157`).
2. **Unified recenter** — `RecenterIncludesYaw` (bool, default false = preserve current F9 translation-only); when true, F9 also runs `CalibrateRotation`.
3. **Cockpit head clamp** — soft spherical clamp of the calibrated head translation offset: beyond `CockpitHeadClampRadius` (float, default 0.75 m, 0 = off), excess displacement is scaled to 25% (soft pushback). Apply where `Translation` is composed (`NOVRHeadsetData.cs:113-117`).
4. **Menu auto-anchor** — re-enable/repair `CaptureMenuAnchor()` (`NativeVrUiRoot.cs:408`): capture head yaw+position when the native menu opens and place the menu in front (existing distance/height config). Honor the unused `AnchorResetAfterHiddenSeconds` idea: re-capture anchor if the menu was hidden > N seconds (config `MenuReanchorAfterHiddenSeconds`, default 10, 0 = never re-capture). Stock pause-menu path: on enable, yaw-anchor the `(0,0,3)` placement in front of the head (`NOVRGameplayUIBehaviour.cs`).
5. **Tactical depth band (opt-in prototype)** — `TacticalMarkerDepth` (float, 10–1000, **default 1000 = unchanged**). When < 1000: threat notch indicator + unit markers project at that distance along the same direction ray, with angular size preserved (`localScale *= depth/1000`). `TacticalMarkersOccluded` (bool, default false): also move those marker GameObjects to the `VrUiClippedHud` layer (29) so the cockpit occludes them. Touch `ThreatItemPatch` (notch line+label) and `HUDUnitMarkerPatch` only.
6. Settings panel: toggles for head clamp, recenter-includes-yaw, tactical depth (slider), occluded markers.

## Sequencing & integration

Batches run **sequentially** in order 1 → 2 → 3 (shared files: `ModConfiguration.cs`, `NativeVrUiSettingsPanel.cs`, HUD patches). Each batch: implement → validate build (0 errors, no new warnings) → commit with `[Area] Brief description` style message. Final: full compile-only build + summary. In-headset tuning of new tunables is left to the user (called out in the final report).

## Explicit non-goals for this pass

Attitude sphere, 360° RWR ring, peripheral halo redesign, MFD near-field migration/clickable MFDs, gaze declutter, VR keyboard, comfort vignette, controller flight input, release/patch-pipeline fixes. These remain per `reports/vr-hud-redesign-directions-2026-07-16.md` sequencing steps 3–5.
