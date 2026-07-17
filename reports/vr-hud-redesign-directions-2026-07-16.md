# NOVR — VR-Native HUD: Diagnosis & Redesign Directions

Date: 2026-07-16
Inputs: `reports/in-flight-ui-inventory-2026-07-16.md` (what the game shows), `reports/in-flight-rendering-ux-analysis-2026-07-16.md` (how NOVR renders it), `reports/vr-ui-ux-improvement-findings-2026-07-16.md` (interaction layer)
Scope: high-level directions, not implementation plans.

---

## 1. The core diagnosis

The flat game renders **everything** as a non-occluded screen overlay (only the TacScreen, camera pages, and warning lamps are world-space). So there was never any depth behavior to preserve — NOVR had to invent an anchoring model per element, and what exists today is:

- **One depth**: everything at 850–1050 m apparent distance (optical infinity), zero vergence separation between a missile warning and a fuel gauge, zero stereo disparity, zero parallax when you lean. The attention hierarchy is carried by color and flashing alone, in one flat green plane.
- **Two layers, but used as an on/off switch**: the clipped layer (cockpit-occluded) holds *only* the pitch ladder; everything else draws `ZTest Always` over canopy frame, pillars, and mirrors. Occlusion says "5 cm away", vergence says "infinity" — chronic stereo rivalry, e.g. a 5 km marker painted onto the canopy bow.
- **Instruments floating at infinity**: MFDs, minimap, weapon panels, and gauges are coplanar with the HUD at 1050 m with their contrast backing stripped — the opposite of how cockpit instruments work (near, shaded, stably lit, physically located).

The multi-layer *concept* is right. What's missing is a **depth hierarchy** and **occlusion discipline** — VR's two unique channels that a flat port can't use but a VR-native design can.

## 2. Design principles

1. **Collimation discipline** — elements that must align with the outside world (flight path, boresight, pitch/horizon, target positions) stay at optical infinity. Everything else is a candidate for nearer depth.
2. **Depth as an information channel** — vergence/stereo depth can encode class and urgency instead of more flashing. The eye separates depth layers instantly and pre-attentively.
3. **Combiner-glass discipline** — world-aligned symbology is occluded by cockpit structure (like a real HUD reflected in glass); only helmet-referenced elements may float over the frame.
4. **Instrument-scan ergonomics** — real pilots scan down to panel instruments and up to the HUD. Keeping instruments at panel depth preserves that embodied workflow and its muscle memory.
5. **Peripheral awareness by design** — VR gives ~100° of usable field; the flat game's "pin to screen edge" model maps poorly. Directional cues belong on a sphere around the pilot.
6. **Legibility is engineered, not inherited** — pixel sizes authored for a 1080p monitor are sub-pixel at 1 km in an HMD. Stroke widths, contrast, and brightness must be managed in angular units.

## 3. Proposed depth-band model

Replace "flat plane vs sphere" with four deliberate bands. Directions stay truthful (a marker pulled nearer keeps its exact direction ray — only its distance along that ray changes); angular sizes get clamped so near elements don't balloon.

| Band | Apparent depth | Contents | Rationale |
|---|---|---|---|
| **∞ — Collimated** | 1000 m sphere | Flight path/velocity vector, boresight cross, pippers, pitch/attitude, waypoint diamonds, target positions, gun/bomb solutions | Must optically align with terrain and targets; vergence at rest |
| **Mid — Tactical halo** | ~15–40 m | Threat/notch indicators, RWR contacts, unit markers, off-boresight cueing arrows, missile-approach warnings | Vergence pop-out from the background HUD without more flashing; occluded naturally by canopy frame like a hologram in the cockpit |
| **Near — Instrument scan** | ~0.7–1.2 m | MFD pages, gauges, minimap, weapon/countermeasure panels, checklist/status feeds | Anchored to the cockpit at panel depth; real accommodation shift when scanning down; stably lit with backing plates |
| **Head-locked — minimal** | ~2–4 m (comfortable vergence) | Slim HMD strip (speed/alt/heading), transient warnings, hurt/blackout (with fade) | Only what must survive any head direction; kept dim and minimal |

Urgency can modulate the mid band: a missile inside 5 km pulls its marker closer and brighter — depth as a threat-urgency dial that flat screens don't have. (Comfort-gated by config.)

## 4. Fixing the occlusion model

Today only the pitch ladder depth-tests. The direction: **world-aligned symbology moves to the clipped layer** (occluded by canopy frame, pillars, glareshield — like real combiner glass), while **helmet-referenced elements stay unoccluded**. Two clean classes instead of one arbitrary one. Requires the cockpit/canopy meshes to write depth correctly against that layer (validate canopy glass behavior in-headset).

## 5. Attitude sphere, not a ladder strip

The current ladder is 37 cards along one great circle referenced to world north — flying east/west it sits at your 9/3 o'clock with no attitude reference ahead, and it's the only occluded element, so the least-covered element has the best treatment. Direction: a **spherical/cylindrical attitude reference around the pilot** — pitch lines wrapping azimuth, horizon line always findable in any bank at any heading, roll index on the canopy bow. This is the single biggest "VR-native HUD" win: a real attitude sphere is impossible on a flat screen.

## 6. Peripheral awareness layer

Replace the hardcoded 50°×50° edge-pin ellipse with spherical cueing:

- **Threat halo**: compact arrows/chevrons on a head-referenced ring at the edge of the FOV (derived from the actual headset projection, not a constant) pointing toward off-screen threats, distance/urgency coded. JHMCS-like.
- **360° RWR ring**: extend the existing notch-stripe idea into a full world-referenced ring around the pilot at mid-band depth, so missile and emitter bearings are directional even behind you — today everything behind the aircraft is hard-culled with no cue.
- Keep the existing ellipse pinning for the *selected* target only.

## 7. Deliberate HMD strip

The head-following speed/alt/bearing/horizon cluster currently **hides itself inside ±25–30° of forward** — the game's hide-cone logic misfires against NOVR's pinned HUDCenter. Beyond fixing that, make the strip deliberate: minimal, dimmable, fast-follow (reduce the ~18° lag at 180°/s), and remove the designator's rest-state overshoot (it biases off-axis designation outward by ~the width of the selection cone). Expose the dead `TargetDesignatorOvershoot` config properly or remove it.

## 8. Instruments back into the cockpit

The game already renders TacScreen and camera pages as world-space RenderTextures on physical cockpit screens — the precedent exists. Direction for the uGUI instruments:

- **Panel-anchored near-field presentation**: MFD pages, minimap, and gauges move from the 1050 m plane to ~panel depth, ideally registered to the physical MFD/panel locations per aircraft, with backing plates and dimming.
- **Clickable MFDs**: bezel buttons become laser-targetable — which only works at near depth (today the 50 m laser against a 1050 m plane gives a ~0.0035° cursor: unpointable). This also fixes "MFD background removed, floats against the world" legibility.
- **Minimap as a kneeboard/map display** at panel depth instead of a corner of the infinity plane.

## 9. Legibility engineering

- **Angular minimums**: define stroke widths and text in arcminutes/px-per-degree from the XR display properties, not desktop 1080p compensation (`ReferencePixelsToHudDistance` currently targets the wrong thing — 1-unit strokes are ~1 HMD pixel and shimmer).
- **Contrast**: reinstate subtle backing plates or halo/outline shaders for sky-critical symbology; the deliberate background stripping washed out green-on-cloud.
- **Brightness discipline**: decouple HUD brightness from scene auto-exposure (HUD currently dims over bright sky and overdrives at night); add a dimmer + NVG-safe palette. Validate bloom/TAA effects on the HUD layers in-headset.

## 10. Gaze-adaptive declutter

The flat game's declutter heuristics (hide-distance cones, minimize-by-range) partially misfire in VR. VR offers a better lever: **head-direction-adaptive density** — secondary markers dim/shrink outside a ~30° gaze cone and restore on glance; labels expand when dwelled on. No eye-tracking hardware needed; head pose is sufficient and already available.

## 11. Foundation fixes before/while redesigning

Small, well-scoped, and they erode trust in the HUD daily: unprojected hit markers (land near world origin), stale lead pipper on projection failure, unbillboarded boresight card, minimap opacity applying only at minimize-time, Update-order dependence vs `FlightHud.Update`, and the dead overshoot config. Plus the P0 interaction items from the findings report (focus-independent pointing, drag capture, menu anchoring) — a near-field clickable cockpit depends on them.

## 12. Open questions to validate in-headset

- Mid-band depth values (15–40 m is a starting guess; vergence comfort varies per user — make it configurable).
- Canopy glass depth-write behavior against the clipped layer.
- Whether urgency-driven depth motion on threat markers is empowering or nauseating (config-gate it).
- Post-processing profile contents (bloom/DoF/TAA) and their effect on HUD layers — only auto-exposure is confirmed in code.
- MFDScreen RenderTexture legibility at panel depth vs at infinity.

## Suggested sequencing (high level)

1. **Foundations**: §11 fixes + P0 interaction items — the HUD can't be trusted until these land.
2. **Occlusion + HMD strip**: move world-aligned symbology to clipped layer; fix the hide-cone and HMD lag — immediate daily-use wins, low risk.
3. **Depth bands**: tactical halo (mid band) first — highest perceived "VR-native" jump for threat awareness; then instrument near-field migration (MFDs/minimap/panels), which unlocks clickable MFDs.
4. **Attitude sphere + peripheral halo**: the showcase features; build once the band infrastructure exists.
5. **Polish layer**: legibility engineering, gaze declutter, brightness/NVG discipline.
