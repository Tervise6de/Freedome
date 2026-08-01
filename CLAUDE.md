# CLAUDE.md

Guidance for AI assistants working in this repository.

## What this project is

A Unity 6 / HDRP environment milestone: one walkable, realistically proportioned
6 m x 4 m timber shed for Windows. **It is not a game and must not become one in
this milestone.**

Read [README.md](README.md) first, then
[docs/ROOM_DESIGN.md](docs/ROOM_DESIGN.md) and
[docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md).

## The most important thing to know

**This project has never been opened in Unity.** No C# has been compiled, no
scene generated, nothing rendered, no build produced, no test run. It was
authored in a container with no Unity, no GPU and no access to Unity's servers.

Do not describe any part of it as working, verified or tested until you have
evidence that it ran. If asked how it performs or how it looks, the answer is
that nobody knows yet.

## Hard scope boundary

Do **not** add, even if it seems obviously useful:

countdown timer, game-over, inventory, item collection, object combination,
kidnapper AI, escape-route logic, puzzles, evidence journal, narrative clues,
dialogue, save system, combat, multiplayer, runtime generative AI, procedural
puzzle generation.

The six future gameplay affordances documented in
[docs/FUTURE_GAMEPLAY_HOOKS.md](docs/FUTURE_GAMEPLAY_HOOKS.md) are architecture
only. Do not make them interactive, do not highlight them, do not add outlines,
symbols, markings or audio cues.

## Tone constraint

The shed must look like an ordinary functional outbuilding. Not haunted,
abandoned, supernatural, grotesque or frightening.

Never add: blood, chains, occult imagery, victim photographs, threatening
writing, restraints, extreme grime, heavy rust, broken furniture, supernatural
effects, theatrical fog, flickering lights, aggressive vignette, heavy film
grain, dramatic red or green lighting, or any environmental storytelling about
kidnapping.

The volume stack has **no vignette and no film grain overrides at all** - they
are absent rather than zeroed, so they cannot be switched on by accident. Keep
it that way.

## Architecture

### One source of truth

`Assets/Game/Scripts/Environment/ShedDimensions.cs` holds every measurement. The
scene generator, the tests and `Tools/generate_diagrams.py` all read it.

**Never hard-code a dimension anywhere else.** If you need a new measurement, add
it there. If you change one, run:

```bash
python3 Tools/generate_diagrams.py
```

### The scene is generated, not committed by hand

`Assets/Game/Scenes/ShedRoom.unity` is output. Editing it by hand and then
regenerating loses the edits. Change the builders instead.

```
Freedome > Generate > Everything (textures, materials, scene)
```

Builders live in `Assets/Game/Editor/Generation/`:

| File | Responsibility |
| --- | --- |
| `MeshBuilder.cs` | Chamfered boxes, cylinders, extrusions, corrugated sheet. Metre-scale planar UVs |
| `TilingNoise.cs` | Periodic noise. Do not substitute `Mathf.PerlinNoise` - it does not tile |
| `ShedTextureGenerator.cs` | The ten procedural material families |
| `ShedMaterialLibrary.cs` | The 23 HDRP/Lit materials |
| `FramingUtility.cs` | Stud walls with real trimmers; panels with openings |
| `ShellBuilder.cs` | Floor platform, walls, exterior |
| `RoofBuilder.cs` | Rafters, purlins, sheeting, eaves blocking |
| `OpeningsBuilder.cs` | Door, window, vent |
| `FixturesBuilder.cs` | Workbench, pegboard, shelving |
| `UtilityBuilder.cs` | Electrics, utility shelf, entrance fittings |
| `PropLibrary.cs` | Reusable prop shapes |
| `PropsBuilder.cs` | Placement of everything dressed |
| `LightingBuilder.cs` | Sun, volume stack, practicals, probes |
| `ShedSceneGenerator.cs` | Orchestrates all of the above |

### Conventions

- Origin at the floor centre, walking surface at y = 0
- +X is the workbench wall, -X storage, +Z utility, -Z entrance
- Wall local space: +X along the wall, +Y up, +Z outward
- One `MeshBuilder` per assembly, one submesh per material, one renderer
- Chamfer architecture and hero props; pass `bevel = 0` for small background items
- Material tiling is always 1:1 - UVs are already in metres

## Before claiming anything works

```bash
./Tools/compile_check.sh                  # type-check + the Unity-free tests
./Tools/run_tests.sh all                  # EditMode + PlayMode (needs Unity)
python3 Tools/generate_diagrams.py        # after any dimension change
```

`verify_hdrp_api.sh` needs only git and network, and is the only check that
says anything true about HDRP.

`compile_check.sh` needs only `dotnet-sdk-8.0` - no Unity, no GPU - and takes
seconds. Run it before every commit. It compiles all four assemblies against
hand-written Unity stand-ins and executes 31 of the 35 EditMode tests. It is
the only check available here that would catch a hard compile error, and it
has already caught two. It says nothing about HDRP; see
[Tools/CompileCheck/README.md](Tools/CompileCheck/README.md).

In the editor: `Freedome > Validate Scene and Settings`.

The build pipeline refuses to build if validation reports errors. Do not weaken
that check to get a build out.

## Style

- Match the surrounding code: explicit types, `_camelCase` private fields,
  braces on their own lines, XML doc comments on public types
- Comments explain *why*, not *what*. The existing comments are the standard -
  they explain reasoning a reader could not recover from the code
- No generic names. Nothing called `NewBehaviourScript`, `Material 1` or `Cube 4`
- Keep the material palette small; every new material is a draw call

## Things that will trip you up

1. **HDRP APIs move between versions.** `LightingBuilder`, `ShedMaterialLibrary`
   and `ProjectConfigurator` are the most likely to fail to compile first. The
   compile check cannot help you here - its HDRP stubs assert what the API
   should be, so they agree with the code by construction. Use
   `./Tools/verify_hdrp_api.sh` instead: it checks every HDRP symbol against
   the real package source. Run it after touching any HDRP call site.
2. **Legacy input is deliberate.** The Input System package is intentionally
   absent from the manifest. Adding it breaks the controller unless Active Input
   Handling is set to "Both".
3. **The scene ships unbaked.** Do not judge the lighting or the art direction
   before running a bake.
4. **`Assets/Game/Prefabs/` is empty on purpose.** Props are appended into shared
   meshes for draw-call reasons. See DECISIONS.md #4.
5. **Generated textures and meshes are gitignored.** They regenerate
   deterministically. Do not commit them.

## Documentation contract

If you change the room, update the document that describes it in the same
commit. ROOM_DESIGN for layout and dimensions, ART_DIRECTION for materials and
detailing, LIGHTING for anything in the volume or the light rig,
FUTURE_GAMEPLAY_HOOKS if an affordance moves, ASSET_REGISTER if any asset is
added, DECISIONS if you overturn one of the recorded choices, KNOWN_ISSUES when
something is verified or found broken.
