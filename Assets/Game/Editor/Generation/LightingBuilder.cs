using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Daylight, two practical fittings, the volume stack and the probes.
    ///
    /// The brief for this room is a quiet weekday morning, not a mood piece. The sun
    /// is set mid-morning and slightly off the window wall so light rakes across the
    /// bench rather than blasting straight through it; the sky is HDRP's physically
    /// based model so no external HDRI is needed; exposure is automatic but clamped
    /// to a narrow range so walking from the dim corner to the window does not pump.
    ///
    /// Deliberately absent: fog, vignette, film grain, coloured gels and any
    /// flickering. Nothing here is trying to make the shed feel threatening.
    /// </summary>
    public static class LightingBuilder
    {
        public const string SettingsFolder = "Assets/Game/Settings";
        public const string VolumeProfilePath = SettingsFolder + "/ShedRoom_VolumeProfile.asset";

        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildSun(ctx, parent);
            BuildVolume(ctx, parent);
            BuildCeilingFixture(ctx, parent);
            BuildTaskLight(ctx, parent);
            BuildProbes(ctx, parent);
        }

        // =====================================================================
        // Daylight
        // =====================================================================

        private static void BuildSun(BuildContext ctx, Transform parent)
        {
            GameObject go = new GameObject("Sun_Directional");
            go.transform.SetParent(parent, false);

            // Mid-morning, coming across the window wall at an angle so the light
            // travels into the room rather than stopping at the sill.
            go.transform.rotation = Quaternion.Euler(46f, -122f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.useColorTemperature = true;
            light.colorTemperature = 5600f;
            light.color = Color.white;

            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null)
            {
                hd = go.AddComponent<HDAdditionalLightData>();
            }

            if (hd != null)
            {
                // 38 klux is a bright but not blown-out morning. Full noon sun is
                // around 100 klux and would flatten everything through the window.
                hd.SetIntensity(38000f, LightUnit.Lux);
                hd.angularDiameter = 1.6f;   // slightly soft-edged shadows
                hd.EnableShadows(true);
                hd.shadowUpdateMode = ShadowUpdateMode.OnEnable;
                hd.affectsVolumetric = false;
                hd.SetShadowResolution(2048);
            }

            // The sun is the scene's sky-lighting source for baked GI.
            RenderSettings.sun = light;
        }

        // =====================================================================
        // Volume stack
        // =====================================================================

        private static void BuildVolume(BuildContext ctx, Transform parent)
        {
            Directory.CreateDirectory(SettingsFolder);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            else
            {
                // Rebuild from scratch so re-running the generator is deterministic.
                foreach (VolumeComponent component in profile.components.ToArray())
                {
                    Object.DestroyImmediate(component, true);
                }
                profile.components.Clear();
            }

            ConfigureSky(profile);
            ConfigureExposure(profile);
            ConfigureShadowsAndOcclusion(profile);
            ConfigureGrade(profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject go = new GameObject("Global_Volume");
            go.transform.SetParent(parent, false);

            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;
        }

        private static void ConfigureSky(VolumeProfile profile)
        {
            VisualEnvironment env = profile.Add<VisualEnvironment>(true);
            env.skyType.overrideState = true;
            env.skyType.value = (int)SkyType.PhysicallyBased;
            env.cloudType.overrideState = true;
            env.cloudType.value = 0;

            PhysicallyBasedSky sky = profile.Add<PhysicallyBasedSky>(true);
            sky.type.overrideState = true;
            sky.type.value = PhysicallyBasedSkyModel.EarthSimple;
            sky.groundTint.overrideState = true;
            // A muted green-grey ground so bounce light off the grass is not lurid.
            sky.groundTint.value = new Color(0.20f, 0.21f, 0.17f);

            // No fog. An interior daytime shed has none, and adding it would push the
            // room straight into the atmosphere this milestone is asked to avoid.
            Fog fog = profile.Add<Fog>(true);
            fog.enabled.overrideState = true;
            fog.enabled.value = false;
        }

        private static void ConfigureExposure(VolumeProfile profile)
        {
            Exposure exposure = profile.Add<Exposure>(true);
            exposure.mode.overrideState = true;
            exposure.mode.value = ExposureMode.AutomaticHistogram;
            exposure.meteringMode.overrideState = true;
            exposure.meteringMode.value = MeteringMode.CenterWeighted;

            // Clamping the range is what stops the window blowing out when the player
            // looks at the dark corner, and stops the corner going black when they
            // look at the window.
            exposure.limitMin.overrideState = true;
            exposure.limitMin.value = 6.0f;
            exposure.limitMax.overrideState = true;
            exposure.limitMax.value = 13.5f;

            exposure.adaptationSpeedDarkToLight.overrideState = true;
            exposure.adaptationSpeedDarkToLight.value = 3.0f;
            exposure.adaptationSpeedLightToDark.overrideState = true;
            exposure.adaptationSpeedLightToDark.value = 1.2f;

            exposure.histogramPercentages.overrideState = true;
            exposure.histogramPercentages.value = new Vector2(45f, 92f);
        }

        private static void ConfigureShadowsAndOcclusion(VolumeProfile profile)
        {
            ContactShadows contact = profile.Add<ContactShadows>(true);
            contact.enable.overrideState = true;
            contact.enable.value = true;
            contact.length.overrideState = true;
            contact.length.value = 0.15f;
            contact.opacity.overrideState = true;
            contact.opacity.value = 0.85f;

            // ScreenSpaceAmbientOcclusion, not AmbientOcclusion: the latter was
            // renamed in 2022.2 and what is left under the old name is an empty
            // [Obsolete] shell that does not derive from VolumeComponent, so
            // profile.Add<AmbientOcclusion>() does not even satisfy the generic
            // constraint. Verified against HDRP's published source.
            ScreenSpaceAmbientOcclusion ao = profile.Add<ScreenSpaceAmbientOcclusion>(true);
            ao.intensity.overrideState = true;
            ao.intensity.value = 0.65f;
            ao.radius.overrideState = true;
            ao.radius.value = 0.35f;
            ao.directLightingStrength.overrideState = true;
            ao.directLightingStrength.value = 0.25f;

            ScreenSpaceReflection ssr = profile.Add<ScreenSpaceReflection>(true);
            ssr.enabled.overrideState = true;
            ssr.enabled.value = true;

            MicroShadowing micro = profile.Add<MicroShadowing>(true);
            micro.enable.overrideState = true;
            micro.enable.value = true;
            micro.opacity.overrideState = true;
            micro.opacity.value = 0.45f;
        }

        private static void ConfigureGrade(VolumeProfile profile)
        {
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;
            // Neutral rather than ACES: it keeps midtone material detail readable,
            // which is exactly what the review pass needs to judge.
            tonemapping.mode.value = TonemappingMode.Neutral;

            ColorAdjustments grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.overrideState = true;
            grade.postExposure.value = 0.15f;
            grade.contrast.overrideState = true;
            grade.contrast.value = 4f;
            grade.saturation.overrideState = true;
            grade.saturation.value = -2f;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.06f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.6f;

            // Present but off by default so the settings menu has something to
            // toggle. No vignette and no film grain are added at all.
            MotionBlur motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.overrideState = true;
            motionBlur.intensity.value = 0.35f;
            motionBlur.active = false;
        }

        // =====================================================================
        // Practical fittings
        // =====================================================================

        /// <summary>
        /// A batten holder screwed to a board under the collar tie, with a plain
        /// enamel shade. This is the light you would actually find in a shed.
        /// </summary>
        private static void BuildCeilingFixture(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("CeilingLight_Fixture", 3);

            float z = Dim.CeilingLightZ;
            float roseY = Dim.CollarTieHeight;

            // Mounting board and ceiling rose.
            mb.AddBox(new Vector3(0f, roseY - 0.010f, 0f), new Vector3(0.140f, 0.020f, 0.140f), 0, 0.004f);
            mb.AddCylinder(new Vector3(0f, roseY - 0.035f, 0f), 0.048f, 0.048f, 0.032f, 14, 1,
                           Quaternion.identity);

            // Flex down to the lampholder.
            float drop = roseY - 0.052f - (Dim.CeilingLightHeight + 0.055f);
            mb.AddCylinder(new Vector3(0f, Dim.CeilingLightHeight + 0.055f + (drop * 0.5f), 0f),
                           0.0045f, 0.0045f, drop, 6, 2, Quaternion.identity);

            // Lampholder and shade.
            mb.AddCylinder(new Vector3(0f, Dim.CeilingLightHeight + 0.058f, 0f), 0.024f, 0.026f, 0.060f, 14,
                           1, Quaternion.identity);
            mb.AddCylinder(new Vector3(0f, Dim.CeilingLightHeight + 0.030f, 0f), 0.030f, 0.130f, 0.105f, 20,
                           1, Quaternion.identity, false, false);

            // Bulb.
            mb.AddCylinder(new Vector3(0f, Dim.CeilingLightHeight - 0.008f, 0f), 0.030f, 0.022f, 0.075f, 14,
                           2, Quaternion.identity);

            ctx.CreateObject("CeilingLight_Fixture", mb,
                new[] { Keys.StructuralPine, Keys.Galvanised, Keys.Bulb },
                parent, new Vector3(0f, 0f, z), Quaternion.identity);

            // The light itself.
            GameObject go = new GameObject("CeilingLight");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, Dim.CeilingLightHeight - 0.01f, z);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.useColorTemperature = true;
            light.colorTemperature = 2900f;
            light.range = 7f;

            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null)
            {
                hd = go.AddComponent<HDAdditionalLightData>();
            }
            if (hd != null)
            {
                // A 9 W LED lamp is about 800 lm. The shade throws most of it down.
                hd.SetIntensity(820f, LightUnit.Lumen);
                hd.shapeRadius = 0.03f;
                hd.EnableShadows(true);
                hd.SetShadowResolution(512);
                hd.affectsVolumetric = false;
            }
        }

        /// <summary>A clamp-on reflector lamp on the pegboard, aimed at the bench.</summary>
        private static void BuildTaskLight(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("TaskLight_Fixture", 3);

            // Clamp on the pegboard batten.
            mb.AddBox(new Vector3(0.020f, 0f, 0f), new Vector3(0.055f, 0.070f, 0.045f), 0, 0.004f);
            mb.AddBox(new Vector3(-0.020f, -0.030f, 0f), new Vector3(0.045f, 0.014f, 0.040f), 0, 0.003f);

            // Stem and swivel.
            mb.AddCylinder(new Vector3(-0.075f, -0.030f, 0f), 0.008f, 0.008f, 0.130f, 8, 0,
                           Quaternion.Euler(0f, 0f, 72f));
            mb.AddCylinder(new Vector3(-0.140f, -0.055f, 0f), 0.016f, 0.016f, 0.024f, 10, 0,
                           Quaternion.Euler(90f, 0f, 0f));

            // Reflector shade, tipped down toward the bench.
            Quaternion shadeRot = Quaternion.Euler(0f, 0f, 128f);
            mb.AddCylinder(new Vector3(-0.185f, -0.095f, 0f), 0.035f, 0.115f, 0.115f, 18, 1, shadeRot,
                           false, false);
            mb.AddCylinder(new Vector3(-0.170f, -0.075f, 0f), 0.026f, 0.022f, 0.060f, 12, 2, shadeRot);

            // Flex heading back to the wall.
            mb.AddCylinder(new Vector3(0.045f, -0.060f, 0.020f), 0.004f, 0.004f, 0.180f, 6, 0,
                           Quaternion.Euler(20f, 0f, 30f));

            Vector3 mount = new Vector3(Dim.HalfWidth - 0.035f, Dim.TaskLightHeight, Dim.TaskLightZ);
            ctx.CreateObject("TaskLight_Fixture", mb,
                new[] { Keys.ElectricalPlastic, Keys.Galvanised, Keys.Bulb },
                parent, mount, Quaternion.Euler(0f, 90f, 0f));

            GameObject go = new GameObject("TaskLight");
            go.transform.SetParent(parent, false);
            go.transform.position = mount + new Vector3(-0.16f, -0.085f, 0.02f);
            // Aimed down and out across the bench top.
            go.transform.rotation = Quaternion.Euler(52f, -118f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.useColorTemperature = true;
            light.colorTemperature = 3100f;
            light.range = 4f;
            light.spotAngle = 78f;

            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null)
            {
                hd = go.AddComponent<HDAdditionalLightData>();
            }
            if (hd != null)
            {
                hd.SetIntensity(430f, LightUnit.Lumen);
                hd.innerSpotPercent = 55f;
                hd.shapeRadius = 0.02f;
                hd.EnableShadows(true);
                hd.SetShadowResolution(512);
                hd.affectsVolumetric = false;
            }
        }

        // =====================================================================
        // Probes
        // =====================================================================

        private static void BuildProbes(BuildContext ctx, Transform parent)
        {
            // --- reflection probe covering the interior ------------------------
            GameObject probeGo = new GameObject("ReflectionProbe_Interior");
            probeGo.transform.SetParent(parent, false);
            probeGo.transform.position = new Vector3(0f, 1.35f, 0f);

            ReflectionProbe probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(Dim.InteriorWidth + 0.4f, 3.2f, Dim.InteriorLength + 0.4f);
            probe.center = Vector3.zero;
            probe.resolution = 256;
            probe.shadowDistance = 12f;
            probe.nearClipPlane = 0.05f;
            probe.farClipPlane = 20f;

            HDAdditionalReflectionData hdProbe = probeGo.GetComponent<HDAdditionalReflectionData>();
            if (hdProbe == null)
            {
                probeGo.AddComponent<HDAdditionalReflectionData>();
            }

            BuildContext.MarkStatic(probeGo);

            // --- a second, tighter probe over the bench -------------------------
            GameObject benchProbeGo = new GameObject("ReflectionProbe_Workbench");
            benchProbeGo.transform.SetParent(parent, false);
            benchProbeGo.transform.position = new Vector3(1.55f, 1.20f, 0.6f);

            ReflectionProbe benchProbe = benchProbeGo.AddComponent<ReflectionProbe>();
            benchProbe.mode = ReflectionProbeMode.Baked;
            benchProbe.size = new Vector3(1.6f, 1.8f, 3.2f);
            benchProbe.resolution = 128;
            benchProbe.importance = 2;

            if (benchProbeGo.GetComponent<HDAdditionalReflectionData>() == null)
            {
                benchProbeGo.AddComponent<HDAdditionalReflectionData>();
            }

            BuildContext.MarkStatic(benchProbeGo);

            // --- light probes ---------------------------------------------------
            BuildLightProbes(parent);
        }

        /// <summary>
        /// A probe lattice through the interior plus a ring just outside the door and
        /// window, so props pick up the change in indirect light as the player moves
        /// from the bright end of the shed to the dim end.
        /// </summary>
        private static void BuildLightProbes(Transform parent)
        {
            GameObject go = new GameObject("LightProbeGroup");
            go.transform.SetParent(parent, false);

            LightProbeGroup group = go.AddComponent<LightProbeGroup>();

            var positions = new System.Collections.Generic.List<Vector3>();

            float[] heights = { 0.25f, 1.10f, 2.00f, 2.70f };
            for (float x = -1.6f; x <= 1.61f; x += 1.6f)
            {
                for (float z = -2.6f; z <= 2.61f; z += 1.3f)
                {
                    foreach (float y in heights)
                    {
                        // Skip probes that would sit inside the roof slope.
                        if (y > Dim.RoofUndersideAt(x) - 0.1f)
                        {
                            continue;
                        }
                        positions.Add(new Vector3(x, y, z));
                    }
                }
            }

            // Denser sampling right at the window and the door, where the gradient is.
            for (float y = 0.6f; y <= 2.1f; y += 0.5f)
            {
                positions.Add(new Vector3(Dim.HalfWidth - 0.35f, y, Dim.WindowCentreZ));
                positions.Add(new Vector3(Dim.HalfWidth - 0.9f, y, Dim.WindowCentreZ));
                positions.Add(new Vector3(Dim.DoorCentreX, y, -Dim.HalfLength + 0.4f));
            }

            group.probePositions = positions.ToArray();
            BuildContext.MarkStatic(go);
        }
    }
}
