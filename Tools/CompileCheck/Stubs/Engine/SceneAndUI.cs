// Signature-only stand-ins for scene management, uGUI, the event system and
// the profiler recorder API. See Math.cs for why these exist.

using System;
using System.Collections.Generic;

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get { return null; } set { } }
        public string path { get { return null; } }
        public int buildIndex { get { return 0; } }
        public bool isDirty { get { return false; } }
        public bool isLoaded { get { return false; } }
        public int rootCount { get { return 0; } }
        public bool IsValid() { return false; }
        public GameObject[] GetRootGameObjects() { return null; }
        public void GetRootGameObjects(List<GameObject> rootGameObjects) { }

        public static bool operator ==(Scene lhs, Scene rhs) { return false; }
        public static bool operator !=(Scene lhs, Scene rhs) { return false; }
        public override bool Equals(object other) { return false; }
        public override int GetHashCode() { return 0; }
    }

    public enum LoadSceneMode { Single = 0, Additive = 1 }

    public static class SceneUtility
    {
        public static string GetScenePathByBuildIndex(int buildIndex) { return null; }
        public static int GetBuildIndexByScenePath(string scenePath) { return 0; }
    }

    public static class SceneManager
    {
        public static int sceneCount { get { return 0; } }
        public static int sceneCountInBuildSettings { get { return 0; } }

        public static Scene GetActiveScene() { return default(Scene); }
        public static bool SetActiveScene(Scene scene) { return false; }
        public static Scene GetSceneAt(int index) { return default(Scene); }
        public static Scene GetSceneByName(string name) { return default(Scene); }
        public static Scene GetSceneByPath(string scenePath) { return default(Scene); }
        public static Scene GetSceneByBuildIndex(int buildIndex) { return default(Scene); }
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(string sceneName, LoadSceneMode mode) { }
        public static void LoadScene(int sceneBuildIndex) { }
        public static void LoadScene(int sceneBuildIndex, LoadSceneMode mode) { }
        public static AsyncOperation LoadSceneAsync(string sceneName) { return null; }
        public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode) { return null; }
        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex) { return null; }
        public static AsyncOperation UnloadSceneAsync(Scene scene) { return null; }

        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static event Action<Scene> sceneUnloaded;
    }
}

namespace UnityEngine.UI
{
    public class Graphic : UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
        public Material material { get; set; }
        public virtual void SetAllDirty() { }
    }

    public abstract class UIBehaviour : MonoBehaviour { }

    public class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public bool preserveAspect { get; set; }
        public float fillAmount { get; set; }

        public enum Type { Simple, Sliced, Tiled, Filled }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; }
    }

    public class Text : MaskableGraphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public float lineSpacing { get; set; }
        public bool supportRichText { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
    }

    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class Selectable : UIBehaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
        public Navigation navigation { get; set; }
        public Transition transition { get; set; }

        public enum Transition { None, ColorTint, SpriteSwap, Animation }
    }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
        public static ColorBlock defaultColorBlock { get { return default(ColorBlock); } }
    }

    public struct Navigation
    {
        public Mode mode { get; set; }
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; set; }

        [Serializable]
        public class ButtonClickedEvent : Events.UnityEvent { }
    }

    public class Slider : Selectable
    {
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public bool wholeNumbers { get; set; }
        public float normalizedValue { get; set; }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public SliderEvent onValueChanged { get; set; }

        public void SetValueWithoutNotify(float input) { }

        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }

        [Serializable]
        public class SliderEvent : Events.UnityEvent<float> { }
    }

    public class Toggle : Selectable
    {
        public bool isOn { get; set; }
        public Graphic graphic { get; set; }
        public ToggleEvent onValueChanged { get; set; }

        public void SetIsOnWithoutNotify(bool value) { }

        [Serializable]
        public class ToggleEvent : Events.UnityEvent<bool> { }
    }

    public class Dropdown : Selectable
    {
        public int value { get; set; }
        public List<OptionData> options { get; set; }
        public Text captionText { get; set; }
        public Text itemText { get; set; }
        public DropdownEvent onValueChanged { get; set; }

        public void SetValueWithoutNotify(int input) { }

        public void AddOptions(List<string> options) { }
        public void AddOptions(List<OptionData> options) { }
        public void ClearOptions() { }
        public void RefreshShownValue() { }

        [Serializable]
        public class OptionData
        {
            public OptionData() { }
            public OptionData(string text) { }
            public string text { get; set; }
            public Sprite image { get; set; }
        }

        [Serializable]
        public class DropdownEvent : Events.UnityEvent<int> { }
    }

    public class CanvasScaler : UIBehaviour
    {
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float referencePixelsPerUnit { get; set; }
        public float scaleFactor { get; set; }

        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
    }

    public class GraphicRaycaster : UIBehaviour
    {
        public bool ignoreReversedGraphics { get; set; }
    }

    public class LayoutElement : UIBehaviour
    {
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public bool ignoreLayout { get; set; }
    }

    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }

    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class ContentSizeFitter : UIBehaviour
    {
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }
}

namespace UnityEngine
{
    public sealed class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public int sortingOrder { get; set; }
        public float planeDistance { get; set; }
        public bool overrideSorting { get; set; }
        public string sortingLayerName { get; set; }
        public bool pixelPerfect { get; set; }
        public int targetDisplay { get; set; }
        public AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }
    }

    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    [Flags]
    public enum AdditionalCanvasShaderChannels { None = 0, TexCoord1 = 1, TexCoord2 = 2, TexCoord3 = 4, Normal = 8, Tangent = 16 }

    public sealed class CanvasRenderer : Component
    {
        public void SetAlpha(float alpha) { }
        public void Clear() { }
    }

    public sealed class CanvasGroup : Behaviour
    {
        public float alpha { get; set; }
        public bool interactable { get; set; }
        public bool blocksRaycasts { get; set; }
        public bool ignoreParentGroups { get; set; }
    }

    public sealed class Sprite : Object
    {
        public Rect rect { get { return default(Rect); } }
        public Texture2D texture { get { return null; } }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) { return null; }
    }

    [Serializable]
    public sealed class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
}

namespace UnityEngine.Events
{
    public abstract class UnityEventBase
    {
        public void RemoveAllListeners() { }
        public int GetPersistentEventCount() { return 0; }
    }

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void Invoke() { }
    }

    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void Invoke(T0 arg0) { }
    }

    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : UIBehaviour
    {
        public GameObject firstSelectedGameObject { get; set; }
        public GameObject currentSelectedGameObject { get { return null; } }
        public bool sendNavigationEvents { get; set; }
        public void SetSelectedGameObject(GameObject selected) { }
        public static EventSystem current { get; set; }
    }

    public abstract class UIBehaviour : MonoBehaviour { }

    public abstract class BaseInputModule : UIBehaviour { }

    public class PointerInputModule : BaseInputModule { }

    public class StandaloneInputModule : PointerInputModule
    {
        public string horizontalAxis { get; set; }
        public string verticalAxis { get; set; }
        public string submitButton { get; set; }
        public string cancelButton { get; set; }
    }

    public class BaseEventData
    {
        public BaseEventData(EventSystem eventSystem) { }
    }

    public class PointerEventData : BaseEventData
    {
        public PointerEventData(EventSystem eventSystem) : base(eventSystem) { }
        public Vector2 position { get; set; }
    }

    public interface IPointerEnterHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData eventData); }
}

namespace Unity.Profiling
{
    public enum ProfilerCategory2 { }

    public readonly struct ProfilerCategory
    {
        public ProfilerCategory(string name) { }
        public static ProfilerCategory Render { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Scripts { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Memory { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Internal { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Gui { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Physics { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Animation { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Ai { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Audio { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Video { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Particles { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Lighting { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Network { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Loading { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Vr { get { return default(ProfilerCategory); } }
        public static ProfilerCategory Input { get { return default(ProfilerCategory); } }
        public static ProfilerCategory FileIO { get { return default(ProfilerCategory); } }
    }

    [Flags]
    public enum ProfilerRecorderOptions
    {
        None = 0,
        StartImmediately = 1,
        KeepAliveDuringDomainReload = 2,
        CollectOnlyOnCurrentThread = 4,
        WrapAroundWhenCapacityReached = 8,
        SumAllSamplesInFrame = 16,
        Default = 8,
    }

    public struct ProfilerRecorderSample
    {
        public long Value { get { return 0L; } }
        public long Count { get { return 0L; } }
        public long RefValue { get { return 0L; } }
    }

    public struct ProfilerRecorder : IDisposable
    {
        public ProfilerRecorder(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default) { }
        public ProfilerRecorder(string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default) { }

        public bool Valid { get { return false; } }
        public long CurrentValue { get { return 0L; } }
        public double CurrentValueAsDouble { get { return 0.0; } }
        public long LastValue { get { return 0L; } }
        public int Count { get { return 0; } }
        public int Capacity { get { return 0; } }
        public bool IsRunning { get { return false; } }

        public void Start() { }
        public void Stop() { }
        public void Reset() { }
        public ProfilerRecorderSample GetSample(int index) { return default(ProfilerRecorderSample); }
        public void CopyTo(System.Collections.Generic.List<ProfilerRecorderSample> outSamples, bool reset = false) { }
        public void Dispose() { }

        public static ProfilerRecorder StartNew(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default) { return default(ProfilerRecorder); }
    }
}
