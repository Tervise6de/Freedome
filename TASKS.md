# Milestone checklist

Shed room environment milestone. Status is honest: **`[x]` means the work is
written; it does not mean it has run.** Nothing in this repository has been
executed in Unity - see [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md).

Legend: `[x]` authored · `[~]` partially done · `[ ]` not done ·
**(unverified)** written but never executed

---

## Phase 0 - Environment audit

- [x] Installed Unity versions - **none found**
- [x] Available Unity modules - **none, no Unity present**
- [x] Windows build support - **not installed**
- [x] Command-line and Git tools - git 2.43, python 3.11, node 22, curl, jq; no dotnet/mono/msbuild
- [x] Existing Unity project - none; repository was empty
- [x] Network reachability - all Unity domains blocked by proxy policy (403)
- [x] GPU and display - neither present

Conclusion: Unity cannot be installed or run here. Everything below is authored
as source and is ready to run on a machine that has Unity 6.

## Phase 1 - Planning

- [x] Project folder structure
- [x] `README.md`, `CLAUDE.md`, `TASKS.md`
- [x] `docs/ROOM_DESIGN.md`, `ART_DIRECTION.md`, `LIGHTING.md`
- [x] `docs/FUTURE_GAMEPLAY_HOOKS.md`, `ASSET_REGISTER.md`
- [x] `docs/DECISIONS.md`, `KNOWN_ISSUES.md`, `BUILD_REPORT.md`
- [x] `ProjectVersion.txt`, `Packages/manifest.json`, assembly definitions
- [x] `.gitignore`, `.gitattributes`

## Phase 2 - Architectural blockout

- [x] Single source of truth for all dimensions (`ShedDimensions.cs`)
- [x] Chamfered mesh builder with metre-scale UVs
- [x] Floor platform: piers, bearers, joists, individual floorboards
- [x] Four stud walls with plates, studs, noggins and opening trimmers
- [x] Gable infill following the roof line
- [x] Pitched roof: rafters, ridge, collar ties, purlins, sheeting, fascia, barges
- [x] Doorway and window openings framed through frame, sheathing and cladding
- [x] Human-height reference (`ScaleReference`, 1.80 m, with set-out gizmos)
- [x] Scale confirmed - **via generated scale drawings, not screenshots**
- [ ] Blockout screenshots **(cannot render here)**

## Phase 3 - Player controller

- [x] WASD movement, 1.5 m/s walk **(unverified)**
- [x] Mouse look with configurable sensitivity and invert **(unverified)**
- [x] Crouch with a headroom check before standing **(unverified)**
- [x] CharacterController collision, gravity, slope and step handling **(unverified)**
- [x] Escape pause menu, resume, restart, quit **(unverified)**
- [x] Graphics settings: FOV, head bob, motion blur, quality, vsync **(unverified)**
- [x] Boundary backstop that logs and is asserted never to fire **(unverified)**
- [x] No jump - deliberate
- [ ] Playable development build **(cannot build here)**

## Phase 4 - Modular detailing

- [x] Door: ledged-and-braced leaf, jambs, stops, threshold, trim
- [x] Door ironmongery: rim lock, lever, escutcheon, barrel bolt, tee hinges, coach bolts
- [x] Window: lining, frame, glazing bar, beads, glass, sill board, architrave, flashing
- [x] Vent: lining, insect mesh, louvre frame and five blades
- [x] Workbench: laminated top, lipping, legs, rails, lower shelf, drawers, cupboard, vice
- [x] Pegboard on battens with six hand tools; separate tool rack
- [x] Shelving: uprights, cleats, paired boards, diagonal brace
- [x] Consumer unit, socket, switch, full conduit circuit, clipped lighting cable
- [x] Utility shelf with radio and fixings jar
- [x] Coat rack, coiled lead, tool bag, boot tray and boots
- [x] Corner boards, eaves blocking, service hatch with trimmers and screws
- [x] No unmodified primitives left visible

## Phase 5 - Materials

- [x] Ten procedural tiling PBR families, 30 maps
- [x] 23 HDRP/Lit materials with physically reasoned roughness
- [x] Consistent texture scale by construction (metre UVs, 1 m maps, 1:1 tiling)
- [x] Grid-aligned patterns chosen to divide into a metre exactly
- [x] Subtle dust, traffic polish, scratches, chalking - no grime or rust
- [ ] Materials reviewed on screen **(cannot render here)**

## Phase 6 - Prop dressing

- [x] Shelf contents across four levels
- [x] Bench-top items including a mug and a pencil
- [x] Floor objects: mower, wheelbarrow, sawhorse, stool, compost, offcuts, boards, bucket
- [x] Deliberate imperfection: varied angles, one door ajar, one drawer out, one lid off
- [x] Circulation preserved - 2.95 m clear aisle, asserted by test
- [x] Three placement clashes found and fixed via the scale drawings

## Phase 7 - Lighting

- [x] Mid-morning sun at 38 klux, 5600 K, soft shadows
- [x] Physically Based Sky, fog explicitly disabled
- [x] Secondary daylight: vent, door undercut, eaves crescents
- [x] Ceiling lamp (820 lm, 2900 K) and bench task light (430 lm, 3100 K)
- [x] Clamped automatic histogram exposure
- [x] Contact shadows, AO, micro shadowing, SSR
- [x] Neutral tonemapping, near-neutral grade, minimal bloom
- [x] No vignette, no film grain, no fog - absent, not zeroed
- [x] Two reflection probes and a light probe lattice
- [x] Mixed lighting with the lightmapper configured
- [ ] Lighting baked **(deliberately not committed; `--bake` flag provided)**
- [ ] Lighting reviewed on screen **(cannot render here)**

## Phase 8 - Visual review

- [x] Eight required viewpoints defined and asserted to be inside the room
- [x] One-click capture rig (`Freedome > Capture Review Screenshots`)
- [x] Review criteria written out in ART_DIRECTION.md
- [ ] Screenshots captured **(cannot render here - `docs/screenshots/` is empty)**
- [ ] Review performed and faults fixed **(blocked on the above)**

## Phase 9 - Performance review

- [x] In-game overlay (F3): fps, frame time, CPU, GPU, triangles, draw calls, batches, memory
- [x] Generator logs its own renderer and triangle totals
- [x] Performance strategy documented (ROOM_DESIGN section 8)
- [ ] Profiled **(no GPU here)**
- [ ] Results recorded **(BUILD_REPORT table is empty)**

## Phase 10 - Windows build

- [x] `ProjectConfigurator`: HDRP asset, linear colour, 1920x1080, D3D12/11, Mono
- [x] `WindowsBuild`: batch entry point, validation gate, non-zero exit on failure
- [x] Automatic build record appended to `docs/BUILD_REPORT.md`
- [x] `Tools/build_windows.sh`, `build_windows.bat`, `run_tests.sh`
- [x] Output path fixed at `Builds/Windows/ShedRoomDemo/`
- [ ] Build produced **(cannot build here)**
- [ ] Verified to launch outside the editor **(blocked on the above)**

## Testing

Automated - all written, none executed:

- [x] Main scene existence
- [x] Missing scripts
- [x] Missing or error-shader materials
- [x] Mesh filters with no mesh; material slots beyond submesh count
- [x] Player spawn validity, camera, controller, look, pause menu present
- [x] Required collision: floor below spawn, all four walls, roof
- [x] Required project settings: colour space, resolution, pipeline, build list
- [x] Dimension and architectural-intent assertions
- [x] Mesh builder: triangle counts, chamfer clamping, normals, UV scale, submeshes, extrusion, corrugation, framing
- [x] Material library resolution
- [x] Review viewpoints inside the room
- [x] PlayMode: spawn, floor, walking into all four walls, pause/resume, no unexpected errors
- [x] Windows build creation gated on validation

Manual - none performed:

- [ ] Cannot fall through the floor
- [ ] Cannot walk through walls
- [ ] Cannot leave the intended area
- [ ] Mouse input works
- [ ] Pause and resume work
- [ ] Graphics settings work
- [ ] Build launches
- [ ] Room is clearly visible
- [ ] Room looks realistic rather than frightening

## Acceptance criteria

| # | Criterion | Status |
| --- | --- | --- |
| 1 | Windows build launches | **Not met** - no build produced |
| 2 | Player can walk around the complete shed | Authored, unverified |
| 3 | Believable human scale | **Met** - confirmed against 1.8 m figure in the generated sections |
| 4 | Resembles a normal functional shed | Authored, unverified visually |
| 5 | No horror imagery or lighting | **Met by construction** - banned list enforced; no vignette/grain/fog overrides exist |
| 6 | Surfaces have thickness and construction detail | **Met** - 125 mm walls, 19 mm boards, full framing |
| 7 | Major edges bevelled | **Met** - chamfer on all architecture and hero props |
| 8 | Consistent texture scale | **Met by construction** - metre UVs, 1 m maps, 1:1 tiling |
| 9 | Coherent workbench, storage and utility areas | **Met** - all three built and dressed |
| 10 | Lighting readable throughout | Authored, unverified |
| 11 | No missing materials or runtime errors | Validator written, never run |
| 12 | Player cannot leave | Walls, roof, door blocker and backstop authored; unverified |
| 13 | Future gameplay locations present, non-interactive | **Met** - six present, none interactive |
| 14 | Asset licensing documented | **Met** - no third-party assets at all |
| 15 | At least eight screenshots | **Not met** - rig written, nothing rendered |
| 16 | Performance results documented | **Not met** - no measurement possible |
| 17 | Build path recorded in BUILD_REPORT.md | **Met** - path recorded; results table empty |

**7 of 17 met. 3 not met and blocked on having Unity. 7 authored but unverified.**

## Next actions

1. Open in Unity 6 with Windows Build Support (Mono); fix compile errors - expect
   them in the HDRP-facing code.
2. `Freedome > Configure Project Settings`, then
   `Freedome > Generate > Everything`.
3. Bake lighting. Do not judge the art before this.
4. `Freedome > Validate Scene and Settings` and fix what it reports.
5. `./Tools/run_tests.sh all`.
6. `Freedome > Capture Review Screenshots`; review against the ART_DIRECTION
   criteria; fix faults.
7. `./Tools/build_windows.sh`; verify it launches; complete the checklist in
   BUILD_REPORT.md.
8. Profile with F3 and fill in the performance table.
9. **Stop.** This milestone ends at the finished environment.


---

## Interaction (added after the environment milestone)

Requested explicitly after the environment was complete, overturning the
original "no interactivity" boundary. See DECISIONS.md.

- [x] `Interactable` base, `PlayerInteractor` raycast and use key, `InteractionHud`
- [x] `HingedPart` - door leaf, service floor panel
- [x] `ToggleSwitch` - light switch drives the ceiling fitting
- [x] `Carryable` - five loose objects, one carried at a time, dropped not thrown
- [x] Movable objects split out of the shared meshes and marked non-static
- [x] Pause menu disables interaction along with movement and look
- [x] `InteractionTests` - 8 tests, all running outside Unity
- [ ] Window casement, vent louvres and radio still static - mesh work, not design
- [ ] Nothing observed. No door has actually opened.

- [x] Bench drawers slide, with a tin of screws and a folding rule inside
- [x] Six-slot inventory, number keys to hold, G to put down
- [x] Ridge cap closed - it had a 6.5 mm gap the full length of the roof

- [x] Mutation-tested InteractionTests; two were vacuous and are now real
- [x] Door swept arc checked - it was opening into the room
- [x] Window casement built (there was none) and hinged
- [x] Carryables and the ridge fix mirrored into the preview renders
- [x] Entrance apron so the open door is walkable, with probes over it
- [x] PlayMode interaction tests written - 7 of them, none ever executed
