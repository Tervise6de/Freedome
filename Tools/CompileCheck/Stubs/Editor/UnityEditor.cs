// Signature-only stand-ins for the UnityEditor surface the project touches.
//
// This assembly is referenced only by the editor and test projects, exactly as
// the asmdefs arrange it. That is deliberate: if runtime code ever reaches for
// an editor API, the runtime project fails to compile here for the same reason
// it would fail in a player build.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    public static class AssetDatabase
    {
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static bool DeleteAsset(string path) { return false; }
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object { return null; }
        public static UnityEngine.Object LoadAssetAtPath(string assetPath, Type type) { return null; }
        public static UnityEngine.Object LoadMainAssetAtPath(string assetPath) { return null; }
        public static void Refresh() { }
        public static void Refresh(ImportAssetOptions options) { }
        public static void SaveAssets() { }
        public static void SaveAssetIfDirty(UnityEngine.Object obj) { }
        public static void StartAssetEditing() { }
        public static void StopAssetEditing() { }
        public static void ImportAsset(string path) { }
        public static void ImportAsset(string path, ImportAssetOptions options) { }
        public static string[] FindAssets(string filter) { return null; }
        public static string[] FindAssets(string filter, string[] searchInFolders) { return null; }
        public static string GUIDToAssetPath(string guid) { return null; }
        public static string AssetPathToGUID(string path) { return null; }
        public static string GetAssetPath(UnityEngine.Object assetObject) { return null; }
        public static bool IsValidFolder(string path) { return false; }
        public static string CreateFolder(string parentFolder, string newFolderName) { return null; }
        public static bool CopyAsset(string path, string newPath) { return false; }
        public static string MoveAsset(string oldPath, string newPath) { return null; }
        public static void AddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject) { }
        public static void SetLabels(UnityEngine.Object obj, string[] labels) { }
    }

    [Flags]
    public enum ImportAssetOptions
    {
        Default = 0,
        ForceUpdate = 1,
        ForceSynchronousImport = 8,
        ImportRecursive = 256,
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) { return false; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return false; }
        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt) { return 0; }
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static bool DisplayCancelableProgressBar(string title, string info, float progress) { return false; }
        public static void ClearProgressBar() { }
        public static void SetDirty(UnityEngine.Object target) { }
        public static string SaveFilePanel(string title, string directory, string defaultName, string extension) { return null; }
        public static string OpenFilePanel(string title, string directory, string extension) { return null; }
        public static string SaveFolderPanel(string title, string folder, string defaultName) { return null; }
        public static void UnloadUnusedAssetsImmediate() { }
        public static bool IsPersistent(UnityEngine.Object target) { return false; }
        public static void CopySerialized(UnityEngine.Object source, UnityEngine.Object dest) { }
    }

    public static class EditorApplication
    {
        public static bool isPlaying { get; set; }
        public static bool isPlayingOrWillChangePlaymode { get { return false; } }
        public static bool isPaused { get; set; }
        public static bool isCompiling { get { return false; } }
        public static bool isUpdating { get { return false; } }
        public static string applicationPath { get { return null; } }
        public static string applicationContentsPath { get { return null; } }

        public static void Exit(int returnValue) { }
        public static void EnterPlaymode() { }
        public static void ExitPlaymode() { }
        public static void Beep() { }

        public static event Action update;
        public static event Action delayCall;
    }

    public static class EditorPrefs
    {
        public static void SetInt(string key, int value) { }
        public static int GetInt(string key, int defaultValue = 0) { return 0; }
        public static void SetFloat(string key, float value) { }
        public static float GetFloat(string key, float defaultValue = 0f) { return 0f; }
        public static void SetString(string key, string value) { }
        public static string GetString(string key, string defaultValue = "") { return null; }
        public static void SetBool(string key, bool value) { }
        public static bool GetBool(string key, bool defaultValue = false) { return false; }
        public static bool HasKey(string key) { return false; }
        public static void DeleteKey(string key) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }

        // Internal in Unity, exactly as here. That is what makes
        // [MenuItem("...", priority = N)] illegal - named attribute arguments
        // need a public read-write field or property.
        internal string menuItem;
        internal bool validate;
        internal int priority;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class InitializeOnLoadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class InitializeOnLoadMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CustomEditor : Attribute
    {
        public CustomEditor(Type inspectedType) { }
    }

    public static class Selection
    {
        public static GameObject activeGameObject { get; set; }
        public static Transform activeTransform { get; set; }
        public static UnityEngine.Object activeObject { get; set; }
        public static GameObject[] gameObjects { get { return null; } }
        public static UnityEngine.Object[] objects { get; set; }
        public static Transform[] transforms { get { return null; } }
    }

    public static class Undo
    {
        public static void RecordObject(UnityEngine.Object objectToUndo, string name) { }
        public static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, string name) { }
        public static void DestroyObjectImmediate(UnityEngine.Object objectToUndo) { }
        public static void SetTransformParent(Transform transform, Transform newParent, string name) { }
        public static T AddComponent<T>(GameObject gameObject) where T : Component { return null; }
    }

    [Flags]
    public enum StaticEditorFlags
    {
        ContributeGI = 1,
        OccluderStatic = 2,
        BatchingStatic = 4,
        NavigationStatic = 8,
        OccludeeStatic = 16,
        OffMeshLinkGeneration = 32,
        ReflectionProbeStatic = 64,
    }

    public static class GameObjectUtility
    {
        public static void SetStaticEditorFlags(GameObject go, StaticEditorFlags flags) { }
        public static StaticEditorFlags GetStaticEditorFlags(GameObject go) { return default(StaticEditorFlags); }
        public static void SetParentAndAlign(GameObject child, GameObject parent) { }
    }

    public class AssetImporter : UnityEngine.Object
    {
        public string assetPath { get; set; }
        public string userData { get; set; }
        public void SaveAndReimport() { }
        public static AssetImporter GetAtPath(string path) { return null; }
    }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public TextureImporterShape textureShape { get; set; }
        public bool sRGBTexture { get; set; }
        public bool mipmapEnabled { get; set; }
        public bool isReadable { get; set; }
        public bool alphaIsTransparency { get; set; }
        public int maxTextureSize { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public bool streamingMipmaps { get; set; }
        public TextureImporterNPOTScale npotScale { get; set; }
        public bool crunchedCompression { get; set; }
        public int compressionQuality { get; set; }
    }

    public enum TextureImporterType
    {
        Default = 0,
        NormalMap = 1,
        GUI = 2,
        Sprite = 8,
        Cursor = 7,
        Cookie = 4,
        Lightmap = 6,
        SingleChannel = 10,
    }

    public enum TextureImporterShape { Texture2D = 1, TextureCube = 2, Texture2DArray = 4, Texture3D = 8 }

    public enum TextureImporterCompression { Uncompressed = 0, Compressed = 1, CompressedHQ = 2, CompressedLQ = 3 }

    public enum TextureImporterNPOTScale { None = 0, ToNearest = 1, ToLarger = 2, ToSmaller = 3 }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }
        public static string applicationIdentifier { get; set; }
        public static ColorSpace colorSpace { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool defaultIsNativeResolution { get; set; }
        public static bool runInBackground { get; set; }
        public static bool captureSingleScreen { get; set; }
        public static bool resizableWindow { get; set; }
        public static bool allowFullscreenSwitch { get; set; }
        public static bool visibleInBackground { get; set; }
        public static bool usePlayerLog { get; set; }
        public static bool forceSingleInstance { get; set; }
        public static FullScreenMode fullScreenMode { get; set; }
        public static bool gpuSkinning { get; set; }
        public static bool graphicsJobs { get; set; }
        public static bool useFlipModelSwapchain { get; set; }
        public static bool virtualRealitySupported { get; set; }
        public static UnityEngine.Rendering.GraphicsDeviceType[] GetGraphicsAPIs(BuildTarget platform) { return null; }
        public static void SetGraphicsAPIs(BuildTarget platform, UnityEngine.Rendering.GraphicsDeviceType[] apis) { }
        public static bool GetUseDefaultGraphicsAPIs(BuildTarget platform) { return false; }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget platform, bool automatic) { }
        public static void SetScriptingBackend(BuildTargetGroup targetGroup, ScriptingImplementation backend) { }
        public static void SetScriptingBackend(Build.NamedBuildTarget buildTarget, ScriptingImplementation backend) { }
        public static ScriptingImplementation GetScriptingBackend(Build.NamedBuildTarget buildTarget) { return default(ScriptingImplementation); }
        public static void SetApiCompatibilityLevel(BuildTargetGroup buildTargetGroup, ApiCompatibilityLevel value) { }
        public static void SetApiCompatibilityLevel(Build.NamedBuildTarget buildTarget, ApiCompatibilityLevel value) { }
        public static ApiCompatibilityLevel GetApiCompatibilityLevel(Build.NamedBuildTarget buildTarget) { return default(ApiCompatibilityLevel); }
        public static void SetManagedStrippingLevel(BuildTargetGroup targetGroup, ManagedStrippingLevel level) { }
        public static void SetManagedStrippingLevel(Build.NamedBuildTarget buildTarget, ManagedStrippingLevel level) { }
        public static ManagedStrippingLevel GetManagedStrippingLevel(Build.NamedBuildTarget buildTarget) { return default(ManagedStrippingLevel); }
        public static void SetIl2CppCompilerConfiguration(BuildTargetGroup targetGroup, Il2CppCompilerConfiguration configuration) { }
        public static string[] GetScriptingDefineSymbols(Build.NamedBuildTarget buildTarget) { return null; }
        public static void SetScriptingDefineSymbols(Build.NamedBuildTarget buildTarget, string[] defines) { }
    }

    public enum ScriptingImplementation { Mono2x = 0, IL2CPP = 1, WinRTDotNET = 2, CoreCLR = 3 }

    public enum ApiCompatibilityLevel
    {
        NET_2_0 = 1,
        NET_2_0_Subset = 2,
        NET_4_6 = 3,
        NET_Standard_2_0 = 6,
        NET_Unity_4_8 = 3,
        NET_Standard = 6,
    }

    public enum ManagedStrippingLevel { Disabled = 0, Low = 1, Medium = 2, High = 3, Minimal = 4 }

    public enum Il2CppCompilerConfiguration { Debug = 0, Release = 1, Master = 2 }

    public enum BuildTarget
    {
        StandaloneWindows = 5,
        StandaloneWindows64 = 19,
        StandaloneOSX = 2,
        StandaloneLinux64 = 24,
        Android = 13,
        iOS = 9,
        WebGL = 20,
        NoTarget = -2,
    }

    public enum BuildTargetGroup
    {
        Unknown = 0,
        Standalone = 1,
        Android = 13,
        iOS = 4,
        WebGL = 13,
    }

    [Flags]
    public enum BuildOptions
    {
        None = 0,
        Development = 1,
        AutoRunPlayer = 4,
        ShowBuiltPlayer = 8,
        BuildAdditionalStreamedScenes = 16,
        AcceptExternalModificationsToPlayer = 32,
        ConnectWithProfiler = 256,
        AllowDebugging = 512,
        SymlinkSources = 1024,
        UncompressedAssetBundle = 2048,
        ConnectToHost = 4096,
        EnableHeadlessMode = 16384,
        BuildScriptsOnly = 32768,
        StrictMode = 512000,
        IncludeTestAssemblies = 1048576,
        DetailedBuildReport = 33554432,
        CleanBuildCache = 268435456,
    }

    public struct BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public string assetBundleManifestPath { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public BuildOptions options { get; set; }
        public Build.NamedBuildTarget subtarget { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

    public static class BuildPipeline
    {
        public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions buildPlayerOptions) { return null; }
        public static Build.Reporting.BuildReport BuildPlayer(string[] levels, string locationPathName, BuildTarget target, BuildOptions options) { return null; }
        public static bool IsBuildTargetSupported(BuildTargetGroup buildTargetGroup, BuildTarget target) { return false; }
        public static BuildTargetGroup GetBuildTargetGroup(BuildTarget platform) { return default(BuildTargetGroup); }
    }

    public sealed class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene() { }
        public EditorBuildSettingsScene(string path, bool enabled) { }
        public string path { get; set; }
        public bool enabled { get; set; }
        public GUID guid { get; set; }
    }

    public struct GUID
    {
        public GUID(string hexRepresentation) { }
        public bool Empty() { return false; }
        public override string ToString() { return null; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }

    public sealed class SceneAsset : UnityEngine.Object { }

    public static class Lightmapping
    {
        public static bool isRunning { get { return false; } }
        public static LightingSettings lightingSettings { get; set; }
        public static GIWorkflowMode giWorkflowMode { get; set; }
        public static bool realtimeGI { get; set; }
        public static bool bakedGI { get; set; }
        public static float indirectOutputScale { get; set; }
        public static float bounceBoost { get; set; }

        public static bool Bake() { return false; }
        public static void BakeAsync() { }
        public static void Cancel() { }
        public static void Clear() { }
        public static void ClearDiskCache() { }
        public static void ClearLightingDataAsset() { }
        public static bool BakeReflectionProbe(ReflectionProbe probe, string usedPath) { return false; }

        public enum GIWorkflowMode { Iterative = 0, OnDemand = 1, Legacy = 2 }

        public static event Action bakeCompleted;
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string propertyPath) { return null; }
        public bool ApplyModifiedProperties() { return false; }
        public bool ApplyModifiedPropertiesWithoutUndo() { return false; }
        public void Update() { }
        public UnityEngine.Object targetObject { get { return null; } }
    }

    public class SerializedProperty
    {
        public string name { get { return null; } }
        public bool boolValue { get; set; }
        public int intValue { get; set; }
        public float floatValue { get; set; }
        public string stringValue { get; set; }
        public Color colorValue { get; set; }
        public Vector2 vector2Value { get; set; }
        public Vector3 vector3Value { get; set; }
        public Vector4 vector4Value { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
        public int enumValueIndex { get; set; }
        public int arraySize { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int index) { return null; }
        public SerializedProperty FindPropertyRelative(string relativePropertyPath) { return null; }
        public bool NextVisible(bool enterChildren) { return false; }
    }

    public class EditorWindow : ScriptableObject
    {
        public string title { get; set; }
        public Rect position { get; set; }
        public void Show() { }
        public void Close() { }
        public void Repaint() { }
        public static T GetWindow<T>() where T : EditorWindow { return null; }
        public static T GetWindow<T>(string title) where T : EditorWindow { return null; }
    }

    public class Editor : ScriptableObject
    {
        public UnityEngine.Object target { get { return null; } }
        public SerializedObject serializedObject { get { return null; } }
        public virtual void OnInspectorGUI() { }
    }

    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) { return null; }
        public static GameObject InstantiatePrefab(UnityEngine.Object assetComponentOrGameObject) { return null; }
        public static bool IsPartOfPrefabAsset(UnityEngine.Object componentOrGameObject) { return false; }
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene = 0, DefaultGameObjects = 1 }

    public enum NewSceneMode { Single = 0, Additive = 1 }

    public enum OpenSceneMode { Single = 0, Additive = 1, AdditiveWithoutLoading = 2 }

    public static class EditorSceneManager
    {
        public static int sceneCount { get { return 0; } }
        public static bool preventCrossSceneReferences { get; set; }

        public static Scene GetActiveScene() { return default(Scene); }
        public static Scene GetSceneAt(int index) { return default(Scene); }
        public static Scene NewScene(NewSceneSetup setup) { return default(Scene); }
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return default(Scene); }
        public static Scene OpenScene(string scenePath) { return default(Scene); }
        public static Scene OpenScene(string scenePath, OpenSceneMode mode) { return default(Scene); }
        public static bool SaveScene(Scene scene) { return false; }
        public static bool SaveScene(Scene scene, string dstScenePath) { return false; }
        public static bool SaveScene(Scene scene, string dstScenePath, bool saveAsCopy) { return false; }
        public static bool SaveOpenScenes() { return false; }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return false; }
        public static bool MarkSceneDirty(Scene scene) { return false; }
        public static void MarkAllScenesDirty() { }
        public static bool CloseScene(Scene scene, bool removeScene) { return false; }
    }
}

namespace UnityEditor.Build
{
    public struct NamedBuildTarget
    {
        public string TargetName { get { return null; } }
        public BuildTargetGroup ToBuildTargetGroup() { return default(BuildTargetGroup); }

        public static NamedBuildTarget Standalone { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Server { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Android { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget iOS { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget WebGL { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Unknown { get { return default(NamedBuildTarget); } }

        public static NamedBuildTarget FromBuildTargetGroup(BuildTargetGroup buildTargetGroup) { return default(NamedBuildTarget); }
    }

    public interface IOrderedCallback { int callbackOrder { get; } }
    public interface IPreprocessBuildWithReport : IOrderedCallback { void OnPreprocessBuild(Reporting.BuildReport report); }
    public interface IPostprocessBuildWithReport : IOrderedCallback { void OnPostprocessBuild(Reporting.BuildReport report); }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown = 0, Succeeded = 1, Failed = 2, Cancelled = 3 }

    public class BuildReport : UnityEngine.Object
    {
        public BuildSummary summary { get { return default(BuildSummary); } }
        public BuildStep[] steps { get { return null; } }
        public BuildFile[] files { get { return null; } }
        public PackedAssets[] packedAssets { get { return null; } }
    }

    public struct BuildSummary
    {
        public DateTime buildStartedAt { get { return default(DateTime); } }
        public DateTime buildEndedAt { get { return default(DateTime); } }
        public TimeSpan totalTime { get { return default(TimeSpan); } }
        public BuildTarget platform { get { return default(BuildTarget); } }
        public BuildTargetGroup platformGroup { get { return default(BuildTargetGroup); } }
        public BuildOptions options { get { return default(BuildOptions); } }
        public string outputPath { get { return null; } }
        public ulong totalSize { get { return 0UL; } }
        public int totalErrors { get { return 0; } }
        public int totalWarnings { get { return 0; } }
        public BuildResult result { get { return default(BuildResult); } }
        public GUID guid { get { return default(GUID); } }
    }

    public struct BuildStep
    {
        public string name { get { return null; } }
        public TimeSpan duration { get { return default(TimeSpan); } }
        public BuildStepMessage[] messages { get { return null; } }
        public int depth { get { return 0; } }
    }

    public struct BuildStepMessage
    {
        public LogType type { get { return default(LogType); } }
        public string content { get { return null; } }
    }

    public struct BuildFile
    {
        public string path { get { return null; } }
        public string role { get { return null; } }
        public ulong size { get { return 0UL; } }
    }

    public class PackedAssets : UnityEngine.Object
    {
        public string shortPath { get { return null; } }
        public PackedAssetInfo[] contents { get { return null; } }
    }

    public struct PackedAssetInfo
    {
        public string sourceAssetPath { get { return null; } }
        public ulong packedSize { get { return 0UL; } }
    }
}
