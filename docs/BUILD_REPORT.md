# Build report

## Final build path

```
Builds/Windows/ShedRoomDemo/ShedRoomDemo.exe
```

Set by `WindowsBuild.OutputDirectory` and `WindowsBuild.ExecutableName`. The
directory is created by the build script; the build output itself is not
committed (see `.gitignore`), because it is a large regenerable binary artefact.

> **No build has been produced.** The authoring environment has no Unity
> installation and its network policy blocks Unity's download servers, so the
> player has never been compiled. The pipeline below is written and ready; it has
> not run. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

---

## Build configuration

| Setting | Value |
| --- | --- |
| Target | `StandaloneWindows64` |
| Target group | `Standalone` |
| Scripting backend | Mono2x |
| API compatibility | .NET Standard |
| Managed stripping | Low |
| Colour space | Linear (required by HDRP) |
| Graphics APIs | Direct3D12, then Direct3D11 |
| Default resolution | 1920 x 1080 |
| Full-screen mode | Full-screen window |
| Resizable window | Yes |
| Run in background | No |
| Company / product | Freedome / Shed Room Demo |
| Version | 0.1.0 |
| Scenes | `Assets/Game/Scenes/ShedRoom.unity` |

Applied by `Freedome.EditorTools.Build.ProjectConfigurator`, which the build
pipeline calls before every build. The EditMode tests assert on the same values.

---

## Procedure

### From the editor

```
Freedome > Configure Project Settings
Freedome > Generate > Everything (textures, materials, scene)
Window > Rendering > Lighting > Generate Lighting
Freedome > Validate Scene and Settings
Freedome > Build Windows Player
```

### Headless

```bash
./Tools/build_windows.sh                            # build only
./Tools/build_windows.sh --regenerate               # regenerate the scene first
./Tools/build_windows.sh --regenerate --bake        # ...and bake lighting
./Tools/build_windows.sh --development              # development build
```

On Windows, `Tools\build_windows.bat` takes the same flags.

Set `UNITY_PATH` if the editor is not in a standard Unity Hub location:

```bash
UNITY_PATH="$HOME/Unity/Hub/Editor/6000.0.58f1/Editor/Unity" ./Tools/build_windows.sh
```

### What the pipeline does

1. Applies all project settings via `ProjectConfigurator`.
2. Regenerates the scene if `--regenerate` was passed or the scene is missing.
3. Runs `SceneValidator` over the scene and the project settings.
4. **Refuses to build if validation reports any error.** Missing scripts, missing
   materials, a shader that failed to compile, an invalid spawn point, a wall
   with no collider or a scene not in Build Settings all stop the build rather
   than shipping broken.
5. Bakes lighting if `--bake` was passed.
6. Builds the player.
7. Logs a summary and appends a row to the results table below.
8. Exits non-zero on failure, so a scripted build fails loudly.

The full Unity log is written to `Builds/Windows/build.log`.

---

## Requirements

| | |
| --- | --- |
| Unity | 6000.0.x with **Windows Build Support (Mono)** |
| Disk | ~5 GB for the project, library and build output |
| Third-party assets | None |
| Accounts or licences | None beyond a Unity licence to run the editor |

---

## Verification checklist

To be completed on the first real build. None of it has been done.

- [ ] The build completes with no errors
- [ ] `Builds/Windows/ShedRoomDemo/ShedRoomDemo.exe` exists
- [ ] It launches outside the Unity editor
- [ ] It opens at 1920 x 1080
- [ ] The shed renders with no missing (magenta) materials
- [ ] WASD moves, the mouse looks
- [ ] Crouch works and standing up is blocked under low geometry
- [ ] The player cannot fall through the floor
- [ ] The player cannot walk through any wall or the door
- [ ] The player cannot leave the demonstration area
- [ ] Esc opens the pause menu and releases the cursor
- [ ] Resume, Restart and Quit all work
- [ ] Every settings control changes what it says it changes
- [ ] No errors accumulate in the player log during five minutes of play
- [ ] The room is clearly visible in every corner
- [ ] The room reads as a normal shed, not as a horror set

---

## Results

<!-- BUILD-RESULTS -->

| Date | Unity | Result | Time | Size | Lighting | Configuration |
| --- | --- | --- | --- | --- | --- | --- |

*Rows are appended automatically by `WindowsBuild.WriteBuildRecord`. The table is
empty because no build has run.*

---

## Performance results

**None recorded.** The scene has never been generated, run or profiled. There is
no GPU in the authoring environment.

To collect them: run the build, press **F3** for the in-game overlay, and walk
the circulation route from the door to the utility wall. The overlay reports
frame rate, average and worst frame time, CPU main thread time, GPU frame time,
triangle count, draw calls, batches and managed heap.

Record here:

| Metric | Target | Measured |
| --- | --- | --- |
| Average frame rate | 60 fps at 1920 x 1080 | |
| Worst frame time | < 16.6 ms | |
| CPU main thread | | |
| GPU frame time | | |
| Triangles | | |
| Draw calls | | |
| Batches | | |
| Memory | | |
| Main bottleneck | | |

The scene generator also logs its own renderer and triangle totals when it runs,
which gives the pre-batching geometry budget independently of the runtime
counters.

The design intent behind these numbers - single-mesh assemblies, a 23-material
palette, chamfers only where they are seen, everything static, mixed lighting,
two reflection probes, no volumetrics - is set out in
[ROOM_DESIGN.md](ROOM_DESIGN.md) section 8. Optimise only if a measurement says
to; do not trade visual quality for headroom that is already adequate.
