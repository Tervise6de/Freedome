using UnityEngine;
using Freedome.Interaction;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The built-in furniture: workbench, pegboard and tool rack on the +X wall,
    /// and the shelving unit on the -X wall.
    ///
    /// Everything is sized from what it has to do. The bench top is 900 mm because
    /// that is a comfortable height to work at standing up; the shelving is 450 mm
    /// deep because that takes a paint tin and a bucket; the pegboard starts at
    /// 1000 mm because that is where the bench stops being useful.
    /// </summary>
    public static class FixturesBuilder
    {
        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildWorkbench(ctx, parent);
            BuildPegboard(ctx, parent);
            BuildToolRack(ctx, parent);
            BuildShelving(ctx, parent);
        }

        // =====================================================================
        // Workbench
        // =====================================================================

        private static void BuildWorkbench(BuildContext ctx, Transform parent)
        {
            // 0 pine, 1 bench ply, 2 dark steel, 3 zinc hardware
            MeshBuilder mb = new MeshBuilder("Workbench", 4);

            float d = Dim.BenchDepth;      // 0.60 out from the wall
            float l = Dim.BenchLength;     // 2.40 along Z
            float h = Dim.BenchHeight;     // 0.90 top surface
            float tt = Dim.BenchTopThickness;
            float leg = Dim.BenchLegSize;

            float halfD = d * 0.5f;
            float halfL = l * 0.5f;
            float topCentreY = h - (tt * 0.5f);

            // --- top: two laminated plies with a hardwood front lipping -------
            mb.AddBox(new Vector3(0f, topCentreY, 0f), new Vector3(d, tt, l), 1, 0.004f);
            mb.AddBox(new Vector3(-halfD - 0.012f, h - 0.030f, 0f),
                      new Vector3(0.024f, 0.060f, l), 0, 0.004f);

            // --- legs ----------------------------------------------------------
            float legTop = h - tt;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(sx * (halfD - (leg * 0.5f) - 0.020f),
                                          legTop * 0.5f,
                                          sz * (halfL - (leg * 0.5f) - 0.040f)),
                              new Vector3(leg, legTop, leg), 0, 0.004f);
                }
            }

            // --- rails under the top and a lower shelf -------------------------
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * (halfD - 0.037f), legTop - 0.055f, 0f),
                          new Vector3(0.035f, 0.090f, l - 0.16f), 0, 0.003f);
            }
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, legTop - 0.055f, sz * (halfL - 0.058f)),
                          new Vector3(d - 0.14f, 0.090f, 0.035f), 0, 0.003f);
            }

            mb.AddBox(new Vector3(0f, 0.200f, 0f),
                      new Vector3(d - 0.10f, Dim.BoardThickness, l - 0.20f), 0, 0.003f);

            // --- drawer bank at the far end -----------------------------------
            Vector3 benchOrigin = new Vector3(Dim.BenchFrontX + halfD, 0f, Dim.BenchStartZ + halfL);
            BuildDrawerBank(ctx, parent, mb, new Vector3(0f, 0f, halfL - 0.34f), d, legTop, benchOrigin);

            // --- small cupboard at the near end -------------------------------
            BuildCupboard(mb, new Vector3(0f, 0f, -halfL + 0.36f), d, legTop);

            // --- bench vice ----------------------------------------------------
            BuildVice(mb, new Vector3(-halfD + 0.010f, h, -halfL + 0.42f));

            ctx.CreateObject("Workbench", mb,
                new[] { Keys.StructuralPine, Keys.BenchPly, Keys.DarkSteel, Keys.Hardware },
                parent,
                new Vector3(Dim.BenchFrontX + halfD, 0f, Dim.BenchStartZ + halfL),
                Quaternion.identity, BuildContext.ColliderKind.Box);
        }

        // ---------------------------------------------------------------------
        // Drawer carcass, as numbers rather than locals
        //
        // The two things that go in a drawer used to be positioned by eye against
        // these, and both were wrong. Anything inside a closed drawer is invisible
        // until somebody opens it, so the only way to be sure is to derive the
        // placement from the box and then check it.
        // ---------------------------------------------------------------------

        public const float DrawerBankWidth = 0.62f;   // along Z

        private static float DrawerCarcassBottom => 0.26f;

        private static float DrawerCarcassHeight =>
            (Dim.BenchHeight - Dim.BenchTopThickness - 0.10f) - DrawerCarcassBottom;

        private static float DrawerCarcassDepth => Dim.BenchDepth - 0.09f;

        public static float DrawerFrontHeight => (DrawerCarcassHeight - 0.014f) * 0.5f;

        /// <summary>Clear height inside the box, front to back.</summary>
        public static float DrawerBoxHeight => DrawerFrontHeight - 0.020f;

        public static float DrawerBoxDepth => DrawerCarcassDepth - 0.030f;

        /// <summary>Centre of the box, along the drawer's own +X (into the carcass).</summary>
        public static float DrawerBoxMidX => (DrawerBoxDepth * 0.5f) + 0.012f;

        public static float DrawerInnerWidth => DrawerBankWidth - 0.030f;

        /// <summary>Thickness of the board a drawer's contents actually sit on.</summary>
        public static float DrawerBottomThickness => Dim.BoardThickness * 0.6f;

        /// <summary>
        /// The carcass stays in the shared bench mesh; the two drawers come out as
        /// their own objects because they slide. Each is a real box - bottom, sides,
        /// back and front - rather than a face, so that what is inside it reads as
        /// being inside something.
        /// </summary>
        private static void BuildDrawerBank(BuildContext ctx, Transform parent, MeshBuilder mb,
                                            Vector3 centre, float depth, float legTop,
                                            Vector3 benchOrigin)
        {
            const float BankWidth = DrawerBankWidth;  // along Z
            float carcassTop = legTop - 0.10f;
            float carcassBottom = DrawerCarcassBottom;
            float height = carcassTop - carcassBottom;
            float d = depth - 0.09f;

            mb.Push(centre + new Vector3(0f, 0f, 0f));

            // Carcass sides and back.
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, carcassBottom + (height * 0.5f), sz * (BankWidth * 0.5f)),
                          new Vector3(d, height, Dim.BoardThickness), 0, 0.002f);
            }

            mb.Pop();

            for (int i = 0; i < 2; i++)
            {
                float fh = (height - 0.014f) * 0.5f;
                float fy = carcassBottom + (fh * 0.5f) + (i * (fh + 0.010f)) + 0.002f;

                // The front's centre, in world space: bench origin, plus the bank's
                // offset inside the bench, plus the front's offset inside the bank.
                Vector3 frontCentre = benchOrigin + centre +
                                      new Vector3(-(d * 0.5f) - 0.009f, fy, 0f);

                BuildDrawer(ctx, parent, i, frontCentre, d, fh, BankWidth);
            }
        }

        private static void BuildDrawer(BuildContext ctx, Transform parent, int index,
                                        Vector3 frontCentre, float carcassDepth,
                                        float frontHeight, float bankWidth)
        {
            // 0 pine, 1 hardware
            MeshBuilder mb = new MeshBuilder($"Drawer_{index}", 2);

            float innerW = bankWidth - 0.030f;
            float boxDepth = carcassDepth - 0.030f;
            float boxHeight = frontHeight - 0.020f;

            // Front, standing at local x = 0 so the pivot is the face the player sees.
            mb.AddBox(Vector3.zero, new Vector3(0.018f, frontHeight - 0.006f, bankWidth - 0.012f),
                      0, 0.003f);

            // Turned timber knob.
            mb.AddCylinder(new Vector3(-0.021f, 0f, 0f), 0.016f, 0.020f, 0.030f, 12, 0,
                           Quaternion.Euler(0f, 0f, 90f));

            // The box behind it: bottom, two sides, back.
            float mid = (boxDepth * 0.5f) + 0.012f;
            mb.AddBox(new Vector3(mid, -(boxHeight * 0.5f), 0f),
                      new Vector3(boxDepth, Dim.BoardThickness * 0.6f, innerW), 0, 0.002f);
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(mid, 0f, sz * (innerW * 0.5f)),
                          new Vector3(boxDepth, boxHeight, Dim.BoardThickness * 0.6f), 0, 0.002f);
            }
            mb.AddBox(new Vector3(mid + (boxDepth * 0.5f), 0f, 0f),
                      new Vector3(Dim.BoardThickness * 0.6f, boxHeight, innerW), 0, 0.002f);

            GameObject drawer = ctx.CreateObject($"Drawer_{index}", mb,
                new[] { Keys.StructuralPine, Keys.Hardware },
                parent, frontCentre, Quaternion.identity,
                BuildContext.ColliderKind.Box, isStatic: false);

            if (drawer == null)
            {
                return;
            }

            // Pulls out into the room, which for the workbench wall is -X.
            SlidingPart slide = drawer.AddComponent<SlidingPart>();
            slide.Configure("Open the drawer", "Close the drawer", Vector3.left, 0.30f, 0.55f);

            if (index == 0)
            {
                // The top drawer has swollen shut. A damp shed does this, and it is
                // why the offcut is worth picking up before the screwdriver is.
                slide.RequireForcing("Swollen shut - it will not pull");

                GameObject edge = new GameObject("Drawer_Edge");
                edge.transform.SetParent(drawer.transform, false);
                edge.transform.localPosition = new Vector3(-0.030f, 0f, 0f);
                BuildContext.MarkMovable(edge);

                BoxCollider reach = edge.AddComponent<BoxCollider>();
                reach.size = new Vector3(0.05f, frontHeight, bankWidth - 0.012f);

                ToolGatedFixture fixture = edge.AddComponent<ToolGatedFixture>();
                fixture.Configure("offcut",
                                  "Swollen shut - it will not pull",
                                  "Lever the drawer open",
                                  "It moves freely now",
                                  ToolGatedFixture.Effect.ForceDrawer);
            }

            BuildDrawerContents(ctx, drawer.transform, index);
        }

        /// <summary>
        /// One thing lying in a drawer, in that drawer's own local frame.
        /// </summary>
        public struct DrawerItem
        {
            public string Name;
            public int Drawer;

            /// <summary>Centre of the object, local to the drawer it rides in.</summary>
            public Vector3 Centre;

            /// <summary>Half-extents of what was actually built, for the tests.</summary>
            public Vector3 HalfSize;
        }

        /// <summary>
        /// What is in the drawers, and where.
        ///
        /// Written down rather than left inline because both of these were placed by
        /// eye against the carcass and both were wrong in opposite directions - the
        /// screwdriver sat 7 mm into the drawer bottom and the folding rule floated
        /// 6 mm over it. Nothing inside a closed drawer can be seen until somebody
        /// opens it, so the placement has to come from the box and then be checked
        /// against it.
        /// </summary>
        public static readonly DrawerItem[] DrawerContents =
        {
            new DrawerItem
            {
                Name = "screwdriver", Drawer = 0,
                Centre = new Vector3(DrawerBoxMidX - 0.05f, RestOn(0.0165f), -0.10f),
                HalfSize = new Vector3(0.100f, 0.0165f, 0.0165f),
            },
            new DrawerItem
            {
                Name = "folding rule", Drawer = 1,
                Centre = new Vector3(DrawerBoxMidX - 0.03f, RestOn(0.0063f), 0.06f),
                HalfSize = new Vector3(0.076f, 0.0063f, 0.014f),
            },
        };

        /// <summary>
        /// The height an object of a given half-height rests at inside a drawer: on
        /// the top face of the bottom board, not through it and not above it.
        /// </summary>
        private static float RestOn(float halfHeight) =>
            -(DrawerBoxHeight * 0.5f) + (DrawerBottomThickness * 0.5f) + halfHeight;

        private static DrawerItem ItemFor(int drawer)
        {
            foreach (DrawerItem item in DrawerContents)
            {
                if (item.Drawer == drawer)
                {
                    return item;
                }
            }

            return default;
        }

        /// <summary>
        /// What is in the drawers. A screwdriver in one, a folding rule in the
        /// other - both carryable, both kinematic until first handled so they ride
        /// the drawer instead of being shoved through its bottom.
        /// </summary>
        private static void BuildDrawerContents(BuildContext ctx, Transform drawer, int index)
        {
            DrawerItem item = ItemFor(index);

            if (index == 0)
            {
                // The screwdriver. It is in a drawer because that is where a
                // screwdriver lives, not because it is hidden - the drawer opens
                // whether or not anybody ever needs what is in it.
                //
                // Turned handle, ferrule, shank, tip: four parts, because a square
                // block with a wire in the end of it does not read as a tool at any
                // range you can pick it up from.
                const float HandleRadius = 0.0165f;
                MeshBuilder mb = new MeshBuilder("Carry_Screwdriver", 2);

                // Handle, waisted the way a moulded one is: fat at the palm, narrow
                // at the ferrule.
                mb.AddCylinder(new Vector3(-0.082f, 0f, 0f), 0.0115f, HandleRadius, 0.028f, 12, 0,
                               Quaternion.Euler(0f, 0f, 90f));
                mb.AddCylinder(new Vector3(-0.050f, 0f, 0f), HandleRadius, HandleRadius * 0.86f,
                               0.036f, 12, 0, Quaternion.Euler(0f, 0f, 90f));
                mb.AddCylinder(new Vector3(-0.022f, 0f, 0f), HandleRadius * 0.86f, 0.0092f,
                               0.020f, 12, 0, Quaternion.Euler(0f, 0f, 90f));

                // Ferrule, then the shank, then a flat tip spread wider than the bar.
                mb.AddCylinder(new Vector3(-0.008f, 0f, 0f), 0.0092f, 0.0092f, 0.014f, 12, 1,
                               Quaternion.Euler(0f, 0f, 90f));
                mb.AddCylinder(new Vector3(0.043f, 0f, 0f), 0.0042f, 0.0042f, 0.088f, 10, 1,
                               Quaternion.Euler(0f, 0f, 90f));
                mb.AddBox(new Vector3(0.093f, 0f, 0f), new Vector3(0.014f, 0.0022f, 0.0090f), 1);

                Attach(ctx, mb, "Carry_Screwdriver", new[] { Keys.PaintedRed, Keys.DarkSteel },
                       drawer, item.Centre,
                       "screwdriver", 0.2f, new Vector3(0.22f, -0.18f, 0.36f));
            }
            else
            {
                // A boxwood rule, folded once and left slightly open, which is how
                // one ends up in a drawer. Two leaves and a brass hinge read as a
                // rule; one lath reads as a piece of scrap.
                const float LeafThickness = 0.006f;
                const float LeafLength = 0.152f;
                MeshBuilder mb = new MeshBuilder("Carry_FoldingRule", 2);

                foreach (int sy in new[] { -1, 1 })
                {
                    // Splayed a few degrees about the hinge, one leaf lying on the
                    // other's edge - a folded rule never quite shuts flat.
                    mb.AddBox(new Vector3((LeafLength * 0.5f) - 0.012f,
                                          sy * LeafThickness * 0.55f, 0f),
                              new Vector3(LeafLength, LeafThickness, 0.026f),
                              Quaternion.Euler(0f, sy * 5f, 0f), 0, 0.0012f);
                }

                mb.AddCylinder(new Vector3(-0.012f, 0f, 0f), 0.0075f, 0.0075f, 0.028f, 10, 1,
                               Quaternion.Euler(90f, 0f, 0f));

                Attach(ctx, mb, "Carry_FoldingRule", new[] { Keys.StructuralPine, Keys.Hardware },
                       drawer, item.Centre,
                       "folding rule", 0.2f, new Vector3(0.24f, -0.20f, 0.40f));
            }
        }

        private static void Attach(BuildContext ctx, MeshBuilder mb, string name, string[] materials,
                                   Transform drawer, Vector3 localPosition, string displayName,
                                   float mass, Vector3 holdOffset)
        {
            GameObject go = ctx.CreateObject(name, mb, materials, drawer, localPosition,
                                             Quaternion.identity,
                                             BuildContext.ColliderKind.Box, isStatic: false);
            if (go == null)
            {
                return;
            }

            Rigidbody body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            Carryable carry = go.AddComponent<Carryable>();
            carry.Configure(displayName, holdOffset, Vector3.zero);
            carry.SetRestingInContainer(true);
        }

        private static void BuildCupboard(MeshBuilder mb, Vector3 centre, float depth, float legTop)
        {
            const float Width = 0.66f;
            float top = legTop - 0.10f;
            float bottom = 0.26f;
            float height = top - bottom;
            float d = depth - 0.09f;

            mb.Push(centre);

            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, bottom + (height * 0.5f), sz * (Width * 0.5f)),
                          new Vector3(d, height, Dim.BoardThickness), 0, 0.002f);
            }

            // Two doors, one left slightly ajar because nobody ever closes both.
            for (int i = 0; i < 2; i++)
            {
                int sz = i == 0 ? -1 : 1;
                float doorWidth = (Width * 0.5f) - 0.008f;
                float ajar = i == 1 ? 6f : 0f;

                mb.Push(new Vector3(-(d * 0.5f) - 0.009f, bottom + (height * 0.5f),
                                    sz * (Width * 0.5f - 0.004f)),
                        Quaternion.Euler(0f, sz * ajar, 0f));
                mb.AddBox(new Vector3(0f, 0f, -sz * doorWidth * 0.5f),
                          new Vector3(0.018f, height - 0.012f, doorWidth), 0, 0.003f);
                mb.AddCylinder(new Vector3(-0.020f, 0f, -sz * (doorWidth - 0.045f)),
                               0.014f, 0.017f, 0.026f, 12, 0, Quaternion.Euler(0f, 0f, 90f));
                mb.Pop();
            }

            mb.Pop();
        }

        /// <summary>
        /// An engineer's vice. It is the single most recognisable object on a
        /// workbench, so it is worth building properly: fixed jaw casting, sliding
        /// jaw, screw, tommy bar and four bolts through the bench top.
        /// </summary>
        private static void BuildVice(MeshBuilder mb, Vector3 mountPoint)
        {
            const int Steel = 2;
            const int Zinc = 3;

            mb.Push(mountPoint);

            // Base plate bolted to the bench.
            mb.AddBox(new Vector3(0.075f, 0.014f, 0f), new Vector3(0.190f, 0.028f, 0.140f), Steel, 0.004f);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    mb.AddCylinder(new Vector3(0.075f + (sx * 0.065f), 0.032f, sz * 0.048f),
                                   0.010f, 0.010f, 0.010f, 8, Zinc, Quaternion.identity);
                }
            }

            // Fixed jaw and body casting.
            mb.AddBox(new Vector3(0.085f, 0.070f, 0f), new Vector3(0.130f, 0.090f, 0.110f), Steel, 0.005f);
            mb.AddBox(new Vector3(0.012f, 0.088f, 0f), new Vector3(0.040f, 0.126f, 0.130f), Steel, 0.004f);

            // Sliding jaw, left open a little.
            mb.AddBox(new Vector3(-0.060f, 0.088f, 0f), new Vector3(0.040f, 0.126f, 0.130f), Steel, 0.004f);
            mb.AddBox(new Vector3(-0.055f, 0.048f, 0f), new Vector3(0.070f, 0.036f, 0.070f), Steel, 0.003f);

            // Screw and tommy bar.
            mb.AddCylinder(new Vector3(-0.090f, 0.088f, 0f), 0.014f, 0.014f, 0.110f, 12, Steel,
                           Quaternion.Euler(0f, 0f, 90f));
            mb.AddCylinder(new Vector3(-0.148f, 0.088f, 0f), 0.009f, 0.009f, 0.230f, 10, Zinc,
                           Quaternion.Euler(90f, 0f, 0f));
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(-0.148f, 0.088f, sz * 0.115f), 0.013f, 0.013f, 0.018f, 10,
                               Zinc, Quaternion.Euler(90f, 0f, 0f));
            }

            mb.Pop();
        }

        // =====================================================================
        // Pegboard and tool rack
        // =====================================================================

        private static void BuildPegboard(BuildContext ctx, Transform parent)
        {
            // 0 hardboard, 1 pine battens, 2 zinc hooks and tools
            MeshBuilder mb = new MeshBuilder("Pegboard", 3);

            const float Width = 0.700f;  // along Z
            float height = Dim.PegboardHeight;
            float t = Dim.PegboardThickness;

            // Battens hold the board off the studs so hooks have somewhere to go.
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0.008f, 0f, sz * (Width * 0.5f - 0.025f)),
                          new Vector3(0.016f, height, 0.045f), 1, 0.002f);
            }

            // The hole grid is baked into the texture at a 25 mm pitch, so the board
            // must be mapped square to itself. Forcing V up keeps the rows level
            // regardless of whether the panel is taller or wider.
            mb.GrainOverride = Vector3.up;
            mb.AddBox(new Vector3(0.016f + (t * 0.5f), 0f, 0f),
                      new Vector3(t, height, Width), 0, 0.001f);
            mb.GrainOverride = null;

            BuildPegboardTools(mb, 0.016f + t);

            ctx.CreateObject("Pegboard", mb,
                new[] { Keys.Pegboard, Keys.StructuralPine, Keys.DarkSteel },
                parent,
                new Vector3(Dim.HalfWidth - 0.001f, Dim.PegboardBottomY + (height * 0.5f), -0.250f),
                Quaternion.Euler(0f, 90f, 0f));
        }

        /// <summary>
        /// Ordinary hand tools hung on the board: two screwdrivers, pliers, a
        /// hacksaw, a hammer and a small square. Hung with a bit of variation in
        /// spacing and tilt, because a perfectly ranked tool wall looks staged.
        /// </summary>
        private static void BuildPegboardTools(MeshBuilder mb, float faceX)
        {
            const int Metal = 2;
            const int Timber = 1;

            // Two hooks and a hammer.
            mb.Push(new Vector3(faceX, 0.230f, -0.230f), Quaternion.Euler(0f, 0f, -4f));
            mb.AddCylinder(new Vector3(0.055f, 0f, 0f), 0.017f, 0.017f, 0.115f, 10, Timber,
                           Quaternion.Euler(0f, 0f, 90f));
            mb.AddBox(new Vector3(0.035f, 0.085f, 0f), new Vector3(0.032f, 0.036f, 0.115f), Metal, 0.004f);
            mb.AddBox(new Vector3(0.035f, 0.085f, -0.075f), new Vector3(0.028f, 0.026f, 0.048f), Metal, 0.003f);
            mb.Pop();

            // Hacksaw.
            mb.Push(new Vector3(faceX, 0.140f, 0.140f), Quaternion.Euler(0f, 0f, 3f));
            mb.AddBox(new Vector3(0.028f, 0.140f, 0f), new Vector3(0.016f, 0.016f, 0.300f), Metal, 0.002f);
            mb.AddBox(new Vector3(0.020f, 0.060f, 0f), new Vector3(0.006f, 0.022f, 0.290f), Metal, 0.001f);
            mb.AddBox(new Vector3(0.028f, 0.100f, -0.170f), new Vector3(0.024f, 0.110f, 0.050f), Timber, 0.005f);
            mb.Pop();

            // Screwdrivers and pliers, hung along the bottom.
            float[] driverZ = { -0.290f, -0.238f, -0.185f };
            float[] driverLen = { 0.190f, 0.230f, 0.165f };
            for (int i = 0; i < driverZ.Length; i++)
            {
                mb.Push(new Vector3(faceX, -0.290f, driverZ[i]), Quaternion.Euler(0f, 0f, (i - 1) * 3f));
                mb.AddCylinder(new Vector3(0.020f, -0.045f, 0f), 0.014f, 0.011f, 0.090f, 10, Timber,
                               Quaternion.identity);
                mb.AddCylinder(new Vector3(0.020f, -0.045f - (driverLen[i] * 0.5f), 0f),
                               0.004f, 0.004f, driverLen[i], 8, Metal, Quaternion.identity);
                mb.Pop();
            }

            mb.Push(new Vector3(faceX, -0.250f, 0.230f), Quaternion.Euler(0f, 0f, -6f));
            mb.AddBox(new Vector3(0.018f, -0.055f, 0f), new Vector3(0.014f, 0.110f, 0.026f), Metal, 0.003f);
            mb.AddBox(new Vector3(0.018f, -0.135f, 0.012f), new Vector3(0.014f, 0.070f, 0.014f), Metal, 0.002f);
            mb.AddBox(new Vector3(0.018f, -0.135f, -0.012f), new Vector3(0.014f, 0.070f, 0.014f), Metal, 0.002f);
            mb.Pop();

            // Try square hung flat.
            mb.Push(new Vector3(faceX, 0.330f, 0.290f), Quaternion.Euler(0f, 0f, 0f));
            mb.AddBox(new Vector3(0.014f, -0.090f, 0f), new Vector3(0.004f, 0.180f, 0.032f), Metal, 0.001f);
            mb.AddBox(new Vector3(0.016f, 0.000f, 0.030f), new Vector3(0.018f, 0.026f, 0.090f), Timber, 0.003f);
            mb.Pop();
        }

        private static void BuildToolRack(BuildContext ctx, Transform parent)
        {
            // A short shelf with hooks on the far side of the window.
            MeshBuilder mb = new MeshBuilder("ToolRack", 2);

            const float Length = 0.600f;

            mb.AddBox(new Vector3(0.100f, 0f, 0f), new Vector3(0.200f, 0.019f, Length), 0, 0.003f);
            foreach (int sz in new[] { -1, 1 })
            {
                // Simple timber brackets.
                mb.AddBox(new Vector3(0.055f, -0.075f, sz * (Length * 0.5f - 0.070f)),
                          new Vector3(0.150f, 0.019f, 0.035f),
                          Quaternion.Euler(0f, 0f, 42f), 0, 0.002f);
            }

            // Three cup hooks under the shelf.
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(-0.20f, 0.20f, i / 2f);
                mb.AddCylinder(new Vector3(0.120f, -0.022f, z), 0.0035f, 0.0035f, 0.030f, 8, 1,
                               Quaternion.identity);
            }

            ctx.CreateObject("ToolRack", mb, new[] { Keys.StructuralPine, Keys.Hardware },
                parent, new Vector3(Dim.HalfWidth - 0.001f, 1.960f, 1.420f),
                Quaternion.Euler(0f, 90f, 0f));
        }

        // =====================================================================
        // Shelving on the storage wall
        // =====================================================================

        private static void BuildShelving(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("ShelvingUnit", 1);

            float len = Dim.ShelfUnitLength;
            float depth = Dim.ShelfUnitDepth;
            float height = Dim.ShelfUnitHeight;
            float up = Dim.ShelfUprightSize;
            float halfL = len * 0.5f;
            float halfD = depth * 0.5f;

            // Uprights: three pairs, front and back.
            float[] uprightZ = { -halfL + (up * 0.5f), 0f, halfL - (up * 0.5f) };
            foreach (float z in uprightZ)
            {
                foreach (int sx in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(sx * (halfD - (up * 0.5f)), height * 0.5f, z),
                              new Vector3(up, height, 0.035f), 0, 0.003f);
                }
            }

            // Shelves: two boards per level with a gap, on cleats.
            foreach (float sh in Dim.ShelfHeights)
            {
                float y = sh - (Dim.BoardThickness * 0.5f);
                for (int b = 0; b < 2; b++)
                {
                    float boardDepth = (depth * 0.5f) - 0.012f;
                    float cx = -halfD + (boardDepth * 0.5f) + 0.006f + (b * (boardDepth + 0.010f));
                    mb.AddBox(new Vector3(cx, y, 0f),
                              new Vector3(boardDepth, Dim.BoardThickness, len - 0.010f), 0, 0.002f);
                }

                // Cleats carrying the boards.
                foreach (int sx in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(sx * (halfD - 0.030f), sh - 0.031f, 0f),
                              new Vector3(0.030f, 0.038f, len - 0.150f), 0, 0.002f);
                }
            }

            // Diagonal brace across the back, the way a site-built shelf is stiffened.
            float braceLen = Mathf.Sqrt((len * len) + (height * height));
            float braceAngle = Mathf.Atan2(height, len) * Mathf.Rad2Deg;
            mb.AddBox(new Vector3(halfD - 0.020f, height * 0.5f, 0f),
                      new Vector3(0.016f, 0.075f, braceLen),
                      Quaternion.Euler(braceAngle, 0f, 0f), 0, 0.002f);

            ctx.CreateObject("ShelvingUnit", mb, new[] { Keys.ShelfBoard },
                parent,
                new Vector3(-Dim.HalfWidth + (depth * 0.5f), 0f,
                            Dim.ShelfUnitStartZ + (len * 0.5f)),
                Quaternion.identity, BuildContext.ColliderKind.Box);
        }
    }
}
