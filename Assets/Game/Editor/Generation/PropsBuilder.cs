using UnityEngine;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Prop dressing: what is on the shelves, what is on the bench, and the handful
    /// of larger objects on the floor.
    ///
    /// The placement rule for the whole room is that a person has to be able to walk
    /// from the door to the utility wall and stand at the bench without turning
    /// sideways. Everything else is arranged around that. Objects are set down at
    /// slightly different angles and are not aligned to each other, because a shed
    /// where every tin faces front looks like a shop display.
    /// </summary>
    public static class PropsBuilder
    {
        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildShelfContents(ctx, parent);
            BuildBenchTop(ctx, parent);
            BuildLawnmower(ctx, parent);
            BuildWheelbarrow(ctx, parent);
            BuildSawhorse(ctx, parent);
            BuildStool(ctx, parent);
            BuildFloorClutter(ctx, parent);
        }

        // Submesh layout shared by the dressing meshes.
        private const int Timber = 0;
        private const int Metal = 1;
        private const int Painted = 2;
        private const int Plastic = 3;
        private const int Fabric = 4;
        private const int Card = 5;

        private static readonly string[] DressingMaterials =
        {
            Keys.ShelfBoard, Keys.Hardware, Keys.PaintedCream, Keys.BucketPlastic,
            Keys.Tarpaulin, Keys.Cardboard,
        };

        // =====================================================================
        // Shelving contents
        // =====================================================================

        private static void BuildShelfContents(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("ShelfContents", 6);

            float wallX = -Dim.HalfWidth;
            float front = wallX + 0.22f;   // roughly the middle of the 450 mm shelf
            float back = wallX + 0.15f;

            float[] y = Dim.ShelfHeights;

            // --- bottom shelf: buckets, a toolbox and pots ---------------------
            PropLibrary.Bucket(mb, new Vector3(front, y[0], -0.92f), 0.290f, 0.135f, Plastic, Metal, 22f);
            PropLibrary.Bucket(mb, new Vector3(front - 0.02f, y[0], -0.55f), 0.270f, 0.128f, Plastic, Metal, -40f);
            PropLibrary.Toolbox(mb, new Vector3(front - 0.01f, y[0], 0.18f), Painted, Metal, 6f);
            PropLibrary.PlantPotStack(mb, new Vector3(front, y[0], 0.86f), 6, Plastic, 15f);

            // --- second shelf: paint and a watering can ------------------------
            float[] tinZ = { -1.02f, -0.86f, -0.70f, -0.52f };
            float[] tinH = { 0.175f, 0.175f, 0.130f, 0.175f };
            for (int i = 0; i < tinZ.Length; i++)
            {
                PropLibrary.PaintTin(mb, new Vector3(back + (i % 2 == 0 ? 0.02f : 0.06f), y[1], tinZ[i]),
                                     tinH[i], 0.072f, Painted, Metal, i * 37f, i == 2);
            }

            PropLibrary.CardboardBox(mb, new Vector3(front - 0.01f, y[1], 0.05f),
                                     new Vector3(0.300f, 0.220f, 0.260f), Card, -8f, true);
            PropLibrary.WateringCan(mb, new Vector3(front - 0.03f, y[1], 0.78f), Metal, 130f);

            // --- third shelf: tarpaulin, jars and offcuts -----------------------
            PropLibrary.FoldedTarpaulin(mb, new Vector3(front - 0.02f, y[2], -0.82f),
                                        new Vector3(0.360f, 0.140f, 0.480f), Fabric, 4f);

            for (int i = 0; i < 3; i++)
            {
                PropLibrary.FixingsJar(mb, new Vector3(back + 0.04f + (i * 0.03f), y[2], -0.18f + (i * 0.10f)),
                                       0.115f, 0.045f, Plastic, Metal);
            }

            PropLibrary.CardboardBox(mb, new Vector3(front, y[2], 0.42f),
                                     new Vector3(0.260f, 0.190f, 0.300f), Card, 11f);

            for (int i = 0; i < 4; i++)
            {
                PropLibrary.Timber(mb, new Vector3(back + 0.05f, y[2] + 0.021f + (i * 0.020f), 0.95f),
                                   new Vector3(0.180f, 0.019f, 0.420f - (i * 0.05f)),
                                   Quaternion.Euler(0f, i * 2.5f, 0f), Timber);
            }

            // --- top shelf: a sack, two tins and a coil of rope -----------------
            PropLibrary.Sack(mb, new Vector3(front - 0.02f, y[3], -0.78f),
                             new Vector3(0.230f, 0.180f, 0.400f), Fabric, -6f);

            PropLibrary.PaintTin(mb, new Vector3(back + 0.05f, y[3], -0.20f), 0.175f, 0.072f, Painted, Metal, 12f);
            PropLibrary.PaintTin(mb, new Vector3(back + 0.08f, y[3], -0.04f), 0.130f, 0.060f, Painted, Metal, 55f);

            BuildRopeCoil(mb, new Vector3(front - 0.02f, y[3] + 0.045f, 0.62f));

            ctx.CreateObject("ShelfContents", mb, DressingMaterials,
                parent, Vector3.zero, Quaternion.identity);
        }

        private static void BuildRopeCoil(MeshBuilder mb, Vector3 centre)
        {
            const int Turns = 18;
            for (int i = 0; i < Turns; i++)
            {
                float a0 = (i / (float)Turns) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)Turns) * Mathf.PI * 2f;
                float r = 0.105f + (Mathf.Sin(i * 1.7f) * 0.006f);

                Vector3 p0 = centre + new Vector3(Mathf.Cos(a0) * r, (i % 2) * 0.006f, Mathf.Sin(a0) * r);
                Vector3 p1 = centre + new Vector3(Mathf.Cos(a1) * r, ((i + 1) % 2) * 0.006f, Mathf.Sin(a1) * r);
                Vector3 d = p1 - p0;

                mb.AddCylinder((p0 + p1) * 0.5f, 0.009f, 0.009f, d.magnitude, 6, Fabric,
                               Quaternion.FromToRotation(Vector3.up, d.normalized));
            }
        }

        // =====================================================================
        // Bench top
        // =====================================================================

        private static void BuildBenchTop(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("BenchTopItems", 6);

            float top = Dim.BenchHeight;
            float x = Dim.BenchFrontX + 0.30f; // mid-depth of the bench

            // A small parts organiser pushed against the wall.
            BuildPartsOrganiser(mb, new Vector3(Dim.HalfWidth - 0.130f, top, 1.520f));

            // Jars and tins of fixings.
            PropLibrary.FixingsJar(mb, new Vector3(Dim.HalfWidth - 0.150f, top, 1.180f), 0.120f, 0.048f,
                                   Plastic, Metal);
            PropLibrary.FixingsJar(mb, new Vector3(Dim.HalfWidth - 0.230f, top, 1.120f), 0.095f, 0.040f,
                                   Plastic, Metal);
            PropLibrary.PaintTin(mb, new Vector3(Dim.HalfWidth - 0.170f, top, 0.980f), 0.100f, 0.052f,
                                 Painted, Metal, 24f);

            // A hand plane sitting on its side, as it should be.
            mb.Push(new Vector3(x - 0.05f, top, 0.640f), Quaternion.Euler(0f, -16f, 0f));
            mb.AddBox(new Vector3(0f, 0.032f, 0f), new Vector3(0.062f, 0.064f, 0.245f), Metal, 0.005f);
            mb.AddBox(new Vector3(0f, 0.072f, -0.020f), new Vector3(0.030f, 0.026f, 0.110f), Timber, 0.006f);
            mb.AddBox(new Vector3(0f, 0.086f, 0.075f), new Vector3(0.024f, 0.070f, 0.030f),
                      Quaternion.Euler(-22f, 0f, 0f), Timber, 0.006f);
            mb.Pop();

            // Offcut being worked on, with a pencil beside it.
            PropLibrary.Timber(mb, new Vector3(x - 0.03f, top + 0.010f, 0.130f),
                               new Vector3(0.185f, 0.019f, 0.560f), Quaternion.Euler(0f, 6f, 0f), Timber);
            mb.AddCylinder(new Vector3(x + 0.10f, top + 0.024f, 0.020f), 0.004f, 0.004f, 0.150f, 6,
                           Painted, Quaternion.Euler(0f, 0f, 90f));

            // A mug. Small human details do more for believability than more clutter.
            mb.Push(new Vector3(x + 0.13f, top, -0.180f));
            mb.AddCylinder(new Vector3(0f, 0.048f, 0f), 0.040f, 0.043f, 0.096f, 14, Painted,
                           Quaternion.identity);
            mb.AddCylinder(new Vector3(0.055f, 0.055f, 0f), 0.010f, 0.010f, 0.052f, 8, Painted,
                           Quaternion.Euler(0f, 0f, 90f));
            mb.Pop();

            // A roll of abrasive paper and a dust brush.
            mb.AddCylinder(new Vector3(x - 0.12f, top + 0.038f, -0.420f), 0.038f, 0.038f, 0.115f, 12,
                           Card, Quaternion.Euler(0f, 0f, 90f));
            mb.Push(new Vector3(x + 0.06f, top, -0.470f), Quaternion.Euler(0f, 28f, 0f));
            mb.AddBox(new Vector3(0f, 0.014f, 0f), new Vector3(0.048f, 0.028f, 0.180f), Timber, 0.005f);
            mb.AddBox(new Vector3(0f, -0.006f, -0.020f), new Vector3(0.042f, 0.032f, 0.110f), Fabric, 0.004f);
            mb.Pop();

            ctx.CreateObject("BenchTopItems", mb, DressingMaterials,
                parent, Vector3.zero, Quaternion.identity);
        }

        private static void BuildPartsOrganiser(MeshBuilder mb, Vector3 basePoint)
        {
            mb.Push(basePoint);

            const float W = 0.180f; // depth, out from the wall
            const float H = 0.230f;
            const float L = 0.310f;

            mb.AddBox(new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H, L), Painted, 0.005f);

            // Nine small transparent drawers in a 3 x 3 grid.
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float dy = 0.036f + (row * 0.070f);
                    float dz = -0.098f + (col * 0.098f);
                    float proud = (row == 1 && col == 2) ? 0.022f : 0f; // one left pulled out

                    mb.AddBox(new Vector3(-(W * 0.5f) - 0.006f - (proud * 0.5f), dy, dz),
                              new Vector3(0.012f + proud, 0.058f, 0.086f), Plastic, 0.003f);
                    mb.AddBox(new Vector3(-(W * 0.5f) - 0.014f - proud, dy, dz),
                              new Vector3(0.008f, 0.016f, 0.030f), Plastic, 0.002f);
                }
            }

            mb.Pop();
        }

        // =====================================================================
        // Floor objects
        // =====================================================================

        /// <summary>
        /// A petrol push mower. Roughly 0.5 m wide over the deck and 1.0 m to the
        /// handle grips, which is what makes it read at the right size next to a
        /// person.
        /// </summary>
        private static void BuildLawnmower(BuildContext ctx, Transform parent)
        {
            // 0 painted green, 1 dark steel, 2 rubber, 3 plastic
            MeshBuilder mb = new MeshBuilder("Lawnmower", 4);

            const float DeckW = 0.500f;
            const float DeckD = 0.440f;
            const float WheelR = 0.090f;
            float deckY = WheelR + 0.045f;

            // Deck with a rolled edge.
            mb.AddBox(new Vector3(0f, deckY, 0f), new Vector3(DeckW, 0.115f, DeckD), 0, 0.014f);
            mb.AddBox(new Vector3(0f, deckY - 0.062f, 0f), new Vector3(DeckW - 0.030f, 0.030f, DeckD - 0.030f),
                      0, 0.008f);

            // Wheels and their axles.
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    Vector3 hub = new Vector3(sx * ((DeckW * 0.5f) + 0.020f), WheelR, sz * (DeckD * 0.38f));
                    mb.AddCylinder(hub, WheelR, WheelR, 0.045f, 14, 2, Quaternion.Euler(0f, 0f, 90f));
                    mb.AddCylinder(hub, WheelR * 0.42f, WheelR * 0.42f, 0.050f, 10, 3,
                                   Quaternion.Euler(0f, 0f, 90f));
                }
            }

            // Engine, fuel tank, air filter and recoil starter.
            mb.AddBox(new Vector3(0f, deckY + 0.135f, 0.015f), new Vector3(0.235f, 0.155f, 0.230f), 1, 0.010f);
            mb.AddBox(new Vector3(0f, deckY + 0.235f, 0.010f), new Vector3(0.215f, 0.070f, 0.200f), 0, 0.012f);
            mb.AddCylinder(new Vector3(0.135f, deckY + 0.140f, 0.020f), 0.062f, 0.062f, 0.055f, 14, 1,
                           Quaternion.Euler(0f, 0f, 90f));
            mb.AddBox(new Vector3(-0.140f, deckY + 0.150f, 0.020f), new Vector3(0.060f, 0.085f, 0.120f), 3,
                      0.008f);
            mb.AddCylinder(new Vector3(0.030f, deckY + 0.150f, -0.135f), 0.016f, 0.016f, 0.045f, 8, 3,
                           Quaternion.Euler(90f, 0f, 0f));

            // Discharge chute.
            mb.AddBox(new Vector3(0.230f, deckY - 0.010f, -0.130f), new Vector3(0.120f, 0.090f, 0.150f),
                      Quaternion.Euler(0f, 34f, 0f), 0, 0.010f);

            // Handle: two tubes and a cross grip.
            const float HandleAngle = 52f;
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(sx * 0.215f, deckY + 0.290f, -0.470f),
                               0.014f, 0.014f, 0.980f, 8, 1,
                               Quaternion.Euler(HandleAngle, 0f, 0f));
            }
            mb.AddCylinder(new Vector3(0f, 1.010f, -0.760f), 0.014f, 0.014f, 0.430f, 8, 1,
                           Quaternion.Euler(0f, 0f, 90f));
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(sx * 0.170f, 1.010f, -0.760f), 0.019f, 0.019f, 0.100f, 10, 3,
                               Quaternion.Euler(0f, 0f, 90f));
            }

            // Starter cord looped back to the handle.
            mb.AddCylinder(new Vector3(0.030f, deckY + 0.400f, -0.290f), 0.003f, 0.003f, 0.560f, 6, 3,
                           Quaternion.Euler(28f, 0f, -3f));

            ctx.CreateObject("Lawnmower", mb,
                new[] { Keys.PaintedGreen, Keys.DarkSteel, Keys.Rubber, Keys.ElectricalPlastic },
                parent, new Vector3(1.100f, 0f, -1.550f), Quaternion.Euler(0f, 8f, 0f),
                BuildContext.ColliderKind.Box);
        }

        private static void BuildWheelbarrow(BuildContext ctx, Transform parent)
        {
            // 0 painted, 1 steel, 2 rubber, 3 timber handles
            MeshBuilder mb = new MeshBuilder("Wheelbarrow", 4);

            const float TrayW = 0.600f;
            const float TrayL = 0.860f;
            float trayY = 0.520f;

            // Tray: a base with four flared sides.
            mb.AddBox(new Vector3(0f, trayY - 0.090f, 0f), new Vector3(TrayW * 0.72f, 0.030f, TrayL * 0.80f),
                      0, 0.010f);
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * TrayW * 0.40f, trayY - 0.020f, 0f),
                          new Vector3(0.026f, 0.190f, TrayL * 0.86f),
                          Quaternion.Euler(0f, 0f, sx * 20f), 0, 0.008f);
            }
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, trayY - 0.020f, sz * TrayL * 0.44f),
                          new Vector3(TrayW * 0.80f, 0.190f, 0.026f),
                          Quaternion.Euler(-sz * 18f, 0f, 0f), 0, 0.008f);
            }

            // Chassis rails running under the tray and out to the handles.
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * 0.185f, trayY - 0.125f, 0.180f),
                          new Vector3(0.040f, 0.035f, 1.480f),
                          Quaternion.Euler(-9f, 0f, 0f), 3, 0.005f);
            }

            // Handle grips.
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(sx * 0.185f, trayY + 0.005f, 0.860f), 0.021f, 0.021f, 0.130f, 10,
                               2, Quaternion.Euler(81f, 0f, 0f));
            }

            // Wheel and its forks.
            mb.AddCylinder(new Vector3(0f, 0.160f, -0.640f), 0.160f, 0.160f, 0.085f, 18, 2,
                           Quaternion.Euler(0f, 0f, 90f));
            mb.AddCylinder(new Vector3(0f, 0.160f, -0.640f), 0.055f, 0.055f, 0.095f, 12, 1,
                           Quaternion.Euler(0f, 0f, 90f));
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * 0.070f, 0.270f, -0.610f), new Vector3(0.020f, 0.260f, 0.030f),
                          Quaternion.Euler(18f, 0f, sx * 7f), 1, 0.004f);
            }

            // Rear legs.
            foreach (int sx in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(sx * 0.190f, 0.200f, 0.330f), new Vector3(0.028f, 0.400f, 0.028f),
                          Quaternion.Euler(-6f, 0f, 0f), 1, 0.004f);
            }
            mb.AddBox(new Vector3(0f, 0.010f, 0.345f), new Vector3(0.420f, 0.024f, 0.028f), 1, 0.004f);

            ctx.CreateObject("Wheelbarrow", mb,
                new[] { Keys.PaintedGreen, Keys.DarkSteel, Keys.Rubber, Keys.StructuralPine },
                parent, new Vector3(-0.900f, 0f, 2.550f), Quaternion.Euler(0f, -90f, 0f),
                BuildContext.ColliderKind.Box);
        }

        private static void BuildSawhorse(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Sawhorse", 1);

            const float TopY = 0.640f;
            const float Length = 0.900f;
            const float Splay = 16f;

            mb.AddBox(new Vector3(0f, TopY - 0.045f, 0f), new Vector3(0.090f, 0.090f, Length), 0, 0.004f);

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(sx * 0.105f, (TopY - 0.090f) * 0.5f, sz * (Length * 0.40f)),
                              new Vector3(0.070f, TopY, 0.035f),
                              Quaternion.Euler(0f, 0f, sx * Splay), 0, 0.004f);
                }
            }

            // Gussets tying each pair of legs together.
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, 0.240f, sz * (Length * 0.40f)),
                          new Vector3(0.300f, 0.140f, 0.019f), 0, 0.003f);
            }

            // Saw cuts across the top rail, from being used properly.
            for (int i = 0; i < 4; i++)
            {
                mb.AddBox(new Vector3(0f, TopY - 0.004f, -0.28f + (i * 0.19f)),
                          new Vector3(0.092f, 0.010f, 0.004f), 0);
            }

            ctx.CreateObject("Sawhorse", mb, new[] { Keys.StructuralPine },
                parent, new Vector3(-0.600f, 0f, 1.350f), Quaternion.Euler(0f, 12f, 0f),
                BuildContext.ColliderKind.Box);
        }

        private static void BuildStool(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Stool", 1);

            const float SeatY = 0.470f;

            mb.AddBox(new Vector3(0f, SeatY - 0.014f, 0f), new Vector3(0.320f, 0.028f, 0.300f), 0, 0.006f);

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(sx * 0.118f, (SeatY - 0.028f) * 0.5f, sz * 0.108f),
                              new Vector3(0.038f, SeatY - 0.028f, 0.038f),
                              Quaternion.Euler(-sz * 6f, 0f, sx * 6f), 0, 0.003f);
                }
            }

            // Stretchers.
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, 0.150f, sz * 0.125f), new Vector3(0.250f, 0.028f, 0.022f), 0, 0.003f);
            }

            ctx.CreateObject("Stool", mb, new[] { Keys.StructuralPine },
                parent, new Vector3(1.120f, 0f, 2.100f), Quaternion.Euler(0f, 26f, 0f),
                BuildContext.ColliderKind.Box);
        }

        /// <summary>
        /// The things that end up on a shed floor: a bag of compost, a stack of
        /// offcuts leaning in the corner, and a couple of boards left flat.
        /// </summary>
        private static void BuildFloorClutter(BuildContext ctx, Transform parent)
        {
            // Compost bag against the storage wall.
            MeshBuilder sack = new MeshBuilder("CompostBag", 1);
            PropLibrary.Sack(sack, Vector3.zero, new Vector3(0.780f, 0.260f, 0.440f), 0);
            ctx.CreateObject("CompostBag", sack, new[] { Keys.SoilBag },
                parent, new Vector3(-1.480f, 0f, -2.150f), Quaternion.Euler(0f, 14f, 0f),
                BuildContext.ColliderKind.Box);

            // Offcuts leaning in the corner behind the door.
            MeshBuilder lean = new MeshBuilder("TimberOffcuts", 1);
            for (int i = 0; i < 7; i++)
            {
                float w = 0.045f + ((i % 3) * 0.045f);
                float len = 1.500f + ((i % 4) * 0.220f);
                float tilt = 9f + (i * 0.9f);

                lean.AddBox(new Vector3((i * 0.052f) - 0.150f, len * 0.48f, (i % 2) * 0.028f),
                            new Vector3(w, len, 0.026f + ((i % 2) * 0.019f)),
                            Quaternion.Euler(-tilt * 0.35f, i * 3f, tilt), 0, 0.002f);
            }
            ctx.CreateObject("TimberOffcuts", lean, new[] { Keys.StructuralPine },
                parent, new Vector3(-1.680f, 0f, -2.760f), Quaternion.Euler(0f, 28f, 0f),
                BuildContext.ColliderKind.Box);

            // Two boards left flat on the floor beside the sawhorse.
            MeshBuilder flat = new MeshBuilder("FloorBoardsLoose", 1);
            PropLibrary.Timber(flat, new Vector3(0f, 0.010f, 0f), new Vector3(0.190f, 0.020f, 1.350f),
                               Quaternion.Euler(0f, 5f, 0f), 0);
            PropLibrary.Timber(flat, new Vector3(0.075f, 0.030f, -0.080f), new Vector3(0.140f, 0.020f, 1.150f),
                               Quaternion.Euler(0f, -3f, 0f), 0);
            ctx.CreateObject("FloorBoardsLoose", flat, new[] { Keys.StructuralPine },
                parent, new Vector3(-1.260f, 0f, 0.980f), Quaternion.Euler(0f, 8f, 0f),
                BuildContext.ColliderKind.Box);

            // A galvanised bucket by the door with a hand brush in it.
            MeshBuilder bucket = new MeshBuilder("EntranceBucket", 2);
            PropLibrary.Bucket(bucket, Vector3.zero, 0.300f, 0.145f, 0, 1, 30f);
            bucket.AddCylinder(new Vector3(0.040f, 0.330f, 0.020f), 0.015f, 0.015f, 0.320f, 8, 0,
                               Quaternion.Euler(12f, 0f, 9f));
            ctx.CreateObject("EntranceBucket", bucket, new[] { Keys.Galvanised, Keys.Hardware },
                parent, new Vector3(-1.780f, 0f, -1.150f), Quaternion.identity,
                BuildContext.ColliderKind.Box);
        }
    }
}
