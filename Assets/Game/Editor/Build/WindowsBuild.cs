using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Freedome.EditorTools.Build
{
    /// <summary>
    /// Produces the Windows 64-bit build.
    ///
    /// Callable from the editor menu or headless:
    ///   Unity -quit -batchmode -projectPath . \
    ///         -executeMethod Freedome.EditorTools.Build.WindowsBuild.PerformBuild
    ///
    /// Recognised extra arguments:
    ///   -regenerate   rebuild textures, materials and the scene before building
    ///   -bakeLighting run the lightmap bake before building
    ///   -development  produce a development build with the profiler attached
    /// </summary>
    public static class WindowsBuild
    {
        public const string OutputDirectory = "Builds/Windows/ShedRoomDemo";
        public const string ExecutableName = "ShedRoomDemo.exe";
        public const string BuildReportPath = "docs/BUILD_REPORT.md";
        private const string ResultsMarker = "<!-- BUILD-RESULTS -->";

        public static string ExecutablePath => Path.Combine(OutputDirectory, ExecutableName);

        [MenuItem("Freedome/Build Windows Player", priority = 60)]
        public static void BuildFromMenu()
        {
            BuildReport report = Run(regenerate: false, bake: false, development: false);
            if (report == null)
            {
                return;
            }

            bool ok = report.summary.result == BuildResult.Succeeded;
            EditorUtility.DisplayDialog("Windows build",
                ok
                    ? $"Build succeeded.\n\n{ExecutablePath}\n\n" +
                      $"{report.summary.totalSize / (1024 * 1024)} MB in " +
                      $"{report.summary.totalTime.TotalSeconds:0} s."
                    : $"Build {report.summary.result}. See the console.",
                "OK");
        }

        /// <summary>Batch-mode entry point.</summary>
        public static void PerformBuild()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool regenerate = Array.IndexOf(args, "-regenerate") >= 0;
            bool bake = Array.IndexOf(args, "-bakeLighting") >= 0;
            bool development = Array.IndexOf(args, "-development") >= 0;

            BuildReport report = Run(regenerate, bake, development);

            bool succeeded = report != null && report.summary.result == BuildResult.Succeeded;
            if (!succeeded)
            {
                // A non-zero exit code is what makes a scripted build fail loudly.
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Batch entry point that bakes lighting and nothing else.
        ///
        /// Separate from PerformBuild because the bake is the slowest step by a
        /// wide margin and is worth being able to run, fail and retry on its own -
        /// particularly on a machine with no GPU, where it is the only part of the
        /// pipeline that is merely slow rather than unreliable.
        /// </summary>
        public static void BakeOnly()
        {
            if (!File.Exists(Generation.ShedSceneGenerator.ScenePath))
            {
                Debug.LogError($"[Freedome] No scene at {Generation.ShedSceneGenerator.ScenePath}. " +
                               "Generate it before baking.");
                EditorApplication.Exit(1);
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                Generation.ShedSceneGenerator.ScenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            Debug.Log("[Freedome] Baking lighting. On the CPU lightmapper this takes a while.");
            DateTime started = DateTime.UtcNow;

            bool ok = Lightmapping.Bake();

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            double minutes = (DateTime.UtcNow - started).TotalMinutes;
            if (ok)
            {
                Debug.Log($"[Freedome] Bake finished in {minutes:0.0} minutes.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Freedome] Bake failed after {minutes:0.0} minutes.");
                EditorApplication.Exit(1);
            }
        }

        public static BuildReport Run(bool regenerate, bool bake, bool development)
        {
            ProjectConfigurator.Configure();

            if (regenerate || !File.Exists(Generation.ShedSceneGenerator.ScenePath))
            {
                Debug.Log("[Freedome] Regenerating the shed scene before building.");
                Generation.ShedSceneGenerator.Generate();
            }

            Validation.SceneValidator.Report validation =
                Validation.SceneValidator.ValidateAll(openScene: true);

            if (validation.Issues.Count > 0)
            {
                Debug.Log("[Freedome] Pre-build validation:\n" + validation.Summarise());
            }

            if (validation.HasErrors)
            {
                Debug.LogError("[Freedome] Refusing to build: validation found errors.");
                return null;
            }

            if (bake)
            {
                Debug.Log("[Freedome] Baking lighting. This can take a long time.");
                Lightmapping.Bake();
            }

            Directory.CreateDirectory(OutputDirectory);

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Freedome] No scenes enabled in Build Settings.");
                return null;
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ExecutablePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            LogSummary(report);
            WriteBuildRecord(report, bake, development);

            return report;
        }

        private static string[] GetEnabledScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    list.Add(scene.path);
                }
            }
            return list.ToArray();
        }

        private static void LogSummary(BuildReport report)
        {
            BuildSummary summary = report.summary;
            Debug.Log($"[Freedome] Build {summary.result}: {summary.outputPath}\n" +
                      $"  size    {summary.totalSize / (1024 * 1024)} MB\n" +
                      $"  time    {summary.totalTime.TotalSeconds:0.0} s\n" +
                      $"  errors  {summary.totalErrors}\n" +
                      $"  warnings {summary.totalWarnings}");
        }

        /// <summary>
        /// Appends the real numbers from this build to docs/BUILD_REPORT.md, so the
        /// document records what was actually produced rather than what was intended.
        /// </summary>
        private static void WriteBuildRecord(BuildReport report, bool baked, bool development)
        {
            try
            {
                BuildSummary summary = report.summary;

                string row = string.Format(
                    "| {0} | {1} | {2} | {3:0.0} s | {4} MB | {5} | {6} |",
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                    Application.unityVersion,
                    summary.result,
                    summary.totalTime.TotalSeconds,
                    summary.totalSize / (1024 * 1024),
                    baked ? "baked" : "unbaked",
                    development ? "development" : "release");

                string absolute = Path.GetFullPath(BuildReportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? ".");

                if (!File.Exists(absolute))
                {
                    File.WriteAllText(absolute,
                        "# Build report\n\n" + ResultsMarker + "\n\n" +
                        "| Date | Unity | Result | Time | Size | Lighting | Configuration |\n" +
                        "| --- | --- | --- | --- | --- | --- | --- |\n");
                }

                string content = File.ReadAllText(absolute);
                StringBuilder sb = new StringBuilder(content);

                if (content.Contains(ResultsMarker))
                {
                    // Insert immediately after the table header that follows the marker.
                    int markerEnd = content.IndexOf(ResultsMarker, StringComparison.Ordinal)
                                    + ResultsMarker.Length;
                    int headerEnd = content.IndexOf("| --- |", markerEnd, StringComparison.Ordinal);
                    if (headerEnd >= 0)
                    {
                        int lineEnd = content.IndexOf('\n', headerEnd);
                        if (lineEnd >= 0)
                        {
                            sb.Insert(lineEnd + 1, row + "\n");
                        }
                        else
                        {
                            sb.AppendLine(row);
                        }
                    }
                    else
                    {
                        sb.AppendLine(row);
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine(row);
                }

                File.WriteAllText(absolute, sb.ToString());
                Debug.Log($"[Freedome] Recorded the build in {BuildReportPath}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Freedome] Could not update {BuildReportPath}: {e.Message}");
            }
        }
    }
}
