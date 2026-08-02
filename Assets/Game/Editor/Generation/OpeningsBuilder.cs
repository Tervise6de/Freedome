using UnityEngine;
using Freedome.Interaction;
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

            // The keep: the staple the rim lock's bolt shoots into, screwed to the
            // reveal of the latch jamb at the height of the case. Without it the
            // lock was a box on a door that fastened to nothing, which is the sort
            // of thing you only notice once you have stood in front of it trying to
            // get out. It clears the leaf because it sits on the room side of the
            // boards, past where the leaf swings.
            const int LiningMetal = 1;
            float keepX = jambCentreX - (jamb * 0.5f);
            float keepY = Dim.DoorHandleHeight + 0.006f;
            float keepZ = DoorLeafClosedZ + 0.042f;

            lining.AddBox(new Vector3(keepX - 0.007f, keepY, keepZ),
                          new Vector3(0.014f, 0.072f, 0.044f), LiningMetal, 0.002f);
            lining.AddBox(new Vector3(keepX - 0.019f, keepY, keepZ),
                          new Vector3(0.010f, 0.030f, 0.030f), LiningMetal, 0.002f);
            foreach (int sy in new[] { -1, 1 })
            {
                lining.AddCylinder(new Vector3(keepX - 0.001f, keepY + (sy * 0.026f), keepZ),
                                   0.0052f, 0.0040f, 0.004f, 10, LiningMetal,
                                   Quaternion.Euler(0f, 0f, 90f));
            }

            ctx.CreateObject("Door_Lining", lining, new[] { Keys.StructuralPine, Keys.Hardware },
                parent, new Vector3(Dim.DoorCentreX, 0f, -Dim.HalfLength),
                Quaternion.identity, BuildContext.ColliderKind.Mesh);

            // --- the leaf -----------------------------------------------------
            BuildDoorLeaf(ctx, parent);
        }

        /// <summary>
        /// World X of the door's hinge line. Exposed because a test that re-derives
        /// this from ShedDimensions is a tautology - it would pass with the door hung
        /// off its centre, which is exactly the fault worth catching.
        /// </summary>
        public static float DoorHingeX => Dim.DoorCentreX - (Dim.DoorLeafWidth * 0.5f);

        /// <summary>The leaf's own offset from that hinge line.</summary>
        public static float DoorLeafLocalX => Dim.DoorLeafWidth * 0.5f;

        /// <summary>
        /// How far the leaf swings, in the sign HingedPart applies about +Y.
        ///
        /// Positive is outward here, and the sign is not obvious: Unity rotates
        /// left-handed, so a point at +X moves to -Z under a positive angle. The
        /// door sits in the -Z wall, which makes -Z away from the room. This was
        /// -92 for one commit, which swung the leaf into the room while the comment
        /// beside it claimed the opposite.
        /// </summary>
        public const float DoorOpenAngleDegrees = 92f;

        /// <summary>
        /// Casement swing. Positive is outward for the same left-handed reason the
        /// door's is, but the window assembly is rotated 90 degrees about Y, so the
        /// sash's local -X is what moves: outward is still positive here.
        /// </summary>
        public const float CasementOpenAngleDegrees = 72f;

        /// <summary>
        /// The rim lock case, in the door leaf's own local frame. Shared by the
        /// geometry, the interaction collider and the tests, because three copies
        /// of 1.020 is how the collider ends up somewhere the lock is not.
        /// </summary>
        public static Vector3 RimLockLocalCentre =>
            new Vector3((Dim.DoorLeafWidth * 0.5f) - 0.075f,
                        Dim.DoorHandleHeight + 0.006f,
                        0.020f + 0.019f);

        public static readonly Vector3 RimLockCaseSize = new Vector3(0.115f, 0.145f, 0.038f);

        /// <summary>Half the sash's width, and its hinge position in the window's
        /// own local frame. Exposed so the tests read the built values.</summary>
        public static float CasementPaneWidth =>
            ((Dim.WindowWidth - 0.04f - (Dim.WindowFrameWidth * 2f)) * 0.5f) - 0.015f;

        public static float CasementSashHalfWidth => (CasementPaneWidth * 0.5f) + 0.032f;

        public static float CasementHingeLocalX =>
            (CasementPaneWidth * 0.5f) + 0.017f + CasementSashHalfWidth;

        /// <summary>Z of the closed leaf, relative to the inner face of the wall.</summary>
        public static float DoorLeafClosedZ => -Dim.WallThickness + 0.0225f + 0.025f;

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
                // 3 mm rather than the default hairline. A ledged-and-braced door is
                // six separate boards, and the shadow line between them is most of
                // what says so - too small a chamfer and it reads as one flat slab.
                mb.AddBox(new Vector3(cx, (h * 0.5f) + BottomGap, -BoardThickness * 0.5f),
                          new Vector3(boardW - 0.0030f, h, BoardThickness), 0, 0.0045f);
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

            // The hinge, not the leaf, is what rotates. HingedPart only ever writes a
            // local rotation, so the leaf needs a parent sitting on the hinge line
            // with the leaf offset half its width away from it.
            GameObject hinge = ctx.CreateGroup("Door_Hinge", parent);
            hinge.transform.localPosition =
                new Vector3(DoorHingeX, 0f, -Dim.HalfLength + leafZ);
            BuildContext.MarkMovable(hinge);

            GameObject leaf = ctx.CreateObject("Door_Leaf", mb,
                new[] { Keys.StructuralPine, Keys.Hardware },
                hinge.transform, new Vector3(DoorLeafLocalX, 0f, 0f),
                Quaternion.identity, BuildContext.ColliderKind.Box, isStatic: false);

            if (leaf != null)
            {
                // Fills the opening while the door is shut and switches off the moment
                // it starts to swing, so a standing-open door is walkable. The play
                // area boundary is what keeps the player near the shed once outside.
                GameObject blockerGo = BuildContext.CreateBlocker("Door_Blocker", leaf.transform,
                    new Vector3(0f, Dim.DoorLeafHeight * 0.5f, 0.02f),
                    new Vector3(Dim.DoorRoughWidth, Dim.DoorRoughHeight, 0.10f));
                BuildContext.MarkMovable(blockerGo);

                // Swings outward, away from the room, which is how a shed door hung on
                // exterior tee hinges actually opens. DoorSwingsOutwardThroughItsWholeArc
                // is what keeps the sign honest.
                HingedPart part = hinge.AddComponent<HingedPart>();
                // Held by the rim lock until its case comes off the inside face.
                part.Gate(true, false, "The rim lock is fast - no key");
                BuildRimLockFixture(ctx, leaf.transform);
                part.Configure("Open the door", "Close the door", Vector3.up,
                               DoorOpenAngleDegrees, 150f,
                               blockerGo != null ? blockerGo.GetComponent<Collider>() : null);
            }
        }

        /// <summary>
        /// Rim lock, lever handle, barrel bolt and the exterior tee hinges. This is
        /// the mechanical locking area the design notes reserve for a future escape
        /// route; nothing here is interactive and nothing is highlighted.
        /// </summary>
        /// <summary>
        /// The rim lock case, as something you can take off.
        ///
        /// A rim lock is screwed to the *inside* face of the door - that is what
        /// makes it a rim lock rather than a mortice - so the fixing screws are on
        /// the player's side of a locked door. Nothing was moved or exposed to make
        /// this true; it is where BuildDoorHardware has drawn the case since the
        /// environment milestone.
        /// </summary>
        private static void BuildRimLockFixture(BuildContext ctx, Transform leaf)
        {
            GameObject go = new GameObject("Door_RimLockCase");
            go.transform.SetParent(leaf, false);
            go.transform.localPosition = RimLockLocalCentre;
            BuildContext.MarkMovable(go);

            // Grown past the case so it is comfortable to aim at, but still smaller
            // than the stile it sits on.
            BoxCollider reach = go.AddComponent<BoxCollider>();
            reach.size = RimLockCaseSize + new Vector3(0.055f, 0.055f, 0.030f);

            ToolGatedFixture fixture = go.AddComponent<ToolGatedFixture>();
            fixture.Configure("screwdriver",
                              "The lock case is screwed to the door",
                              "Take the lock case off",
                              "The lock case is off",
                              ToolGatedFixture.Effect.RemoveLock);

            // The case is its own object, not part of the leaf mesh, so that taking
            // it off can actually take it off. Unscrewing a lock and watching it stay
            // screwed to the door was the one place in the route where doing the
            // right thing changed nothing you could see.
            GameObject shell = ctx.CreateObject("Door_RimLockShell", BuildRimLockMesh(),
                new[] { Keys.Hardware }, go.transform, Vector3.zero, Quaternion.identity,
                BuildContext.ColliderKind.None, isStatic: false);

            if (shell != null)
            {
                BuildContext.MarkMovable(shell);
                fixture.Removes(shell);
            }
        }

        /// <summary>
        /// The case, its lever, escutcheon and four fixing screws, in the case's own
        /// local frame. Everything here is on the room side of the leaf, which is
        /// what a rim lock is.
        /// </summary>
        private static MeshBuilder BuildRimLockMesh()
        {
            MeshBuilder mb = new MeshBuilder("Door_RimLockShell", 1);

            mb.AddBox(Vector3.zero, RimLockCaseSize, 0, 0.004f);

            // Spindle and lever.
            mb.AddCylinder(new Vector3(0f, 0f, 0.023f), 0.0165f, 0.0165f, 0.014f, 14, 0,
                           Quaternion.Euler(90f, 0f, 0f));
            mb.AddBox(new Vector3(-0.048f, 0f, 0.033f), new Vector3(0.105f, 0.020f, 0.018f),
                      0, 0.004f);

            // Four countersunk fixing screws, one near each corner of the case. A
            // player who cannot see fixings has no reason to think the case comes off.
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sy in new[] { -1, 1 })
                {
                    mb.AddCylinder(new Vector3(sx * 0.042f, sy * 0.056f, 0.019f),
                                   0.0055f, 0.0042f, 0.004f, 10, 0,
                                   Quaternion.Euler(90f, 0f, 0f));
                }
            }

            // Keyhole escutcheon below the lever.
            mb.AddCylinder(new Vector3(0f, -0.052f, 0.021f), 0.011f, 0.011f, 0.005f, 12, 0,
                           Quaternion.Euler(90f, 0f, 0f));

            return mb;
        }

        private static void BuildDoorHardware(MeshBuilder mb, float halfW, float bottomGap,
                                              float boardThickness, float ledgeThickness)
        {
            const int Metal = 1;
            float latchX = halfW - 0.075f;
            float y = Dim.DoorHandleHeight + bottomGap;

            // The rim lock case, its lever, escutcheon and fixing screws are not
            // here: they are their own object under Door_RimLockCase, because the
            // one thing that has to happen when you unscrew them is that they stop
            // being on the door. See BuildRimLockMesh.

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

            // Only the left light is glazed into the frame. The right one is an
            // opening casement, built separately below - which is how a two-light
            // shed window is normally made: one fixed, one that opens.
            float paneWidth = ((Dim.WindowWidth - 0.04f - (fw * 2f)) * 0.5f) - 0.015f;
            mb.AddBox(new Vector3(-((paneWidth * 0.5f) + 0.017f),
                                  (glassTop + glassBottom) * 0.5f,
                                  frameZ + (fd * 0.5f)),
                      new Vector3(paneWidth, glassTop - glassBottom, Dim.GlassThickness), 1);

            // Glazing beads holding the fixed pane in on the room side.
            mb.AddBox(new Vector3(-((paneWidth * 0.5f) + 0.017f), glassBottom + 0.006f,
                                  frameZ + 0.012f),
                      new Vector3(paneWidth, 0.012f, 0.012f), 0, 0.002f);
            mb.AddBox(new Vector3(-((paneWidth * 0.5f) + 0.017f), glassTop - 0.006f,
                                  frameZ + 0.012f),
                      new Vector3(paneWidth, 0.012f, 0.012f), 0, 0.002f);

            // Interior sill board. A shed window always ends up as a shelf.
            // It sits ON the framing sill trimmer rather than flush with it: the
            // trimmer's top face is also at y = sill, and two coplanar faces
            // z-fight into a speckled mess right where the player leans in.
            const float SillDepth = 0.165f;
            const float SillBoardThickness = 0.025f;
            mb.AddBox(new Vector3(0f, sill + (SillBoardThickness * 0.5f),
                                  (SillDepth * 0.5f) - Dim.WindowSillProjection),
                      new Vector3(Dim.WindowWidth + 0.100f, SillBoardThickness, SillDepth), 0, 0.004f);

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

            Vector3 assemblyPosition = new Vector3(Dim.HalfWidth, 0f, Dim.WindowCentreZ);
            Quaternion assemblyRotation = Quaternion.Euler(0f, 90f, 0f);

            ctx.CreateObject("Window_Assembly", mb,
                new[] { Keys.StructuralPine, Keys.Glass, Keys.Galvanised },
                parent, assemblyPosition, assemblyRotation, BuildContext.ColliderKind.Mesh);

            BuildWindowCasement(ctx, parent, assemblyPosition, assemblyRotation,
                                paneWidth, glassBottom, glassTop, frameZ);
        }

        /// <summary>
        /// The opening light: a sash of stiles and rails around one pane, hung on the
        /// outer stile and swinging outward.
        ///
        /// Built in the window assembly's own frame - local +X runs along the wall,
        /// local +Z points out of the building - so the hinge group carries the
        /// assembly's rotation and the sash only ever needs a local rotation.
        /// </summary>
        private static void BuildWindowCasement(BuildContext ctx, Transform parent,
                                                Vector3 assemblyPosition,
                                                Quaternion assemblyRotation,
                                                float paneWidth, float glassBottom,
                                                float glassTop, float frameZ)
        {
            const float SashSection = 0.032f;
            const float SashDepth = 0.042f;

            float lightCentreX = (paneWidth * 0.5f) + 0.017f;
            float halfW = (paneWidth * 0.5f) + SashSection;
            float halfH = ((glassTop - glassBottom) * 0.5f) + SashSection;
            float centreY = (glassTop + glassBottom) * 0.5f;
            float z = frameZ + (SashDepth * 0.5f);

            // 0 timber, 1 glass, 2 hardware
            MeshBuilder mb = new MeshBuilder("Window_Casement", 3);

            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * (halfW - (SashSection * 0.5f)), 0f, 0f),
                          new Vector3(SashSection, halfH * 2f, SashDepth), 0, 0.002f);
            }
            foreach (int sy in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, sy * (halfH - (SashSection * 0.5f)), 0f),
                          new Vector3((halfW - SashSection) * 2f, SashSection, SashDepth), 0, 0.002f);
            }

            mb.AddBox(Vector3.zero,
                      new Vector3(paneWidth, glassTop - glassBottom, Dim.GlassThickness), 1);

            // A stay and a simple lever catch on the stile that swings.
            mb.AddBox(new Vector3(-(halfW - 0.012f), 0f, -(SashDepth * 0.5f) - 0.008f),
                      new Vector3(0.070f, 0.016f, 0.010f), 2, 0.002f);

            // Hinge on the outer stile, sash offset back toward the glazing bar.
            Vector3 hingeLocal = new Vector3(lightCentreX + halfW, centreY, z);
            GameObject hinge = ctx.CreateGroup("Window_CasementHinge", parent);
            hinge.transform.localPosition = assemblyPosition + (assemblyRotation * hingeLocal);
            hinge.transform.localRotation = assemblyRotation;
            BuildContext.MarkMovable(hinge);

            GameObject sash = ctx.CreateObject("Window_Casement", mb,
                new[] { Keys.StructuralPine, Keys.Glass, Keys.Hardware },
                hinge.transform, new Vector3(-halfW, 0f, 0f), Quaternion.identity,
                BuildContext.ColliderKind.Box, isStatic: false);

            if (sash != null)
            {
                HingedPart part = hinge.AddComponent<HingedPart>();
                part.Configure("Open the window", "Close the window", Vector3.up,
                               CasementOpenAngleDegrees, 90f, null);
            }
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
