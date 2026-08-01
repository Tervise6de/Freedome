using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Freedome.EditorTools.Build
{
    /// <summary>
    /// Applies the project settings this milestone requires, from code.
    ///
    /// Doing it in a script rather than by hand-editing ProjectSettings YAML means
    /// the configuration is reviewable, repeatable and testable: the EditMode tests
    /// assert on exactly the values this class sets.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string SettingsFolder = "Assets/Game/Settings";
        public const string HdrpAssetPath = SettingsFolder + "/ShedRoom_HDRPAsset.asset";

        public const int TargetWidth = 1920;
        public const int TargetHeight = 1080;

        [MenuItem("Freedome/Configure Project Settings", false, 40)]
        public static void ConfigureFromMenu()
        {
            Configure();
            EditorUtility.DisplayDialog("Project settings",
                "Applied the Shed Room Demo project settings.\n\n" +
                "If the colour space changed, Unity will reimport assets.", "OK");
        }

        public static void Configure()
        {
            Directory.CreateDirectory(SettingsFolder);

            ConfigureRenderPipeline();
            ConfigurePlayer();
            ConfigureQuality();

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // Render pipeline
        // ------------------------------------------------------------------

        private static void ConfigureRenderPipeline()
        {
            HDRenderPipelineAsset asset =
                AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(HdrpAssetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                asset.name = "ShedRoom_HDRPAsset";
                AssetDatabase.CreateAsset(asset, HdrpAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Freedome] Created HDRP asset at {HdrpAssetPath}.");
            }

            GraphicsSettings.defaultRenderPipeline = asset;

            int levels = QualitySettings.names.Length;
            int original = QualitySettings.GetQualityLevel();
            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(original, false);
        }

        // ------------------------------------------------------------------
        // Player settings
        // ------------------------------------------------------------------

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Freedome";
            PlayerSettings.productName = "Shed Room Demo";
            PlayerSettings.bundleVersion = "0.1.0";

            // HDRP only supports linear colour.
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }

            PlayerSettings.defaultScreenWidth = TargetWidth;
            PlayerSettings.defaultScreenHeight = TargetHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = false;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.captureSingleScreen = false;
            PlayerSettings.usePlayerLog = true;

            NamedBuildTarget windows = NamedBuildTarget.Standalone;

            // Mono keeps the build reproducible on any machine with the Windows
            // module installed. IL2CPP additionally needs a Visual Studio toolchain,
            // which is not something this project should quietly require.
            PlayerSettings.SetScriptingBackend(windows, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(windows, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(windows, ManagedStrippingLevel.Low);

            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D12,
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
            });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        }

        // ------------------------------------------------------------------
        // Quality
        // ------------------------------------------------------------------

        private static void ConfigureQuality()
        {
            int original = QualitySettings.GetQualityLevel();
            int levels = QualitySettings.names.Length;

            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 0; // HDRP handles AA in its own settings
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.skinWeights = SkinWeights.TwoBones;
            }

            QualitySettings.SetQualityLevel(original, false);
        }
    }
}
