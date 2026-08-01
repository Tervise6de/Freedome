using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Generates the project's PBR texture set procedurally.
    ///
    /// Every map is authored to cover exactly one square metre and to tile
    /// seamlessly, which pairs with MeshBuilder's metre-scale planar UVs: a board,
    /// a wall and a bench top all receive the same grain density with no per-object
    /// tiling values to get wrong.
    ///
    /// Each family produces three maps in HDRP's expected layout:
    ///   _Albedo  sRGB base colour
    ///   _Normal  tangent-space normal derived from the recipe's height field
    ///   _Mask    R metallic, G ambient occlusion, B detail mask, A smoothness
    ///
    /// These are generated assets, not third-party art. They are recorded in
    /// docs/ASSET_REGISTER.md as project-original procedural content.
    /// </summary>
    public static class ShedTextureGenerator
    {
        public const string OutputFolder = "Assets/Game/Art/Textures/Generated";

        /// <summary>Default map size. One metre across, so this is texels per metre.</summary>
        private const int Resolution = 1024;

        /// <summary>
        /// Size for surfaces the player gets close to. Half a millimetre per texel
        /// on the bench top and the floor is worth the generation time; the ground
        /// outside the window is not.
        /// </summary>
        private const int HeroResolution = 2048;

        /// <summary>One sample of a surface at a point, in linear terms.</summary>
        public struct SurfaceSample
        {
            public Color Albedo;
            public float Height;     // 0..1, drives the normal map
            public float Smoothness; // 0..1
            public float Occlusion;  // 0..1, 1 = fully open
            public float Metallic;   // 0..1
        }

        public delegate SurfaceSample Sampler(float u, float v);

        private sealed class Recipe
        {
            public string Name;
            public Sampler Sample;
            public float BumpStrength;
            public int Resolution = ShedTextureGenerator.Resolution;
        }

        [MenuItem("Freedome/Generate/Textures", priority = 10)]
        public static void GenerateFromMenu()
        {
            GenerateAll();
            EditorUtility.DisplayDialog("Shed textures",
                "Generated the procedural texture set into " + OutputFolder + ".", "OK");
        }

        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutputFolder);

            List<Recipe> recipes = new List<Recipe>
            {
                new Recipe { Name = "Pine",           Sample = SamplePine,          BumpStrength = 0.010f, Resolution = HeroResolution },
                new Recipe { Name = "PineFloorboard", Sample = SampleFloorboard,    BumpStrength = 0.012f, Resolution = HeroResolution },
                new Recipe { Name = "Weatherboard",   Sample = SampleWeatherboard,  BumpStrength = 0.030f },
                new Recipe { Name = "PlyBench",       Sample = SamplePlyBench,      BumpStrength = 0.008f, Resolution = HeroResolution },
                new Recipe { Name = "Galvanised",     Sample = SampleGalvanised,    BumpStrength = 0.004f },
                new Recipe { Name = "Pegboard",       Sample = SamplePegboard,      BumpStrength = 0.020f },
                new Recipe { Name = "Concrete",       Sample = SampleConcrete,      BumpStrength = 0.010f },
                new Recipe { Name = "Gravel",         Sample = SampleGravel,        BumpStrength = 0.040f },
                new Recipe { Name = "Grass",          Sample = SampleGrass,         BumpStrength = 0.030f },
                new Recipe { Name = "Tarpaulin",      Sample = SampleTarpaulin,     BumpStrength = 0.006f },
            };

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < recipes.Count; i++)
                {
                    Recipe r = recipes[i];
                    EditorUtility.DisplayProgressBar("Generating shed textures", r.Name,
                        (float)i / recipes.Count);
                    Generate(r);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            ApplyImportSettings(recipes);
            Debug.Log($"[Freedome] Generated {recipes.Count * 3} texture maps in {OutputFolder}.");
        }

        private static void Generate(Recipe recipe)
        {
            int size = recipe.Resolution;
            Color[] albedo = new Color[size * size];
            Color[] mask = new Color[size * size];
            float[] height = new float[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    SurfaceSample s = recipe.Sample(u, v);
                    int i = (y * size) + x;

                    albedo[i] = new Color(s.Albedo.r, s.Albedo.g, s.Albedo.b, 1f);
                    height[i] = s.Height;
                    mask[i] = new Color(
                        Mathf.Clamp01(s.Metallic),
                        Mathf.Clamp01(s.Occlusion),
                        0f,
                        Mathf.Clamp01(s.Smoothness));
                }
            }

            Color[] normal = HeightToNormal(height, size, recipe.BumpStrength);

            WritePng(recipe.Name + "_Albedo", albedo, size);
            WritePng(recipe.Name + "_Normal", normal, size);
            WritePng(recipe.Name + "_Mask", mask, size);
        }

        /// <summary>Sobel-style derivative of the height field, wrapping at the edges.</summary>
        private static Color[] HeightToNormal(float[] height, int size, float strength)
        {
            Color[] result = new Color[size * size];
            float texelWorldSize = 1f / size; // one metre across the map

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int xm = ((x - 1) + size) % size;
                    int xp = (x + 1) % size;
                    int ym = ((y - 1) + size) % size;
                    int yp = (y + 1) % size;

                    float hl = height[(y * size) + xm];
                    float hr = height[(y * size) + xp];
                    float hd = height[(ym * size) + x];
                    float hu = height[(yp * size) + x];

                    float dx = (hr - hl) * strength;
                    float dy = (hu - hd) * strength;

                    Vector3 n = new Vector3(-dx, -dy, 2f * texelWorldSize).normalized;
                    result[(y * size) + x] = new Color(
                        (n.x * 0.5f) + 0.5f,
                        (n.y * 0.5f) + 0.5f,
                        (n.z * 0.5f) + 0.5f,
                        1f);
                }
            }

            return result;
        }

        private static void WritePng(string name, Color[] pixels, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), png);

            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ApplyImportSettings(List<Recipe> recipes)
        {
            foreach (Recipe r in recipes)
            {
                SetImporter($"{OutputFolder}/{r.Name}_Albedo.png", TextureImporterType.Default, true, r.Resolution);
                SetImporter($"{OutputFolder}/{r.Name}_Normal.png", TextureImporterType.NormalMap, false, r.Resolution);
                SetImporter($"{OutputFolder}/{r.Name}_Mask.png", TextureImporterType.Default, false, r.Resolution);
            }
            AssetDatabase.Refresh();
        }

        private static void SetImporter(string path, TextureImporterType type, bool srgb, int maxSize = 1024)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        // =====================================================================
        // Recipes. All coordinates are 0..1 across one square metre.
        // =====================================================================

        private static Color Lerp3(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        // ---------------------------------------------------------------------
        // The wood model
        //
        // A tiling texture has to be exactly periodic, and the figure a plainsawn
        // board shows comes from growth rings concentric about a pith outside the
        // board. Concentric circles are not periodic, so they cannot be used.
        //
        // Instead a periodic stripe pattern is warped by periodic noise: the
        // stripes wander into arches and flames locally while the map still
        // repeats exactly. Both warps have to stay well under one ring of
        // displacement, or the rings stop being continuous lines and break into
        // dashes - which is what separates grain from noise.
        // ---------------------------------------------------------------------

        private const float RingsPerMetre = 60f;    // integer, so the stripes stay periodic
        private const float LatewoodFraction = 0.30f;

        private struct WoodSample
        {
            public float Ring;   // 0 in earlywood, 1 in the middle of a latewood band
            public float Knot;
            public float Fibre;
        }

        /// <param name="sharpness">
        /// 1 gives the hard latewood band of sawn softwood; near 0 gives the broad
        /// soft banding of rotary-cut veneer. Plywood needs the soft end - the hard
        /// band closes into a crazed network of loops once it is warped.
        /// </param>
        private static WoodSample WoodFigure(float u, float v, int seed, float warp = 1.35f,
                                             float ringScale = 1f, float sharpness = 1f)
        {
            float broad = TilingNoise.Fbm(u * 1.6f, v * 0.5f, 2, seed + 11, 3) - 0.5f;
            float fine = TilingNoise.Fbm(u * 5.0f, v * 1.1f, 5, seed + 29, 2) - 0.5f;

            // Ring spacing varies with how the tree grew: bands of tight rings sit
            // next to bands of wide ones. Without this the grain is a comb.
            float density = 0.72f + (0.56f * TilingNoise.Fbm(u * 1.1f, v * 0.35f, 2, seed + 71, 2));

            float s = (u * RingsPerMetre * ringScale * density)
                      + (broad * warp * 5.0f) + (fine * warp * 0.9f);

            float phase = s - Mathf.Floor(s);
            float band = Mathf.Clamp01((phase - (1f - LatewoodFraction)) / LatewoodFraction);
            band = Mathf.Pow(Mathf.Sin(band * Mathf.PI * 0.5f), 1.4f);
            float soft = 0.5f - (0.5f * Mathf.Cos(phase * Mathf.PI * 2f));

            return new WoodSample
            {
                Ring = (band * sharpness) + (soft * (1f - sharpness)),
                // Stretched about sixty to one: the aspect ratio is what makes this
                // read as fibre rather than as noise.
                Fibre = TilingNoise.Fbm(u * 96f, v * 1.6f, 96, seed + 53, 2),
                // Sparse. Two knots per square metre is generous for clean stock.
                Knot = Mathf.Pow(Mathf.Clamp01(1f - (TilingNoise.Cell(u * 1.7f, v * 1.1f, 2, seed + 101) * 7.5f)), 2.4f),
            };
        }

        private static SurfaceSample SamplePineTinted(float u, float v, int seed, Color pale, Color dark)
        {
            WoodSample w = WoodFigure(u, v, seed);

            float t = Mathf.Clamp01((w.Ring * 0.62f) + ((w.Fibre - 0.5f) * 0.13f) + 0.06f);
            Color c = Lerp3(pale, dark, t);
            c = Lerp3(c, new Color(0.180f, 0.116f, 0.068f), w.Knot * 0.88f);

            // Mill marks: the faint regular ripple a saw leaves across the grain.
            float saw = (Mathf.Sin(v * 190f * Mathf.PI * 2f) * 0.5f) + 0.5f;
            float tint = 0.972f + (0.028f * saw);
            c = new Color(c.r * tint, c.g * tint, c.b * tint);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.5f + ((w.Ring - 0.5f) * 0.30f) - (w.Knot * 0.22f)
                                       + ((saw - 0.5f) * 0.04f)),
                Smoothness = Mathf.Clamp01(0.235f - (w.Ring * 0.070f) - (w.Knot * 0.05f)),
                Occlusion = Mathf.Clamp01(1f - (w.Ring * 0.11f) - (w.Knot * 0.30f)),
                Metallic = 0f,
            };
        }

        /// <summary>Sawn structural pine.</summary>
        private static SurfaceSample SamplePine(float u, float v)
        {
            return SamplePineTinted(u, v, 11,
                new Color(0.512f, 0.408f, 0.278f), new Color(0.335f, 0.243f, 0.150f));
        }

        /// <summary>Floorboards: pine that has been walked on.</summary>
        private static SurfaceSample SampleFloorboard(float u, float v)
        {
            SurfaceSample s = SamplePineTinted(u, v, 131,
                new Color(0.470f, 0.372f, 0.256f), new Color(0.300f, 0.216f, 0.134f));

            // Traffic polish is broad and shows only in the smoothness.
            float polish = TilingNoise.Fbm(u * 2.2f, v * 2.2f, 3, 211, 3);
            s.Smoothness = Mathf.Clamp01(s.Smoothness + (polish * 0.20f));

            // A light, even settling of dust. A used shed, not a neglected one.
            float dust = TilingNoise.Fbm(u * 5f, v * 5f, 5, 223, 3);
            float amount = Mathf.Clamp01((dust - 0.46f) * 1.15f) * 0.17f;
            s.Albedo = Lerp3(s.Albedo, new Color(0.472f, 0.440f, 0.396f), amount);
            s.Smoothness = Mathf.Clamp01(s.Smoothness - (amount * 0.55f));

            // Drag scuffs, stretched along the direction things get pulled.
            float scuff = TilingNoise.Ridge(u * 16f, v * 3f, 16, 307, 2);
            float mask = Mathf.Clamp01((scuff - 0.87f) * 9f);
            s.Albedo = Lerp3(s.Albedo, new Color(0.300f, 0.235f, 0.160f), mask * 0.30f);

            return s;
        }

        /// <summary>
        /// Painted exterior weatherboard. Seven 143 mm boards per metre, so the map
        /// tiles exactly. The grain reads through the paint as height far more than
        /// as colour, which is what separates painted timber from bare timber.
        /// </summary>
        private static SurfaceSample SampleWeatherboard(float u, float v)
        {
            const int BoardsPerMetre = 7;
            float boardV = v * BoardsPerMetre;
            float within = boardV - Mathf.Floor(boardV);

            float lap = Mathf.Clamp01(1f - (within / 0.085f));
            float taper = 0.35f + (0.65f * within);

            WoodSample w = WoodFigure(u, v, 401, 0.55f);
            Color paint = new Color(0.318f, 0.340f, 0.300f);
            float grainTint = 0.975f + (0.05f * w.Fibre);
            Color c = new Color(paint.r * grainTint, paint.g * grainTint, paint.b * grainTint);

            float wear = TilingNoise.Fbm(u * 4f, v * 4f, 4, 433, 3);
            float worn = Mathf.Clamp01((wear - 0.60f) * 3.2f);
            c = Lerp3(c, new Color(0.430f, 0.368f, 0.276f), worn * 0.42f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01((taper * 0.72f) + (w.Ring * 0.10f) - (lap * 0.58f)),
                Smoothness = Mathf.Clamp01(0.42f - (worn * 0.20f) - (lap * 0.10f)),
                Occlusion = Mathf.Clamp01(1f - (lap * 0.55f)),
                Metallic = 0f,
            };
        }

        /// <summary>Worn plywood bench top: broad rotary-cut veneer figure, honest use.</summary>
        private static SurfaceSample SamplePlyBench(float u, float v)
        {
            WoodSample w = WoodFigure(u, v, 601, 0.85f, 0.30f, 0.15f);

            float t = Mathf.Clamp01((w.Ring * 0.42f) + ((w.Fibre - 0.5f) * 0.16f) + 0.10f);
            Color c = Lerp3(new Color(0.500f, 0.392f, 0.252f), new Color(0.352f, 0.262f, 0.162f), t);

            // Working surfaces darken where things are repeatedly set down.
            float use = TilingNoise.Fbm(u * 3f, v * 3f, 3, 617, 3);
            c = Lerp3(c, new Color(0.268f, 0.205f, 0.140f), Mathf.Clamp01((use - 0.5f) * 1.4f) * 0.42f);

            // Scratches are made by something dragged across, so they are long and
            // directional. Isotropic noise gives a crazed network instead.
            float scratch = TilingNoise.Ridge(u * 44f, v * 3.2f, 44, 631, 2);
            float scratchMask = Mathf.Clamp01((scratch - 0.93f) * 13f);
            c = Lerp3(c, new Color(0.560f, 0.452f, 0.310f), scratchMask * 0.34f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.5f + ((w.Ring - 0.5f) * 0.18f) - (scratchMask * 0.10f)),
                Smoothness = Mathf.Clamp01(0.20f + (use * 0.26f) - (scratchMask * 0.10f)),
                Occlusion = Mathf.Clamp01(1f - (scratchMask * 0.20f)),
                Metallic = 0f,
            };
        }

        /// <summary>
        /// Hot-dip galvanised steel. The spangle is drawn from cell boundaries, not
        /// cell centres: zinc crystallises into flat angular facets, and the
        /// nearest-point distance only ever gives round blobs that read as dents.
        /// </summary>
        private static SurfaceSample SampleGalvanised(float u, float v)
        {
            float edge = TilingNoise.CellEdge(u * 22f, v * 22f, 22, 701);
            float crystal = Mathf.Clamp01(edge * 2.0f);

            // Each facet gets its own tone, the way a real spangle catches light.
            float facet = TilingNoise.Hash01(Mathf.FloorToInt(u * 22f), Mathf.FloorToInt(v * 22f), 22, 705);
            float spangle = Mathf.Clamp01(crystal * (0.55f + (0.75f * facet)));

            Color baseGrey = new Color(0.470f, 0.486f, 0.500f);
            float lift = 0.92f + (0.15f * spangle);
            Color c = new Color(baseGrey.r * lift, baseGrey.g * lift, baseGrey.b * lift);

            float dirt = TilingNoise.Fbm(u * 5f, v * 5f, 5, 733, 3);
            c = Lerp3(c, new Color(0.430f, 0.430f, 0.420f), Mathf.Clamp01((dirt - 0.6f) * 1.5f) * 0.30f);

            float scratch = TilingNoise.Ridge(u * 44f, v * 7f, 44, 757, 2);
            float scratchMask = Mathf.Clamp01((scratch - 0.92f) * 11f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.5f + ((spangle - 0.5f) * 0.10f)),
                Smoothness = Mathf.Clamp01(0.44f + (spangle * 0.18f) + (scratchMask * 0.12f) - (dirt * 0.08f)),
                Occlusion = 1f,
                Metallic = 1f,
            };
        }

        /// <summary>
        /// Perforated hardboard. Holes sit on a 25 mm grid, which is the real pitch
        /// and gives exactly 40 holes per metre - so it tiles without a seam.
        /// </summary>
        private static SurfaceSample SamplePegboard(float u, float v)
        {
            const int HolesPerMetre = 40;
            float gx = (u * HolesPerMetre) - Mathf.Floor(u * HolesPerMetre) - 0.5f;
            float gy = (v * HolesPerMetre) - Mathf.Floor(v * HolesPerMetre) - 0.5f;
            float d = Mathf.Sqrt((gx * gx) + (gy * gy));

            // 5 mm hole on a 25 mm pitch => radius 0.1 of a cell.
            float hole = Mathf.Clamp01(1f - Mathf.SmoothStep(0.10f, 0.14f, d));

            float fibre = TilingNoise.Fbm(u * 30f, v * 30f, 30, 811, 3);
            Color board = Lerp3(new Color(0.352f, 0.252f, 0.170f), new Color(0.278f, 0.196f, 0.128f), fibre);
            Color c = Lerp3(board, new Color(0.045f, 0.038f, 0.032f), hole);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.85f - (hole * 0.85f) + (fibre * 0.08f)),
                Smoothness = Mathf.Clamp01(0.30f - (hole * 0.25f) - (fibre * 0.06f)),
                Occlusion = Mathf.Clamp01(1f - (hole * 0.75f)),
                Metallic = 0f,
            };
        }

        private static SurfaceSample SampleConcrete(float u, float v)
        {
            float grit = TilingNoise.Fbm(u * 40f, v * 40f, 40, 907, 4);
            float blotch = TilingNoise.Fbm(u * 4f, v * 4f, 4, 911, 3);

            Color c = Lerp3(new Color(0.402f, 0.398f, 0.382f), new Color(0.510f, 0.505f, 0.492f),
                (grit * 0.5f) + (blotch * 0.5f));

            float pit = Mathf.Clamp01((TilingNoise.Cell(u * 50f, v * 50f, 50, 919) - 0.05f) * -6f + 1f);

            return new SurfaceSample
            {
                Albedo = Lerp3(c, c * 0.75f, pit * 0.5f),
                Height = Mathf.Clamp01(0.6f + (grit * 0.3f) - (pit * 0.4f)),
                Smoothness = Mathf.Clamp01(0.20f - (grit * 0.08f)),
                Occlusion = Mathf.Clamp01(1f - (pit * 0.35f)),
                Metallic = 0f,
            };
        }

        private static SurfaceSample SampleGravel(float u, float v)
        {
            float stone = TilingNoise.Cell(u * 26f, v * 26f, 26, 1009);
            float tone = TilingNoise.Fbm(u * 26f, v * 26f, 26, 1013, 2);

            Color c = Lerp3(new Color(0.318f, 0.305f, 0.288f), new Color(0.482f, 0.462f, 0.432f), tone);
            c = Lerp3(c * 0.65f, c, Mathf.Clamp01(stone * 2.5f));

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(stone * 1.8f),
                Smoothness = Mathf.Clamp01(0.24f - (tone * 0.10f)),
                Occlusion = Mathf.Clamp01(0.55f + (stone * 1.2f)),
                Metallic = 0f,
            };
        }

        private static SurfaceSample SampleGrass(float u, float v)
        {
            float blade = TilingNoise.Fbm(u * 60f, v * 60f, 60, 1103, 3);
            float patch = TilingNoise.Fbm(u * 5f, v * 5f, 5, 1109, 3);

            Color dry = new Color(0.318f, 0.312f, 0.178f);
            Color green = new Color(0.192f, 0.278f, 0.128f);
            Color c = Lerp3(green, dry, (patch * 0.65f) + (blade * 0.2f));

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(blade),
                Smoothness = Mathf.Clamp01(0.16f + (blade * 0.08f)),
                Occlusion = Mathf.Clamp01(0.7f + (blade * 0.3f)),
                Metallic = 0f,
            };
        }

        private static SurfaceSample SampleTarpaulin(float u, float v)
        {
            const int ThreadsPerMetre = 120;
            float wu = Mathf.Sin(u * ThreadsPerMetre * Mathf.PI * 2f);
            float wv = Mathf.Sin(v * ThreadsPerMetre * Mathf.PI * 2f);
            float weave = ((wu * wv) * 0.5f) + 0.5f;

            float fade = TilingNoise.Fbm(u * 6f, v * 6f, 6, 1201, 3);
            Color c = Lerp3(new Color(0.115f, 0.212f, 0.318f), new Color(0.190f, 0.290f, 0.382f), fade);
            c = Lerp3(c * 0.88f, c * 1.06f, weave);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(weave),
                Smoothness = Mathf.Clamp01(0.30f + (weave * 0.10f) - (fade * 0.08f)),
                Occlusion = Mathf.Clamp01(0.85f + (weave * 0.15f)),
                Metallic = 0f,
            };
        }
    }
}
