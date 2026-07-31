using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Shared plumbing for the scene generators: turns a MeshBuilder into a saved
    /// mesh asset plus a configured GameObject, and keeps a running tally of the
    /// geometry budget so the performance notes can be written from real numbers.
    /// </summary>
    public sealed class BuildContext
    {
        public const string MeshFolder = "Assets/Game/Models/Generated";

        private readonly Dictionary<string, int> _nameCounters = new Dictionary<string, int>();

        public Transform Root { get; }

        public int TotalTriangles { get; private set; }

        public int TotalRenderers { get; private set; }

        public BuildContext(Transform root)
        {
            Root = root;
            Directory.CreateDirectory(MeshFolder);
        }

        /// <summary>
        /// Creates a static, lightmapped GameObject from a MeshBuilder.
        /// </summary>
        /// <param name="collider">
        /// None for decorative detail, Mesh for architecture the player leans on,
        /// Box for props where a tight convex approximation is cheaper and gives the
        /// character controller something stable to slide along.
        /// </param>
        public GameObject CreateObject(string name, MeshBuilder builder, string[] materialKeys,
                                       Transform parent, Vector3 position, Quaternion rotation,
                                       ColliderKind collider = ColliderKind.None)
        {
            if (builder.VertexCount == 0)
            {
                return null;
            }

            Mesh mesh = builder.ToMesh();
            mesh.name = UniqueName(name);
            SaveMesh(mesh);

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            Material[] materials = new Material[Mathf.Max(1, builder.SubmeshCount)];
            for (int i = 0; i < materials.Length; i++)
            {
                string key = i < materialKeys.Length ? materialKeys[i] : materialKeys[materialKeys.Length - 1];
                materials[i] = ShedMaterialLibrary.Get(key);
            }
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            switch (collider)
            {
                case ColliderKind.Mesh:
                {
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    break;
                }
                case ColliderKind.Box:
                {
                    BoxCollider bc = go.AddComponent<BoxCollider>();
                    bc.center = mesh.bounds.center;
                    bc.size = mesh.bounds.size;
                    break;
                }
            }

            MarkStatic(go);

            TotalTriangles += builder.TriangleCount;
            TotalRenderers++;
            return go;
        }

        public enum ColliderKind
        {
            None,
            Mesh,
            Box,
        }

        /// <summary>
        /// Everything in the shed is static geometry, which lets Unity batch it,
        /// include it in the lightmap and use it for occlusion and reflections.
        /// </summary>
        public static void MarkStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ReflectionProbeStatic |
                StaticEditorFlags.OffMeshLinkGeneration);
        }

        public GameObject CreateGroup(string name, Transform parent = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>
        /// Adds an invisible box collider. Used for the door leaf and for the safety
        /// shell that guarantees the player cannot leave the demonstration area even
        /// if they find a gap in the framing.
        /// </summary>
        public static GameObject CreateBlocker(string name, Transform parent, Vector3 centre, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;

            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = size;

            MarkStatic(go);
            return go;
        }

        private void SaveMesh(Mesh mesh)
        {
            string path = $"{MeshFolder}/{mesh.name}.asset";
            AssetDatabase.CreateAsset(mesh, path);
        }

        private string UniqueName(string baseName)
        {
            if (!_nameCounters.TryGetValue(baseName, out int count))
            {
                count = 0;
            }
            _nameCounters[baseName] = count + 1;
            return count == 0 ? baseName : $"{baseName}_{count:00}";
        }

        /// <summary>Deletes previously generated mesh assets so a rebuild stays clean.</summary>
        public static void ClearGeneratedMeshes()
        {
            if (!Directory.Exists(MeshFolder))
            {
                return;
            }

            AssetDatabase.DeleteAsset(MeshFolder);
            Directory.CreateDirectory(MeshFolder);
            AssetDatabase.Refresh();
        }
    }
}
