using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Freedome.Environment;
using Freedome.Player;
using Freedome.Settings;
using Freedome.UI;
using Dim = Freedome.Environment.ShedDimensions;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Builds the whole ShedRoom scene from code, start to finish.
    ///
    /// The scene is generated rather than hand-placed on purpose. Every dimension
    /// comes from <see cref="ShedDimensions"/>, so the room, the documentation and
    /// the automated checks cannot disagree; and a reviewer can see exactly why a
    /// stud is where it is by reading the builder that put it there. Re-running the
    /// generator reproduces the scene byte for byte.
    /// </summary>
    public static class ShedSceneGenerator
    {
        public const string ScenePath = "Assets/Game/Scenes/ShedRoom.unity";
        public const string SceneFolder = "Assets/Game/Scenes";

        [MenuItem("Freedome/Generate/Shed Room Scene", false, 1)]
        public static void GenerateFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Generate shed room",
                    "This rebuilds " + ScenePath + " and the generated mesh assets from scratch.\n\n" +
                    "Any hand edits made to the scene will be lost.", "Generate", "Cancel"))
            {
                return;
            }

            Generate();
        }

        /// <summary>Entry point used by both the menu item and the batch build.</summary>
        public static void Generate()
        {
            EditorUtility.DisplayProgressBar("Shed room", "Preparing assets", 0.05f);

            try
            {
                EnsureTextures();
                ShedMaterialLibrary.CreateAll();
                BuildContext.ClearGeneratedMeshes();

                EditorUtility.DisplayProgressBar("Shed room", "Creating scene", 0.15f);

                UnityEngine.SceneManagement.Scene scene =
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject root = new GameObject("ShedRoom");
                BuildContext ctx = new BuildContext(root.transform);

                Transform structure = ctx.CreateGroup("Structure").transform;
                Transform openings = ctx.CreateGroup("Openings").transform;
                Transform fixtures = ctx.CreateGroup("Fixtures").transform;
                Transform dressing = ctx.CreateGroup("Dressing").transform;
                Transform lighting = ctx.CreateGroup("Lighting").transform;
                Transform exterior = ctx.CreateGroup("Exterior").transform;

                EditorUtility.DisplayProgressBar("Shed room", "Floor and walls", 0.25f);
                ShellBuilder.BuildFloor(ctx, structure);
                ShellBuilder.BuildWalls(ctx, structure);

                EditorUtility.DisplayProgressBar("Shed room", "Roof", 0.40f);
                RoofBuilder.Build(ctx, structure);

                EditorUtility.DisplayProgressBar("Shed room", "Door, window and vent", 0.50f);
                OpeningsBuilder.Build(ctx, openings);

                EditorUtility.DisplayProgressBar("Shed room", "Bench and shelving", 0.60f);
                FixturesBuilder.Build(ctx, fixtures);
                UtilityBuilder.Build(ctx, fixtures);

                EditorUtility.DisplayProgressBar("Shed room", "Prop dressing", 0.72f);
                PropsBuilder.Build(ctx, dressing);

                EditorUtility.DisplayProgressBar("Shed room", "Exterior", 0.80f);
                ShellBuilder.BuildExterior(ctx, exterior);

                EditorUtility.DisplayProgressBar("Shed room", "Lighting", 0.86f);
                LightingBuilder.Build(ctx, lighting);

                EditorUtility.DisplayProgressBar("Shed room", "Player and systems", 0.93f);
                CreatePlayerRig(root.transform);
                CreateSystems(root.transform);
                CreateScaleReference(root.transform);

                ConfigureSceneLightingSettings();

                Directory.CreateDirectory(SceneFolder);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);

                RegisterInBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[Freedome] Generated {ScenePath}: {ctx.TotalRenderers} renderers, " +
                          $"{ctx.TotalTriangles:N0} triangles before batching.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureTextures()
        {
            string probe = $"{ShedTextureGenerator.OutputFolder}/Pine_Albedo.png";
            if (!File.Exists(probe))
            {
                ShedTextureGenerator.GenerateAll();
            }
        }

        // =====================================================================
        // Player
        // =====================================================================

        private static void CreatePlayerRig(Transform parent)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = Dim.PlayerSpawnPosition;
            player.transform.rotation = Quaternion.Euler(0f, Dim.PlayerSpawnYaw, 0f);
            player.tag = "Player";

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = Dim.PlayerStandingHeight;
            controller.radius = Dim.PlayerRadius;
            controller.center = new Vector3(0f, Dim.PlayerStandingHeight * 0.5f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.25f;
            controller.skinWidth = 0.02f;

            GameObject cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(player.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, Dim.PlayerEyeHeight, 0f);
            cameraGo.tag = "MainCamera";

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.fieldOfView = GameSettings.DefaultFieldOfView;

            HDAdditionalCameraData hdCamera = cameraGo.GetComponent<HDAdditionalCameraData>();
            if (hdCamera == null)
            {
                cameraGo.AddComponent<HDAdditionalCameraData>();
            }

            cameraGo.AddComponent<AudioListener>();

            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerLook>();
            player.AddComponent<HeadBob>();
        }

        private static void CreateSystems(Transform parent)
        {
            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(parent, false);

            systems.AddComponent<PauseMenuController>();
            systems.AddComponent<GraphicsSettingsController>();
            systems.AddComponent<PlayAreaBoundary>();
            systems.AddComponent<PerformanceOverlay>();
        }

        private static void CreateScaleReference(Transform parent)
        {
            GameObject go = new GameObject("ScaleReference_1m80");
            go.transform.SetParent(parent, false);
            // Stood in the middle of the room, clear of the circulation route.
            go.transform.position = new Vector3(0.35f, 0f, 0.60f);
            go.AddComponent<ScaleReference>();
            go.SetActive(false);
        }

        // =====================================================================
        // Scene lighting settings
        // =====================================================================

        /// <summary>
        /// Configures the lightmapper for a mixed-lighting bake. The scene ships
        /// unbaked; Tools/build_windows.sh --bake runs the bake before packaging.
        /// </summary>
        private static void ConfigureSceneLightingSettings()
        {
            LightingSettings settings = new LightingSettings
            {
                name = "ShedRoom_LightingSettings",
                bakedGI = true,
                realtimeGI = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveCPU,
                lightmapResolution = 20f,
                lightmapPadding = 4,
                lightmapMaxSize = 1024,
                directSampleCount = 32,
                indirectSampleCount = 256,
                maxBounces = 3,
                denoiserTypeIndirect = LightingSettings.DenoiserType.Optix,
                filteringMode = LightingSettings.FilterMode.Auto,
                ao = false, // HDRP applies screen-space AO from the volume instead
                mixedBakeMode = MixedLightingMode.Shadowmask,
                lightmapCompression = LightmapCompression.HighQuality,
            };

            Lightmapping.lightingSettings = settings;

            string path = $"{LightingBuilder.SettingsFolder}/ShedRoom_LightingSettings.lighting";
            Directory.CreateDirectory(LightingBuilder.SettingsFolder);
            if (AssetDatabase.LoadAssetAtPath<LightingSettings>(path) == null)
            {
                AssetDatabase.CreateAsset(settings, path);
            }
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            bool present = scenes.Exists(s => s.path == ScenePath);
            if (!present)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        // =====================================================================
        // Convenience: regenerate everything, textures included
        // =====================================================================

        [MenuItem("Freedome/Generate/Everything (textures, materials, scene)", false, 0)]
        public static void GenerateEverything()
        {
            ShedTextureGenerator.GenerateAll();
            // CreateAll rather than the menu wrapper: the wrapper opens a dialog,
            // which a headless run cannot answer.
            ShedMaterialLibrary.CreateAll();
            Generate();
        }

        /// <summary>
        /// Batch entry point. Same work, but it reports failure through the exit
        /// code so a scripted run stops instead of carrying on to build a scene
        /// that was never generated.
        /// </summary>
        public static void GenerateEverythingBatch()
        {
            try
            {
                GenerateEverything();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Freedome] Generation failed: {e}");
                EditorApplication.Exit(1);
                return;
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Freedome] Generation reported success but {ScenePath} is absent.");
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }
    }
}
