// Signature-only stand-ins for the IMGUI types. Only the performance overlay
// uses these - it draws through OnGUI so it works in a player build without a
// canvas. See Math.cs for why these exist.

using System;

namespace UnityEngine
{
    public sealed class GUIStyleState
    {
        public Color textColor { get; set; }
        public Texture2D background { get; set; }
    }

    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }

        public string name { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public bool stretchWidth { get; set; }
        public bool stretchHeight { get; set; }
        public float lineHeight { get { return 0f; } }
        public float fixedWidth { get; set; }
        public float fixedHeight { get; set; }
        public RectOffset padding { get; set; }
        public RectOffset margin { get; set; }
        public RectOffset border { get; set; }
        public RectOffset overflow { get; set; }
        public Vector2 contentOffset { get; set; }
        public TextClipping clipping { get; set; }

        public GUIStyleState normal { get { return null; } set { } }
        public GUIStyleState hover { get { return null; } set { } }
        public GUIStyleState active { get { return null; } set { } }
        public GUIStyleState focused { get { return null; } set { } }
        public GUIStyleState onNormal { get { return null; } set { } }
        public GUIStyleState onHover { get { return null; } set { } }
        public GUIStyleState onActive { get { return null; } set { } }

        public Vector2 CalcSize(GUIContent content) { return default(Vector2); }
        public float CalcHeight(GUIContent content, float width) { return 0f; }
        public void Draw(Rect position, GUIContent content, int controlID) { }

        public static GUIStyle none { get { return null; } }
    }

    public enum TextClipping { Overflow = 0, Clip = 1 }

    public sealed class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public GUIContent(Texture image) { }
        public GUIContent(string text, Texture image) { }
        public GUIContent(string text, string tooltip) { }
        public GUIContent(GUIContent src) { }

        public string text { get; set; }
        public Texture image { get; set; }
        public string tooltip { get; set; }

        public static GUIContent none { get { return null; } }
    }

    public class GUISkin : ScriptableObject
    {
        public Font font { get; set; }
        public GUIStyle box { get; set; }
        public GUIStyle label { get; set; }
        public GUIStyle button { get; set; }
        public GUIStyle toggle { get; set; }
        public GUIStyle textField { get; set; }
        public GUIStyle window { get; set; }
        public GUIStyle GetStyle(string styleName) { return null; }
        public GUIStyle FindStyle(string styleName) { return null; }
    }

    public static class GUI
    {
        public static GUISkin skin { get; set; }
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static Color contentColor { get; set; }
        public static int depth { get; set; }
        public static bool enabled { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static bool changed { get; set; }

        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, GUIContent content) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static void Label(Rect position, GUIContent content, GUIStyle style) { }
        public static void Box(Rect position, string text) { }
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static bool Button(Rect position, string text) { return false; }
        public static bool Button(Rect position, GUIContent content, GUIStyle style) { return false; }
        public static void DrawTexture(Rect position, Texture image) { }
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode) { }
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend) { }
        public static string TextField(Rect position, string text) { return null; }
        public static bool Toggle(Rect position, bool value, string text) { return false; }
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue) { return 0f; }
        public static void BeginGroup(Rect position) { }
        public static void EndGroup() { }
    }

    public enum ScaleMode { StretchToFill = 0, ScaleAndCrop = 1, ScaleToFit = 2 }

    public static class GUILayout
    {
        public static void Label(string text) { }
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static bool Button(string text, params GUILayoutOption[] options) { return false; }
        public static void Space(float pixels) { }
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void EndVertical() { }
        public static GUILayoutOption Width(float width) { return null; }
        public static GUILayoutOption Height(float height) { return null; }
        public static GUILayoutOption ExpandWidth(bool expand) { return null; }
    }

    public sealed class GUILayoutOption { }

    public sealed class GUIUtility
    {
        public static int GetControlID(FocusType focus) { return 0; }
        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint) { return default(Vector2); }
        public static Vector2 GUIToScreenPoint(Vector2 guiPoint) { return default(Vector2); }
    }

    public enum FocusType { Native = 0, Keyboard = 1, Passive = 2 }

    public sealed class Event
    {
        public static Event current { get { return null; } }
        public EventType type { get; set; }
        public KeyCode keyCode { get; set; }
        public Vector2 mousePosition { get; set; }
        public int button { get; set; }
        public void Use() { }
    }

    public enum EventType
    {
        MouseDown = 0,
        MouseUp = 1,
        MouseMove = 2,
        MouseDrag = 3,
        KeyDown = 4,
        KeyUp = 5,
        ScrollWheel = 6,
        Repaint = 7,
        Layout = 8,
    }
}
