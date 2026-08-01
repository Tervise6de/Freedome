using System.Collections.Generic;
using UnityEngine;
using Freedome.Interaction;
using Freedome.Environment;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Floor platform, the four stud walls and the exterior ground.
    ///
    /// The shed is framed the way a small timber building actually is: piers carry
    /// bearers, bearers carry joists, joists carry the floorboards, and the walls
    /// stand on the completed platform. That order is worth following because it is
    /// what makes the thicknesses come out right - the player sees a 19 mm board
    /// edge at the door threshold, not a zero-thickness plane.
    /// </summary>
    public static class ShellBuilder
    {
        // Wall local frames. Local +X runs along the wall, +Z points outward.
        public struct WallFrame
        {
            public Vector3 Origin;
            public Quaternion Rotation;
            public float Length;

            public WallFrame(Vector3 origin, float yawDegrees, float length)
            {
                Origin = origin;
                Rotation = Quaternion.Euler(0f, yawDegrees, 0f);
                Length = length;
            }
        }

        /// <summary>Outer extent of the floor platform, which the walls stand on.</summary>
        public static float DeckHalfWidth => Dim.HalfWidth + Dim.StudDepth;   // 2.09

        public static float DeckHalfLength => Dim.HalfLength + Dim.StudDepth; // 3.09

        public static WallFrame UtilityWall => new WallFrame(
            new Vector3(-Dim.HalfWidth, 0f, Dim.HalfLength), 0f, Dim.InteriorWidth);

        public static WallFrame EntranceWall => new WallFrame(
            new Vector3(Dim.HalfWidth, 0f, -Dim.HalfLength), 180f, Dim.InteriorWidth);

        public static WallFrame StorageWall => new WallFrame(
            new Vector3(-Dim.HalfWidth, 0f, -DeckHalfLength), -90f, DeckHalfLength * 2f);

        public static WallFrame WorkbenchWall => new WallFrame(
            new Vector3(Dim.HalfWidth, 0f, DeckHalfLength), 90f, DeckHalfLength * 2f);

        // ---------------------------------------------------------------------
        // Openings, expressed in each wall's local coordinates.
        // ---------------------------------------------------------------------

        public static FramingUtility.Opening DoorOpening
        {
            get
            {
                float halfRough = Dim.DoorRoughWidth * 0.5f;
                // Entrance wall local X counts back from world x = +2.0.
                float u0 = Dim.HalfWidth - (Dim.DoorCentreX + halfRough);
                float u1 = Dim.HalfWidth - (Dim.DoorCentreX - halfRough);
                return new FramingUtility.Opening(u0, u1, 0f, Dim.DoorRoughHeight);
            }
        }

        public static FramingUtility.Opening WindowOpening
        {
            get
            {
                float half = Dim.WindowWidth * 0.5f;
                // Workbench wall local X counts back from world z = +3.09.
                float u0 = DeckHalfLength - (Dim.WindowCentreZ + half);
                float u1 = DeckHalfLength - (Dim.WindowCentreZ - half);
                return new FramingUtility.Opening(u0, u1, Dim.WindowSillHeight, Dim.WindowHeadHeight);
            }
        }

        public static FramingUtility.Opening VentOpening
        {
            get
            {
                float halfW = Dim.VentWidth * 0.5f;
                float halfH = Dim.VentHeight * 0.5f;
                // Utility wall local X counts forward from world x = -2.0.
                float u0 = Dim.VentCentreX - halfW + Dim.HalfWidth;
                float u1 = Dim.VentCentreX + halfW + Dim.HalfWidth;
                return new FramingUtility.Opening(u0, u1, Dim.VentCentreY - halfH, Dim.VentCentreY + halfH);
            }
        }

        // =====================================================================
        // Floor
        // =====================================================================

        public static void BuildFloor(BuildContext ctx, Transform parent)
        {
            BuildSubstructure(ctx, parent);
            BuildDeck(ctx, parent);
            BuildServicePanel(ctx, parent);
        }

        private static void BuildSubstructure(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_FloorStructure", 2);

            float deckW = DeckHalfWidth;
            float deckL = DeckHalfLength;

            float joistTop = -Dim.FloorBoardThickness;                    // -0.019
            float joistBottom = joistTop - Dim.FloorJoistDepth;           // -0.109
            float bearerTop = joistBottom;
            float bearerBottom = bearerTop - Dim.BearerDepth;             // -0.249

            // Bearers run the length of the shed on three lines of piers.
            float[] bearerX = { -1.75f, 0f, 1.75f };
            foreach (float x in bearerX)
            {
                mb.AddBox(new Vector3(x, (bearerTop + bearerBottom) * 0.5f, 0f),
                          new Vector3(0.090f, Dim.BearerDepth, deckL * 2f), 0);
            }

            // Joists span across the width between the bearers.
            for (float z = -deckL + (Dim.FloorJoistWidth * 0.5f); z <= deckL; z += Dim.FloorJoistSpacing)
            {
                mb.AddBox(new Vector3(0f, (joistTop + joistBottom) * 0.5f, z),
                          new Vector3(deckW * 2f, Dim.FloorJoistDepth, Dim.FloorJoistWidth), 0);
            }

            // Rim joists close the ends.
            mb.AddBox(new Vector3(0f, (joistTop + joistBottom) * 0.5f, deckL - (Dim.FloorJoistWidth * 0.5f)),
                      new Vector3(deckW * 2f, Dim.FloorJoistDepth, Dim.FloorJoistWidth), 0);
            for (int s = -1; s <= 1; s += 2)
            {
                mb.AddBox(new Vector3(s * (deckW - (Dim.FloorJoistWidth * 0.5f)),
                                      (joistTop + joistBottom) * 0.5f, 0f),
                          new Vector3(Dim.FloorJoistWidth, Dim.FloorJoistDepth, deckL * 2f), 0);
            }

            // Trimmers framing the service hatch, visible in the reveal around it.
            float px0 = Dim.ServicePanelCentreX - (Dim.ServicePanelWidth * 0.5f);
            float px1 = Dim.ServicePanelCentreX + (Dim.ServicePanelWidth * 0.5f);
            float pz0 = Dim.ServicePanelCentreZ - (Dim.ServicePanelLength * 0.5f);
            float pz1 = Dim.ServicePanelCentreZ + (Dim.ServicePanelLength * 0.5f);

            foreach (float pz in new[] { pz0, pz1 })
            {
                mb.AddBox(new Vector3((px0 + px1) * 0.5f, joistTop - 0.030f, pz),
                          new Vector3(px1 - px0, 0.060f, Dim.FloorJoistWidth), 0, 0.002f);
            }
            foreach (float px in new[] { px0, px1 })
            {
                mb.AddBox(new Vector3(px, joistTop - 0.030f, (pz0 + pz1) * 0.5f),
                          new Vector3(Dim.FloorJoistWidth, 0.060f, pz1 - pz0), 0, 0.002f);
            }

            // Concrete piers. Only the outer ring is ever visible from the ground.
            foreach (float x in bearerX)
            {
                foreach (float z in new[] { -2.6f, 0f, 2.6f })
                {
                    mb.AddBox(new Vector3(x, bearerBottom - 0.125f, z),
                              new Vector3(0.200f, 0.250f, 0.200f), 1, 0.008f);
                }
            }

            ctx.CreateObject("Shed_FloorStructure", mb,
                new[] { Keys.StructuralPine, Keys.Concrete },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        /// <summary>
        /// Individual floorboards. Modelling each board rather than texturing a slab
        /// is what gives the floor its shadow lines, and the chamfered arris on every
        /// board edge is the single most effective anti-blockout detail in the scene.
        /// </summary>
        private static void BuildDeck(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_Floorboards", 1);

            float deckW = DeckHalfWidth;
            float deckL = DeckHalfLength;
            float boardW = Dim.FloorBoardWidth;
            float y = -Dim.FloorBoardThickness * 0.5f;

            float px0 = Dim.ServicePanelCentreX - (Dim.ServicePanelWidth * 0.5f);
            float px1 = Dim.ServicePanelCentreX + (Dim.ServicePanelWidth * 0.5f);
            float pz0 = Dim.ServicePanelCentreZ - (Dim.ServicePanelLength * 0.5f);
            float pz1 = Dim.ServicePanelCentreZ + (Dim.ServicePanelLength * 0.5f);

            int board = 0;
            float x = -deckW;
            while (x < deckW - 0.001f)
            {
                float w = Mathf.Min(boardW, deckW - x);
                float cx = x + (w * 0.5f);
                bool overHatch = cx > px0 && cx < px1;

                // Each board samples a different square of the one-metre map, so
                // the floor reads as twenty-nine boards rather than as one board
                // repeated. Derived from the index, so it is reproducible.
                mb.UvOffset = new Vector2(
                    ((board * 37) % 100) / 100f,
                    ((board * 61) % 100) / 100f);
                board++;

                if (overHatch)
                {
                    // Board is interrupted by the hatch.
                    mb.AddBox(new Vector3(cx, y, (-deckL + pz0) * 0.5f),
                              new Vector3(w, Dim.FloorBoardThickness, pz0 + deckL), 0, 0.0015f);
                    mb.AddBox(new Vector3(cx, y, (pz1 + deckL) * 0.5f),
                              new Vector3(w, Dim.FloorBoardThickness, deckL - pz1), 0, 0.0015f);
                }
                else
                {
                    mb.AddBox(new Vector3(cx, y, 0f),
                              new Vector3(w, Dim.FloorBoardThickness, deckL * 2f), 0, 0.0015f);
                }

                x += w;
            }

            mb.UvOffset = Vector2.zero;

            ctx.CreateObject("Shed_Floorboards", mb, new[] { Keys.Floorboard },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        /// <summary>
        /// The removable service panel. It is a plain hatch: five board offcuts on a
        /// rebated ledger, four countersunk screws and a finger slot. Nothing marks
        /// it out - it simply looks like the way you would reach a stop-cock or a
        /// cable run under the floor.
        /// </summary>
        /// <summary>World Z of the floor panel's hinge line. See DoorHingeX.</summary>
        public static float ServicePanelHingeZ =>
            Dim.ServicePanelCentreZ - (Dim.ServicePanelLength * 0.5f);

        /// <summary>The panel's own offset from that hinge line.</summary>
        public static float ServicePanelLocalZ => Dim.ServicePanelLength * 0.5f;

        private static void BuildServicePanel(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_ServicePanel", 2);

            float w = Dim.ServicePanelWidth - 0.006f;  // 3 mm reveal each side
            float l = Dim.ServicePanelLength - 0.006f;
            float t = Dim.FloorBoardThickness;

            int boards = 5;
            float boardW = w / boards;
            for (int i = 0; i < boards; i++)
            {
                float cx = -w * 0.5f + (boardW * (i + 0.5f));
                mb.AddBox(new Vector3(cx, -t * 0.5f, 0f),
                          new Vector3(boardW - 0.001f, t, l), 0, 0.0015f);
            }

            // Ledger battens screwed across the underside hold the boards together.
            foreach (float z in new[] { -l * 0.5f + 0.09f, l * 0.5f - 0.09f })
            {
                mb.AddBox(new Vector3(0f, -t - 0.0095f, z),
                          new Vector3(w - 0.02f, 0.019f, 0.070f), 0, 0.002f);
            }

            // Four countersunk screws, one near each corner.
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    mb.AddCylinder(new Vector3(sx * (w * 0.5f - 0.045f), -0.0015f, sz * (l * 0.5f - 0.070f)),
                                   0.0055f, 0.0040f, 0.003f, 10, 1, Quaternion.identity);
                }
            }

            // Recessed finger slot, cut as a shallow dark rebate rather than a handle.
            mb.AddBox(new Vector3(0f, -0.010f, l * 0.5f - 0.055f),
                      new Vector3(0.080f, 0.012f, 0.022f), 1, 0.002f);

            // Hinged along its far edge so it lifts like a floor hatch rather than
            // sliding. Still an ordinary service panel: no marking, no highlight, and
            // nothing under it but joists and the underside of the platform.
            GameObject hinge = ctx.CreateGroup("ServicePanel_Hinge", parent);
            hinge.transform.localPosition = new Vector3(
                Dim.ServicePanelCentreX, 0f, ServicePanelHingeZ);
            BuildContext.MarkMovable(hinge);

            GameObject panel = ctx.CreateObject("Shed_ServicePanel", mb,
                new[] { Keys.Floorboard, Keys.Hardware },
                hinge.transform, new Vector3(0f, 0f, ServicePanelLocalZ),
                Quaternion.identity, BuildContext.ColliderKind.Box, isStatic: false);

            if (panel != null)
            {
                HingedPart part = hinge.AddComponent<HingedPart>();
                part.Configure("Lift the floor panel", "Lower the floor panel",
                               Vector3.right, -78f, 110f, null);
            }
        }

        // =====================================================================
        // Walls
        // =====================================================================

        public static void BuildWalls(BuildContext ctx, Transform parent)
        {
            BuildWall(ctx, parent, "Wall_Entrance", EntranceWall,
                new List<FramingUtility.Opening> { DoorOpening }, true);

            BuildWall(ctx, parent, "Wall_Utility", UtilityWall,
                new List<FramingUtility.Opening> { VentOpening }, true);

            BuildWall(ctx, parent, "Wall_Storage", StorageWall,
                new List<FramingUtility.Opening>(), false);

            BuildWall(ctx, parent, "Wall_Workbench", WorkbenchWall,
                new List<FramingUtility.Opening> { WindowOpening }, false);

            BuildCornerTrim(ctx, parent);
        }

        private static void BuildWall(BuildContext ctx, Transform parent, string name, WallFrame frame,
                                      List<FramingUtility.Opening> openings, bool isGable)
        {
            // 0 framing, 1 sheathing, 2 painted weatherboard
            MeshBuilder mb = new MeshBuilder(name, 3);

            FramingUtility.BuildStudWall(mb, frame.Length, Dim.WallHeight, openings, 0);

            float sheathingZ = Dim.StudDepth;
            float claddingZ = sheathingZ + Dim.BoardThickness;

            // The panels are split into cells around the openings, and a cell's own
            // proportions are no guide to which way its boards run - a tall narrow
            // cell beside a door would end up grained at right angles to the wide
            // one above it. Both layers are boarded horizontally, so both are forced
            // along the wall.
            mb.GrainOverride = Vector3.right;
            FramingUtility.AddPanelWithOpenings(mb, frame.Length, Dim.WallHeight,
                Dim.BoardThickness, sheathingZ, openings, 1, 0.002f);

            // Weatherboards lap horizontally whichever way round the wall is, and
            // the generated texture stacks its boards along V - so V must be up.
            mb.GrainOverride = Vector3.up;
            FramingUtility.AddPanelWithOpenings(mb, frame.Length, Dim.WallHeight,
                Dim.CladdingThickness, claddingZ, openings, 2, 0.002f);

            mb.GrainOverride = null;

            if (isGable)
            {
                BuildGableInfill(mb, frame);
            }

            ctx.CreateObject(name, mb,
                new[] { Keys.StructuralPine, Keys.StructuralPine, Keys.Weatherboard },
                parent, frame.Origin, frame.Rotation, BuildContext.ColliderKind.Mesh);
        }

        /// <summary>
        /// The triangle above the top plate on a gable end: studs following the roof
        /// line, then a sheathed and clad prism.
        /// </summary>
        private static void BuildGableInfill(MeshBuilder mb, WallFrame frame)
        {
            float half = frame.Length * 0.5f;
            float baseY = Dim.WallHeight;

            // Gable studs, cut to the underside of the rafter line.
            for (float u = Dim.StudSpacing; u < frame.Length - 0.05f; u += Dim.StudSpacing)
            {
                float worldOffset = u - half;                 // distance from the ridge line
                float topY = Dim.RoofUndersideAt(worldOffset);
                float h = topY - baseY;
                if (h < 0.05f)
                {
                    continue;
                }

                mb.AddBox(new Vector3(u, baseY + (h * 0.5f), Dim.StudDepth * 0.5f),
                          new Vector3(Dim.StudWidth, h, Dim.StudDepth), 0, 0.0025f);
            }

            // A king stud on the ridge line.
            float apex = Dim.RoofUndersideAt(0f);
            mb.AddBox(new Vector3(half, baseY + ((apex - baseY) * 0.5f), Dim.StudDepth * 0.5f),
                      new Vector3(Dim.StudWidth, apex - baseY, Dim.StudDepth), 0, 0.0025f);

            // The prism runs the full roof width, past the wall face, so the top
            // corners of the gable meet the roof with no daylight gap. It is taken
            // all the way up to the underside of the corrugation troughs rather than
            // to the rafter line: the rafters and purlins sit ~146 mm above that
            // line, and stopping short leaves an open slot the length of the rake.
            AddGablePrism(mb, frame.Length, baseY, Dim.StudDepth, Dim.BoardThickness, 1, Dim.WallThickness);
            AddGablePrism(mb, frame.Length, baseY, Dim.StudDepth + Dim.BoardThickness,
                          Dim.CladdingThickness, 2, Dim.WallThickness);
        }

        /// <summary>A triangular prism following the roof line, with real thickness.</summary>
        private static void AddGablePrism(MeshBuilder mb, float length, float baseY, float zOffset,
                                          float thickness, int submesh, float extend)
        {
            float half = length * 0.5f;
            float x0 = -extend;
            float x1 = length + extend;
            float apexY = RoofBuilder.TroughY(0f);
            float edgeY = RoofBuilder.TroughY(half + extend);

            float z0 = zOffset;
            float z1 = zOffset + thickness;

            Vector3 l0 = new Vector3(x0, baseY, z0);
            Vector3 r0 = new Vector3(x1, baseY, z0);
            Vector3 a0 = new Vector3(half, apexY, z0);
            Vector3 le0 = new Vector3(x0, edgeY, z0);
            Vector3 re0 = new Vector3(x1, edgeY, z0);

            Vector3 l1 = new Vector3(x0, baseY, z1);
            Vector3 r1 = new Vector3(x1, baseY, z1);
            Vector3 a1 = new Vector3(half, apexY, z1);
            Vector3 le1 = new Vector3(x0, edgeY, z1);
            Vector3 re1 = new Vector3(x1, edgeY, z1);

            // Inner and outer faces as a quad plus the apex triangle, so the shape is
            // a proper pentagon rather than a bare triangle sitting on the plate.
            mb.AddQuad(l0, r0, re0, le0, submesh);
            mb.AddTriangle(le0, re0, a0, submesh);

            mb.AddQuad(r1, l1, le1, re1, submesh);
            mb.AddTriangle(re1, le1, a1, submesh);

            // Sloping edges and the bottom.
            mb.AddQuad(le0, a0, a1, le1, submesh);
            mb.AddQuad(a0, re0, re1, a1, submesh);
            mb.AddQuad(l1, l0, le0, le1, submesh);
            mb.AddQuad(r0, r1, re1, re0, submesh);
            mb.AddQuad(l0, l1, r1, r0, submesh);
        }

        /// <summary>
        /// Vertical corner boards. Every timber building has them - they cover the
        /// end grain where two runs of cladding meet - and they read strongly at eye
        /// level, so they are worth the handful of triangles.
        /// </summary>
        private static void BuildCornerTrim(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_CornerTrim", 1);

            float outerX = Dim.HalfWidth + Dim.WallThickness;
            float outerZ = Dim.HalfLength + Dim.WallThickness;
            const float TrimWidth = 0.090f;
            const float TrimThickness = 0.019f;

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    // One board on each face, lapped at the corner.
                    mb.AddBox(new Vector3(sx * (outerX + (TrimThickness * 0.5f)),
                                          Dim.WallHeight * 0.5f,
                                          sz * (outerZ - (TrimWidth * 0.5f))),
                              new Vector3(TrimThickness, Dim.WallHeight, TrimWidth), 0, 0.003f);

                    mb.AddBox(new Vector3(sx * (outerX + TrimThickness - (TrimWidth * 0.5f)),
                                          Dim.WallHeight * 0.5f,
                                          sz * (outerZ + (TrimThickness * 0.5f))),
                              new Vector3(TrimWidth, Dim.WallHeight, TrimThickness), 0, 0.003f);
                }
            }

            ctx.CreateObject("Shed_CornerTrim", mb, new[] { Keys.Weatherboard },
                parent, Vector3.zero, Quaternion.identity);
        }

        // =====================================================================
        // Exterior ground - only ever seen through the window and the eaves gaps
        // =====================================================================

        public static void BuildExterior(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Exterior_Ground", 2);
            mb.UvScale = 0.5f; // one tile per two metres reads better at distance

            const float GroundSize = 60f;
            float groundY = -Dim.FloorStructureDepth - 0.20f;

            mb.AddBox(new Vector3(0f, groundY - 0.25f, 0f),
                      new Vector3(GroundSize, 0.5f, GroundSize), 0);

            // Gravel apron and a step outside the door.
            float apronZ = -(Dim.HalfLength + Dim.WallThickness) - 0.9f;
            mb.AddBox(new Vector3(Dim.DoorCentreX, groundY + 0.02f, apronZ),
                      new Vector3(2.2f, 0.06f, 1.8f), 1, 0.01f);

            ctx.CreateObject("Exterior_Ground", mb, new[] { Keys.Grass, Keys.Gravel },
                parent, Vector3.zero, Quaternion.identity);

            BuildDoorStep(ctx, parent, groundY);
        }

        private static void BuildDoorStep(BuildContext ctx, Transform parent, float groundY)
        {
            MeshBuilder mb = new MeshBuilder("Exterior_DoorStep", 1);

            float outerZ = Dim.HalfLength + Dim.WallThickness;
            float stepTop = -0.020f;
            float stepHeight = stepTop - groundY;

            mb.AddBox(new Vector3(0f, groundY + (stepHeight * 0.5f), 0f),
                      new Vector3(1.10f, stepHeight, 0.45f), 0, 0.010f);

            ctx.CreateObject("Exterior_DoorStep", mb, new[] { Keys.Concrete },
                parent, new Vector3(Dim.DoorCentreX, 0f, -(outerZ + 0.225f)),
                Quaternion.identity, BuildContext.ColliderKind.Box);
        }
    }
}
