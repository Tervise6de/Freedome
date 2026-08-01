using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Freedome.EditorTools.Generation;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// Tests for the mesh builder that every piece of the shed is made from. If
    /// these break, the geometry breaks everywhere at once.
    /// </summary>
    public sealed class GeometryTests
    {
        [Test]
        public void PlainBoxHasTwelveTriangles()
        {
            MeshBuilder mb = new MeshBuilder("test");
            mb.AddBox(Vector3.zero, Vector3.one, 0);

            Assert.AreEqual(12, mb.TriangleCount);
        }

        [Test]
        public void ChamferedBoxAddsEdgeAndCornerGeometry()
        {
            MeshBuilder mb = new MeshBuilder("test");
            mb.AddBox(Vector3.zero, Vector3.one, 0, 0.05f);

            // 6 faces + 12 chamfers = 36 triangles, plus 8 corner triangles.
            Assert.AreEqual(44, mb.TriangleCount);
        }

        [Test]
        public void ChamferIsClampedSoThinBoardsDoNotCollapse()
        {
            // A 19 mm board asked for a 50 mm chamfer must still be a board.
            MeshBuilder mb = new MeshBuilder("test");
            mb.AddBox(Vector3.zero, new Vector3(1f, 0.019f, 1f), 0, 0.05f);

            Mesh mesh = mb.ToMesh();
            Assert.Greater(mesh.bounds.size.y, 0.001f, "the board was chamfered out of existence");
            Assert.AreEqual(0.019f, mesh.bounds.size.y, 0.0005f);
        }

        [Test]
        public void NormalsAreUnitLength()
        {
            MeshBuilder mb = new MeshBuilder("test");
            mb.AddBox(Vector3.zero, new Vector3(0.5f, 1.2f, 0.3f), 0, 0.01f);
            mb.AddCylinder(new Vector3(2f, 0f, 0f), 0.1f, 0.4f, 12, 0);

            Mesh mesh = mb.ToMesh();
            foreach (Vector3 normal in mesh.normals)
            {
                Assert.AreEqual(1f, normal.magnitude, 0.001f, "a normal is not normalised");
            }
        }

        [Test]
        public void UvsAreMeasuredInMetresSoTextureScaleIsConsistent()
        {
            // Two boxes of very different sizes must produce the same UV density.
            MeshBuilder small = new MeshBuilder("small");
            small.AddBox(Vector3.zero, new Vector3(1f, 1f, 1f), 0);

            MeshBuilder large = new MeshBuilder("large");
            large.AddBox(Vector3.zero, new Vector3(4f, 4f, 4f), 0);

            Bounds smallUv = UvBounds(small.ToMesh());
            Bounds largeUv = UvBounds(large.ToMesh());

            Assert.AreEqual(1f, smallUv.size.x, 0.001f, "a 1 m face should span one UV unit");
            Assert.AreEqual(4f, largeUv.size.x, 0.001f, "a 4 m face should span four UV units");
        }

        // ------------------------------------------------------------------
        // Grain direction
        //
        // The generated timber textures run their fibre along V. If V is chosen
        // from the dominant world axis of each face - the obvious approach - then
        // every horizontal member gets vertical grain, because the side of a top
        // plate faces sideways whichever way the plate runs. These tests pin the
        // rule down: V follows the length of the piece.
        // ------------------------------------------------------------------

        [Test]
        public void GrainRunsAlongTheLengthOfAHorizontalMember()
        {
            // A top plate: 4 m long, 45 thick, 90 deep.
            MeshBuilder mb = new MeshBuilder("plate");
            mb.AddBox(Vector3.zero, new Vector3(4.0f, 0.045f, 0.090f), 0);

            Mesh mesh = mb.ToMesh();

            Assert.AreEqual(4.0f, UvBounds(mesh).size.y, 0.001f,
                "V should span the 4 m length, so the fibre runs along the plate");

            // The top face is the 4 m x 90 mm one. Its V must climb along X.
            AssertVRunsAlong(mesh, Vector3.up, Vector3.right,
                "grain on a top plate should run along its length");
        }

        [Test]
        public void GrainStaysVerticalOnAVerticalMember()
        {
            // A stud: 45 x 90 section, 2.265 m long.
            MeshBuilder mb = new MeshBuilder("stud");
            mb.AddBox(Vector3.zero, new Vector3(0.045f, 2.265f, 0.090f), 0);

            Mesh mesh = mb.ToMesh();

            Assert.AreEqual(2.265f, UvBounds(mesh).size.y, 0.001f,
                "V should span the stud's length");

            // The 45 x 2265 face looks along Z. Its V must climb vertically.
            AssertVRunsAlong(mesh, Vector3.forward, Vector3.up,
                "grain on a stud should run up it, not across it");
        }

        [Test]
        public void RotatedMemberCarriesItsGrainWithIt()
        {
            // A rafter laid on the roof pitch. Its length no longer points along a
            // world axis, which is exactly the case the old mapping got wrong.
            MeshBuilder mb = new MeshBuilder("rafter");
            mb.AddBox(Vector3.zero, new Vector3(2.60f, 0.090f, 0.045f),
                      Quaternion.Euler(0f, 0f, -22f), 0);

            Mesh mesh = mb.ToMesh();

            Assert.AreEqual(2.60f, UvBounds(mesh).size.y, 0.002f,
                "V should still span the rafter's length after it is tilted");

            // A rotation about Z leaves the +Z face pointing the same way, so that
            // face is where the tilted length axis shows up cleanly.
            Vector3 alongRafter = Quaternion.Euler(0f, 0f, -22f) * Vector3.right;
            AssertVRunsAlong(mesh, Vector3.forward, alongRafter,
                "grain should follow the rafter, not the world axis nearest to it");
        }

        [Test]
        public void GrainOverrideBeatsThePiecesOwnProportions()
        {
            // A wall panel wider than it is tall. Weatherboards still have to lap
            // horizontally, so the override has to win.
            MeshBuilder mb = new MeshBuilder("panel") { GrainOverride = Vector3.up };
            mb.AddBox(Vector3.zero, new Vector3(2.40f, 1.20f, 0.016f), 0);

            Bounds uv = UvBounds(mb.ToMesh());

            Assert.AreEqual(1.20f, uv.size.y, 0.001f,
                "V should follow the forced up axis, not the panel's longer side");
        }

        [Test]
        public void GrainOverrideAppliesToHandBuiltFaces()
        {
            // Gable ends are emitted as raw quads rather than boxes, so the
            // override has to reach them too.
            MeshBuilder mb = new MeshBuilder("gable") { GrainOverride = Vector3.up };
            mb.AddQuad(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f),
                       new Vector3(3f, 1.1f, 0f), new Vector3(0f, 1.1f, 0f), 0);

            Bounds uv = UvBounds(mb.ToMesh());

            Assert.AreEqual(1.1f, uv.size.y, 0.001f, "V should follow the forced axis");
            Assert.AreEqual(3.0f, uv.size.x, 0.001f, "U should run across it");
        }

        [Test]
        public void SawnEndsFallBackToEndGrain()
        {
            // On the end of a board the grain points straight out of the face, so
            // there is no direction to follow and the axis-aligned mapping applies.
            // The check is simply that it produces sane finite UVs rather than a
            // division by a zero-length vector.
            MeshBuilder mb = new MeshBuilder("post");
            mb.AddBox(Vector3.zero, new Vector3(0.09f, 2.0f, 0.09f), 0);

            Mesh mesh = mb.ToMesh();
            foreach (Vector2 uv in mesh.uv)
            {
                Assert.IsFalse(float.IsNaN(uv.x) || float.IsNaN(uv.y), "UV is not finite");
                Assert.Less(Mathf.Abs(uv.x), 10f);
                Assert.Less(Mathf.Abs(uv.y), 10f);
            }
        }

        [Test]
        public void UvOffsetSlidesTheMapWithoutChangingItsScale()
        {
            // Each floorboard gets its own offset so the floor does not read as one
            // board repeated. It must move the map, not stretch it.
            MeshBuilder plain = new MeshBuilder("plain");
            plain.AddBox(Vector3.zero, new Vector3(1f, 1f, 1f), 0);

            MeshBuilder shifted = new MeshBuilder("shifted") { UvOffset = new Vector2(0.37f, 0.61f) };
            shifted.AddBox(Vector3.zero, new Vector3(1f, 1f, 1f), 0);

            Bounds a = UvBounds(plain.ToMesh());
            Bounds b = UvBounds(shifted.ToMesh());

            Assert.AreEqual(a.size.x, b.size.x, 0.0005f, "the offset changed the U scale");
            Assert.AreEqual(a.size.y, b.size.y, 0.0005f, "the offset changed the V scale");
            Assert.AreEqual(0.37f, b.center.x - a.center.x, 0.0005f);
            Assert.AreEqual(0.61f, b.center.y - a.center.y, 0.0005f);
        }

        [Test]
        public void SubmeshesAreKeptSeparate()
        {
            MeshBuilder mb = new MeshBuilder("test", 3);
            mb.AddBox(Vector3.zero, Vector3.one, 0);
            mb.AddBox(Vector3.right, Vector3.one, 2);

            Mesh mesh = mb.ToMesh();
            Assert.AreEqual(3, mesh.subMeshCount);
            Assert.AreEqual(36, mesh.GetTriangles(0).Length);
            Assert.AreEqual(0, mesh.GetTriangles(1).Length);
            Assert.AreEqual(36, mesh.GetTriangles(2).Length);
        }

        [Test]
        public void TransformStackNestsCorrectly()
        {
            MeshBuilder mb = new MeshBuilder("test");
            mb.Push(new Vector3(10f, 0f, 0f));
            mb.Push(new Vector3(0f, 5f, 0f));
            mb.AddBox(Vector3.zero, Vector3.one, 0);
            mb.Pop();
            mb.Pop();

            Mesh mesh = mb.ToMesh();
            Assert.AreEqual(new Vector3(10f, 5f, 0f), mesh.bounds.center);
        }

        [Test]
        public void ExtrusionProducesAClosedSolid()
        {
            Vector2[] profile =
            {
                new Vector2(0f, 0f),
                new Vector2(0.09f, 0f),
                new Vector2(0.09f, 0.15f),
                new Vector2(0f, 0.19f),
            };

            MeshBuilder mb = new MeshBuilder("wedge");
            mb.AddExtrusion(profile, 0.55f, Vector3.zero, Quaternion.identity, 0);

            Mesh mesh = mb.ToMesh();
            // Four side quads (8 triangles) plus two capping fans (4 triangles).
            Assert.AreEqual(12, mb.TriangleCount);
            Assert.AreEqual(0.55f, mesh.bounds.size.z, 0.001f);
            Assert.AreEqual(0.19f, mesh.bounds.size.y, 0.001f);
        }

        [Test]
        public void CorrugatedSheetUndulatesByTheGivenAmplitude()
        {
            MeshBuilder mb = new MeshBuilder("sheet");
            mb.AddCorrugatedSheet(Vector3.zero, Quaternion.identity, 1.0f, 2.0f, 0.076f, 0.016f, 0);

            Mesh mesh = mb.ToMesh();
            Assert.AreEqual(0.016f, mesh.bounds.size.y, 0.002f,
                "the corrugation profile is not the requested depth");
            Assert.AreEqual(2.0f, mesh.bounds.size.z, 0.01f);
        }

        [Test]
        public void FramingProducesPlatesStudsAndTrimmersAroundAnOpening()
        {
            MeshBuilder solid = new MeshBuilder("solid");
            FramingUtility.BuildStudWall(solid, 4.0f, 2.4f, new List<FramingUtility.Opening>(), 0);

            MeshBuilder withDoor = new MeshBuilder("withDoor");
            FramingUtility.BuildStudWall(withDoor, 4.0f, 2.4f,
                new List<FramingUtility.Opening>
                {
                    new FramingUtility.Opening(1.5f, 2.4f, 0f, 2.08f),
                }, 0);

            Assert.Greater(solid.TriangleCount, 0, "a plain wall produced no geometry");

            // An opening removes a stud but adds jack studs, king studs, a lintel and
            // cripples, so the framed wall should end up with more geometry, not less.
            Assert.Greater(withDoor.TriangleCount, solid.TriangleCount * 0.9f,
                "framing an opening lost more geometry than it added");
        }

        [Test]
        public void PanelWithOpeningLeavesAHole()
        {
            List<FramingUtility.Opening> openings = new List<FramingUtility.Opening>
            {
                new FramingUtility.Opening(1.0f, 2.0f, 1.0f, 1.8f),
            };

            MeshBuilder solid = new MeshBuilder("solid");
            FramingUtility.AddPanelWithOpenings(solid, 4f, 2.4f, 0.019f, 0f,
                new List<FramingUtility.Opening>(), 0);

            MeshBuilder holed = new MeshBuilder("holed");
            FramingUtility.AddPanelWithOpenings(holed, 4f, 2.4f, 0.019f, 0f, openings, 0);

            Assert.AreEqual(12, solid.TriangleCount, "a plain panel should be one box");
            Assert.Greater(holed.TriangleCount, solid.TriangleCount,
                "the panel was not split around the opening");
        }

        // ------------------------------------------------------------------
        // Roof closure
        // ------------------------------------------------------------------

        [Test]
        public void RidgeCapClosesTheApex()
        {
            // The corrugated sheets stop at the ridge board, so the two cap wings are
            // the only thing covering the slot between them. Each wing's inner edge
            // has to cross the centreline, not merely reach the board: at the
            // original offset they stopped 3.2 mm short each, leaving 6.5 mm of open
            // sky down the entire 6.9 m ridge.
            float innerEdgeX = RoofBuilder.RidgeCapInnerEdgeX;

            Assert.Less(innerEdgeX, 0f,
                $"the ridge cap stops {innerEdgeX * 1000f:0.0} mm short of the centreline, " +
                "so there is a slot straight through the roof");

            // Crossing zero is necessary but not sufficient. The wings are 6 mm
            // boxes in two planes that meet at the apex, so they only overlap in
            // height within a narrow band either side of it. Stopping inside that
            // band leaves the chamfered edges as the only thing meeting, and a
            // hairline of sky still shows - which is exactly what the preview
            // render caught after the first attempt at this fix.
            float sealBand = RoofBuilder.RidgeCapSealBandHalfWidth;
            Assert.Less(innerEdgeX, -sealBand,
                $"the wings cross by only {-innerEdgeX * 1000f:0.0} mm but need to clear " +
                $"the {sealBand * 1000f:0.0} mm seal band to overlap solidly");
        }

        [Test]
        public void RidgeCapStillCoversTheSheetEdge()
        {
            // Closing the apex must not be done by sliding the cap up the roof until
            // its outer edge no longer laps the sheeting it is supposed to weather.
            float outerAlongSlope = RoofBuilder.CapCentreOffset + (RoofBuilder.CapWidth * 0.5f);

            Assert.Greater(outerAlongSlope, 0.20f,
                $"the cap only reaches {outerAlongSlope * 1000f:0} mm down the slope, " +
                "which is not enough lap over the sheet");
        }

        private static Bounds UvBounds(Mesh mesh)
        {
            Vector2[] uvs = mesh.uv;
            Bounds bounds = new Bounds(uvs[0], Vector3.zero);
            foreach (Vector2 uv in uvs)
            {
                bounds.Encapsulate(uv);
            }
            return bounds;
        }

        /// <summary>
        /// The world direction along which V increases on one specific face.
        ///
        /// This exists because UvBounds cannot tell you anything about grain: it
        /// unions every face of the box, and a box has faces in all orientations,
        /// so its V extent is the piece's longest dimension no matter which way
        /// the grain actually runs. Checking one face is what pins the direction.
        /// </summary>
        private static Vector3 VAxisOnFace(Mesh mesh, Vector3 faceNormal)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;

            List<int> face = new List<int>();
            for (int i = 0; i < verts.Length; i++)
            {
                if (Vector3.Dot(normals[i], faceNormal) > 0.99f)
                {
                    face.Add(i);
                }
            }

            Assert.Greater(face.Count, 2, $"no face found with normal {faceNormal}");

            Vector3 meanPosition = Vector3.zero;
            float meanV = 0f;
            foreach (int i in face)
            {
                meanPosition += verts[i];
                meanV += uvs[i].y;
            }
            meanPosition /= face.Count;
            meanV /= face.Count;

            // Covariance of position against V: the direction V climbs in.
            Vector3 covariance = Vector3.zero;
            foreach (int i in face)
            {
                covariance += (verts[i] - meanPosition) * (uvs[i].y - meanV);
            }

            Assert.Greater(covariance.magnitude, 1e-6f, "V does not vary across the face");
            return covariance.normalized;
        }

        private static void AssertVRunsAlong(Mesh mesh, Vector3 faceNormal, Vector3 expected, string because)
        {
            Vector3 actual = VAxisOnFace(mesh, faceNormal);

            // Sign is irrelevant - the fibre has no head or tail.
            float alignment = Mathf.Abs(Vector3.Dot(actual, expected.normalized));
            Assert.Greater(alignment, 0.99f, $"{because} (V runs along {actual}, expected {expected})");
        }
    }
}
