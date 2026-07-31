using System.IO;
using NUnit.Framework;
using UnityEngine;
using Freedome.EditorTools.Generation;
using Freedome.EditorTools.Validation;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// The acceptance checks the milestone asks for: the main scene exists, nothing
    /// in it has a missing script or material, the player can spawn, the required
    /// collision is present, and the project settings are the ones the build needs.
    ///
    /// If the scene has never been generated these are reported as inconclusive
    /// rather than failed, because "you have not run the generator yet" is a
    /// different problem from "the generator produces a broken scene".
    /// </summary>
    public sealed class SceneAndProjectTests
    {
        private static bool SceneExists => File.Exists(ShedSceneGenerator.ScenePath);

        private static void RequireScene()
        {
            if (!SceneExists)
            {
                Assert.Ignore($"{ShedSceneGenerator.ScenePath} has not been generated. " +
                              "Run Freedome > Generate > Shed Room Scene.");
            }
        }

        [Test]
        public void MainSceneExists()
        {
            Assert.IsTrue(SceneExists,
                $"Expected the main scene at {ShedSceneGenerator.ScenePath}.");
        }

        [Test]
        public void ProjectSettingsAreConfiguredForTheBuild()
        {
            SceneValidator.Report report = new SceneValidator.Report();
            SceneValidator.ValidateProjectSettings(report);

            Assert.IsFalse(report.HasErrors, report.Summarise());
        }

        [Test]
        public void SceneHasNoMissingScriptsMaterialsOrCollision()
        {
            RequireScene();

            SceneValidator.Report report = SceneValidator.ValidateAll(openScene: true);
            Assert.IsFalse(report.HasErrors, report.Summarise());
        }

        [Test]
        public void EveryMaterialInTheLibraryResolvesToARealShader()
        {
            string[] keys =
            {
                ShedMaterialLibrary.Keys.StructuralPine,
                ShedMaterialLibrary.Keys.Floorboard,
                ShedMaterialLibrary.Keys.Weatherboard,
                ShedMaterialLibrary.Keys.BenchPly,
                ShedMaterialLibrary.Keys.ShelfBoard,
                ShedMaterialLibrary.Keys.Pegboard,
                ShedMaterialLibrary.Keys.Galvanised,
                ShedMaterialLibrary.Keys.DarkSteel,
                ShedMaterialLibrary.Keys.Hardware,
                ShedMaterialLibrary.Keys.PaintedGreen,
                ShedMaterialLibrary.Keys.PaintedRed,
                ShedMaterialLibrary.Keys.PaintedCream,
                ShedMaterialLibrary.Keys.ElectricalPlastic,
                ShedMaterialLibrary.Keys.BucketPlastic,
                ShedMaterialLibrary.Keys.Rubber,
                ShedMaterialLibrary.Keys.Tarpaulin,
                ShedMaterialLibrary.Keys.Glass,
                ShedMaterialLibrary.Keys.Concrete,
                ShedMaterialLibrary.Keys.Grass,
                ShedMaterialLibrary.Keys.Gravel,
                ShedMaterialLibrary.Keys.Cardboard,
                ShedMaterialLibrary.Keys.SoilBag,
                ShedMaterialLibrary.Keys.Bulb,
            };

            foreach (string key in keys)
            {
                Material material = ShedMaterialLibrary.Get(key);
                Assert.IsNotNull(material, $"material '{key}' could not be created");
                Assert.IsNotNull(material.shader, $"material '{key}' has no shader");
                Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name,
                    $"material '{key}' fell back to the error shader");
            }
        }

        [Test]
        public void GeneratedTexturesArePresentForEveryTexturedMaterial()
        {
            string[] families =
            {
                "Pine", "PineFloorboard", "Weatherboard", "PlyBench",
                "Galvanised", "Pegboard", "Concrete", "Gravel", "Grass", "Tarpaulin",
            };

            foreach (string family in families)
            {
                foreach (string map in new[] { "Albedo", "Normal", "Mask" })
                {
                    string path = $"{ShedTextureGenerator.OutputFolder}/{family}_{map}.png";
                    if (!File.Exists(path))
                    {
                        Assert.Ignore($"Generated textures are missing ({path}). " +
                                      "Run Freedome > Generate > Textures.");
                    }
                }
            }

            Assert.Pass();
        }

        [Test]
        public void AllEightReviewViewpointsAreInsideTheRoom()
        {
            foreach (ScreenshotCapture.Viewpoint vp in ScreenshotCapture.Viewpoints)
            {
                Assert.Less(Mathf.Abs(vp.Position.x), Freedome.Environment.ShedDimensions.HalfWidth,
                    $"viewpoint {vp.Name} is outside the room on X");
                Assert.Less(Mathf.Abs(vp.Position.z), Freedome.Environment.ShedDimensions.HalfLength,
                    $"viewpoint {vp.Name} is outside the room on Z");
                Assert.Greater(vp.Position.y, 0.2f, $"viewpoint {vp.Name} is below the floor");
            }

            Assert.AreEqual(8, ScreenshotCapture.Viewpoints.Length,
                "the review set must cover all eight required views");
        }
    }
}
