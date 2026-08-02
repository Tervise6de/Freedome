# Lighting

A quiet weekday mid-morning. The room has to be clearly readable everywhere, and
it has to look like daylight rather than like a mood.

## Design brief

1. The player can see the whole room. No corner is unreadable.
2. The window is bright but not blown out.
3. Materials stay legible - you can tell timber from painted timber from metal.
4. Darkness is never used to hide unfinished geometry or weak materials.
5. Nothing flickers, nothing is coloured, nothing is theatrical.

---

## Daylight

### Sun

| Property | Value | Why |
| --- | --- | --- |
| Rotation | 46 deg elevation, -122 deg azimuth | Mid-morning. The azimuth puts the sun across the workbench wall at an angle rather than square to it, so light rakes down the bench and reaches the floor instead of stopping at the sill |
| Intensity | 38 000 lux | Bright morning. Full noon sun is around 100 klux and flattens everything through a 900 x 600 window |
| Colour temperature | 5600 K | Neutral daylight |
| Angular diameter | 1.6 | Slightly larger than the real sun, softening the shadow edge |
| Shadows | Soft, 2048, on | |
| Mode | Mixed | Indirect baked, direct at runtime |

### Sky

HDRP's **Physically Based Sky** (`EarthSimple`), with a muted green-grey ground
tint so bounce off the grass outside does not tint the interior. Chosen over an
HDRI because it needs no external asset - which matters given this project uses
no third-party art at all.

Fog is present in the volume and explicitly **disabled**. An interior shed on a
clear morning has none, and adding it is the fastest route to the atmosphere this
milestone is asked to avoid.

### Secondary daylight

A single window at one end of a 6 m room gives a bright end and a black end.
Three further openings fix that, and all three are things the building has anyway:

| Source | Size | Contribution |
| --- | --- | --- |
| Window | 900 x 600 mm, sill 1.20 m | The main source. Direct sun on the bench, bounce onto the ceiling |
| Wall vent | 300 x 200 mm at 2.05 m | A genuine hole through the utility wall. Puts light on the far gable, which is otherwise the darkest part of the room |
| Door undercut | 6 mm x 820 mm | A thin bright line on the threshold |
| Eaves crescents | ~80 per side | Explained below |

### The eaves light leak

The roof is corrugated steel sitting on purlins over the rafters. Each rafter bay
is closed at the eave with a tapered timber block whose top follows the underside
of the corrugation **troughs**. Where the profile rises to a crest it lifts off
the blocking, leaving a small crescent open.

The result is a row of roughly 80 thin crescents of sky light along the top of
each long wall, about 16 mm at their widest. This is exactly what an unfilled
corrugated eave does on a real shed - the foam filler strip is the thing people
leave out - so it is honest construction rather than a lighting cheat. It puts
soft light high on both long walls, which is the single most useful thing that
could happen to a room this shape.

It is tunable: `RoofBuilder.TroughY()` sets the blocking height. Raising it
toward crest level closes the gaps.

**This has not been rendered.** The total open area is roughly 0.05 m2 per side,
which is small, but whether it reads as a pleasant row of highlights or as an
obvious seam is a judgement that needs a frame on screen. It is the first thing
to look at in the lighting review.

---

## Practical fittings

### Ceiling lamp

Hung from the collar tie at the centre of the room (z = 0), bulb at 2.300 m.
Modelled as a mounting board, a ceiling rose, a flex, a lampholder, a plain
enamel shade and the bulb.

| Property | Value |
| --- | --- |
| Type | Point, soft shadows, 512 |
| Intensity | 820 lumen |
| Colour temperature | 2900 K |
| Range | 7 m |
| Shape radius | 30 mm |
| Mode | Mixed |

820 lm is a 9 W LED lamp - what is actually in a shed. It is warm against the
5600 K daylight, which gives the room two distinguishable light colours without
anything being tinted.

### Bench task light

A clamp-on reflector lamp on the pegboard batten, tipped down across the bench.

| Property | Value |
| --- | --- |
| Type | Spot, soft shadows, 512 |
| Intensity | 430 lumen |
| Colour temperature | 3100 K |
| Cone | 78 deg, 55 % inner |
| Mode | Mixed |

Aimed at the bench top rather than at the wall, so it does what a task light does:
puts light on the work and leaves a pool.

---

## Exposure

Automatic histogram metering, centre-weighted, clamped:

| Property | Value |
| --- | --- |
| Limit min | 6.0 EV |
| Limit max | 13.5 EV |
| Dark to light | 3.0 |
| Light to dark | 1.2 |
| Histogram | 45 % - 92 % |

The clamp is the important part. Unclamped auto-exposure in a room with one
bright window and one dim end pumps constantly as the player turns, which reads
as cheap and makes the room feel unstable. A 7.5 EV window is enough to keep both
ends readable without letting either extreme drive the whole image.

Asymmetric adaptation speeds - faster into brightness than into darkness - match
how eyes behave and avoid a lag when the player turns toward the window.

---

## Post-processing

| Override | Setting | Note |
| --- | --- | --- |
| Tonemapping | Neutral | Not ACES. Neutral keeps midtone material detail readable, which is what the review pass needs to judge |
| Colour adjustments | Exposure +0.15, contrast +4, saturation -2 | Near neutral |
| Bloom | 0.06, scatter 0.6 | Just enough to sell the window |
| Ambient occlusion (`ScreenSpaceAmbientOcclusion`) | 0.65, radius 0.35, direct 0.25 | Contact darkening in the framing and under the shelves |
| Contact shadows | On, length 0.15, opacity 0.85 | Small-scale grounding for props |
| Micro shadowing | On, 0.45 | Normal-map detail catching the light |
| Screen-space reflections | On | For the galvanised steel and the glass |
| Motion blur | 0.35, **inactive by default** | Present only so the settings menu has something to toggle |
| Vignette | **not present** | Not added at all |
| Film grain | **not present** | Not added at all |
| Chromatic aberration | **not present** | Not added at all |

---

## Global illumination and probes

**Mixed lighting with shadowmask.** The lightmapper is configured
(Progressive CPU, 20 texels/unit, 3 bounces, 256 indirect samples, 1024 max
lightmap, high-quality compression) but **the scene ships unbaked**.

Bake it with `Tools/build_windows.sh --bake`, or `Freedome > Generate` then
Window > Rendering > Lighting > Generate Lighting.

Without a bake the interior has direct light and sky ambient only. It will be
noticeably flatter and darker in the corners than intended. **Baking is not
optional for the intended look** - it is the difference between the room being
lit and the room being illuminated.

### Reflection probes

| Probe | Extent | Resolution | Importance |
| --- | --- | --- | --- |
| Interior | 4.4 x 3.2 x 6.4 m | 256 | default |
| Workbench | 1.6 x 1.8 x 3.2 m | 128 | 2 |

The second one exists because the bench is where the metal is - the vice, the
tools, the plane - and a room-wide probe reflects the room average onto them.

### Light probes

A 5 x 7 lattice spanning the full interior - out to 60 mm inside each wall
lining - at five heights (0.05, 0.55, 1.10, 1.80, 2.55 m), skipping any position
that would fall inside the roof slope. Denser sampling at the window and the
door, where the indirect gradient actually is.

**The lattice has to reach the walls and the floor.** Probe interpolation is
only defined inside the convex hull of the probes; outside it an object clamps
to whatever the nearest outer tetrahedron holds and stays there, which reads as
lighting going wrong near walls and as a pop when a carried object crosses the
boundary. The first version ran x = +/-1.6 against walls at 2.0 and started at
y = 0.25 over a floor at 0 - so everything resting on the floor, and everything
within 400 mm of a wall, was outside it. That is three of the five loose objects
and most of where the player stands.
`LightingBuilder.ProbeHull()` reports the box; `TheProbeHullReachesTheWallsAndTheFloor`
checks the room and every carryable against it.

---

## Review procedure

`Freedome > Capture Review Screenshots` renders eight fixed viewpoints at
1920 x 1080 into `docs/screenshots/`. All are at standing eye height unless the
shot is deliberately a close-up.

1. Entrance toward the workbench
2. Workbench toward the entrance
3. Rear corner, most of the room
4. Workbench close-up
5. Electrical utility area
6. Ceiling and roof structure
7. Floor materials and object contact
8. Through the window toward the exterior

Check each for:

- Dark areas still showing material detail
- The window bright but not clipped white
- Timber readable as timber, metal readable as metal
- No light leaking through geometry that should be solid
- Contact shadows reaching every object's supporting surface
- Nothing that reads as horror lighting

**Not yet performed** - see [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Tuning notes for whoever runs this first

In rough order of likelihood that they need adjusting:

1. **Bake first.** Do not judge anything unbaked.
2. **Eaves crescents.** If they read as a seam rather than as highlights, raise
   the blocking in `RoofBuilder.TroughY()`.
3. **Sun intensity.** 38 klux is a considered starting point, not a measured
   one. If the window clips, come down before touching exposure.
4. **Exposure clamp.** If the room pumps, narrow the 6.0-13.5 range rather than
   changing light intensities.
5. **Ceiling lamp.** At 820 lm it should be a visible warm presence, not the
   thing lighting the room. If it dominates, the daylight is too weak.


## Lighting things that move

The door leaf, the floor panel, the switch rocker, the window casement and the
carryables are all non-static. They take no lightmap and are lit by the light
probe group instead.

Probe interpolation is only defined inside the convex hull of the probes, so the
grid has to cover everywhere a movable object can go. Two additions:

- **Outside the door.** The door opens, so a carried object can leave the
  building. Without probes out there it would keep sampling interior lighting
  while standing in daylight. Three rows now cover the entrance apron.
- **The floor and the walls.** See above: the interior lattice now reaches
  both, which it did not.

### Adaptive Probe Volumes were considered and not used

APVs would do this better - automatic placement, denser sampling near geometry,
no hand-placed grid to keep in step with the room. They are also a change to the
HDRP asset and the bake pipeline, on a project where nothing has ever rendered a
frame.

Switching now would mean replacing a probe layout whose failure modes are at
least legible with one nobody can inspect until the first bake, and it would
make the first bake harder to debug rather than easier. The probe group is
adequate and explicit. Revisit once the room has been seen.
