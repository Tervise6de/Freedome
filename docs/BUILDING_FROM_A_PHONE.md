# Building this from a phone

You can get a good way through the milestone from a phone, using GitHub Actions.
You cannot get all the way, and this document is honest about where the line is.

The trick is that a GitHub runner has unrestricted network access, so it can
reach `packages.unity.com` for HDRP - which a Claude Code cloud session cannot -
and Unity's manual licence activation is entirely a browser flow.

## What you can finish this way

| | |
| --- | --- |
| **Compile the C#** | Yes. This is the single biggest unverified risk in the project |
| **EditMode tests** | Yes |
| **Generate the scene** | Yes |
| **Bake lighting** | Yes, optionally. CPU lightmapper, slow |
| **Windows build produced** (criterion 1) | Yes |
| **Build recorded** (criterion 17) | Yes |
| **Build launches** (criterion 1, second half) | No. Needs an actual Windows machine |
| **Eight screenshots** (criterion 15) | **No.** Runners have no GPU |
| **Performance figures** (criterion 16) | **No**, and they would be meaningless if they existed |

So a phone gets you compilation, tests, a baked scene and a packaged build. It
does not get you the screenshots or the profiling.

That is still the most valuable half. `./Tools/compile_check.sh` has since
type-checked everything except HDRP without Unity, so the open question has
narrowed to "does the C# build against **HDRP 17** specifically" - which no
stub can answer and this can.

## What it costs

Nothing, for this repository.

`Tervise6de/Freedome` is **public**, and GitHub Actions gives public
repositories unlimited free minutes on standard runners, with free artifact
storage. A private repo would draw on the 2,000 minutes a month the free tier
allows, and a Unity build would make a dent in that; that does not apply here.

Unity Personal is free under Unity's revenue and funding threshold, which is the
licence the activation flow issues.

The real cost is time. Expect 30 to 60 minutes for the first run - it pulls a
12 GB editor image and resolves HDRP from scratch. Later runs reuse the cached
`Library`. Leave the bake off until you know the project compiles.

One thing that has to be true for any of this to work: the workflow files must
exist on the repository's **default branch**, or GitHub will not show the *Run
workflow* button at all. This repository's default branch is
`claude/shed-environment-demo-9obg2y`, which is where they are, so the buttons
appear. If the default branch is ever changed, move or merge the workflows with
it.

## Steps

All of these work in a mobile browser. Both workflows are registered and active
on GitHub, so the *Run workflow* buttons are there now:

- [Unity - request activation file](https://github.com/Tervise6de/Freedome/actions/workflows/unity-activation.yml)
- [Unity - build and test](https://github.com/Tervise6de/Freedome/actions/workflows/unity-build.yml)

### 1. Get a Unity licence

1. Open [Unity - request activation
   file](https://github.com/Tervise6de/Freedome/actions/workflows/unity-activation.yml)
   and tap **Run workflow**. Leave the version as `6000.0.58f1` unless you want
   another.
2. When it finishes, download the **unity-activation-file** artifact. It is a
   zip containing a `.alf`.
3. Unzip it. On iOS, tap the zip in Files and it expands in place; on Android,
   most file managers do the same.
4. Go to <https://license.unity3d.com/manual>, sign in, upload the `.alf`, pick
   **Unity Personal** (or whatever you hold), and download the `.ulf` it gives
   back.
5. Open the `.ulf` as text and copy all of it.
6. On GitHub: **Settings -> Secrets and variables -> Actions -> New repository
   secret**. Name it `UNITY_LICENSE`, paste the `.ulf` contents, save.

The fiddly part is step 3 to 5 - handling files on a phone. Everything else is
taps.

### 2. Build

Open [Unity - build and
test](https://github.com/Tervise6de/Freedome/actions/workflows/unity-build.yml)
and tap **Run workflow**.

Leave *Run the EditMode tests* on. Leave *Bake lighting* off for the first run:
you want to find out whether it compiles before spending an hour on a bake.

Expect the first run to take a while. It pulls a 12 GB editor image and resolves
HDRP from scratch. Later runs reuse the cached `Library`.

### 3. Read the result

- **It failed during "Build Windows player" with compile errors.** Still
  possible, and genuinely useful - this is the HDRP API surface that
  `docs/KNOWN_ISSUES.md` flags as the highest risk. Download the **unity-logs**
  artifact, and either fix them yourself or paste them into a Claude Code
  session.

  Two compile errors that *would* have stopped this run have since been found
  and fixed without Unity, by `./Tools/compile_check.sh` - including one in
  `WindowsBuild.PerformBuild`, the very method this workflow calls. So the run
  should get further than it would have. Everything except HDRP is now
  type-checked.
- **It succeeded.** Download the **ShedRoomDemo-Windows** artifact. That is the
  build. Run it on a Windows machine to close the rest of criterion 1, take the
  screenshots with `Freedome > Capture Review Screenshots`, and press F3 in the
  build for the performance table.

Once it compiles cleanly, re-run with *Bake lighting* on. The room is
substantially better lit baked, and it is the change that most affects how the
environment reads.

## Caveats

- **None of this has been tested.** The workflows were written without Unity
  available, like everything else here. They may need adjusting.
- **They use third-party actions** - GameCI's `unity-request-activation-file`,
  `unity-test-runner` and `unity-builder`. GameCI is the standard way to run
  Unity in CI, but they are third-party code running with your licence secret.
  Read them if that matters to you.
- **Both workflows are manual trigger only.** Nothing runs on push, so they
  cannot consume Actions minutes by accident.
- **Your licence secret is readable by anyone who can write to the repository.**
  That is how GitHub secrets work. Use a Personal licence, not a seat you care
  about.
- **Disk is tight.** Runners have about 14 GB free and the editor image is
  12-15 GB, so the workflow deletes the preinstalled .NET, Android and GHC
  toolchains first. If a future GameCI image grows, this is what breaks.

## If you would rather wait for a desk

Everything above is also one command on any machine with Unity installed:

```bash
./Tools/build_windows.sh --regenerate --bake
```

and that path additionally gets you the screenshots and the performance
numbers, because it has a GPU. See [RUNNING_IN_CLOUD.md](RUNNING_IN_CLOUD.md).
