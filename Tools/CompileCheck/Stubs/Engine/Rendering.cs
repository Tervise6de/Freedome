// Signature-only stand-ins for UnityEngine's rendering types and the
// UnityEngine.Rendering namespace. See Math.cs for why these exist.

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// A real, working mesh container. The geometry tests read back triangles,
    /// normals, UVs and bounds, so this has to store what the builders write
    /// rather than swallow it.
    /// </summary>
    public sealed class Mesh : Object
    {
        private readonly List<List<int>> _submeshes = new List<List<int>>();
        private Vector3[] _vertices = new Vector3[0];
        private Vector3[] _normals = new Vector3[0];
        private Vector4[] _tangents = new Vector4[0];
        private Vector2[] _uv = new Vector2[0];
        private Vector2[] _uv2 = new Vector2[0];
        private Color[] _colors = new Color[0];
        private Bounds _bounds;

        public Mesh() { }

        public Vector3[] vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new Vector3[0]; }
        }

        public Vector3[] normals
        {
            get { return _normals; }
            set { _normals = value ?? new Vector3[0]; }
        }

        public Vector4[] tangents
        {
            get { return _tangents; }
            set { _tangents = value ?? new Vector4[0]; }
        }

        public Vector2[] uv
        {
            get { return _uv; }
            set { _uv = value ?? new Vector2[0]; }
        }

        public Vector2[] uv2
        {
            get { return _uv2; }
            set { _uv2 = value ?? new Vector2[0]; }
        }

        public Vector2[] uv3 { get; set; }
        public Vector2[] uv4 { get; set; }

        public Color[] colors
        {
            get { return _colors; }
            set { _colors = value ?? new Color[0]; }
        }

        public Color32[] colors32 { get; set; }

        public int[] triangles
        {
            get
            {
                List<int> all = new List<int>();
                foreach (List<int> sub in _submeshes) all.AddRange(sub);
                return all.ToArray();
            }
            set
            {
                _submeshes.Clear();
                _submeshes.Add(new List<int>(value ?? new int[0]));
            }
        }

        public int subMeshCount
        {
            get { return _submeshes.Count; }
            set
            {
                while (_submeshes.Count > value) _submeshes.RemoveAt(_submeshes.Count - 1);
                while (_submeshes.Count < value) _submeshes.Add(new List<int>());
            }
        }

        public int vertexCount { get { return _vertices.Length; } }

        public Bounds bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }

        public Rendering.IndexFormat indexFormat { get; set; }
        public bool isReadable { get { return true; } }

        public void Clear() { _submeshes.Clear(); _vertices = new Vector3[0]; _normals = new Vector3[0]; _uv = new Vector2[0]; }
        public void Clear(bool keepVertexLayout) { Clear(); }

        public void SetVertices(List<Vector3> inVertices) { _vertices = inVertices == null ? new Vector3[0] : inVertices.ToArray(); }
        public void SetVertices(Vector3[] inVertices) { vertices = inVertices; }
        public void SetNormals(List<Vector3> inNormals) { _normals = inNormals == null ? new Vector3[0] : inNormals.ToArray(); }
        public void SetNormals(Vector3[] inNormals) { normals = inNormals; }
        public void SetTangents(List<Vector4> inTangents) { _tangents = inTangents == null ? new Vector4[0] : inTangents.ToArray(); }
        public void SetTangents(Vector4[] inTangents) { tangents = inTangents; }
        public void SetColors(List<Color> inColors) { _colors = inColors == null ? new Color[0] : inColors.ToArray(); }
        public void SetColors(Color[] inColors) { colors = inColors; }

        public void SetUVs(int channel, List<Vector2> uvs)
        {
            SetUVs(channel, uvs == null ? new Vector2[0] : uvs.ToArray());
        }

        public void SetUVs(int channel, Vector2[] uvs)
        {
            switch (channel)
            {
                case 0: _uv = uvs ?? new Vector2[0]; break;
                case 1: _uv2 = uvs ?? new Vector2[0]; break;
                case 2: uv3 = uvs; break;
                default: uv4 = uvs; break;
            }
        }

        public void SetTriangles(List<int> inTriangles, int submesh) { SetTriangles(inTriangles, submesh, true); }

        public void SetTriangles(List<int> inTriangles, int submesh, bool calculateBounds)
        {
            SetTriangles(inTriangles == null ? new int[0] : inTriangles.ToArray(), submesh, calculateBounds);
        }

        public void SetTriangles(int[] tris, int submesh) { SetTriangles(tris, submesh, true); }

        public void SetTriangles(int[] tris, int submesh, bool calculateBounds)
        {
            while (_submeshes.Count <= submesh) _submeshes.Add(new List<int>());
            _submeshes[submesh] = new List<int>(tris ?? new int[0]);
            if (calculateBounds) RecalculateBounds();
        }

        public int[] GetTriangles(int submesh)
        {
            return submesh < 0 || submesh >= _submeshes.Count ? new int[0] : _submeshes[submesh].ToArray();
        }

        public void GetVertices(List<Vector3> outVertices)
        {
            outVertices.Clear();
            outVertices.AddRange(_vertices);
        }

        public void RecalculateNormals()
        {
            Vector3[] accum = new Vector3[_vertices.Length];
            foreach (List<int> sub in _submeshes)
            {
                for (int i = 0; i + 2 < sub.Count; i += 3)
                {
                    int a = sub[i], b = sub[i + 1], c = sub[i + 2];
                    if (a >= accum.Length || b >= accum.Length || c >= accum.Length) continue;
                    Vector3 n = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);
                    accum[a] += n; accum[b] += n; accum[c] += n;
                }
            }
            for (int i = 0; i < accum.Length; i++) accum[i] = accum[i].normalized;
            _normals = accum;
        }

        public void RecalculateTangents() { }

        public void RecalculateBounds()
        {
            if (_vertices.Length == 0) { _bounds = new Bounds(Vector3.zero, Vector3.zero); return; }
            Vector3 lo = _vertices[0], hi = _vertices[0];
            for (int i = 1; i < _vertices.Length; i++)
            {
                lo = Vector3.Min(lo, _vertices[i]);
                hi = Vector3.Max(hi, _vertices[i]);
            }
            Bounds b = new Bounds();
            b.SetMinMax(lo, hi);
            _bounds = b;
        }

        public void Optimize() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
        public void MarkDynamic() { }
        public Rendering.SubMeshDescriptor GetSubMesh(int index) { return default(Rendering.SubMeshDescriptor); }
        public void SetSubMesh(int index, Rendering.SubMeshDescriptor desc) { }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }

        public Shader shader { get; set; }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Vector2 mainTextureScale { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public int renderQueue { get; set; }
        public string[] shaderKeywords { get; set; }

        public bool HasProperty(string name) { return false; }
        public bool HasProperty(int nameID) { return false; }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public void SetInt(string name, int value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetColor(string name, Color value) { }
        public void SetTexture(string name, Texture value) { }
        public void SetTextureScale(string name, Vector2 value) { }
        public void SetTextureOffset(string name, Vector2 value) { }
        public void SetMatrix(string name, Matrix4x4 value) { }
        public float GetFloat(string name) { return 0f; }
        public int GetInt(string name) { return 0; }
        public Color GetColor(string name) { return default(Color); }
        public Vector4 GetVector(string name) { return default(Vector4); }
        public Texture GetTexture(string name) { return null; }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public bool IsKeywordEnabled(string keyword) { return false; }
        public void CopyPropertiesFromMaterial(Material mat) { }
        public void SetShaderPassEnabled(string passName, bool enabled) { }
    }

    public sealed class Shader : Object
    {
        public bool isSupported { get { return false; } }
        public int renderQueue { get { return 0; } }

        public static Shader Find(string name) { return null; }
        public static int PropertyToID(string name) { return 0; }
        public static void SetGlobalFloat(string name, float value) { }
        public static void SetGlobalColor(string name, Color value) { }
        public static void SetGlobalVector(string name, Vector4 value) { }
        public static void SetGlobalTexture(string name, Texture value) { }
    }

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public float mipMapBias { get; set; }
    }

    public sealed class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) { }
        public Texture2D(int width, int height, Experimental.Rendering.GraphicsFormat format, Experimental.Rendering.TextureCreationFlags flags) { }

        public TextureFormat format { get { return default(TextureFormat); } }
        public int mipmapCount { get { return 0; } }

        public Color GetPixel(int x, int y) { return default(Color); }
        public Color GetPixelBilinear(float u, float v) { return default(Color); }
        public Color[] GetPixels() { return null; }
        public Color32[] GetPixels32() { return null; }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void SetPixels32(Color32[] colors) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public void ReadPixels(Rect source, int destX, int destY) { }
        public void ReadPixels(Rect source, int destX, int destY, bool recalculateMipMaps) { }
        public byte[] EncodeToPNG() { return null; }
        public byte[] EncodeToEXR() { return null; }
        public byte[] EncodeToJPG() { return null; }
        public bool LoadImage(byte[] data) { return false; }

        public static Texture2D whiteTexture { get { return null; } }
        public static Texture2D blackTexture { get { return null; } }
        public static Texture2D normalTexture { get { return null; } }
    }

    public sealed class Cubemap : Texture { }

    public class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite) { }

        public int depth { get; set; }
        public int antiAliasing { get; set; }
        public bool useMipMap { get; set; }
        public bool sRGB { get { return false; } }
        public RenderTextureFormat format { get; set; }

        public void Create() { }
        public void Release() { }
        public bool IsCreated() { return false; }

        public static RenderTexture active { get; set; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer) { return null; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format) { return null; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite) { return null; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing) { return null; }
        public static void ReleaseTemporary(RenderTexture temp) { }
    }

    public enum TextureFormat
    {
        Alpha8 = 1,
        RGB24 = 3,
        RGBA32 = 4,
        ARGB32 = 5,
        R8 = 63,
        R16 = 9,
        RGBAHalf = 17,
        RGBAFloat = 20,
        RGB9e5Float = 22,
        RG16 = 62,
        RG32 = 74,
        RGB48 = 75,
        RGBA64 = 76,
        DXT1 = 10,
        DXT5 = 12,
        BC7 = 25,
    }

    public enum RenderTextureFormat
    {
        ARGB32 = 0,
        Depth = 1,
        ARGBHalf = 2,
        RGB565 = 4,
        ARGB4444 = 5,
        ARGB1555 = 6,
        Default = 7,
        ARGBFloat = 11,
        RGFloat = 12,
        RGHalf = 13,
        RFloat = 14,
        RHalf = 15,
        R8 = 16,
        DefaultHDR = 17,
        ARGB64 = 18,
        RGB111110Float = 22,
    }

    public enum RenderTextureReadWrite { Default, Linear, sRGB }

    public enum FilterMode { Point = 0, Bilinear = 1, Trilinear = 2 }

    public enum TextureWrapMode { Repeat = 0, Clamp = 1, Mirror = 2, MirrorOnce = 3 }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material material { get; set; }
        public Material[] materials { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] sharedMaterials { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public Rendering.LightProbeUsage lightProbeUsage { get; set; }
        public Rendering.ReflectionProbeUsage reflectionProbeUsage { get; set; }
        public int lightmapIndex { get; set; }
        public Vector4 lightmapScaleOffset { get; set; }
        public float scaleInLightmap { get; set; }
        public bool isVisible { get { return false; } }
        public int sortingOrder { get; set; }
        public Transform probeAnchor { get; set; }
        public Rendering.MotionVectorGenerationMode motionVectorGenerationMode { get; set; }
        public bool allowOcclusionWhenDynamic { get; set; }
        public bool staticShadowCaster { get; set; }
    }

    public sealed class MeshRenderer : Renderer
    {
        public Mesh additionalVertexStreams { get; set; }
        public int subMeshStartIndex { get { return 0; } }
    }

    public sealed class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh { get; set; }
    }

    public sealed class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public sealed class Camera : Behaviour
    {
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float aspect { get; set; }
        public int cullingMask { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public float depth { get; set; }
        public RenderTexture targetTexture { get; set; }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public Rect rect { get; set; }
        public Rect pixelRect { get; set; }
        public int pixelWidth { get { return 0; } }
        public int pixelHeight { get { return 0; } }
        public Matrix4x4 projectionMatrix { get; set; }
        public Matrix4x4 worldToCameraMatrix { get; set; }
        public bool allowHDR { get; set; }
        public bool allowMSAA { get; set; }
        public bool useOcclusionCulling { get; set; }

        public void Render() { }
        public Ray ScreenPointToRay(Vector3 position) { return default(Ray); }
        public Vector3 WorldToScreenPoint(Vector3 position) { return default(Vector3); }
        public Vector3 ScreenToWorldPoint(Vector3 position) { return default(Vector3); }
        public Vector3 WorldToViewportPoint(Vector3 position) { return default(Vector3); }

        public static Camera main { get { return null; } }
        public static Camera current { get { return null; } }
        public static Camera[] allCameras { get { return null; } }
        public static int allCamerasCount { get { return 0; } }
    }

    public enum CameraClearFlags
    {
        Skybox = 1,
        Color = 2,
        SolidColor = 2,
        Depth = 3,
        Nothing = 4,
    }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
        public float innerSpotAngle { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public float shadowBias { get; set; }
        public float shadowNormalBias { get; set; }
        public float shadowNearPlane { get; set; }
        public LightShadowResolution shadowResolution { get; set; }
        public LightmapBakeType lightmapBakeType { get; set; }
        public int cullingMask { get; set; }
        public float bounceIntensity { get; set; }
        public float colorTemperature { get; set; }
        public bool useColorTemperature { get; set; }
        public Rendering.LightShadowCasterMode lightShadowCasterMode { get; set; }
        public Cubemap cookie { get; set; }
        public float cookieSize { get; set; }
    }

    public enum LightType { Spot = 0, Directional = 1, Point = 2, Area = 3, Rectangle = 3, Disc = 4 }

    public enum LightShadows { None = 0, Hard = 1, Soft = 2 }

    public enum LightShadowResolution { FromQualitySettings = -1, Low = 0, Medium = 1, High = 2, VeryHigh = 3 }

    public enum LightmapBakeType { Realtime = 4, Baked = 2, Mixed = 1 }

    public sealed class ReflectionProbe : Behaviour
    {
        public ReflectionProbeMode mode { get; set; }
        public ReflectionProbeRefreshMode refreshMode { get; set; }
        public ReflectionProbeTimeSlicingMode timeSlicingMode { get; set; }
        public Vector3 size { get; set; }
        public Vector3 center { get; set; }
        public float intensity { get; set; }
        public float blendDistance { get; set; }
        public int resolution { get; set; }
        public bool boxProjection { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public int cullingMask { get; set; }
        public float shadowDistance { get; set; }
        public Texture bakedTexture { get; set; }
        public Texture customBakedTexture { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
        public int importance { get; set; }
        public ReflectionProbeClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public bool hdr { get; set; }

        public int RenderProbe() { return 0; }
    }

    public enum ReflectionProbeMode { Baked = 0, Realtime = 1, Custom = 2 }
    public enum ReflectionProbeRefreshMode { OnAwake = 0, EveryFrame = 1, ViaScripting = 2 }
    public enum ReflectionProbeTimeSlicingMode { AllFacesAtOnce = 0, IndividualFaces = 1, NoTimeSlicing = 2 }
    public enum ReflectionProbeClearFlags { Skybox = 1, SolidColor = 2 }

    public sealed class LightProbeGroup : Behaviour
    {
        public Vector3[] probePositions { get; set; }
        public bool dering { get; set; }
    }

    public sealed class LightProbes : Object
    {
        public static SphericalHarmonicsL2[] bakedProbes { get; set; }
        public static Vector3[] positions { get { return null; } }
        public static int count { get { return 0; } }
        public static void GetInterpolatedProbe(Vector3 position, Renderer renderer, out SphericalHarmonicsL2 probe) { probe = default(SphericalHarmonicsL2); }
    }

    public struct SphericalHarmonicsL2
    {
        public float this[int rgb, int coefficient] { get { return 0f; } set { } }
        public void Clear() { }
        public void AddAmbientLight(Color color) { }
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static int antiAliasing { get; set; }
        public static float shadowDistance { get; set; }
        public static ShadowQuality shadows { get; set; }
        public static ShadowResolution shadowResolution { get; set; }
        public static ShadowmaskMode shadowmaskMode { get; set; }
        public static int shadowCascades { get; set; }
        public static AnisotropicFiltering anisotropicFiltering { get; set; }
        public static SkinWeights skinWeights { get; set; }
        public static float lodBias { get; set; }
        public static int masterTextureLimit { get; set; }
        public static int pixelLightCount { get; set; }
        public static bool realtimeReflectionProbes { get; set; }
        public static bool softParticles { get; set; }
        public static string[] names { get { return null; } }
        public static int count { get { return 0; } }
        public static Rendering.RenderPipelineAsset renderPipeline { get; set; }

        public static int GetQualityLevel() { return 0; }
        public static void SetQualityLevel(int index) { }
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
    }

    public enum ShadowQuality { Disable = 0, HardOnly = 1, All = 2 }
    public enum ShadowResolution { Low = 0, Medium = 1, High = 2, VeryHigh = 3 }
    public enum ShadowmaskMode { Shadowmask = 0, DistanceShadowmask = 1 }
    public enum AnisotropicFiltering { Disable = 0, Enable = 1, ForceEnable = 2 }

    public enum SkinWeights { OneBone = 1, TwoBones = 2, FourBones = 4, Unlimited = 255 }

    public enum LightmapCompression { None = 0, LowQuality = 1, NormalQuality = 2, HighQuality = 3 }

    public sealed class AudioListener : Behaviour
    {
        public static float volume { get; set; }
        public static bool pause { get; set; }
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public bool spatialize { get; set; }
        public float spatialBlend { get; set; }
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip clip) { }
    }

    public sealed class AudioClip : Object
    {
        public float length { get { return 0f; } }
        public int frequency { get { return 0; } }
    }

    public static class RenderSettings
    {
        public static Material skybox { get; set; }
        public static Light sun { get; set; }
        public static Color ambientLight { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static float ambientIntensity { get; set; }
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static SphericalHarmonicsL2 ambientProbe { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogDensity { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Rendering.DefaultReflectionMode defaultReflectionMode { get; set; }
        public static int defaultReflectionResolution { get; set; }
        public static float reflectionIntensity { get; set; }
        public static int reflectionBounces { get; set; }
        public static Cubemap customReflection { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class Graphics
    {
        public static void Blit(Texture source, RenderTexture dest) { }
        public static void Blit(Texture source, RenderTexture dest, Material mat) { }
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer) { }
        public static void CopyTexture(Texture src, Texture dst) { }
    }

    public static class Gizmos
    {
        public static Color color { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static void DrawLine(Vector3 from, Vector3 to) { }
        public static void DrawRay(Vector3 from, Vector3 direction) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawCube(Vector3 center, Vector3 size) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation) { }
    }

    public sealed class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) { return null; }
        public int fontSize { get { return 0; } }
    }
}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16 = 0, UInt32 = 1 }

    public enum ShadowCastingMode { Off = 0, On = 1, TwoSided = 2, ShadowsOnly = 3 }

    public enum LightProbeUsage { Off = 0, BlendProbes = 1, UseProxyVolume = 2, CustomProvided = 4 }

    public enum ReflectionProbeUsage { Off = 0, BlendProbes = 1, BlendProbesAndSkybox = 2, Simple = 3 }

    public enum MotionVectorGenerationMode { Camera = 0, Object = 1, ForceNoMotion = 2 }

    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }

    public enum DefaultReflectionMode { Skybox = 0, Custom = 1 }

    public enum LightShadowCasterMode { Default = 0, NonLightmappedOnly = 1, Everything = 2 }

    public enum MeshTopology { Triangles = 0, Quads = 2, Lines = 3, LineStrip = 4, Points = 5 }

    public enum CompareFunction { Disabled = 0, Never = 1, Less = 2, Equal = 3, LessEqual = 4, Greater = 5, NotEqual = 6, GreaterEqual = 7, Always = 8 }

    public enum CullMode { Off = 0, Front = 1, Back = 2 }

    public enum BlendMode { Zero = 0, One = 1, SrcAlpha = 5, OneMinusSrcAlpha = 10 }

    public struct SubMeshDescriptor
    {
        public Bounds bounds { get; set; }
        public MeshTopology topology { get; set; }
        public int indexStart { get; set; }
        public int indexCount { get; set; }
        public int baseVertex { get; set; }
        public int firstVertex { get; set; }
        public int vertexCount { get; set; }
    }

    public abstract class RenderPipelineAsset : ScriptableObject
    {
        public virtual string renderPipelineShaderTag { get { return null; } }
    }

    public abstract class RenderPipeline { }

    public static class GraphicsSettings
    {
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset renderPipelineAsset { get; set; }
        public static RenderPipelineAsset currentRenderPipeline { get { return null; } }
        public static bool lightsUseLinearIntensity { get; set; }
        public static bool lightsUseColorTemperature { get; set; }
    }

    // The volume framework. VolumeComponent lives here, and every override
    // parameter derives from VolumeParameter<T>.
    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get; set; }
        public float priority { get; set; }
        public float blendDistance { get; set; }
        public float weight { get; set; }
        public VolumeProfile profile { get; set; }
        public VolumeProfile sharedProfile { get; set; }
        public VolumeProfile profileRef { get { return null; } }
    }

    public sealed class VolumeProfile : ScriptableObject
    {
        public List<VolumeComponent> components { get { return null; } }
        public bool isDirty { get; set; }

        public T Add<T>() where T : VolumeComponent { return null; }
        public T Add<T>(bool overrides) where T : VolumeComponent { return null; }
        public VolumeComponent Add(Type type, bool overrides = false) { return null; }
        public void Remove<T>() where T : VolumeComponent { }
        public void Remove(Type type) { }
        public bool Has<T>() where T : VolumeComponent { return false; }
        public bool Has(Type type) { return false; }
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
        public bool TryGet(Type type, out VolumeComponent component) { component = null; return false; }
        public void Reset() { }
    }

    public class VolumeComponent : ScriptableObject
    {
        public bool active { get; set; }
        public virtual bool displayName { get { return false; } }
        public void SetAllOverridesTo(bool state) { }
    }

    public abstract class VolumeParameter
    {
        public bool overrideState { get; set; }
    }

    public class VolumeParameter<T> : VolumeParameter
    {
        public VolumeParameter() { }
        public VolumeParameter(T value, bool overrideState = false) { }

        public T value { get; set; }

        public virtual void Override(T x) { }
        public void SetValue(VolumeParameter parameter) { }

        public static implicit operator T(VolumeParameter<T> prop) { return default(T); }
    }

    public class BoolParameter : VolumeParameter<bool>
    {
        public BoolParameter(bool value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class IntParameter : VolumeParameter<int>
    {
        public IntParameter(int value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class MinIntParameter : IntParameter
    {
        public MinIntParameter(int value, int min, bool overrideState = false) : base(value, overrideState) { }
    }

    public class ClampedIntParameter : IntParameter
    {
        public ClampedIntParameter(int value, int min, int max, bool overrideState = false) : base(value, overrideState) { }
    }

    public class FloatParameter : VolumeParameter<float>
    {
        public FloatParameter(float value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class MinFloatParameter : FloatParameter
    {
        public MinFloatParameter(float value, float min, bool overrideState = false) : base(value, overrideState) { }
    }

    public class MaxFloatParameter : FloatParameter
    {
        public MaxFloatParameter(float value, float max, bool overrideState = false) : base(value, overrideState) { }
    }

    public class ClampedFloatParameter : FloatParameter
    {
        public ClampedFloatParameter(float value, float min, float max, bool overrideState = false) : base(value, overrideState) { }
    }

    public class ColorParameter : VolumeParameter<Color>
    {
        public ColorParameter(Color value, bool overrideState = false) : base(value, overrideState) { }
        public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false) : base(value, overrideState) { }
        public bool hdr { get; set; }
    }

    public class Vector2Parameter : VolumeParameter<Vector2>
    {
        public Vector2Parameter(Vector2 value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class Vector3Parameter : VolumeParameter<Vector3>
    {
        public Vector3Parameter(Vector3 value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class TextureParameter : VolumeParameter<Texture>
    {
        public TextureParameter(Texture value, bool overrideState = false) : base(value, overrideState) { }
    }
}

namespace UnityEngine.Experimental.Rendering
{
    public enum GraphicsFormat
    {
        None = 0,
        R8_UNorm = 1,
        R8G8_UNorm = 4,
        R8G8B8_UNorm = 7,
        R8G8B8A8_UNorm = 8,
        R8G8B8A8_SRGB = 9,
        R16G16B16A16_SFloat = 49,
        R32G32B32A32_SFloat = 52,
    }

    [Flags]
    public enum TextureCreationFlags
    {
        None = 0,
        MipChain = 1,
        Crunch = 64,
    }
}

namespace UnityEngine
{
    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }

    // Lighting settings live in UnityEngine even though only the editor writes
    // them, which is why Lightmapping.lightingSettings type-checks from an
    // editor assembly against a runtime type.
    public sealed class LightingSettings : Object
    {
        public LightingSettings() { }

        public bool autoGenerate { get; set; }
        public bool realtimeGI { get; set; }
        public bool bakedGI { get; set; }
        public LightingSettings.Lightmapper lightmapper { get; set; }
        public LightingSettings.Sampling sampling { get; set; }
        public int directSampleCount { get; set; }
        public int indirectSampleCount { get; set; }
        public int environmentSampleCount { get; set; }
        public int maxBounces { get; set; }
        public int minBounces { get; set; }
        public float lightmapResolution { get; set; }
        public int lightmapPadding { get; set; }
        public int lightmapMaxSize { get; set; }
        public bool compressLightmaps { get; set; }
        public bool ao { get; set; }
        public float aoMaxDistance { get; set; }
        public float aoExponentDirect { get; set; }
        public float aoExponentIndirect { get; set; }
        public MixedLightingMode mixedBakeMode { get; set; }
        public LightingSettings.FilterMode filteringMode { get; set; }
        public LightmapCompression lightmapCompression { get; set; }
        public LightingSettings.DenoiserType denoiserTypeDirect { get; set; }
        public LightingSettings.DenoiserType denoiserTypeIndirect { get; set; }
        public LightingSettings.DenoiserType denoiserTypeAO { get; set; }
        public bool exportTrainingData { get; set; }
        public bool extractAmbientOcclusion { get; set; }

        public enum Lightmapper { Enlighten = 0, ProgressiveCPU = 1, ProgressiveGPU = 2 }
        public enum Sampling { Auto = 0, Fixed = 1 }
        public enum FilterMode { None = 0, Auto = 1, Advanced = 2 }
        public enum DenoiserType { None = 0, Optix = 1, OpenImage = 2, RadeonPro = 3 }
    }

    public enum MixedLightingMode { IndirectOnly = 0, Shadowmask = 2, Subtractive = 1 }
}

namespace UnityEngine.Rendering
{
    public enum GraphicsDeviceType
    {
        Direct3D11 = 2,
        OpenGLES3 = 11,
        Direct3D12 = 18,
        Vulkan = 21,
        Metal = 16,
    }
}
