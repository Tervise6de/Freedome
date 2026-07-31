using UnityEngine;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The three holes in the shell and everything that fills them: the door and its
    /// ironmongery, the window, and the wall vent.
    ///
    /// These get more detail than anything else in the building because they are
    /// what the player walks up to. A door is the classic place where a first-person
    /// environment gives itself away - too wide, too tall, a leaf with no thickness,
    /// hardware that floats. This one is an 820 x 2040 ledged-and-braced leaf, 45 mm
    /// thick, hung in a lined reveal with a 6 mm gap under it.
    /// </summary>
    public static class OpeningsBuilder
    {
        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildDoor(ctx, parent);
            BuildWindow(ctx, parent);
            BuildVent(ctx, parent);
        }

        // =====================================================================
        // Door - entrance wall, z = -3.0
        // =====================================================================

        private static void BuildDoor(BuildContext ctx, Transform parent)
        {
            // Local space: origin on the interior wall face at the door centre.
            // +X across the opening, +Y up, +Z into the room (so the wall is at -Z).
            float halfRough = Dim.DoorRoughWidth * 0.5f;
            float jamb = Dim.DoorJambThickness;
            float wall = Dim.WallThickness;
            float clearHeight = Dim.DoorRoughHeight - jamb;

            // --- lining, threshold and stops ---------------------------------
            MeshBuilder lining = new MeshBuilder("Door_Lining", 2);

            float jambCentreX = halfRough - (jamb * 0.5f);
            float revealZ = -wall * 0.5f;

            foreach (int s in new[] { -1, 1 })
            {
                lining.AddBox(new Vector3(s * jambCentreX, clearHeight * 0.5f, revealZ),
                              new Vector3(jamb, clearHeight, wall), 0, 0.003f);
            }

            lining.AddBox(new Vector3(0f, clearHeight + (jamb * 0.5f), revealZ),
                          new Vector3(Dim.DoorRoughWidth, jamb, wall), 0, 0.003f);

            // Hardwood threshold, weathered outward.
            lining.AddBox(new Vector3(0f, 0.009f, revealZ),
                          new Vector3(Dim.DoorRoughWidth, 0.018f, wall), 0, 0.004f);

            // Door stops: a 12 mm bead the leaf closes against.
            const float StopSize = 0.012f;
            float stopZ = -0.0455f - (StopSize * 0.5f);
            foreach (int s in new[] { -1, 1 })
            {
                lining.AddBox(new Vector3(s * (jambCentreX - (jamb * 0.5f) - (StopSize * 0.5f)),
                                          clearHeight * 0.5f, stopZ),
                              new Vector3(StopSize, clearHeight, StopSize), 0, 0.002f);
            }
            lining.AddBox(new Vector3(0f, clearHeight - (StopSize * 0.5f), stopZ),
                          new Vector3(Dim.DoorLeafWidth, StopSize, StopSize), 0, 0.002f);

            // Reveal trim covering the joint between lining and framing.
            foreach (int s in new[] { -1, 1 })
            {
                lining.AddBox(new Vector3(s * (halfRough + (Dim.DoorTrimWidth * 0.5f) - 0.02f),
                                          clearHeight * 0.5f, Dim.DoorTrimThickness * 0.5f),
                              new Vector3(Dim.DoorTrimWidth, clearHeight + Dim.DoorTrimWidth,
                                          Dim.DoorTrimThickness), 0, 0.003f);
            }
            lining.AddBox(new Vector3(0f, clearHeight + jamb + (Dim.DoorTrimWidth * 0.5f) - 0.02f,
                                      Dim.DoorTrimThickness * 0.5f),
                          new Vector3(Dim.DoorRoughWidth + (Dim.DoorTrimWidth * 2f) - 0.04f,
                                      Dim.DoorTrimWidth, Dim.DoorTrimThickness), 0, 0.003f);

            ctx.CreateObject("Door_Lining", lining, new[] { Keys.StructuralPine, Keys.StructuralPine },
                parent, new Vector3(Dim.DoorCentreX, 0f, -Dim.HalfLength),
                Quaternion.identity, BuildContext.ColliderKind.Mesh);

            // --- the leaf -----------------------------------------------------
            BuildDoorLeaf(ctx, parent);
        }

        private static void BuildDoorLeaf(BuildContext ctx, Transform parent)
        {
            // 0 timber, 1 zinc hardware
            MeshBuilder mb = new MeshBuilder("Door_Leaf", 2);

            const float BoardThickness = 0.025f;
            const float LedgeThickness = 0.020f;
            const float BottomGap = 0.006f;

            float w = Dim.DoorLeafWidth;
            float h = Dim.DoorLeafHeight;
            float halfW = w * 0.5f;

            // Boards run vertically. Local z: 0 is the inside face of the boards, the
            // leaf extends to -BoardThickness on the outside.
            const int BoardCount = 6;
            float boardW = w / BoardCount;
            for (int i = 0; i < BoardCount; i++)
            {
                float cx = -halfW + (boardW * (i + 0.5f));
                mb.AddBox(new Vector3(cx, (h * 0.5f) + BottomGap, -BoardThickness * 0.5f),
                          new Vector3(boardW - 0.0015f, h, BoardThickness), 0, 0.0025f);
            }

            // Three ledges on the inside face.
            float[] ledgeY = { 0.220f, 1.030f, 1.840f };
            const float LedgeHeight = 0.095f;
            foreach (float ly in ledgeY)
            {
                mb.AddBox(new Vector3(0f, ly + BottomGap, LedgeThickness * 0.5f),
                          new Vector3(w - 0.02f, LedgeHeight, LedgeThickness), 0, 0.003f);
            }

            // Two braces, running up from the hinge side so they work in compression.
            for (int i = 0; i < 2; i++)
            {
                float y0 = ledgeY[i] + (LedgeHeight * 0.5f) + BottomGap;
                float y1 = ledgeY[i + 1] - (LedgeHeight * 0.5f) + BottomGap;
                float x0 = -halfW + 0.06f;
                float x1 = halfW - 0.06f;

                Vector2 d = new Vector2(x1 - x0, y1 - y0);
                float len = d.magnitude;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

                mb.AddBox(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, LedgeThickness * 0.5f),
                          new Vector3(len, 0.090f, LedgeThickness),
                          Quaternion.Euler(0f, 0f, angle), 0, 0.003f);
            }

            // Coach bolts through the boards into the ledges - the heads are what you
            // actually notice on a door like this.
            foreach (float ly in ledgeY)
            {
                for (int i = 0; i < BoardCount; i++)
                {
                    float cx = -halfW + (boardW * (i + 0.5f));
                    mb.AddCylinder(new Vector3(cx, ly + BottomGap, LedgeThickness + 0.004f),
                                   0.0075f, 0.0075f, 0.008f, 8, 1,
                                   Quaternion.Euler(90f, 0f, 0f));
                }
            }

            BuildDoorHardware(mb, halfW, BottomGap, BoardThickness, LedgeThickness);

            // The leaf sits back from the cladding face in its rebate, closed.
            float leafZ = -Dim.WallThickness + 0.0225f + BoardThickness;

            GameObject leaf = ctx.CreateObject("Door_Leaf", mb,
                new[] { Keys.StructuralPine, Keys.Hardware },
                parent, new Vector3(Dim.DoorCentreX, 0f, -Dim.HalfLength + leafZ),
                Quaternion.identity, BuildContext.ColliderKind.Box);

            // The door is scenery in this milestone. A dedicated blocker keeps the
            // player inside regardless of how the leaf's own collider is shaped.
            if (leaf != null)
            {
                BuildContext.CreateBlocker("Door_Blocker", leaf.transform,
                    new Vector3(0f, Dim.DoorLeafHeight * 0.5f, 0.02f),
                    new Vector3(Dim.DoorRoughWidth, Dim.DoorRoughHeight, 0.10f));
            }
        }

        /// <summary>
        /// Rim lock, lever handle, barrel bolt and the exterior tee hinges. This is
        /// the mechanical locking area the design notes reserve for a future escape
        /// route; nothing here is interactive and nothing is highlighted.
        /// </summary>
        private static void BuildDoorHardware(MeshBuilder mb, float halfW, float bottomGap,
                                              float boardThickness, float ledgeThickness)
        {
            const int Metal = 1;
            float latchX = halfW - 0.075f;
            float y = Dim.DoorHandleHeight + bottomGap;

            // Rim lock case on the inside face.
            mb.AddBox(new Vector3(latchX, y, ledgeThickness + 0.019f),
                      new Vector3(0.115f, 0.145f, 0.038f), Metal, 0.004f);

            // Spindle and lever.
            mb.AddCylinder(new Vector3(latchX, y, ledgeThickness + 0.042f),
                           0.0165f, 0.0165f, 0.014f, 14, Metal, Quaternion.Euler(90f, 0f, 0f));
            mb.AddBox(new Vector3(latchX - 0.048f, y, ledgeThickness + 0.052f),
                      new Vector3(0.105f, 0.020f, 0.018f), Metal, 0.004f);

            // Keyhole escutcheon below the lever.
            mb.AddCylinder(new Vector3(latchX, y - 0.052f, ledgeThickness + 0.040f),
                           0.011f, 0.011f, 0.005f, 12, Metal, Quaternion.Euler(90f, 0f, 0f));

            // Striking plate side of the latch, poking out of the leaf edge.
            mb.AddBox(new Vector3(halfW + 0.006f, y, ledgeThickness * 0.5f),
                      new Vector3(0.014f, 0.022f, 0.016f), Metal, 0.002f);

            // Barrel bolt higher up.
            float boltY = 1.720f + bottomGap;
            mb.AddBox(new Vector3(latchX, boltY, ledgeThickness + 0.012f),
                      new Vector3(0.130f, 0.038f, 0.014f), Metal, 0.003f);
            mb.AddCylinder(new Vector3(latchX + 0.045f, boltY, ledgeThickness + 0.020f),
                           0.008f, 0.008f, 0.090f, 10, Metal, Quaternion.Euler(0f, 0f, 90f));

            // Exterior tee hinges on the hinge stile.
            float[] hingeY = { 0.220f, 1.030f, 1.840f };
            foreach (float hy in hingeY)
            {
                float hz = -boardThickness - 0.004f;
                // Strap across the leaf.
                mb.AddBox(new Vector3(-halfW + 0.150f, hy + bottomGap, hz),
                          new Vector3(0.290f, 0.038f, 0.005f), Metal, 0.002f);
                // Knuckle at the edge.
                mb.AddCylinder(new Vector3(-halfW - 0.006f, hy + bottomGap, hz),
                               0.011f, 0.011f, 0.070f, 10, Metal, Quaternion.identity);
            }
        }

        // =====================================================================
        // Window - workbench wall, x = +2.0
        // =====================================================================

        private static void BuildWindow(BuildContext ctx, Transform parent)
        {
            // 0 timber, 1 glass, 2 galvanised flashing
            MeshBuilder mb = new MeshBuilder("Window_Assembly", 3);

            float halfW = Dim.WindowWidth * 0.5f;
            float sill = Dim.WindowSillHeight;
            float head = Dim.WindowHeadHeight;
            float fw = Dim.WindowFrameWidth;
            float fd = Dim.WindowFrameDepth;
            float wall = Dim.WallThickness;

            float frameZ = 0.060f; // set back from the interior face into the reveal

            // Lining boards around the reveal.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (halfW - 0.010f), (sill + head) * 0.5f, wall * 0.5f),
                          new Vector3(0.020f, head - sill, wall), 0, 0.003f);
            }
            mb.AddBox(new Vector3(0f, head - 0.010f, wall * 0.5f),
                      new Vector3(Dim.WindowWidth, 0.020f, wall), 0, 0.003f);

            // Window frame proper.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (halfW - (fw * 0.5f) - 0.020f), (sill + head) * 0.5f,
                                      frameZ + (fd * 0.5f)),
                          new Vector3(fw, head - sill - 0.04f, fd), 0, 0.003f);
            }
            mb.AddBox(new Vector3(0f, head - (fw * 0.5f) - 0.020f, frameZ + (fd * 0.5f)),
                      new Vector3(Dim.WindowWidth - 0.04f, fw, fd), 0, 0.003f);
            mb.AddBox(new Vector3(0f, sill + (fw * 0.5f) + 0.010f, frameZ + (fd * 0.5f)),
                      new Vector3(Dim.WindowWidth - 0.04f, fw, fd), 0, 0.003f);

            // Central glazing bar, so it reads as two lights rather than one big pane.
            float glassTop = head - fw - 0.020f;
            float glassBottom = sill + fw + 0.010f;
            mb.AddBox(new Vector3(0f, (glassTop + glassBottom) * 0.5f, frameZ + (fd * 0.5f)),
                      new Vector3(0.030f, glassTop - glassBottom, fd), 0, 0.002f);

            // Glass, one pane each side of the bar.
            float paneWidth = ((Dim.WindowWidth - 0.04f - (fw * 2f)) * 0.5f) - 0.015f;
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * ((paneWidth * 0.5f) + 0.017f),
                                      (glassTop + glassBottom) * 0.5f,
                                      frameZ + (fd * 0.5f)),
                          new Vector3(paneWidth, glassTop - glassBottom, Dim.GlassThickness), 1);
            }

            // Glazing beads holding the panes in on the room side.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * ((paneWidth * 0.5f) + 0.017f), glassBottom + 0.006f,
                                      frameZ + 0.012f),
                          new Vector3(paneWidth, 0.012f, 0.012f), 0, 0.002f);
                mb.AddBox(new Vector3(s * ((paneWidth * 0.5f) + 0.017f), glassTop - 0.006f,
                                      frameZ + 0.012f),
                          new Vector3(paneWidth, 0.012f, 0.012f), 0, 0.002f);
            }

            // Interior sill board. A shed window always ends up as a shelf.
            const float SillDepth = 0.165f;
            mb.AddBox(new Vector3(0f, sill - 0.0125f, (SillDepth * 0.5f) - Dim.WindowSillProjection),
                      new Vector3(Dim.WindowWidth + 0.100f, 0.025f, SillDepth), 0, 0.004f);

            // Exterior architrave and a galvanised head flashing.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (halfW + 0.035f), (sill + head) * 0.5f, wall + 0.009f),
                          new Vector3(0.070f, head - sill + 0.140f, 0.018f), 0, 0.003f);
            }
            mb.AddBox(new Vector3(0f, head + 0.035f, wall + 0.009f),
                      new Vector3(Dim.WindowWidth + 0.140f, 0.070f, 0.018f), 0, 0.003f);
            mb.AddBox(new Vector3(0f, head + 0.076f, wall + 0.020f),
                      new Vector3(Dim.WindowWidth + 0.180f, 0.008f, 0.055f), 2, 0.002f);

            ctx.CreateObject("Window_Assembly", mb,
                new[] { Keys.StructuralPine, Keys.Glass, Keys.Galvanised },
                parent, new Vector3(Dim.HalfWidth, 0f, Dim.WindowCentreZ),
                Quaternion.Euler(0f, 90f, 0f), BuildContext.ColliderKind.Mesh);
        }

        // =====================================================================
        // Wall vent - utility wall, z = +3.0
        // =====================================================================

        /// <summary>
        /// A galvanised louvre vent with insect mesh behind it. It is a real hole in
        /// the wall, so it puts a small amount of genuine daylight high on the utility
        /// wall - which is both good lighting and an honest piece of construction.
        /// </summary>
        private static void BuildVent(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Vent_Louvre", 2);

            float halfW = Dim.VentWidth * 0.5f;
            float halfH = Dim.VentHeight * 0.5f;
            float wall = Dim.WallThickness;

            // Timber lining to the opening.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (halfW - 0.009f), 0f, wall * 0.5f),
                          new Vector3(0.018f, Dim.VentHeight, wall), 0, 0.002f);
            }
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, s * (halfH - 0.009f), wall * 0.5f),
                          new Vector3(Dim.VentWidth - 0.036f, 0.018f, wall), 0, 0.002f);
            }

            // Insect mesh, sitting just inside the louvre.
            mb.AddBox(new Vector3(0f, 0f, wall - 0.030f),
                      new Vector3(Dim.VentWidth - 0.030f, Dim.VentHeight - 0.030f, 0.002f), 1);

            // Louvre frame and blades on the outside face.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (halfW + 0.014f), 0f, wall + 0.012f),
                          new Vector3(0.028f, Dim.VentHeight + 0.056f, 0.024f), 1, 0.002f);
                mb.AddBox(new Vector3(0f, s * (halfH + 0.014f), wall + 0.012f),
                          new Vector3(Dim.VentWidth + 0.056f, 0.028f, 0.024f), 1, 0.002f);
            }

            const int Blades = 5;
            for (int i = 0; i < Blades; i++)
            {
                float t = (i + 0.5f) / Blades;
                float y = Mathf.Lerp(halfH - 0.012f, -halfH + 0.012f, t);
                mb.AddBox(new Vector3(0f, y, wall + 0.010f),
                          new Vector3(Dim.VentWidth - 0.010f, 0.030f, 0.004f),
                          Quaternion.Euler(-35f, 0f, 0f), 1, 0.001f);
            }

            ctx.CreateObject("Vent_Louvre", mb, new[] { Keys.StructuralPine, Keys.Galvanised },
                parent, new Vector3(Dim.VentCentreX, Dim.VentCentreY, Dim.HalfLength),
                Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }
    }
}
