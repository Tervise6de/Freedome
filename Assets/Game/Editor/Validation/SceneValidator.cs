using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Freedome.Environment;
using Freedome.Player;
using Freedome.UI;
using Dim = Freedome.Environment.ShedDimensions;

namespace Freedome.EditorTools.Validation
{
    /// <summary>
    /// Automated checks over the generated scene and the project settings.
    ///
    /// The same code backs the menu item and the EditMode tests, so "it passed in
    /// CI" and "it passed when I clicked it" mean the same thing. Each check returns
    /// a human-readable failure rather than just a boolean, because the point is to
    /// tell whoever broke it what to look at.
    /// </summary>
    public static class SceneValidator
    {
        public sealed class Issue
        {
            public string Category;
            public string Message;
            public bool IsError;

            public override string ToString() =>
                $"[{(IsError ? "ERROR" : "WARN")}] {Category}: {Message}";
        }

        public sealed class Report
        {
            public readonly List<Issue> Issues = new List<Issue>();

            public bool HasErrors
            {
                get
                {
                    foreach (Issue issue in Issues)
                    {
                        if (issue.IsError)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public void Error(string category, string message) =>
                Issues.Add(new Issue { Category = category, Message = message, IsError = true });

            public void Warn(string category, string message) =>
                Issues.Add(new Issue { Category = category, Message = message, IsError = false });

            public string Summarise()
            {
                if (Issues.Count == 0)
                {
                    return "All checks passed.";
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (Issue issue in Issues)
                {
                    sb.AppendLine(issue.ToString());
                }
                return sb.ToString();
            }
        }

        [MenuItem("Freedome/Validate Scene and Settings", priority = 41)]
        public static void ValidateFromMenu()
        {
            Report report = ValidateAll(openScene: true);

            if (report.Issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "All checks passed.", "OK");
            }
            else
            {
                Debug.Log("[Freedome] Validation report:\n" + report.Summarise());
                EditorUtility.DisplayDialog("Validation",
                    $"{report.Issues.Count} issue(s) found. See the console for details.", "OK");
            }
        }

        public static Report ValidateAll(bool openScene)
        {
            Report report = new Report();

            ValidateProjectSettings(report);

            if (!System.IO.File.Exists(Generation.ShedSceneGenerator.ScenePath))
            {
                report.Error("Scene",
                    $"Main scene missing at {Generation.ShedSceneGenerator.ScenePath}. " +
                    "Run Freedome > Generate > Shed Room Scene.");
                return report;
            }

            if (openScene)
            {
                EditorSceneManager.OpenScene(Generation.ShedSceneGenerator.ScenePath, OpenSceneMode.Single);
            }

            ValidateSceneContents(report);
            return report;
        }

        // ------------------------------------------------------------------
        // Project settings
        // ------------------------------------------------------------------

        public static void ValidateProjectSettings(Report report)
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                report.Error("Settings", "Colour space is not Linear. HDRP requires linear colour.");
            }

            if (PlayerSettings.defaultScreenWidth != Build.ProjectConfigurator.TargetWidth ||
                PlayerSettings.defaultScreenHeight != Build.ProjectConfigurator.TargetHeight)
            {
                report.Error("Settings",
                    $"Default resolution is {PlayerSettings.defaultScreenWidth}x" +
                    $"{PlayerSettings.defaultScreenHeight}, expected " +
                    $"{Build.ProjectConfigurator.TargetWidth}x{Build.ProjectConfigurator.TargetHeight}.");
            }

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                report.Error("Settings", "No render pipeline asset assigned. " +
                                         "Run Freedome > Configure Project Settings.");
            }
            else if (!GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains("HDRenderPipeline"))
            {
                report.Error("Settings",
                    $"Render pipeline is {GraphicsSettings.defaultRenderPipeline.GetType().Name}, " +
                    "expected an HDRP asset.");
            }

            bool sceneInBuild = false;
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path == Generation.ShedSceneGenerator.ScenePath && s.enabled)
                {
                    sceneInBuild = true;
                    break;
                }
            }

            if (!sceneInBuild)
            {
                report.Error("Settings", "The main scene is not enabled in Build Settings.");
            }
        }

        // ------------------------------------------------------------------
        // Scene contents
        // ------------------------------------------------------------------

        public static void ValidateSceneContents(Report report)
        {
            GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            List<GameObject> all = new List<GameObject>();
            foreach (GameObject root in roots)
            {
                Collect(root, all);
            }

            CheckMissingScripts(report, all);
            CheckRenderers(report, all);
            CheckPlayer(report);
            CheckCollision(report);
            CheckLighting(report);
        }

        private static void Collect(GameObject go, List<GameObject> into)
        {
            into.Add(go);
            foreach (Transform child in go.transform)
            {
                Collect(child.gameObject, into);
            }
        }

        private static void CheckMissingScripts(Report report, List<GameObject> all)
        {
            foreach (GameObject go in all)
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        report.Error("MissingScript",
                            $"'{Path(go)}' has a missing script in component slot {i}.");
                    }
                }
            }
        }

        private static void CheckRenderers(Report report, List<GameObject> all)
        {
            int rendererCount = 0;

            foreach (GameObject go in all)
            {
                MeshFilter filter = go.GetComponent<MeshFilter>();
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();

                if (filter != null && filter.sharedMesh == null)
                {
                    report.Error("MissingMesh", $"'{Path(go)}' has a MeshFilter with no mesh.");
                }

                if (renderer == null)
                {
                    continue;
                }

                rendererCount++;
                Material[] materials = renderer.sharedMaterials;

                if (materials == null || materials.Length == 0)
                {
                    report.Error("MissingMaterial", $"'{Path(go)}' has no materials.");
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        report.Error("MissingMaterial",
                            $"'{Path(go)}' has a null material in slot {i}.");
                    }
                    else if (materials[i].shader == null ||
                             materials[i].shader.name == "Hidden/InternalErrorShader")
                    {
                        report.Error("MissingMaterial",
                            $"'{Path(go)}' slot {i} uses material '{materials[i].name}' " +
                            "whose shader failed to compile or is missing.");
                    }

                    if (filter != null && filter.sharedMesh != null &&
                        i >= filter.sharedMesh.subMeshCount)
                    {
                        report.Warn("MaterialSlots",
                            $"'{Path(go)}' has more material slots than submeshes.");
                    }
                }
            }

            if (rendererCount == 0)
            {
                report.Error("Scene", "The scene contains no renderers at all.");
            }
        }

        private static void CheckPlayer(Report report)
        {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player == null)
            {
                report.Error("Player", "No FirstPersonController in the scene.");
                return;
            }

            if (player.GetComponent<CharacterController>() == null)
            {
                report.Error("Player", "The player has no CharacterController.");
            }

            if (player.GetComponent<PlayerLook>() == null)
            {
                report.Error("Player", "The player has no PlayerLook component, so mouse look is missing.");
            }

            Camera camera = player.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                report.Error("Player", "The player rig has no camera.");
            }
            else if (!camera.CompareTag("MainCamera"))
            {
                report.Warn("Player", "The player camera is not tagged MainCamera.");
            }

            Vector3 spawn = player.transform.position;
            PlayAreaBoundary boundary = Object.FindAnyObjectByType<PlayAreaBoundary>();

            if (boundary == null)
            {
                report.Warn("Player", "No PlayAreaBoundary in the scene.");
            }
            else if (!boundary.IsInside(spawn))
            {
                report.Error("Player", $"The spawn point {spawn} is outside the play area.");
            }

            if (Object.FindAnyObjectByType<PauseMenuController>() == null)
            {
                report.Error("Systems", "No PauseMenuController in the scene, so Escape does nothing.");
            }
        }

        /// <summary>
        /// Fires rays from inside the room to confirm the player is actually enclosed:
        /// floor below the spawn, and a wall in each of the four horizontal directions.
        /// </summary>
        private static void CheckCollision(Report report)
        {
            Vector3 origin = Dim.PlayerSpawnPosition + new Vector3(0f, Dim.PlayerEyeHeight, 0f);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit floorHit, 4f))
            {
                report.Error("Collision", "No collider found below the player spawn point.");
            }
            else if (Mathf.Abs(floorHit.point.y) > 0.05f)
            {
                report.Warn("Collision",
                    $"The floor under the spawn point is at y = {floorHit.point.y:0.000}, " +
                    "expected approximately 0.");
            }

            Vector3 centre = new Vector3(0f, 1.2f, 0f);
            (Vector3 direction, string name, float expected)[] probes =
            {
                (Vector3.forward, "utility wall", Dim.HalfLength),
                (Vector3.back, "entrance wall", Dim.HalfLength),
                (Vector3.right, "workbench wall", Dim.HalfWidth),
                (Vector3.left, "storage wall", Dim.HalfWidth),
            };

            foreach (var probe in probes)
            {
                if (!Physics.Raycast(centre, probe.direction, out RaycastHit hit, probe.expected + 1.5f))
                {
                    report.Error("Collision",
                        $"No collider hit looking toward the {probe.name}; the player could walk out.");
                }
                else if (hit.distance > probe.expected + 0.6f)
                {
                    report.Warn("Collision",
                        $"The first collider toward the {probe.name} is {hit.distance:0.00} m away, " +
                        $"further than the expected {probe.expected:0.00} m.");
                }
            }

            if (!Physics.Raycast(centre, Vector3.up, out _, 5f))
            {
                report.Error("Collision", "No collider overhead; the roof has no collision.");
            }
        }

        private static void CheckLighting(Report report)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectional = false;
            int practicals = 0;

            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectional = true;
                }
                else
                {
                    practicals++;
                }
            }

            if (!hasDirectional)
            {
                report.Error("Lighting", "No directional light; there is no daylight in the scene.");
            }

            if (practicals < 2)
            {
                report.Warn("Lighting",
                    $"Expected two practical fittings (ceiling and task light), found {practicals}.");
            }

            if (Object.FindAnyObjectByType<Volume>() == null)
            {
                report.Error("Lighting", "No global Volume, so exposure and post-processing are unset.");
            }

            if (Object.FindAnyObjectByType<ReflectionProbe>() == null)
            {
                report.Warn("Lighting", "No reflection probe; metal surfaces will reflect the sky only.");
            }
        }

        private static string Path(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
