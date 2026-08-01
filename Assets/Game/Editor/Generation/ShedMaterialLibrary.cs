using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Creates and caches the HDRP/Lit materials the shed uses.
    ///
    /// The palette is deliberately small. Every extra material is another draw call
    /// per renderer that uses it, and a shed made of pine, galvanised steel, painted
    /// metal and a few plastics genuinely does not need more than this.
    ///
    /// Roughness values are chosen from what the real surface would do rather than
    /// picked for looks: sawn framing timber is very rough (smoothness ~0.15),
    /// a bench top polished by years of use is noticeably smoother in patches,
    /// galvanised steel sits in the middle, and glass is near mirror.
    /// </summary>
    public static class ShedMaterialLibrary
    {
        public const string MaterialFolder = "Assets/Game/Materials";

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>Named material keys used by every builder in the project.</summary>
        public static class Keys
        {
            public const string StructuralPine = "Timber_Structural_Pine";
            public const string Floorboard = "Timber_Floorboard";
            public const string Weatherboard = "Timber_Weatherboard_Painted";
            public const string BenchPly = "Timber_Ply_Bench";
            public const string ShelfBoard = "Timber_Shelf_Board";
            public const string Pegboard = "Hardboard_Pegboard";
            public const string Galvanised = "Metal_Galvanised";
            public const string DarkSteel = "Metal_Steel_Dark";
            public const string Hardware = "Metal_Hardware_Zinc";
            public const string PaintedGreen = "Paint_Metal_Green";
            public const string PaintedRed = "Paint_Metal_Red";
            public const string PaintedCream = "Paint_Metal_Cream";
            public const string ElectricalPlastic = "Plastic_Grey_Electrical";
            public const string BucketPlastic = "Plastic_Blue_Bucket";
            public const string Rubber = "Rubber_Black";
            public const string Tarpaulin = "Fabric_Tarpaulin";
            public const string Glass = "Glass_Window";
            public const string Concrete = "Concrete_Pier";
            public const string Grass = "Ground_Grass";
            public const string Gravel = "Ground_Gravel";
            public const string Cardboard = "Card_Box";
            public const string SoilBag = "Plastic_Soil_Bag";
            public const string Bulb = "Light_Bulb_Emissive";
        }

        private sealed class Definition
        {
            public string TextureFamily;   // null means untextured, constant values
            public Color BaseColor = Color.white;
            public float Smoothness = 0.3f;
            public float Metallic;
            public float NormalScale = 1f;
            public bool Transparent;
            public Color EmissiveColor = Color.black;
        }

        private static readonly Dictionary<string, Definition> Definitions = new Dictionary<string, Definition>
        {
            // --- timber -------------------------------------------------------
            [Keys.StructuralPine] = new Definition
            {
                TextureFamily = "Pine", BaseColor = new Color(0.95f, 0.93f, 0.90f), Smoothness = 0.16f,
            },
            [Keys.Floorboard] = new Definition
            {
                TextureFamily = "PineFloorboard", BaseColor = new Color(0.88f, 0.85f, 0.82f), Smoothness = 0.28f,
            },
            [Keys.Weatherboard] = new Definition
            {
                TextureFamily = "Weatherboard", BaseColor = Color.white, Smoothness = 0.35f, NormalScale = 1.2f,
            },
            [Keys.BenchPly] = new Definition
            {
                TextureFamily = "PlyBench", BaseColor = Color.white, Smoothness = 0.34f,
            },
            [Keys.ShelfBoard] = new Definition
            {
                // Older, greyer timber than the framing - shelves get more light and dust.
                TextureFamily = "Pine", BaseColor = new Color(0.82f, 0.80f, 0.78f), Smoothness = 0.20f,
            },
            [Keys.Pegboard] = new Definition
            {
                TextureFamily = "Pegboard", BaseColor = Color.white, Smoothness = 0.30f, NormalScale = 1.1f,
            },

            // --- metal --------------------------------------------------------
            [Keys.Galvanised] = new Definition
            {
                TextureFamily = "Galvanised", BaseColor = Color.white, Smoothness = 0.52f, Metallic = 1f,
            },
            [Keys.DarkSteel] = new Definition
            {
                BaseColor = new Color(0.180f, 0.182f, 0.190f), Smoothness = 0.38f, Metallic = 1f,
            },
            [Keys.Hardware] = new Definition
            {
                // Zinc-plated hinges, screws, latches.
                BaseColor = new Color(0.520f, 0.528f, 0.535f), Smoothness = 0.58f, Metallic = 1f,
            },
            [Keys.PaintedGreen] = new Definition
            {
                BaseColor = new Color(0.118f, 0.245f, 0.135f), Smoothness = 0.55f,
            },
            [Keys.PaintedRed] = new Definition
            {
                BaseColor = new Color(0.372f, 0.098f, 0.078f), Smoothness = 0.48f,
            },
            [Keys.PaintedCream] = new Definition
            {
                BaseColor = new Color(0.680f, 0.652f, 0.585f), Smoothness = 0.44f, Metallic = 0.85f,
            },

            // --- plastics and soft goods ---------------------------------------
            [Keys.ElectricalPlastic] = new Definition
            {
                BaseColor = new Color(0.640f, 0.632f, 0.608f), Smoothness = 0.42f,
            },
            [Keys.BucketPlastic] = new Definition
            {
                BaseColor = new Color(0.132f, 0.238f, 0.372f), Smoothness = 0.46f,
            },
            [Keys.Rubber] = new Definition
            {
                BaseColor = new Color(0.052f, 0.052f, 0.055f), Smoothness = 0.22f,
            },
            [Keys.Tarpaulin] = new Definition
            {
                TextureFamily = "Tarpaulin", BaseColor = Color.white, Smoothness = 0.34f,
            },
            [Keys.Cardboard] = new Definition
            {
                BaseColor = new Color(0.452f, 0.360f, 0.262f), Smoothness = 0.14f,
            },
            [Keys.SoilBag] = new Definition
            {
                BaseColor = new Color(0.128f, 0.150f, 0.118f), Smoothness = 0.52f,
            },

            // --- glass and ground ----------------------------------------------
            [Keys.Glass] = new Definition
            {
                BaseColor = new Color(0.92f, 0.95f, 0.94f, 0.12f), Smoothness = 0.96f, Transparent = true,
            },
            [Keys.Concrete] = new Definition
            {
                TextureFamily = "Concrete", BaseColor = Color.white, Smoothness = 0.20f,
            },
            [Keys.Grass] = new Definition
            {
                TextureFamily = "Grass", BaseColor = Color.white, Smoothness = 0.16f,
            },
            [Keys.Gravel] = new Definition
            {
                TextureFamily = "Gravel", BaseColor = Color.white, Smoothness = 0.22f,
            },

            // --- emissive --------------------------------------------------------
            [Keys.Bulb] = new Definition
            {
                BaseColor = new Color(0.92f, 0.88f, 0.80f), Smoothness = 0.90f,
                // Warm incandescent, moderate intensity so it does not bloom out.
                EmissiveColor = new Color(6.0f, 4.6f, 2.9f),
            },
        };

        [MenuItem("Freedome/Generate/Materials", false, 11)]
        public static void GenerateFromMenu()
        {
            Cache.Clear();
            CreateAll();
            EditorUtility.DisplayDialog("Shed materials",
                $"Created or refreshed {Definitions.Count} materials in {MaterialFolder}.", "OK");
        }

        public static void CreateAll()
        {
            Directory.CreateDirectory(MaterialFolder);
            foreach (string key in Definitions.Keys)
            {
                Get(key);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Returns the material for a key, creating the asset on first use.</summary>
        public static Material Get(string key)
        {
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            string path = $"{MaterialFolder}/{key}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("HDRP/Lit");
                if (shader == null)
                {
                    Debug.LogError("[Freedome] HDRP/Lit shader not found. Is the High Definition RP package installed?");
                    return null;
                }

                Directory.CreateDirectory(MaterialFolder);
                material = new Material(shader) { name = key };
                AssetDatabase.CreateAsset(material, path);
            }

            Configure(material, key);
            Cache[key] = material;
            return material;
        }

        private static void Configure(Material material, string key)
        {
            if (!Definitions.TryGetValue(key, out Definition def))
            {
                Debug.LogWarning($"[Freedome] No material definition for '{key}'.");
                return;
            }

            material.SetColor("_BaseColor", def.BaseColor);
            material.SetFloat("_Metallic", def.Metallic);
            material.SetFloat("_Smoothness", def.Smoothness);

            if (!string.IsNullOrEmpty(def.TextureFamily))
            {
                Texture2D albedo = LoadTexture(def.TextureFamily, "Albedo");
                Texture2D normal = LoadTexture(def.TextureFamily, "Normal");
                Texture2D maskMap = LoadTexture(def.TextureFamily, "Mask");

                if (albedo != null)
                {
                    material.SetTexture("_BaseColorMap", albedo);
                }

                if (normal != null)
                {
                    material.SetTexture("_NormalMap", normal);
                    material.SetFloat("_NormalScale", def.NormalScale);
                    material.EnableKeyword("_NORMALMAP");
                }

                if (maskMap != null)
                {
                    material.SetTexture("_MaskMap", maskMap);
                    material.EnableKeyword("_MASKMAP");
                    // With a mask map present HDRP reads smoothness from its alpha and
                    // remaps it between these two values.
                    material.SetFloat("_SmoothnessRemapMin", 0f);
                    material.SetFloat("_SmoothnessRemapMax", 1f);
                    material.SetFloat("_AORemapMin", 0f);
                    material.SetFloat("_AORemapMax", 1f);
                    material.SetFloat("_MetallicRemapMin", 0f);
                    material.SetFloat("_MetallicRemapMax", 1f);
                }
            }

            // UVs arrive from MeshBuilder already measured in metres, so tiling is 1:1.
            material.SetTextureScale("_BaseColorMap", Vector2.one);
            material.SetTextureOffset("_BaseColorMap", Vector2.zero);

            if (def.Transparent)
            {
                material.SetFloat("_SurfaceType", 1f);       // transparent
                material.SetFloat("_BlendMode", 0f);         // alpha
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_TransparentSortPriority", 0f);
                material.SetFloat("_RefractionModel", 0f);
            }
            else
            {
                material.SetFloat("_SurfaceType", 0f);
            }

            if (def.EmissiveColor.maxColorComponent > 0f)
            {
                material.SetFloat("_UseEmissiveIntensity", 0f);
                material.SetColor("_EmissiveColor", def.EmissiveColor);
                material.SetColor("_EmissiveColorLDR", def.EmissiveColor);
                material.EnableKeyword("_EMISSIVE_COLOR_MAP");
            }

            // Let HDRP rebuild keywords, render queue and pass state from the values
            // above rather than trying to set all of that by hand.
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
        }

        private static Texture2D LoadTexture(string family, string map)
        {
            string path = $"{ShedTextureGenerator.OutputFolder}/{family}_{map}.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"[Freedome] Missing generated texture '{path}'. " +
                                 "Run Freedome > Generate > Textures first.");
            }
            return texture;
        }

        /// <summary>Convenience for builders: resolve a set of keys to a material array.</summary>
        public static Material[] Resolve(params string[] keys)
        {
            Material[] result = new Material[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                result[i] = Get(keys[i]);
            }
            return result;
        }
    }
}
