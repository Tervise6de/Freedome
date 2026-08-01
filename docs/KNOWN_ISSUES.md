# Known issues

An honest account of what is unverified, what is a placeholder, and what is most
likely to be wrong.

---

## The headline

**This project has never been opened in Unity.** It was authored in a Linux
container that has no Unity installation, no GPU and no display, and whose
network policy blocks Unity's download servers. The environment audit that
established this:

| Check | Result |
| --- | --- |
| Unity installed | None found anywhere on the filesystem |
| Unity Hub | Not installed |
| .NET / Mono / MSBuild | Not installed |
| `download.unity3d.com` | HTTP 403 at the proxy (policy denial) |
| `public-cdn.cloud.unity3d.com` | HTTP 403 at the proxy |
| `services.unity.com` | HTTP 403 at the proxy |
| GPU | No `/dev/dri`, no VGA device |
| Display | None |

Unity could not be installed and could not have been run if it had been.
Consequently:

- **The scene has never been generated.** No builder has executed.
- **No C# has been compiled.** Not the runtime, not the editor code, not the
  tests.
- **Nothing has been rendered.** No screenshot exists.
- **Nothing has been profiled.** Every performance figure in the docs is a design
  intent, not a measurement.
- **No Windows build has been produced.** `Builds/Windows/ShedRoomDemo/` is
  empty.
- **No test has run.** The suites are written but unexecuted.

What *was* verified, by two independent passes that read the same dimension
table the scene generator reads:

- **Scale drawings** (`docs/diagrams/`, from `Tools/generate_diagrams.py`) -
  confirmed proportions, layout, circulation width and human-scale
  relationships, and caught three placement faults.
- **Preview renders** (`docs/previews/`, from `Tools/preview_render.py`) - a
  software rasterisation of the reconstructed room from the eight review
  viewpoints. Confirmed that the framing, roof structure, openings and prop
  placement read correctly, and caught three more real defects in the C#.

Both are recorded below. Neither is a Unity render: no HDRP material, no baked
light, no texture and no performance figure has been seen.

Treat everything else as a careful first draft that compiles in the author's head.

### What to do first

```bash
# 1. Open in Unity 6 with the Windows Build Support (Mono) module.
# 2. Fix whatever compile errors appear. Expect some.
# 3. In the editor:
#      Freedome > Configure Project Settings
#      Freedome > Generate > Everything (textures, materials, scene)
#      Freedome > Validate Scene and Settings
# 4. Window > Rendering > Lighting > Generate Lighting
# 5. Freedome > Capture Review Screenshots
# 6. ./Tools/run_tests.sh all
# 7. ./Tools/build_windows.sh
```

---

## Likely to be wrong

Ordered by how likely they are to bite, with what to do about each.

### 1. HDRP API surface

**Risk: high.** `LightingBuilder`, `ShedMaterialLibrary` and `ProjectConfigurator`
call HDRP APIs that move between versions: `HDMaterial.ValidateMaterial`,
`HDAdditionalLightData.SetIntensity`, `SetShadowResolution`, `EnableShadows`,
volume override property names (`Exposure.limitMin`, `AmbientOcclusion.intensity`,
`PhysicallyBasedSky.groundTint`, `VisualEnvironment.skyType`), and
`HDRenderPipelineAsset` creation.

Most of these are stable in HDRP 17, but this is the code most likely to fail to
compile on first open. Every call site is small and isolated; expect to fix
member names rather than rewrite logic.

### 2. Assembly definition references

**Risk: medium.** The asmdefs reference `Unity.RenderPipelines.HighDefinition.Runtime`,
`.Editor`, `Unity.RenderPipelines.Core.Runtime`, `.Editor` and `UnityEngine.UI` by
name. These names are stable, but a mismatch produces a wall of type-not-found
errors that looks worse than it is. Check the asmdef inspector first.

### 3. Package versions

**Risk: medium.** `Packages/manifest.json` pins HDRP 17.0.4, ProBuilder 6.0.4,
Test Framework 1.4.5. If the installed Unity 6 patch resolves different versions,
let the package manager update them rather than forcing these.

### 4. The eaves light leak

**Risk: medium, visual.** The row of crescents along the top of both long walls
(see LIGHTING.md) is geometrically correct but has never been seen. It could read
as a lovely row of highlights or as an obvious seam. Tunable via
`RoofBuilder.TroughY()`. Look at this early.

### 5. Unbaked lighting

**Risk: certain until fixed.** The scene ships unbaked. Until lighting is baked,
the interior has direct light and sky ambient only and will look flat and dark in
the corners. Do not judge the art direction before baking.

### 6. The utility wall is the darkest surface in the room

**Risk: medium, visual.** The preview render of the electrical area
(`docs/previews/05_electrical_utility_area.png`) shows the far gable end
noticeably darker than anywhere else - it is 6 m from the window and its own
vent is off to one side. The preview has no bounce light, so a bake will improve
it, but this is the wall the acceptance criteria most plausibly fail on
("lighting readable throughout", "dark areas retain visible detail"). Check it
first after baking. If it is still too dark, the honest fixes in order are:
widen the vent's contribution, raise the ceiling lamp's output, or move the
lamp toward the utility end - not a fill light with no fixture.

### 7. Geometry intersections at the eaves

**Risk: medium, visual.** The tapered eaves blocking is generated per rafter bay
and butts against rafters whose position is computed independently. Small
overlaps or gaps at those junctions are plausible. Screenshot 6 (ceiling and roof
structure) is the one that would show it.

### 8. Wood grain direction

**Risk: low, visual.** UVs are planar-projected against each face's dominant
axis, so grain direction follows geometry rather than following each board's
length. On a stud seen face-on it is right; on some faces the grain will run
across the board instead of along it. Fixing it properly means per-object UV axis
control in `MeshBuilder`. Probably not noticeable at 1 m; check screenshot 4.

### 9. Prop contact with the floor

**Risk: low.** Props are placed by computed base points, not dropped onto
collision. Anything whose local origin is not exactly at its base will float or
sink by a few millimetres. Screenshot 7 exists specifically to catch this.

### 10. `ProjectVersion.txt`

**Risk: low, cosmetic.** `6000.0.58f1` was written without a Unity install to
confirm the patch number. Any 6000.0.x opens the project with an upgrade prompt.

### 11. Legacy input assumption

**Risk: low.** The controller uses the legacy `Input` class. The new Input System
package is deliberately absent from the manifest so the old input handling stays
active. If anyone adds `com.unity.inputsystem`, set Active Input Handling to
"Both" or the controller stops responding.

### 12. Pause menu at non-16:9 aspect ratios

**Risk: low.** The canvas scales with a 1920 x 1080 reference at match 0.5.
Untested at ultrawide or 4:3.

### 13. Screenshot capture without a bake

**Risk: low.** `ScreenshotCapture` renders through a temporary camera. If it is
run before lighting is baked the shots will show the unbaked room, which is not
what the review is meant to judge.

---

## Placeholders

Fully detailed in [ASSET_REGISTER.md](ASSET_REGISTER.md).

| Item | Status |
| --- | --- |
| All 30 generated texture maps | **Placeholder quality.** Procedural approximations with correct roughness and scale; not authored or scanned material |
| Audio | **Absent.** No ambience, footsteps or room tone |
| Prefabs | **Absent.** Reuse is via `PropLibrary` mesh appending, not prefab assets |
| HDRP asset | **Defaults.** Created untuned |
| `docs/screenshots/` | **Empty.** Nothing has been rendered |
| `docs/BUILD_REPORT.md` results table | **Empty.** No build has run |
| Performance figures | **None exist.** Every number in the docs is an intent |

---

## Fixed during authoring

Recorded because the method that caught them is worth repeating.

| Issue | Found by | Fix |
| --- | --- | --- |
| Wheelbarrow handles passed through the storage wall | Floor plan drawing. At -118 deg its 1.73 m length reached x = -2.12 against a wall at -2.00 | Reparked parallel to the utility wall at (-0.90, 2.55), yaw -90 |
| Lawnmower overlapped the boot tray by the door | Floor plan drawing | Moved from z = -1.95 to z = -1.55 |
| Sawhorse clipped the wheelbarrow's footprint | Floor plan drawing | Moved to (-0.60, 1.35) |
| Service hatch did not align to the floorboards | Design review - a hatch cut by a builder follows the boards | Widened to exactly five 140 mm boards, 700 mm, recentred at x = -0.62 |
| Ceiling light hung between collar ties | Design review | Moved from z = -0.20 to z = 0.0, onto the tie |
| Door rough height left no top clearance | Arithmetic check | Rough height changed to leaf + jamb + 10 mm |
| Gable prism left triangular gaps at the top corners | Geometry review | Extended past the wall face by the full wall thickness |
| Gable sheathing stopped at the rafter underside, leaving an open slot the full length of both gable rakes | Preview render 03 - sky visible through the roof junction | Taken up to `RoofBuilder.TroughY` instead, matching the eaves blocking |
| Window sill board's top face was coplanar with the framing sill trimmer | Preview render 08 - z-fighting speckle right where the player leans in | Sill board now sits on the trimmer rather than flush with it |
| Top-shelf sack used the tarpaulin material, so a bag of feed read as a folded blue groundsheet | Preview render 02 | Switched to the card material - a paper sack |

Drawing the room to scale, and then rasterising it, from the same numbers the
generator uses caught six faults that no amount of reading the code would have.
That is the argument for keeping `Tools/generate_diagrams.py` and
`Tools/preview_render.py` working.

---

## Not implemented, by design

Excluded from this milestone and not to be added to it: countdown timer,
game-over, inventory, item collection, object combination, kidnapper AI,
escape-route logic, puzzles, evidence journal, narrative clues, dialogue, save
system, combat, multiplayer, runtime generative AI, procedural puzzle generation.

Also absent, as scope decisions rather than omissions: jumping (nothing to jump
onto, and it is the easiest way to clip out of a building), sprinting (a shed is
6 m long), and any interaction system at all.
