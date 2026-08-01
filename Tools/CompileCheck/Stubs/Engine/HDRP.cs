// Signature-only stand-ins for the HDRP types the project touches.
//
// IMPORTANT: this is the weakest part of the harness. These signatures are
// written from HDRP's documented API, but nothing here proves that HDRP 17.0.4
// actually declares them - only Unity resolving the real package can do that.
// A clean compile against this file means "the project's use of HDRP is
// internally consistent and matches HDRP as documented", not "HDRP has these
// members". docs/KNOWN_ISSUES.md #1 stays open regardless of what this says.

using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum LightUnit { Lumen, Candela, Lux, Nits, Ev100 }

    public enum ShadowUpdateMode { EveryFrame = 0, OnEnable = 1, OnDemand = 2 }

    public enum SkyType { HDRI = 1, Gradient = 2, PhysicallyBased = 3 }

    public enum CloudType { None = 0, CloudLayer = 1 }

    public enum PhysicallyBasedSkyModel { EarthSimple = 0, EarthAdvanced = 1, Custom = 2 }

    public enum ExposureMode
    {
        Fixed = 0,
        Automatic = 1,
        CurveMapping = 2,
        UsePhysicalCamera = 3,
        AutomaticHistogram = 4,
    }

    public enum MeteringMode { Average = 0, Spot = 1, CenterWeighted = 2, MaskWeighted = 3, ProceduralMask = 4 }

    public enum TonemappingMode { None = 0, Neutral = 1, ACES = 2, Custom = 3, External = 4 }

    public enum FogColorMode { ConstantColor = 0, SkyColor = 1 }

    public enum SpotLightShape { Cone = 0, Pyramid = 1, Box = 2 }

    public enum AreaLightShape { Rectangle = 0, Tube = 1, Disc = 2 }

    // ---- Volume parameter wrappers ---------------------------------------
    // HDRP declares a concrete parameter class per enum rather than using
    // VolumeParameter<T> directly, so the project's `.value` assignments only
    // type-check if these exist with the right element type.

    public sealed class SkyTypeParameter : VolumeParameter<int>
    {
        public SkyTypeParameter(int value, bool overrideState = false) : base(value, overrideState) { }
    }

    public sealed class PhysicallyBasedSkyModelParameter : VolumeParameter<PhysicallyBasedSkyModel>
    {
        public PhysicallyBasedSkyModelParameter(PhysicallyBasedSkyModel value, bool overrideState = false) : base(value, overrideState) { }
    }

    public sealed class ExposureModeParameter : VolumeParameter<ExposureMode>
    {
        public ExposureModeParameter(ExposureMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public sealed class MeteringModeParameter : VolumeParameter<MeteringMode>
    {
        public MeteringModeParameter(MeteringMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        public FogColorParameter(FogColorMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    // ---- Volume components ------------------------------------------------

    public class VisualEnvironment : VolumeComponent
    {
        public SkyTypeParameter skyType = new SkyTypeParameter(0);
        public VolumeParameter<int> cloudType = new VolumeParameter<int>(0);
        public VolumeParameter<int> windOrientation = new VolumeParameter<int>(0);
        public VolumeParameter<float> windSpeed = new VolumeParameter<float>(0f);
    }

    public class PhysicallyBasedSky : VolumeComponent
    {
        public PhysicallyBasedSkyModelParameter type = new PhysicallyBasedSkyModelParameter(PhysicallyBasedSkyModel.EarthSimple);
        public ColorParameter groundTint = new ColorParameter(default(Color));
        public MinFloatParameter planetaryRadius = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter aerosolDensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ColorParameter aerosolTint = new ColorParameter(default(Color));
        public ClampedFloatParameter airMaximumAltitude = new ClampedFloatParameter(0f, 0f, 1f);
        public FloatParameter exposure = new FloatParameter(0f);
        public ClampedFloatParameter multiplier = new ClampedFloatParameter(0f, 0f, 10f);
    }

    public class Fog : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public FogColorParameter colorMode = new FogColorParameter(FogColorMode.SkyColor);
        public ColorParameter color = new ColorParameter(default(Color));
        public MinFloatParameter meanFreePath = new MinFloatParameter(0f, 0f);
        public FloatParameter baseHeight = new FloatParameter(0f);
        public FloatParameter maximumHeight = new FloatParameter(0f);
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
    }

    public class Exposure : VolumeComponent
    {
        public ExposureModeParameter mode = new ExposureModeParameter(ExposureMode.Fixed);
        public MeteringModeParameter meteringMode = new MeteringModeParameter(MeteringMode.Average);
        public FloatParameter fixedExposure = new FloatParameter(0f);
        public FloatParameter compensation = new FloatParameter(0f);
        public FloatParameter limitMin = new FloatParameter(0f);
        public FloatParameter limitMax = new FloatParameter(0f);
        public MinFloatParameter adaptationSpeedDarkToLight = new MinFloatParameter(0f, 0f);
        public MinFloatParameter adaptationSpeedLightToDark = new MinFloatParameter(0f, 0f);
        public Vector2Parameter histogramPercentages = new Vector2Parameter(default(Vector2));
    }

    public class ContactShadows : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter length = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter opacity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter distanceScaleFactor = new ClampedFloatParameter(0f, 0f, 1f);
        public MinFloatParameter maxDistance = new MinFloatParameter(0f, 0f);
        public MinFloatParameter minDistance = new MinFloatParameter(0f, 0f);
    }

    public class ScreenSpaceAmbientOcclusion : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter radius = new ClampedFloatParameter(0f, 0.25f, 5f);
        public BoolParameter rayTracing = new BoolParameter(false);
    }

    /// <summary>
    /// Reproduced exactly as HDRP declares it: renamed in 2022.2, and what is
    /// left is an empty shell that is NOT a VolumeComponent. Keeping it this
    /// shape is the point - it is what makes profile.Add&lt;AmbientOcclusion&gt;()
    /// fail here, as it would in Unity.
    /// </summary>
    [Obsolete("AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead")]
    public sealed class AmbientOcclusion
    {
    }

    public class ScreenSpaceReflection : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0f, 0f, 1f);
        public BoolParameter reflectSky = new BoolParameter(true);
        public BoolParameter rayTracing = new BoolParameter(false);
    }

    public class MicroShadowing : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter opacity = new ClampedFloatParameter(0f, 0f, 1f);
    }

    public class GlobalIllumination : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public BoolParameter rayTracing = new BoolParameter(false);
    }

    public class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);
        public ClampedFloatParameter toeStrength = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter shoulderStrength = new ClampedFloatParameter(0f, 0f, 1f);
    }

    public class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new FloatParameter(0f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);
        public ColorParameter colorFilter = new ColorParameter(default(Color));
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);
    }

    public class WhiteBalance : VolumeComponent
    {
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100f, 100f);
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100f, 100f);
    }

    public class ShadowsMidtonesHighlights : VolumeComponent
    {
        public Vector4Parameter shadows = new Vector4Parameter(default(Vector4));
        public Vector4Parameter midtones = new Vector4Parameter(default(Vector4));
        public Vector4Parameter highlights = new Vector4Parameter(default(Vector4));
    }

    public class Bloom : VolumeComponent
    {
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0f, 0f, 1f);
        public ColorParameter tint = new ColorParameter(default(Color));
        public ClampedFloatParameter threshold = new ClampedFloatParameter(0f, 0f, 10f);
    }

    public class MotionBlur : VolumeComponent
    {
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(0, 2, 64);
        public ClampedFloatParameter maximumVelocity = new ClampedFloatParameter(0f, 0f, 1500f);
    }

    public class DepthOfField : VolumeComponent
    {
        public BoolParameter focusMode = new BoolParameter(false);
        public MinFloatParameter focusDistance = new MinFloatParameter(0f, 0f);
    }

    public class IndirectLightingController : VolumeComponent
    {
        public MinFloatParameter indirectDiffuseLightingMultiplier = new MinFloatParameter(1f, 0f);
        public MinFloatParameter reflectionLightingMultiplier = new MinFloatParameter(1f, 0f);
    }

    // ---- Components -------------------------------------------------------

    public class HDAdditionalLightData : MonoBehaviour
    {
        public float intensity { get; set; }
        public float range { get; set; }
        public float angularDiameter { get; set; }
        public float shapeRadius { get; set; }
        public float shapeWidth { get; set; }
        public float shapeHeight { get; set; }
        public float innerSpotPercent { get; set; }
        public float spotIESCutoffPercent { get; set; }
        public bool affectsVolumetric { get; set; }
        public bool affectDiffuse { get; set; }
        public bool affectSpecular { get; set; }
        public bool applyRangeAttenuation { get; set; }
        public float volumetricDimmer { get; set; }
        public float fadeDistance { get; set; }
        public float shadowFadeDistance { get; set; }
        public float shadowDimmer { get; set; }
        public float volumetricShadowDimmer { get; set; }
        public ShadowUpdateMode shadowUpdateMode { get; set; }
        public SpotLightShape spotLightShape { get; set; }
        public AreaLightShape areaLightShape { get; set; }
        public bool useCustomSpotLightShadowCone { get; set; }
        public float customSpotLightShadowCone { get; set; }
        public float shadowNearPlane { get; set; }
        public LightUnit lightUnit { get; set; }
        public float lightDimmer { get; set; }

        public void SetIntensity(float intensity) { }
        public void SetIntensity(float intensity, LightUnit unit) { }
        public void SetLightUnit(LightUnit unit) { }
        public void SetRange(float range) { }
        public void SetColor(Color color) { }
        public void SetColor(Color color, float colorTemperature) { }
        public void EnableShadows(bool enabled) { }
        public void SetShadowResolution(int resolution) { }
        public void SetShadowResolutionLevel(int level) { }
        public void SetShadowResolutionOverride(bool useOverride) { }
        public void SetShadowNearPlane(float nearPlane) { }
        public void SetShadowUpdateMode(ShadowUpdateMode updateMode) { }
        public void SetAreaLightSize(Vector2 size) { }
        public void SetSpotAngle(float angle) { }
        public void SetSpotAngle(float angle, float innerSpotPercent) { }
        public void SetCookie(Texture cookie) { }
        public void RequestShadowMapRendering() { }
    }

    public class HDAdditionalCameraData : MonoBehaviour
    {
        public bool clearDepth { get; set; }
        public ClearColorMode clearColorMode { get; set; }
        public Color backgroundColorHDR { get; set; }
        public bool customRenderingSettings { get; set; }
        public bool invertFaceCulling { get; set; }
        public bool allowDynamicResolution { get; set; }
        public FlipYMode flipYMode { get; set; }
        public AntialiasingMode antialiasing { get; set; }
        public SMAAQualityLevel SMAAQuality { get; set; }
        public bool dithering { get; set; }
        public bool stopNaNs { get; set; }
        public bool taaAntiRinging { get; set; }
        public LayerMask volumeLayerMask { get; set; }
        public Transform volumeAnchorOverride { get; set; }
        public float probeLayerMask { get; set; }

        public enum ClearColorMode { Sky, Color, None }
        public enum FlipYMode { Automatic, ForceFlipY }
        public enum AntialiasingMode
        {
            None,
            FastApproximateAntialiasing,
            TemporalAntialiasing,
            SubpixelMorphologicalAntiAliasing,
        }
        public enum SMAAQualityLevel { Low, Medium, High }
    }

    public class HDProbe : MonoBehaviour
    {
        public ProbeSettings.Mode mode { get; set; }
        public float weight { get; set; }
        public float multiplier { get; set; }
    }

    public class HDAdditionalReflectionData : HDProbe { }

    public class PlanarReflectionProbe : HDProbe { }

    public static class ProbeSettings
    {
        public enum Mode { Baked, Realtime, Custom }
    }

    public class HDRenderPipelineAsset : RenderPipelineAsset
    {
        public RenderPipelineSettings currentPlatformRenderPipelineSettings { get; set; }
        public bool allowShaderVariantStripping { get; set; }
        public bool enableSRPBatcher { get; set; }
        public string diffusionProfileSettingsList { get; set; }
    }

    public struct RenderPipelineSettings
    {
        public bool supportSSR { get; set; }
        public bool supportSSAO { get; set; }
        public bool supportMotionVectors { get; set; }
        public bool supportProbeVolume { get; set; }
        public bool supportVolumetrics { get; set; }
        public bool supportRayTracing { get; set; }
        public bool supportDecals { get; set; }
        public bool supportedLitShaderMode { get; set; }
    }

    public class HDRenderPipeline : RenderPipeline
    {
        public static HDRenderPipeline currentPipeline { get { return null; } }
        public static bool isReady { get { return false; } }
    }

    public static class HDMaterial
    {
        public static void ValidateMaterial(Material material) { }
        public static void SetSurfaceType(Material material, SurfaceType surfaceType) { }
        public static void SetAlphaClipping(Material material, bool enable) { }
        public static void SetUseEmissiveIntensity(Material material, bool enable) { }
        public static void SetEmissiveIntensity(Material material, float intensity, EmissiveIntensityUnit unit) { }

        public enum SurfaceType { Opaque, Transparent }
        public enum EmissiveIntensityUnit { Nits, EV100 }
    }

    public sealed class Vector4Parameter : VolumeParameter<Vector4>
    {
        public Vector4Parameter(Vector4 value, bool overrideState = false) : base(value, overrideState) { }
    }

    public class DiffusionProfileSettings : ScriptableObject { }

    public class DecalProjector : MonoBehaviour
    {
        public Material material { get; set; }
        public Vector3 size { get; set; }
        public float fadeFactor { get; set; }
    }

    public class LocalVolumetricFog : MonoBehaviour { }
}
