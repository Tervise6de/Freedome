using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Dim = Freedome.Environment.ShedDimensions;

namespace Freedome.EditorTools.Validation
{
    /// <summary>
    /// Renders the fixed review viewpoints to PNG files in docs/screenshots.
    ///
    /// The eight positions are the ones the visual review pass has to cover. They
    /// are defined here rather than framed by hand so that the same shots can be
    /// re-taken after any change and compared directly - which is the only reliable
    /// way to notice that a prop started floating or a material stopped tiling.
    ///
    /// All shots are taken from standing eye height unless the view is deliberately
    /// a close-up, because a screenshot from an impossible camera position proves
    /// nothing about how the room reads in play.
    /// </summary>
    public static class ScreenshotCapture
    {
        public const string OutputFolder = "docs/screenshots";
        private const int Width = 1920;
        private const int Height = 1080;

        public struct Viewpoint
        {
            public string Name;
            public Vector3 Position;
            public Vector3 LookAt;
            public float FieldOfView;

            public Viewpoint(string name, Vector3 position, Vector3 lookAt, float fov = 70f)
            {
                Name = name;
                Position = position;
                LookAt = lookAt;
                FieldOfView = fov;
            }
        }

        /// <summary>The eight required review viewpoints, in the order the brief lists them.</summary>
        public static Viewpoint[] Viewpoints => new[]
        {
            new Viewpoint("01_entrance_to_workbench",
                new Vector3(Dim.DoorCentreX, Dim.PlayerEyeHeight, -2.55f),
                new Vector3(1.55f, 1.10f, 0.70f)),

            new Viewpoint("02_workbench_to_entrance",
                new Vector3(1.05f, Dim.PlayerEyeHeight, 0.60f),
                new Vector3(-0.85f, 1.25f, -2.95f)),

            new Viewpoint("03_rear_corner_overview",
                new Vector3(-1.50f, Dim.PlayerEyeHeight, 2.45f),
                new Vector3(0.65f, 0.95f, -1.60f), 78f),

            new Viewpoint("04_workbench_closeup",
                new Vector3(0.95f, 1.38f, 0.25f),
                new Vector3(1.85f, 0.94f, 0.55f), 55f),

            new Viewpoint("05_electrical_utility_area",
                new Vector3(0.85f, 1.55f, 1.75f),
                new Vector3(0.85f, 1.45f, 3.00f), 58f),

            new Viewpoint("06_ceiling_and_roof_structure",
                new Vector3(0.00f, 1.60f, -0.60f),
                new Vector3(0.00f, 3.05f, 0.90f), 74f),

            new Viewpoint("07_floor_and_object_contact",
                new Vector3(-0.62f, 1.30f, -0.30f),
                new Vector3(-0.62f, 0.02f, -1.55f), 62f),

            new Viewpoint("08_window_and_exterior",
                new Vector3(1.05f, 1.45f, 0.60f),
                new Vector3(2.60f, 1.32f, 0.60f), 60f),
        };

        [MenuItem("Freedome/Capture Review Screenshots", false, 42)]
        public static void CaptureFromMenu()
        {
            int count = CaptureAll();
            EditorUtility.DisplayDialog("Screenshots",
                $"Captured {count} screenshots into {OutputFolder}.", "OK");
        }

        /// <summary>
        /// Batch entry point. Unity's -executeMethod wants a static void with no
        /// arguments, and it needs a non-zero exit when nothing was captured -
        /// otherwise a headless run that rendered nothing reports success.
        /// </summary>
        public static void CaptureAllFromBatch()
        {
            int captured = 0;
            try
            {
                captured = CaptureAll();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Freedome] Screenshot capture threw: {e}");
            }

            if (captured < Viewpoints.Length)
            {
                Debug.LogError($"[Freedome] Captured {captured} of {Viewpoints.Length} views. " +
                               "On a machine with no GPU this usually means no graphics device " +
                               "was available - check that a software Vulkan driver is installed " +
                               "and that the editor was not run with -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static int CaptureAll()
        {
            if (EditorSceneManager.GetActiveScene().path != Generation.ShedSceneGenerator.ScenePath)
            {
                EditorSceneManager.OpenScene(Generation.ShedSceneGenerator.ScenePath, OpenSceneMode.Single);
            }

            Directory.CreateDirectory(OutputFolder);

            GameObject rig = new GameObject("ScreenshotCamera");
            Camera camera = rig.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 150f;

            if (rig.GetComponent<HDAdditionalCameraData>() == null)
            {
                rig.AddComponent<HDAdditionalCameraData>();
            }

            RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
            };

            int captured = 0;

            try
            {
                foreach (Viewpoint vp in Viewpoints)
                {
                    rig.transform.position = vp.Position;
                    rig.transform.rotation = Quaternion.LookRotation(
                        (vp.LookAt - vp.Position).normalized, Vector3.up);
                    camera.fieldOfView = vp.FieldOfView;

                    camera.targetTexture = rt;
                    camera.Render();

                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = rt;

                    Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    image.Apply();

                    RenderTexture.active = previous;
                    camera.targetTexture = null;

                    File.WriteAllBytes(Path.Combine(OutputFolder, vp.Name + ".png"), image.EncodeToPNG());
                    Object.DestroyImmediate(image);

                    captured++;
                    EditorUtility.DisplayProgressBar("Screenshots", vp.Name,
                        captured / (float)Viewpoints.Length);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(rig);
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            Debug.Log($"[Freedome] Captured {captured} review screenshots into {OutputFolder}.");
            return captured;
        }
    }
}
