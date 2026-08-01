// Signature-only stand-ins for UnityEngine's object model, components and
// attributes. See Math.cs for why these exist and what they are worth.

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum HideFlags
    {
        None = 0,
        HideInHierarchy = 1,
        HideInInspector = 2,
        DontSaveInEditor = 4,
        NotEditable = 8,
        DontSaveInBuild = 16,
        DontUnloadUnusedAsset = 32,
        DontSave = 52,
        HideAndDontSave = 61,
    }

    public enum Space { World, Self }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public enum RuntimePlatform { WindowsPlayer, WindowsEditor, OSXPlayer, OSXEditor, LinuxPlayer, LinuxEditor }

    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }

    public enum CursorLockMode { None, Locked, Confined }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight,
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }

        public int GetInstanceID() { return 0; }

        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float t) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets) { }
        public static void DontDestroyOnLoad(Object target) { }

        public static Object Instantiate(Object original) { return null; }
        public static Object Instantiate(Object original, Transform parent) { return null; }
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) { return null; }
        public static T Instantiate<T>(T original) where T : Object { return null; }
        public static T Instantiate<T>(T original, Transform parent) where T : Object { return null; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object { return null; }

        public static Object[] FindObjectsOfType(Type type) { return null; }
        public static T[] FindObjectsOfType<T>() where T : Object { return null; }
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object { return null; }
        public static Object FindObjectOfType(Type type) { return null; }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static T FindAnyObjectByType<T>() where T : Object { return null; }

        public static bool operator ==(Object x, Object y) { return false; }
        public static bool operator !=(Object x, Object y) { return false; }
        public static implicit operator bool(Object exists) { return false; }

        public override bool Equals(object other) { return false; }
        public override int GetHashCode() { return 0; }
        public override string ToString() { return null; }
    }

    public enum FindObjectsSortMode { None, InstanceID }
    public enum FindObjectsInactive { Exclude, Include }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }

        public Transform transform { get { return null; } }
        public bool activeSelf { get { return false; } }
        public bool activeInHierarchy { get { return false; } }
        public string tag { get; set; }
        public int layer { get; set; }
        public bool isStatic { get; set; }
        public SceneManagement.Scene scene { get { return default(SceneManagement.Scene); } }

        public void SetActive(bool value) { }
        public Component AddComponent(Type componentType) { return null; }
        public T AddComponent<T>() where T : Component { return null; }
        public Component GetComponent(Type type) { return null; }
        public Component GetComponent(string type) { return null; }
        public T GetComponent<T>() { return default(T); }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponents<T>() { return null; }
        public Component[] GetComponents(Type type) { return null; }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T[] GetComponentsInParent<T>() { return null; }
        public void SendMessage(string methodName) { }
        public void SendMessage(string methodName, object value) { }
        public void BroadcastMessage(string methodName) { }
        public bool CompareTag(string tag) { return false; }

        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
        public static GameObject Find(string name) { return null; }
        public static GameObject FindWithTag(string tag) { return null; }
        public static GameObject[] FindGameObjectsWithTag(string tag) { return null; }
    }

    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public string tag { get; set; }

        public Component GetComponent(Type type) { return null; }
        public T GetComponent<T>() { return default(T); }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponents<T>() { return null; }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T[] GetComponentsInParent<T>() { return null; }
        public void SendMessage(string methodName) { }
        public void SendMessage(string methodName, object value) { }
        public bool CompareTag(string tag) { return false; }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return false; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public bool useGUILayout { get; set; }

        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public Coroutine StartCoroutine(string methodName) { return null; }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public void Invoke(string methodName, float time) { }
        public void InvokeRepeating(string methodName, float time, float repeatRate) { }
        public void CancelInvoke() { }
        public bool IsInvoking() { return false; }

        public static void print(object message) { }
    }

    public class ScriptableObject : Object
    {
        public static ScriptableObject CreateInstance(Type type) { return null; }
        public static T CreateInstance<T>() where T : ScriptableObject { return null; }
    }

    public sealed class Coroutine : YieldInstruction { }

    public class YieldInstruction { }

    public sealed class WaitForSeconds : YieldInstruction
    {
        public WaitForSeconds(float seconds) { }
    }

    public sealed class WaitForSecondsRealtime : CustomYieldInstruction
    {
        public WaitForSecondsRealtime(float time) { }
        public override bool keepWaiting { get { return false; } }
    }

    public sealed class WaitForEndOfFrame : YieldInstruction { }

    public sealed class WaitForFixedUpdate : YieldInstruction { }

    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting { get; }
        public object Current { get { return null; } }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }

    public sealed class WaitUntil : CustomYieldInstruction
    {
        public WaitUntil(Func<bool> predicate) { }
        public override bool keepWaiting { get { return false; } }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get { return default(Vector3); } }
        public Vector3 forward { get; set; }
        public Vector3 up { get; set; }
        public Vector3 right { get; set; }
        public Transform parent { get; set; }
        public Transform root { get { return null; } }
        public int childCount { get { return 0; } }
        public Matrix4x4 localToWorldMatrix { get { return default(Matrix4x4); } }
        public Matrix4x4 worldToLocalMatrix { get { return default(Matrix4x4); } }
        public bool hasChanged { get; set; }

        public void SetParent(Transform p) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation) { }
        public void Translate(Vector3 translation) { }
        public void Translate(Vector3 translation, Space relativeTo) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Rotate(float xAngle, float yAngle, float zAngle) { }
        public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void LookAt(Transform target) { }
        public void LookAt(Vector3 worldPosition) { }
        public void LookAt(Vector3 worldPosition, Vector3 worldUp) { }
        public Vector3 TransformPoint(Vector3 position) { return default(Vector3); }
        public Vector3 TransformDirection(Vector3 direction) { return default(Vector3); }
        public Vector3 TransformVector(Vector3 vector) { return default(Vector3); }
        public Vector3 InverseTransformPoint(Vector3 position) { return default(Vector3); }
        public Vector3 InverseTransformDirection(Vector3 direction) { return default(Vector3); }
        public Vector3 InverseTransformVector(Vector3 vector) { return default(Vector3); }
        public Transform Find(string n) { return null; }
        public Transform GetChild(int index) { return null; }
        public int GetSiblingIndex() { return 0; }
        public void SetSiblingIndex(int index) { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void DetachChildren() { }
        public bool IsChildOf(Transform parent) { return false; }

        public IEnumerator GetEnumerator() { return null; }
    }

    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect { get { return default(Rect); } }

        public enum Axis { Horizontal, Vertical }
        public enum Edge { Left, Right, Top, Bottom }

        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }
        public void SetSizeWithCurrentAnchors(Axis axis, float size) { }
        public void GetWorldCorners(Vector3[] fourCornersArray) { }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogFormat(string format, params object[] args) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogWarningFormat(string format, params object[] args) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogErrorFormat(string format, params object[] args) { }
        public static void LogException(Exception exception) { }
        public static void LogAssertion(object message) { }
        public static void Assert(bool condition) { }
        public static void Assert(bool condition, object message) { }
        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void DrawRay(Vector3 start, Vector3 dir) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color) { }
        public static void Break() { }

        public static bool isDebugBuild { get { return false; } }
        public static bool developerConsoleVisible { get; set; }
    }

    public static class Application
    {
        public static string dataPath { get { return null; } }
        public static string persistentDataPath { get { return null; } }
        public static string streamingAssetsPath { get { return null; } }
        public static string temporaryCachePath { get { return null; } }
        public static string productName { get { return null; } }
        public static string companyName { get { return null; } }
        public static string version { get { return null; } }
        public static string unityVersion { get { return null; } }
        public static bool isPlaying { get { return false; } }
        public static bool isEditor { get { return false; } }
        public static bool isFocused { get { return false; } }
        public static bool isBatchMode { get { return false; } }
        public static RuntimePlatform platform { get { return default(RuntimePlatform); } }
        public static int targetFrameRate { get; set; }
        public static bool runInBackground { get; set; }

        public static void Quit() { }
        public static void Quit(int exitCode) { }

        public static event Action quitting;
    }

    public static class Time
    {
        public static float time { get { return 0f; } }
        public static float timeSinceLevelLoad { get { return 0f; } }
        public static float deltaTime { get { return 0f; } }
        public static float unscaledTime { get { return 0f; } }
        public static float unscaledDeltaTime { get { return 0f; } }
        public static float fixedDeltaTime { get; set; }
        public static float fixedTime { get { return 0f; } }
        public static float smoothDeltaTime { get { return 0f; } }
        public static float maximumDeltaTime { get; set; }
        public static float timeScale { get; set; }
        public static int frameCount { get { return 0; } }
        public static float realtimeSinceStartup { get { return 0f; } }
        public static bool inFixedTimeStep { get { return false; } }
    }

    public static class Screen
    {
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
        public static float dpi { get { return 0f; } }
        public static bool fullScreen { get; set; }
        public static FullScreenMode fullScreenMode { get; set; }
        public static Resolution currentResolution { get { return default(Resolution); } }
        public static Resolution[] resolutions { get { return null; } }
        public static int vSyncCount { get; set; }

        public static void SetResolution(int width, int height, bool fullscreen) { }
        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode) { }
        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, int preferredRefreshRate) { }
    }

    public enum FullScreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    public struct Resolution
    {
        public int width { get { return 0; } set { } }
        public int height { get { return 0; } set { } }
        public int refreshRate { get { return 0; } set { } }
        public override string ToString() { return null; }
    }

    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; }
        public static bool visible { get; set; }
    }

    public static class Input
    {
        public static float GetAxis(string axisName) { return 0f; }
        public static float GetAxisRaw(string axisName) { return 0f; }
        public static bool GetButton(string buttonName) { return false; }
        public static bool GetButtonDown(string buttonName) { return false; }
        public static bool GetButtonUp(string buttonName) { return false; }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKey(string name) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetKeyDown(string name) { return false; }
        public static bool GetKeyUp(KeyCode key) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButtonUp(int button) { return false; }

        public static Vector3 mousePosition { get { return default(Vector3); } }
        public static bool anyKey { get { return false; } }
        public static bool anyKeyDown { get { return false; } }
        public static string inputString { get { return null; } }
        public static bool mousePresent { get { return false; } }
    }

    public enum KeyCode
    {
        None = 0,
        Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Delete = 127,
        Alpha0 = 48, Alpha1 = 49, Alpha2 = 50, Alpha3 = 51, Alpha4 = 52,
        Alpha5 = 53, Alpha6 = 54, Alpha7 = 55, Alpha8 = 56, Alpha9 = 57,
        A = 97, B = 98, C = 99, D = 100, E = 101, F = 102, G = 103, H = 104,
        I = 105, J = 106, K = 107, L = 108, M = 109, N = 110, O = 111, P = 112,
        Q = 113, R = 114, S = 115, T = 116, U = 117, V = 118, W = 119, X = 120,
        Y = 121, Z = 122,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        F1 = 282, F2 = 283, F3 = 284, F4 = 285, F5 = 286, F6 = 287,
        F7 = 288, F8 = 289, F9 = 290, F10 = 291, F11 = 292, F12 = 293,
        LeftShift = 304, RightShift = 303,
        LeftControl = 306, RightControl = 305,
        LeftAlt = 308, RightAlt = 307,
        Mouse0 = 323, Mouse1 = 324, Mouse2 = 325,
    }

    public static class Resources
    {
        public static Object Load(string path) { return null; }
        public static T Load<T>(string path) where T : Object { return null; }
        public static Object[] LoadAll(string path) { return null; }
        public static T[] LoadAll<T>(string path) where T : Object { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
        public static Object GetBuiltinResource(Type type, string path) { return null; }
        public static void UnloadAsset(Object assetToUnload) { }
        public static AsyncOperation UnloadUnusedAssets() { return null; }
        public static T[] FindObjectsOfTypeAll<T>() where T : Object { return null; }
    }

    public class AsyncOperation : YieldInstruction
    {
        public bool isDone { get { return false; } }
        public float progress { get { return 0f; } }
        public bool allowSceneActivation { get; set; }
        public int priority { get; set; }
        public event Action<AsyncOperation> completed;
    }

    public static class Physics
    {
        public const int DefaultRaycastLayers = -5;
        public const int AllLayers = -1;
        public const int IgnoreRaycastLayer = 4;

        public static Vector3 gravity { get; set; }

        public static bool Raycast(Vector3 origin, Vector3 direction) { return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance) { return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance) { return null; }
        public static bool CheckSphere(Vector3 position, float radius) { return false; }
        public static Collider[] OverlapSphere(Vector3 position, float radius) { return null; }
        public static bool ComputePenetration(Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance) { direction = default(Vector3); distance = 0f; return false; }
        public static void SyncTransforms() { }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { hitInfo = default(RaycastHit); return false; }
        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { hitInfo = default(RaycastHit); return false; }
        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool CheckSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { return false; }
    }

    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }

    public struct RaycastHit
    {
        public Vector3 point { get { return default(Vector3); } set { } }
        public Vector3 normal { get { return default(Vector3); } set { } }
        public float distance { get { return 0f; } set { } }
        public Collider collider { get { return null; } }
        public Transform transform { get { return null; } }
        public Rigidbody rigidbody { get { return null; } }
    }

    public struct LayerMask
    {
        public int value { get { return 0; } set { } }
        public static int NameToLayer(string layerName) { return 0; }
        public static string LayerToName(int layer) { return null; }
        public static int GetMask(params string[] layerNames) { return 0; }
        public static implicit operator int(LayerMask mask) { return 0; }
        public static implicit operator LayerMask(int intVal) { return default(LayerMask); }
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
        public PhysicMaterial material { get; set; }
        public PhysicMaterial sharedMaterial { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
        public Vector3 ClosestPoint(Vector3 position) { return default(Vector3); }
    }

    public class BoxCollider : Collider
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
    }

    public class SphereCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
    }

    public class CapsuleCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
        public float height { get; set; }
        public int direction { get; set; }
    }

    public class MeshCollider : Collider
    {
        public Mesh sharedMesh { get; set; }
        public bool convex { get; set; }
    }

    public class PhysicMaterial : Object
    {
        public PhysicMaterial() { }
        public PhysicMaterial(string name) { }
        public float dynamicFriction { get; set; }
        public float staticFriction { get; set; }
        public float bounciness { get; set; }
    }

    public class Rigidbody : Component
    {
        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public float mass { get; set; }
        public float drag { get; set; }
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
        public bool detectCollisions { get; set; }
        public bool freezeRotation { get; set; }
        public RigidbodyInterpolation interpolation { get; set; }
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public RigidbodyConstraints constraints { get; set; }
        public void AddForce(Vector3 force) { }
        public void MovePosition(Vector3 position) { }
    }

    public enum RigidbodyInterpolation { None = 0, Interpolate = 1, Extrapolate = 2 }

    public enum CollisionDetectionMode { Discrete = 0, Continuous = 1, ContinuousDynamic = 2, ContinuousSpeculative = 3 }

    [Flags]
    public enum RigidbodyConstraints
    {
        None = 0,
        FreezePositionX = 2, FreezePositionY = 4, FreezePositionZ = 8,
        FreezeRotationX = 16, FreezeRotationY = 32, FreezeRotationZ = 64,
        FreezePosition = 14, FreezeRotation = 112, FreezeAll = 126,
    }

    [Flags]
    public enum CollisionFlags
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4,
        CollidedSides = 1,
        CollidedAbove = 2,
        CollidedBelow = 4,
    }

    public class CharacterController : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
        public float height { get; set; }
        public float slopeLimit { get; set; }
        public float stepOffset { get; set; }
        public float skinWidth { get; set; }
        public float minMoveDistance { get; set; }
        public bool detectCollisions { get; set; }
        public bool enableOverlapRecovery { get; set; }
        public bool isGrounded { get { return false; } }
        public Vector3 velocity { get { return default(Vector3); } }
        public CollisionFlags collisionFlags { get { return default(CollisionFlags); } }

        public CollisionFlags Move(Vector3 motion) { return default(CollisionFlags); }
        public bool SimpleMove(Vector3 speed) { return false; }
    }

    public class ControllerColliderHit
    {
        public CharacterController controller { get { return null; } }
        public Collider collider { get { return null; } }
        public Transform transform { get { return null; } }
        public Vector3 point { get { return default(Vector3); } }
        public Vector3 normal { get { return default(Vector3); } }
        public Vector3 moveDirection { get { return default(Vector3); } }
        public GameObject gameObject { get { return null; } }
    }

    public class Collision
    {
        public Collider collider { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public Vector3 relativeVelocity { get { return default(Vector3); } }
    }

    // ---- Attributes -------------------------------------------------------

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class HideInInspector : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : Attribute
    {
        public RequireComponent(Type requiredComponent) { }
        public RequireComponent(Type requiredComponent, Type requiredComponent2) { }
        public RequireComponent(Type requiredComponent, Type requiredComponent2, Type requiredComponent3) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RangeAttribute : PropertyAttribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class TooltipAttribute : PropertyAttribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class HeaderAttribute : PropertyAttribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SpaceAttribute : PropertyAttribute
    {
        public SpaceAttribute() { }
        public SpaceAttribute(float height) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MultilineAttribute : PropertyAttribute
    {
        public MultilineAttribute() { }
        public MultilineAttribute(int lines) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TextAreaAttribute : PropertyAttribute
    {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) { }
    }

    public abstract class PropertyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string menuName) { }
        public AddComponentMenu(string menuName, int order) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ExecuteInEditMode : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ExecuteAlways : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DisallowMultipleComponent : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; }
        public string menuName { get; set; }
        public int order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ContextMenu : Attribute
    {
        public ContextMenu(string itemName) { }
        public ContextMenu(string itemName, bool isValidateFunction) { }
        public ContextMenu(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SelectionBaseAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class SerializableAttribute2 : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad,
        BeforeSceneLoad,
        AfterAssembliesLoaded,
        BeforeSplashScreen,
        SubsystemRegistration,
    }
}

namespace UnityEngine
{
    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }

    public static class JsonUtility
    {
        public static string ToJson(object obj) { return null; }
        public static string ToJson(object obj, bool prettyPrint) { return null; }
        public static T FromJson<T>(string json) { return default(T); }
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }

    public static class PlayerPrefs
    {
        public static void SetInt(string key, int value) { }
        public static int GetInt(string key) { return 0; }
        public static int GetInt(string key, int defaultValue) { return 0; }
        public static void SetFloat(string key, float value) { }
        public static float GetFloat(string key) { return 0f; }
        public static float GetFloat(string key, float defaultValue) { return 0f; }
        public static void SetString(string key, string value) { }
        public static string GetString(string key) { return null; }
        public static string GetString(string key, string defaultValue) { return null; }
        public static bool HasKey(string key) { return false; }
        public static void DeleteKey(string key) { }
        public static void DeleteAll() { }
        public static void Save() { }
    }
}
