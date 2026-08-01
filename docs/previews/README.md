# Preview renders

**These are not the review screenshots and they do not satisfy acceptance
criterion 15.** They come from `Tools/preview_render.py`, a small software
rasteriser written because this project was authored without Unity and without a
GPU, and a room nobody has ever seen is a room nobody can review.

Regenerate with:

```bash
python3 Tools/preview_render.py --width 1280 --height 720 --exposure 0.46
```

There is also an interactive version. `Tools/export_web_walkthrough.py` bakes
the same shading into vertex colours and writes a self-contained WebGL page you
can walk around, including on a phone:

```bash
python3 Tools/export_web_walkthrough.py --tessellate 1.2   # -> docs/walkthrough.html
```

## Material swatches

`Tools/preview_textures.py` renders the material set on its own, one square
metre each, relit so the normal map reads:

```bash
python3 Tools/preview_textures.py --size 512
```

Output is `docs/previews/materials/`. It mirrors `ShedTextureGenerator.cs`,
which is the source of truth; if the two drift, the C# is right.

## What it does

Reads `Assets/Game/Scripts/Environment/ShedDimensions.cs` - the same table the
scene generator reads - reconstructs the shed in Python, and rasterises it with
a z-buffer. Shading is flat per-triangle (which is what the generated meshes
actually have), with:

- geometry tessellated to ~300 mm before shading, so light varies **across** a
  surface rather than per face. Without this a six-metre floorboard is one flat
  tone and the sun pool from the window cannot appear at all
- one directional sun at the same angle and rough intensity as `LightingBuilder`
- a shadow test that only lets sunlight in where a ray from the surface leaves
  through the window or the vent, so the sun pool on the bench is real
- a crude window fill term standing in for the bounce a bake would produce
- the ceiling lamp and bench task light as point lights

## What it is good for

Scale, proportion, sightlines, composition, prop placement, whether the
framing and roof structure read correctly, and whether the room looks like an
ordinary shed. It answers the question the milestone most needed answered and
could not otherwise.

## What it cannot show

HDRP materials, the procedural textures, real global illumination or bounce
light, soft shadows, contact shadows, ambient occlusion, exposure adaptation,
reflections, the bevels on every edge, or anything at all about performance.
The interior will look considerably better once lighting is baked in Unity.

Two deliberate differences from the real scene: the window panes are omitted
(the rasteriser has no transparency, so glass would render as an opaque bright
rectangle and hide the view outside), and the sheathing between the studs is
darkened by hand to stand in for the cavity occlusion a real renderer computes.

## The views

| File | View |
| --- | --- |
| `01_entrance_to_workbench.png` | From the door, down the length toward the bench |
| `02_workbench_to_entrance.png` | From the bench back toward the door |
| `03_rear_corner_overview.png` | Rear corner, most of the room |
| `04_workbench_closeup.png` | Bench top, vice end, window above |
| `05_electrical_utility_area.png` | Consumer unit and conduit on the utility wall |
| `06_ceiling_and_roof_structure.png` | Looking up at rafters, purlins and sheeting |
| `07_floor_and_object_contact.png` | Floorboards, service hatch, object contact |
| `08_window_and_exterior.png` | Through the window toward the outside |

The camera positions are the same eight defined in
`Assets/Game/Editor/Validation/ScreenshotCapture.cs`, so these previews and the
eventual Unity screenshots are directly comparable.

## What this pass found

Five real defects, four of them in the Unity source rather than in the preview:

| Finding | Where | Fix |
| --- | --- | --- |
| Gable sheathing stopped at the rafter underside, leaving an open slot the full length of both gable rakes - daylight straight through the roof junction | `ShellBuilder.AddGablePrism` | Taken up to `RoofBuilder.TroughY` instead, matching the eaves blocking |
| Window sill board's top face was coplanar with the framing sill trimmer, which would z-fight into a speckled mess right where the player leans in | `OpeningsBuilder.BuildWindow` | Sill board now sits **on** the trimmer rather than flush with it |
| The top-shelf sack used the tarpaulin material, so a bag of feed rendered as a folded blue groundsheet | `PropsBuilder.BuildShelfContents` | Switched to the card material - a paper sack |
| The utility wall is by a clear margin the darkest surface in the room | Lighting | Not a defect yet, but it is the first thing to check after baking. Recorded in KNOWN_ISSUES |
| Camera basis was left-handed-wrong, mirroring every image | `preview_raster.Camera` | Preview only; fixed |

That is the argument for keeping this script working. It costs 25 seconds to
render all eight views and it has already paid for itself twice.


## Kept in step with the scene

The reconstruction in `Tools/preview_render.py` is a second implementation of
the same room, not a render of the real one, so it drifts unless it is kept up
deliberately. Two things it now mirrors that it did not before:

- the five carryable objects, from `CarryablesBuilder.Placements`
- the ridge cap offset and width, from `RoofBuilder.CapCentreOffset` / `CapWidth`

That second one earned its keep immediately. The bright line down the apex in
earlier copies of `06_ceiling_and_roof_structure.png` was not a rendering
artefact - it was 6.5 mm of real sky through an unclosed ridge, and the render
is what proved the first attempt at closing it had not worked.
