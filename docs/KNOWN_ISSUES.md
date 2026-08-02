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
- **No C# has been compiled by Unity.** It has since been compiled by Roslyn
  against stand-in assemblies - see the compile check below - which is not the
  same thing, but is no longer nothing.
- **Nothing has been rendered.** No screenshot exists.
- **Nothing has been profiled.** Every performance figure in the docs is a design
  intent, not a measurement.
- **No Windows build has been produced.** `Builds/Windows/ShedRoomDemo/` is
  empty.
- **No test has run in Unity.** 53 of them run outside it; see the compile check
  below.
- **Nothing has been interacted with.** No door has opened, no object has been
  picked up, no dropped rigidbody has landed on anything, and nobody has ever
  escaped from the shed.

What *was* verified, by four independent passes:

- **Scale drawings** (`docs/diagrams/`, from `Tools/generate_diagrams.py`) -
  confirmed proportions, layout, circulation width and human-scale
  relationships, and caught three placement faults.
- **Preview renders** (`docs/previews/`, from `Tools/preview_render.py`) - a
  software rasterisation of the reconstructed room from the eight review
  viewpoints. Confirmed that the framing, roof structure, openings and prop
  placement read correctly, and caught three more real defects in the C#.
- **Compile check** (`./Tools/compile_check.sh`) - all 9,000 lines type-checked
  with Roslyn against hand-written Unity stand-ins, plus 53 of the 57 EditMode
  tests actually executed. Caught five defects, two of them hard compile
  failures. See [Tools/CompileCheck/README.md](../Tools/CompileCheck/README.md).
- **HDRP API check** (`./Tools/verify_hdrp_api.sh`) - every HDRP symbol the
  project uses, checked against HDRP's published source instead of against the
  stubs. Caught a sixth defect, also a hard compile failure. This is the one
  thing the compile check is structurally incapable of doing for itself.

None of the four is a Unity render or a Unity compile: no HDRP material, no
baked light, no texture and no performance figure has been seen, and no Unity
editor has resolved the HDRP package.

Treat everything else as a careful first draft.

### What the two static checks change, and what they do not

The compile check closes "does the C# parse, resolve and hold together", and it
turns the dimension table and `MeshBuilder` from asserted into tested. On its
own it says nothing about HDRP - its stubs describe what HDRP's API *should*
look like, so they agree with the project by construction.

The HDRP check closes that gap from the other side, by reading the real package
source. Between them, six defects were found that no amount of re-reading this
project's own code would have surfaced.

What neither touches is behaviour. Every call site now resolves against a real
declaration; whether the room then looks like the design says it will is still
entirely unknown, and needs an editor, a bake and a GPU.

### Getting it to run here instead

The blocker is the environment's network policy, which is changeable, plus a
GPU, which is not - cloud sessions have 4 vCPUs, 16 GB RAM and 30 GB of disk,
and no hardware option exists. `Tools/bootstrap_unity.sh` installs Unity
headlessly and runs the whole pipeline once the domains are allowed. See
[RUNNING_IN_CLOUD.md](RUNNING_IN_CLOUD.md) for the exact steps and for which
criteria that would and would not close.

### What to do first

```bash
# 1. Open in Unity 6 with the Windows Build Support (Mono) module.
# 2. Fix whatever compile errors appear. Fewer than there were - see above -
#    but nothing has been through the real compiler, so do not assume zero.
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

**Risk: downgraded from high to low.** Every HDRP symbol the project uses has
now been checked against HDRP's published source rather than against an
assumption:

```bash
./Tools/verify_hdrp_api.sh
```

That clones Unity's public `Unity-Technologies/Graphics` mirror and greps the
real package. All 40-odd symbols resolve - `HDMaterial.ValidateMaterial`, the
whole `HDAdditionalLightData` surface, every volume override property name,
every enum member.

It found one hard compile error, now fixed: **`AmbientOcclusion` was renamed to
`ScreenSpaceAmbientOcclusion` in 2022.2**, and what is left under the old name
is an empty `[Obsolete]` shell that does not derive from `VolumeComponent`. So
`profile.Add<AmbientOcclusion>(true)` failed the generic constraint outright,
and all three property accesses under it failed too.

Two caveats keep this from being closed entirely:

- The public mirror's tags stop at HDRP 10, so the check runs against the tip
  of `master` - 17.6.0 at the time of writing, against the pinned 17.0.4. Same
  major, so the surface is nearly identical, but the signal is asymmetric: a
  symbol *absent* in 17.6 is almost certainly broken in 17.0.4, while a symbol
  *present* in 17.6 could in principle have been added after 17.0.4.
- Three members are deprecated-but-legal, and the project uses them knowingly:
  `SetIntensity` (`#from(2023.3)`), `shapeRadius` (`(UnityUpgradable)`) and
  `innerSpotPercent` (`#from(6000.3)`, so not yet deprecated in 17.0.4 at all).
  All three are warnings, not errors. Worth migrating eventually; not worth
  churning the light rig before anything has ever rendered.

What is still unverified is behaviour: that these calls do what the lighting
design assumes once HDRP actually executes them.

### 2. Interaction, none of which has ever run

**Risk: high, and entirely unobserved.** The interaction layer is the newest
code in the project and the least verifiable without a running game. Specific
things to look at first, in order:

- ~~**Does the door clear what is around it?**~~ **Checked.** The swept arc is
  sampled at 24 angles by 9 points along the leaf, and every sample must stay
  outside the interior wall face. This found the door opening the *wrong way*:
  the angle was -92, which under Unity's left-handed rotation swings an 820 mm
  leaf into a 4 m room, while the comment beside it said outward. Now +92, with
  `DoorSwingsOutwardThroughItsWholeArc` holding the sign.
  Still unchecked by anything but arithmetic: whether the leaf fouls the tee
  hinges or the weatherboard as it passes, which needs geometry the test does
  not model.
- **Does the blocker switch off in time?** `HingedPart` disables the door's
  blocker as soon as the leaf moves past 1 degree. If that reads as the player
  being able to walk through a nearly-shut door, raise the threshold.
- **Do dropped objects settle?** They are dropped 550 mm in front of the player
  at 120 mm up, with a sphere check to avoid dropping into geometry. Rigidbodies
  landing on a procedural floor are exactly where jitter shows up.
- **Is the switch hittable?** Its collider is grown to 90 x 90 mm, larger than
  the 30 x 46 mm rocker it draws. Too large and it will catch raycasts aimed at
  the wall beside it.
- **Does the held object clip the camera?** Hold offsets are guesses -
  `Carryable.holdOffset`, roughly 0.3 m right and 0.5 m forward. The near clip
  plane is 0.05 m.
- **Baked lighting on moving parts.** The door leaf, panel, rocker and
  carryables are marked movable, so they take no lightmap and rely on probes.
  Whether the probe volume actually covers where a carried object goes is
  unknown.

- **Do the drawers behave?** They slide 300 mm out into the room on the -X axis.
  Nothing has checked that against the player's own collider standing at the
  bench, or that the drawer box clears the carcass it sits in.
- **Does stowing survive?** Taking an object into the inventory deactivates its
  GameObject and reparents it under the player. Deactivating a rigidbody
  mid-scene and waking it up somewhere else is exactly where physics surprises
  live, and an object taken out of a drawer that is then shut has never been
  tried.

The arithmetic that *is* checked - hinge offsets, carryable placements, reach -
is in `InteractionTests`, and it caught one real fault already: a jar of fixings
placed 350 mm in front of the bench, at bench height, in mid-air.

### 3. The escape chain, which nobody has played

**Deadlock is now checked.** `TheChainCannotDeadlock` walks
`EscapeRouteBuilder.Chain` and requires every tool to be reachable strictly
before the step needing it. That is the one class of escape-room bug that makes
a room literally unfinishable, and it takes one careless move to create - the
test fails if the offcut is put behind the drawer it opens.


**Risk: high, and unanswerable here.** The chain is four beats long and every
one of them is checked arithmetically - the crawl space is deep enough, the
skirt board is in the wall line and in line with the hatch, the tools exist.
None of that is the real question.

The real question is whether it is **discoverable**. A player who never thinks
to look under the floor never finds the way out, and there is deliberately no
hint system to rescue them. Specific worries:

- **Is the panel readable as screwed down?** Its four countersunk screws are
  3 mm cylinders. They have never been rendered at any size. The panel is a dead
  end now, so this matters less, but a dead end that is invisible is just a
  floor.
- **Does anyone open the drawer?** The screwdriver is in it. Nothing says so.
- ~~**Can a character controller physically get down the hatch?**~~ **No, and
  the route changed because of it.** The underfloor void is 230 mm of clear
  space; a person needs 350 to 400 mm to crawl. That route was impossible in the
  world, not merely in Unity. The way out is now the door's rim lock - see
  DECISIONS.md. `NothingInTheRouteRequiresCrawlingUnderTheFloor` holds the line.
- **Is the rim lock case readable as something screwed on?** It now has four
  countersunk fixing screws near its corners, in both the generator and the
  preview reconstruction, because a case with no visible fixings gives the
  player no reason to think it comes off. Still never rendered at any size, and
  5.5 mm screw heads at arm's length is the whole question.
- **Does the drawer read as stuck rather than as broken?** The prompt says
  "Swollen shut - it will not pull". If a player reads that as "this drawer is
  scenery" they never come back to it with the offcut.

### 4. Assembly definition references

**Risk: medium.** The asmdefs reference `Unity.RenderPipelines.HighDefinition.Runtime`,
`.Editor`, `Unity.RenderPipelines.Core.Runtime`, `.Editor` and `UnityEngine.UI` by
name. These names are stable, but a mismatch produces a wall of type-not-found
errors that looks worse than it is. Check the asmdef inspector first.

### 5. Package versions

**Risk: medium.** `Packages/manifest.json` pins HDRP 17.0.4, ProBuilder 6.0.4,
Test Framework 1.4.5. If the installed Unity 6 patch resolves different versions,
let the package manager update them rather than forcing these.

### 6. The eaves light leak

**Risk: medium, visual.** The row of crescents along the top of both long walls
(see LIGHTING.md) is geometrically correct but has never been seen. It could read
as a lovely row of highlights or as an obvious seam. Tunable via
`RoofBuilder.TroughY()`. Look at this early.

### 7. Unbaked lighting

**Risk: certain until fixed.** The scene ships unbaked. Until lighting is baked,
the interior has direct light and sky ambient only and will look flat and dark in
the corners. Do not judge the art direction before baking.

### 8. The utility wall is the darkest surface in the room

**Risk: medium, visual.** The preview render of the electrical area
(`docs/previews/05_electrical_utility_area.png`) shows the far gable end
noticeably darker than anywhere else - it is 6 m from the window and its own
vent is off to one side. The preview has no bounce light, so a bake will improve
it, but this is the wall the acceptance criteria most plausibly fail on
("lighting readable throughout", "dark areas retain visible detail"). Check it
first after baking. If it is still too dark, the honest fixes in order are:
widen the vent's contribution, raise the ceiling lamp's output, or move the
lamp toward the utility end - not a fill light with no fixture.

### 9. Geometry intersections at the eaves

**Risk: medium, visual.** The tapered eaves blocking is generated per rafter bay
and butts against rafters whose position is computed independently. Small
overlaps or gaps at those junctions are plausible. Screenshot 6 (ceiling and roof
structure) is the one that would show it.

### 10. Prop contact with the floor

**Risk: low.** Props are placed by computed base points, not dropped onto
collision. Anything whose local origin is not exactly at its base will float or
sink by a few millimetres. Screenshot 7 exists specifically to catch this.

### 11. `ProjectVersion.txt`

**Resolved.** `6000.0.58f1` was originally a guess. It has since been checked
against the list of released editor builds (via the GameCI image tags, which are
published per real Unity release) and it is a genuine 6000.0 patch - one of 79.
The invented revision hash that sat beside it has been removed, since a wrong
one can stop Unity Hub locating the install.

### 12. Legacy input assumption

**Risk: low.** The controller uses the legacy `Input` class. The new Input System
package is deliberately absent from the manifest so the old input handling stays
active. If anyone adds `com.unity.inputsystem`, set Active Input Handling to
"Both" or the controller stops responding.

### 13. Pause menu at non-16:9 aspect ratios

**Risk: low.** The canvas scales with a 1920 x 1080 reference at match 0.5.
Untested at ultrawide or 4:3.

### 14. Screenshot capture without a bake

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
| The ridge cap wings stopped 3.2 mm short of the centreline each, leaving a 6.5 mm slot straight through the apex for the full 6.9 m of the ridge - daylight down the middle of the ceiling, and rain into the room. Visible as a hard bright line down the apex in preview 06 | Reading `RoofBuilder.BuildCovering`, then confirmed in the render | Wings widened to 340 mm and taken up the slope, so they cross the centreline by 43 mm. The first attempt crossed by only 6 mm and **still showed the line**: the wings are 6 mm boxes in two planes meeting at the apex, so they overlap in height only within 7.4 mm of it, and the chamfered edges landed inside that band. `RidgeCapClosesTheApex` now compares against `RidgeCapSealBandHalfWidth` rather than against zero |
| **The whole -X roof slope had no sheeting.** `AddCorrugatedSheet` lays a sheet from a corner, and `SlopeRotation`'s local +X runs down the +X slope but *up* the -X one - a sign that no box in the roof cares about, because a box is symmetric about its centre. So the -X sheet was laid from the ridge upwards and over: both sheets ended up on the workbench side, one of them floating above the ridge, and the storage half of the building was open to the sky | Ray-casting the built covering mesh in `NothingSeesThroughTheRoof`. Every arithmetic check of this roof had passed, including one written specifically for the ridge, because the sums being checked were not the sums the builder used | New `SheetRotation`, mirrored properly between the slopes, used only for laying sheet. The test now casts 6,710 vertical rays through the built triangles instead of comparing numbers; with the old rotation restored it reports 471 open columns starting at x = -2.1 m |
| The ridge cap's inner edge was computed without the horizontal component of its own lift. Offsetting the cap along the slope normal moves it sideways by sin(pitch) x lift - 58 mm here - so `RidgeCapInnerEdgeX` reported 43 mm past the apex while the real edge sat 15 mm short of the ridge board face, leaving a 2.5 mm slot down each side of the board for the full 6.9 m. The test agreed with the code by construction and passed throughout | The same ray cast | Cap width and offset are now derived from `CapOverlapPastApex` - how far past the centreline each wing has to reach - and `RidgeCapInnerEdgeX` reads the corner off the box the builder places, through the same rotation |
| The corrugation ran the wrong way. The profile modulated along the sheet's local x, which `SlopeRotation` puts down the slope, so the ribs lay parallel to the ridge: every trough would have held water, and the eaves blocking - cut to the trough height precisely so the crests would leave crescents of daylight above the walls - met a sheet of constant height and left none | Reading the sheet builder against what LIGHTING.md says the eaves do | Ribs now run down the slope. The crescents described in LIGHTING.md are geometry for the first time |
| The sheet was centred on the purlin face rather than resting on it, so the trough line cut 8 mm into every purlin it crossed. Invisible while the ribs ran the wrong way; the moment they ran down the slope it became a dotted line of bare timber from ridge to eave on every purlin | Preview render, exterior, after the corrugation was turned | Sheet lifted by half the amplitude so it sits on the purlin at its troughs. `TroughY` loses its amplitude term, which is what the eaves blocking and the gable infill both close up to |
| A finished `ToolGatedFixture` kept its collider, and its collider sits in front of the thing it is holding shut. On the drawer, where it covers the whole drawer front, that meant a drawer that had just been levered free could never be opened - the way out of the shed stopped at step two, with the screwdriver visible through a drawer that would not move | Tracing the interaction ray from `PlayerInteractor.FindFocus` through what the generator actually parents where | The fixture disables its collider once its job is done, so the thing behind it becomes reachable. `AFinishedFixtureStopsBlockingWhatIsBehindIt` covers it |
| `Carryable.Release` re-parented a dropped object to whatever it was originally a child of. The screwdriver and the folding rule start life parented to a drawer, so putting either one down in the middle of the room re-adopted it, and closing the drawer afterwards dragged it back across the floor | Reading `Release` against how the drawer contents are built | Dropped objects go back to the container's parent, not the container |
| The screwdriver sat 7 mm inside the drawer bottom and the folding rule floated 6 mm above it. Both offsets were worked by hand against a carcass whose dimensions are derived, and neither is visible until somebody opens the drawer - which, in this shed, means levering it with a piece of timber first | Arithmetic, prompted by being told the screwdriver looked wrong | Drawer contents are a table with half-extents, resting height derived from the box, and two tests that check they sit on the bottom board and fit between the sides |
| `PlayerInteractor.Drop`'s fallback put the object at the player's own position - inside the character capsule - and its overlap test counted the player's own collider as an obstruction, so the fallback fired far more often than it looked like it would | Reading `Drop` while tracing the interaction ray | It now steps in from arm's length keeping the last clear spot, ignores the player's own collider, and never returns a point inside the capsule |
| The door opened inward. `HingedPart` was configured with -92 degrees, and Unity rotates left-handed, so a leaf at +X swings toward +Z - into a 4 m room - while the comment beside the call said "swings outward, away from the room" | Working out the swept arc for the arc-clearance test | Changed to +92 and exposed as `OpeningsBuilder.DoorOpenAngleDegrees`, with a test that samples the whole arc against the interior wall face |
| Top-shelf sack used the tarpaulin material, so a bag of feed read as a folded blue groundsheet | Preview render 02 | Switched to the card material - a paper sack |
| Wood grain ran across every horizontal member instead of along it. UVs projected against each face's dominant world axis, and the side of a top plate faces sideways whichever way the plate runs, so V ended up vertical on plates, rafters, purlins, collar ties, noggins and bench rails | Reading `MeshBuilder.PlanarUv` against what `SamplePine` actually generates | `MeshBuilder` now derives the UV frame from a grain direction: V follows the piece's own longest axis, carried through its rotation, with `GrainOverride` for materials that have a direction the geometry does not imply |
| `[MenuItem("...", priority = N)]` on all eight menu entries. Unity's `priority` is an internal field, so a named attribute argument cannot bind to it - this is a hard compile error, and it took out every entry point to the project's tooling | Compile check | Changed to the positional form, `[MenuItem("...", false, N)]` |
| `Environment.GetCommandLineArgs()` in `WindowsBuild.PerformBuild` resolved to the `Freedome.Environment` namespace, not `System.Environment`, because the file sits inside `Freedome.EditorTools.Build`. Another hard compile error, in the exact method the GitHub Actions workflow names as its `buildMethod` | Compile check | Fully qualified as `System.Environment` |
| `PlayerLook` stopped writing the camera pivot's rotation while input was disabled, but `HeadBob` multiplies its roll into that same value every frame in `LateUpdate`. With the pause menu open the roll had nothing resetting it, so the camera rotated about 21 degrees a second and snapped back on resume | Compile check - the dead field `HeadBob._baseLocalPosition` was the thread to pull | `PlayerLook` now writes the pivot rotation unconditionally; that channel is its to own absolutely |
| `profile.Add<AmbientOcclusion>(true)` in `LightingBuilder`. HDRP renamed the type to `ScreenSpaceAmbientOcclusion` in 2022.2 and left an empty `[Obsolete]` shell behind that is not a `VolumeComponent`, so this failed the generic constraint and took the three property accesses under it with it | `Tools/verify_hdrp_api.sh`, reading HDRP's published source | Renamed to `ScreenSpaceAmbientOcclusion`. The compile-check stub now reproduces HDRP's shape exactly, so the harness catches it too |
| Three grain tests passed with `MeshBuilder.LongestAxis` deliberately broken. They asserted against `UvBounds`, which unions every face of a box - and a box has faces in all orientations, so the V extent is the piece's longest dimension however the grain runs | Mutation-testing the compile check | Added `AssertVRunsAlong`, which checks the V direction on one named face. The mutation is now caught |

Drawing the room to scale, rasterising it, type-checking it and then reading
the real HDRP source caught twelve faults that no amount of re-reading this
project's own code would have. That is the
argument for keeping `Tools/generate_diagrams.py`, `Tools/preview_render.py`,
`Tools/compile_check.sh` and `Tools/verify_hdrp_api.sh` working.

The compile check is also the one of the three that a contributor should run
before every commit: it takes seconds and it is the only one that would have
stopped two hard compile errors reaching the repository.

---

## Not implemented, by design

Excluded from this milestone and not to be added to it: countdown timer,
game-over, inventory, item collection, object combination, kidnapper AI,
escape-route logic, puzzles, evidence journal, narrative clues, dialogue, save
system, combat, multiplayer, runtime generative AI, procedural puzzle generation.

Also absent, as scope decisions rather than omissions: jumping (nothing to jump
onto, and it is the easiest way to clip out of a building), sprinting (a shed is
6 m long), and any interaction system at all.
