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
        private const int Resolution = 1024;

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
                new Recipe { Name = "Pine",           Sample = SamplePine,          BumpStrength = 0.010f },
                new Recipe { Name = "PineFloorboard", Sample = SampleFloorboard,    BumpStrength = 0.012f },
                new Recipe { Name = "Weatherboard",   Sample = SampleWeatherboard,  BumpStrength = 0.030f },
                new Recipe { Name = "PlyBench",       Sample = SamplePlyBench,      BumpStrength = 0.008f },
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
            int size = Resolution;
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
                SetImporter($"{OutputFolder}/{r.Name}_Albedo.png", TextureImporterType.Default, true);
                SetImporter($"{OutputFolder}/{r.Name}_Normal.png", TextureImporterType.NormalMap, false);
                SetImporter($"{OutputFolder}/{r.Name}_Mask.png", TextureImporterType.Default, false);
            }
            AssetDatabase.Refresh();
        }

        private static void SetImporter(string path, TextureImporterType type, bool srgb)
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
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        // =====================================================================
        // Recipes. All coordinates are 0..1 across one square metre.
        // =====================================================================

        private static Color Lerp3(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        /// <summary>Sawn structural pine: long fibre grain, occasional knot, mill marks.</summary>
        private static SurfaceSample SamplePine(float u, float v)
        {
            // Grain runs along V. Stretching the noise 20:1 gives fibre, not blobs.
            float fibre = TilingNoise.Fbm(u * 24f, v * 2f, 24, 11, 4);
            float band = TilingNoise.Fbm(u * 7f, v * 1f, 7, 23, 3);
            float grain = Mathf.Pow(Mathf.Abs(Mathf.Sin((band * 9f) + (fibre * 2.2f))), 1.6f);

            // Sparse knots.
            float knotDist = TilingNoise.Cell(u * 3f, v * 2f, 3, 57);
            float knot = Mathf.Clamp01(1f - (knotDist * 7f));
            knot *= knot;

            Color pale = new Color(0.512f, 0.408f, 0.278f);
            Color dark = new Color(0.352f, 0.258f, 0.160f);
            Color c = Lerp3(pale, dark, (grain * 0.55f) + (band * 0.20f));
            c = Lerp3(c, new Color(0.212f, 0.145f, 0.086f), knot * 0.85f);

            // Fine saw marks across the grain keep large boards from looking flat.
            float saw = TilingNoise.Value(u * 3f, v * 160f, 3, 91) * 0.06f;
            c.r += saw - 0.03f;
            c.g += saw - 0.03f;
            c.b += saw - 0.03f;

            float h = 0.5f + ((grain - 0.5f) * 0.6f) - (knot * 0.25f) + (saw * 0.4f);
            float smooth = Mathf.Lerp(0.24f, 0.12f, grain) - (knot * 0.05f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(h),
                Smoothness = Mathf.Clamp01(smooth),
                Occlusion = Mathf.Clamp01(1f - (grain * 0.14f) - (knot * 0.30f)),
                Metallic = 0f,
            };
        }

        /// <summary>Floorboards: pine that has been walked on, so slightly polished and dustier.</summary>
        private static SurfaceSample SampleFloorboard(float u, float v)
        {
            SurfaceSample s = SamplePine(u, v);

            // Traffic polish: broad low-frequency variation in smoothness only.
            float polish = TilingNoise.Fbm(u * 2.5f, v * 2.5f, 3, 131, 3);
            s.Smoothness = Mathf.Clamp01(s.Smoothness + (polish * 0.16f));

            // A light, even settling of dust. Kept subtle - this is a used shed,
            // not a neglected one.
            float dust = TilingNoise.Fbm(u * 5f, v * 5f, 5, 211, 3);
            float dustAmount = Mathf.Clamp01((dust - 0.45f) * 1.1f) * 0.16f;
            s.Albedo = Lerp3(s.Albedo, new Color(0.470f, 0.435f, 0.390f), dustAmount);
            s.Smoothness = Mathf.Clamp01(s.Smoothness - (dustAmount * 0.5f));

            // Scattered scuffs from dragged objects.
            float scuff = TilingNoise.Ridge(u * 14f, v * 3f, 14, 307, 2);
            if (scuff > 0.86f)
            {
                s.Albedo = Lerp3(s.Albedo, new Color(0.300f, 0.235f, 0.160f), 0.35f);
                s.Smoothness = Mathf.Clamp01(s.Smoothness - 0.06f);
            }

            return s;
        }

        /// <summary>
        /// Painted exterior weatherboard. Seven 143 mm boards per metre, so the map
        /// tiles exactly. Paint is a muted sage that reads as ordinary shed paint.
        /// </summary>
        private static SurfaceSample SampleWeatherboard(float u, float v)
        {
            const int BoardsPerMetre = 7;
            float boardV = v * BoardsPerMetre;
            float withinBoard = boardV - Mathf.Floor(boardV);

            // Lap shadow along the bottom edge of each board.
            float lap = Mathf.Clamp01(1f - (withinBoard / 0.09f));
            float taper = Mathf.Lerp(0.35f, 1f, withinBoard); // boards are wedge shaped

            Color paint = new Color(0.318f, 0.340f, 0.300f);
            float grain = TilingNoise.Fbm(u * 20f, v * 3f, 20, 401, 3);
            Color c = Lerp3(paint, paint * 1.18f, grain * 0.5f);

            // Mild chalking and a few worn spots where the paint has thinned.
            float wear = TilingNoise.Fbm(u * 4f, v * 4f, 4, 433, 3);
            float worn = Mathf.Clamp01((wear - 0.62f) * 3f);
            c = Lerp3(c, new Color(0.420f, 0.360f, 0.270f), worn * 0.40f);

            float h = (taper * 0.7f) + (grain * 0.10f) - (lap * 0.55f);
            float smooth = Mathf.Lerp(0.42f, 0.22f, worn) - (lap * 0.10f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(h),
                Smoothness = Mathf.Clamp01(smooth),
                Occlusion = Mathf.Clamp01(1f - (lap * 0.55f)),
                Metallic = 0f,
            };
        }

        /// <summary>Worn plywood bench top: veneer figure, edge wear, no gore or grime.</summary>
        private static SurfaceSample SamplePlyBench(float u, float v)
        {
            float figure = TilingNoise.Fbm(u * 10f, v * 3f, 10, 601, 4);
            float streak = Mathf.Pow(Mathf.Abs(Mathf.Sin((figure * 7f) + (u * 2f))), 1.3f);

            Color light = new Color(0.482f, 0.372f, 0.238f);
            Color mid = new Color(0.352f, 0.262f, 0.162f);
            Color c = Lerp3(light, mid, streak * 0.6f);

            // Working surfaces pick up ring marks and a general darkening where tools
            // are set down. Kept to a plausible amount of honest use.
            float use = TilingNoise.Fbm(u * 3f, v * 3f, 3, 617, 3);
            c = Lerp3(c, new Color(0.268f, 0.205f, 0.140f), Mathf.Clamp01((use - 0.5f) * 1.4f) * 0.45f);

            float scratch = TilingNoise.Ridge(u * 30f, v * 30f, 30, 631, 2);
            float scratchMask = Mathf.Clamp01((scratch - 0.90f) * 8f);
            c = Lerp3(c, new Color(0.545f, 0.440f, 0.300f), scratchMask * 0.5f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.5f + ((streak - 0.5f) * 0.3f) - (scratchMask * 0.4f)),
                Smoothness = Mathf.Clamp01(Mathf.Lerp(0.20f, 0.44f, use) - (scratchMask * 0.15f)),
                Occlusion = Mathf.Clamp01(1f - (scratchMask * 0.2f)),
                Metallic = 0f,
            };
        }

        /// <summary>Hot-dip galvanised steel: spangle crystals, faint scratches.</summary>
        private static SurfaceSample SampleGalvanised(float u, float v)
        {
            float cell = TilingNoise.Cell(u * 16f, v * 16f, 16, 701);
            float spangle = Mathf.Clamp01(cell * 2.2f);

            Color baseGrey = new Color(0.560f, 0.575f, 0.585f);
            Color c = Lerp3(baseGrey * 0.86f, baseGrey * 1.10f, spangle);

            float dirt = TilingNoise.Fbm(u * 5f, v * 5f, 5, 733, 3);
            c = Lerp3(c, new Color(0.430f, 0.430f, 0.420f), Mathf.Clamp01((dirt - 0.6f) * 1.5f) * 0.30f);

            float scratch = TilingNoise.Ridge(u * 40f, v * 6f, 40, 757, 2);
            float scratchMask = Mathf.Clamp01((scratch - 0.92f) * 10f);

            return new SurfaceSample
            {
                Albedo = c,
                Height = Mathf.Clamp01(0.5f + ((spangle - 0.5f) * 0.25f)),
                Smoothness = Mathf.Clamp01(Mathf.Lerp(0.44f, 0.62f, spangle) + (scratchMask * 0.12f) - (dirt * 0.08f)),
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
