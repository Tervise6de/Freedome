# Running the Unity pipeline in a cloud session

What it takes to get Unity running in a Claude Code cloud session, what that
would actually finish, and what it would not.

## The short version

One of the two things this needs is configurable and one is not.

| | |
| --- | --- |
| **Network access to Unity's domains** | **Configurable.** You can change it yourself in about a minute |
| **A GPU** | **Not available.** Cloud sessions have no GPU and no setting exposes one |
| **A Unity licence** | **Yours to supply.** Nothing here will ask for your credentials |

Cloud session resource limits are 4 vCPUs, 16 GB RAM and 30 GB of disk. The
documented environment options are network access, environment variables and a
setup script. There is no hardware tier to pick.

## Routes that were tried and do not work

Recorded so nobody spends time re-discovering them. All verified in a live
session, not assumed.

### Direct download

`download.unity3d.com`, `public-cdn.cloud.unity3d.com` (Unity Hub),
`services.unity.com` and `license.unity3d.com` are all rejected at the proxy.

### The Docker route

Worth taking seriously, because Docker Hub **is** on the default allowlist and
Docker is pre-installed. GameCI publishes complete Unity editor images, and
`unityci/editor` has 1002 tags for 6000.0.x with Windows Mono support - the
newest being `ubuntu-6000.0.80f1-windows-mono-3.2.2` at 5.84 GB compressed,
about 12 GB unpacked, which fits in the 30 GB allowance.

The registry API works: tags list, manifests resolve, auth tokens issue. The
daemon starts. The pull still fails, and the reason is specific:
`registry-1.docker.io` serves manifests but 307-redirects blob downloads to
**`production.cloudfront.docker.com`**, which is not on the allowlist. The list
contains `production.cloudflare.docker.com` - Cloudflare, not CloudFront - and
Docker Hub chose the CloudFront edge. So the pull dies on the first blob with
`Forbidden`. `pkg-containers.githubusercontent.com`, which serves ghcr.io blobs,
is blocked for the same reason.

### The package registry

This one matters more than the editor, and is easy to miss. HDRP is a registry
package, not part of the editor install, so even a working editor image cannot
build this project without it. `packages.unity.com`,
`download.packages.unity.com` and `package.openupm.com` are all blocked.

### What not to do

`registry.npmjs.org` is allowlisted and does return an entry for
`com.unity.render-pipelines.high-definition`. It is a squatted name carrying
npm's `0.0.1-security` placeholder, not Unity's package. Installing it would be
a supply-chain compromise dressed up as a workaround. Don't.

## 1. Allow Unity's domains

Currently the environment blocks them - `download.unity3d.com`,
`public-cdn.cloud.unity3d.com`, `services.unity.com` and `license.unity3d.com`
all fail at the proxy, while `pypi.org` and `archive.ubuntu.com` return 200. It
is a domain allowlist, not a general block.

To change it:

1. Go to **claude.ai/code**
2. In the row above the message box, click the **cloud icon** showing the
   environment's name
3. Hover the environment and click the **settings (gear)** icon
4. Set **Network access** to **Custom**, tick **Also include default list of
   common package managers**, and add:

   ```
   download.unity3d.com
   packages.unity.com
   *.unity3d.com
   *.unity.com
   unity.com
   ```

   `packages.unity.com` is the one that is easy to leave out and the one that
   stops the build dead, because HDRP is fetched from there rather than shipped
   with the editor.

   **Full** also works and allows any domain.
5. Save, then start a **new** session. Changing allowed hosts rebuilds the
   environment cache.

There is no organization-level allowlist; each environment carries its own.

## 2. Supply a licence

**This is the blocker that no network setting removes.** Unity will not run
unlicensed, activation requires signing in to a Unity account, and this
repository will not enter your credentials. Even with **Full** network access,
the run stops here without a licence file from you.

Supply your own licence file instead:

```bash
export UNITY_ULF_PATH=/path/to/Unity_lic.ulf     # preferred
# or
export UNITY_LICENSE="$(cat Unity_lic.ulf)"
```

Get a `.ulf` by activating Unity once on a machine you control and copying
`~/.local/share/unity3d/Unity/Unity_lic.ulf` (Linux) or the equivalent under
`%ProgramData%\Unity` (Windows).

Prefer `UNITY_ULF_PATH` over putting the licence in an environment variable:
cloud environment variables are readable by anyone who can use that environment
and there is no secrets store.

## 3. Run it

```bash
./Tools/bootstrap_unity.sh
```

The script installs host dependencies including a software Vulkan device,
downloads the editor and the Windows Mono module, activates with your licence,
then runs configure, generate, bake, tests, screenshots and build in order,
logging each to `Builds/bootstrap/`. Use `--install-only` to stop after the
install, or `--skip-bake` to trade lighting quality for time.

Budget roughly 20 GB of disk and a long first run: about 5 GB of editor to pull,
then a CPU lightmap bake on 4 cores.

## What this would actually finish

Honest per acceptance criterion, assuming network access but still no GPU.

| Criterion | Outcome |
| --- | --- |
| 11 - no missing materials or runtime errors | **Yes.** The validator runs headless |
| Automated tests | **Yes** for EditMode. PlayMode needs a graphics device, so it runs under the software driver |
| Lighting bake | **Yes.** The Progressive CPU lightmapper needs no GPU. Slow, but it is the step that most improves the room |
| 1 - Windows build produced | **Yes**, built. "Launches successfully" still needs an actual Windows machine to launch it on |
| 17 - build path recorded | **Yes.** The build appends its own row to BUILD_REPORT.md |
| 15 - eight screenshots | **Maybe.** Only through Mesa's lavapipe software Vulkan. HDRP leans on compute shaders and this is untested; it may fail outright, and it will be slow |
| 16 - performance results | **No.** Frame times from a software rasteriser on 4 vCPUs say nothing about 60 fps on a mid-to-high-end PC. Reporting them would be worse than reporting nothing |

So: enabling network access unblocks compilation, validation, tests, the bake
and the build. It does not honestly close criterion 16, and it only might close
criterion 15.

## If you want 15 and 16 properly

Run it on a machine with a GPU. Same script works on any Ubuntu box, or use the
editor directly:

```
Freedome > Configure Project Settings
Freedome > Generate > Everything
Window > Rendering > Lighting > Generate Lighting
Freedome > Capture Review Screenshots
Freedome > Build Windows Player
```

Then press **F3** in the build and walk the route from the door to the utility
wall to fill in the performance table in
[BUILD_REPORT.md](BUILD_REPORT.md).
