# Compile check

Type-checks all 9,000 lines of the project's C#, and runs the part of the test
suite that does not need Unity — using the .NET SDK and a set of hand-written
stand-ins for the Unity API.

```bash
sudo apt-get install -y dotnet-sdk-8.0     # once
./Tools/compile_check.sh                   # build everything, then test
```

Roughly 30 seconds cold, a couple of seconds warm.

## Why this exists

The project has never been opened in Unity — no compiler had ever seen this
code. That made "does the C# even build" the largest unanswered question in the
repository, and it was not going to be answered until somebody ran a Unity
editor. This answers most of it without one.

It found five defects directly, and its stubs were corrected by a sixth
that `Tools/verify_hdrp_api.sh` found. All six are listed at the bottom.

## What it is

Five projects that mirror the four `.asmdef` files exactly:

| Project | Mirrors | References |
| --- | --- | --- |
| `Freedome.Runtime` | `Assets/Game/Scripts` | engine stubs only |
| `Freedome.Editor` | `Assets/Game/Editor` | engine + editor stubs + runtime |
| `Freedome.Tests.EditMode` | `Assets/Game/Tests/EditMode` | all of the above |
| `Freedome.Tests.PlayMode` | `Assets/Game/Tests/PlayMode` | engine + runtime, no editor |
| `Freedome.Tests.Run` | the two Unity-free test files | real NUnit, runs them |

The reference graph is the point, not an accident. `Freedome.Runtime` cannot see
the `UnityEditor` stubs, so editor APIs leaking into runtime code fail here for
the same reason they would fail a player build.

### The stubs

`Stubs/Engine`, `Stubs/Editor` and `Stubs/Testing` are signature-only
stand-ins — the members exist, the bodies return defaults. They are written
from Unity's documented API rather than from what the project happens to call;
copying the call sites would make the check circular and prove nothing.

Two exceptions, which are real working implementations:

- **`Stubs/Engine/Math.cs`** — `Vector2/3/4`, `Quaternion`, `Matrix4x4`, `Color`,
  `Bounds`, `Rect`, `Mathf`. Vector and quaternion semantics are well-defined
  and documented, so implementing them faithfully is not circular. Unity's
  conventions are matched deliberately: left-handed with Y up, `Quaternion.Euler`
  composing as `qy * qx * qz`, `Matrix4x4.TRS` as `T * R * S`.
- **`Mesh`** in `Stubs/Engine/Rendering.cs` — stores what is written to it, so
  the geometry tests can read triangles, normals, UVs and bounds back.

Those two are what let 31 tests actually execute rather than merely compile.

## What it proves, and what it does not

**Proves:**

- Every file parses and type-checks; every method resolves; no unreachable
  code, no missing returns, no wrong argument counts, no bad overloads
- The assembly boundaries hold — runtime code touches no editor API
- The dimension table's arithmetic is internally consistent (13 tests)
- `MeshBuilder` produces the geometry it claims to: triangle counts, chamfer
  clamping, unit normals, metre-scale UVs, grain direction, submesh separation,
  transform nesting, extrusion, corrugation, framing and panel openings
  (18 tests)

**Does not prove:**

- **That HDRP 17.0.4 declares the members `Stubs/Engine/HDRP.cs` says it does.**
  This is structurally impossible for this harness: the stubs describe what
  HDRP's API should be, so they agree with the project by construction. A clean
  compile here means the project's use of HDRP is internally consistent, not
  that the package agrees.

  `./Tools/verify_hdrp_api.sh` closes that gap from the other side, by reading
  HDRP's published source. It found a hard compile error this harness could
  never have seen — `AmbientOcclusion` is a renamed, empty `[Obsolete]` shell
  that is not even a `VolumeComponent`. The stub in `HDRP.cs` now reproduces
  that shape exactly, so the harness catches it too, but only because the other
  tool found it first. Run both.
- That anything renders, bakes, performs or looks right
- That the scene generates. `SceneAndProjectTests` is excluded from the run: it
  asks `AssetDatabase` about a scene that only exists once Unity has made one.
- Anything at all about the PlayMode tests, which need a running player. They
  are type-checked here, not executed.

## The harness is mutation-tested

A green suite is worthless if it cannot go red. Two mutations were introduced
and confirmed to fail the run:

| Mutation | Result |
| --- | --- |
| `ShedDimensions.WallHeight` 2.4 → 3.4 | 2 failures |
| `MeshBuilder.LongestAxis` always returns `Vector3.right` | 1 failure |

The second one is why `GeometryTests` changed. The three grain tests originally
asserted against `UvBounds`, which unions every face of the box — and a box has
faces in all orientations, so its V extent is the piece's longest dimension no
matter which way the grain actually runs. All three passed with the grain logic
destroyed. They now check the V direction on one named face, via
`AssertVRunsAlong`, and the mutation is caught.

## What it found

| # | Defect | Severity |
| --- | --- | --- |
| 1 | `[MenuItem("...", priority = N)]` in all 8 menu entries. Unity's `priority` is an internal field, so a named attribute argument cannot bind to it | **Would not compile.** Every entry point to the project's tooling |
| 2 | `Environment.GetCommandLineArgs()` in `WindowsBuild.PerformBuild` bound to the `Freedome.Environment` namespace, not `System.Environment` | **Would not compile.** This is the method the GitHub Actions workflow calls as `buildMethod` |
| 3 | `PlayerLook` skipped writing the camera pivot's rotation while input was disabled, and `HeadBob` multiplies its roll into that same value every frame in `LateUpdate` | Camera rolls continuously while the pause menu is open, then snaps back |
| 4 | `HeadBob._baseLocalPosition` declared and never used | Dead code — but it is the fingerprint of #3 |
| 5 | Three grain tests passed with the grain logic destroyed | A test that cannot fail is worse than no test |
| 6 | `profile.Add<AmbientOcclusion>(true)` — the type was renamed to `ScreenSpaceAmbientOcclusion` in 2022.2, and the shell left behind is not a `VolumeComponent` | **Would not compile.** Found by `verify_hdrp_api.sh`, not by this harness |

Defects 1, 2 and 6 are the ones worth the exercise on their own: all three are
hard compile failures, and between them they take out every menu command, the
headless build entry point, and the volume stack.
