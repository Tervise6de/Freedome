using UnityEngine;
using Freedome.Interaction;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The handful of loose objects the player can pick up and put down.
    ///
    /// These are separate GameObjects with their own renderer, collider and
    /// rigidbody, which is a deliberate exception to the shared-mesh rule in
    /// DECISIONS #4: an object appended into a combined mesh cannot move. Five of
    /// them is enough for the shed to feel handled without undoing the draw-call
    /// argument for everything else.
    ///
    /// They are ordinary shed objects in ordinary places. None of them is a clue,
    /// none is needed for anything, and nothing marks them out - the only way to
    /// discover them is to walk up and look, which is the same way you would find
    /// them in a real shed.
    /// </summary>
    public static class CarryablesBuilder
    {
        /// <summary>
        /// Placements, kept here rather than inline so the tests can check them
        /// against the room without duplicating the numbers.
        /// </summary>
        public struct Placement
        {
            public string Name;
            public Vector3 Base;

            /// <summary>
            /// Set for things that live on the cupboard bottom. Their height and Z
            /// come from the cupboard rather than from a number typed here, for the
            /// same reason the drawer contents do: nothing renders the inside of a
            /// closed cupboard, so a wrong offset would not show up until somebody
            /// opened it.
            /// </summary>
            public bool InCupboard;

            /// <summary>Offset along Z from the cupboard's centre.</summary>
            public float Along;

            /// <summary>Where the object actually goes.</summary>
            public Vector3 Position => InCupboard
                ? new Vector3(Base.x, FixturesBuilder.CupboardShelfY,
                              FixturesBuilder.CupboardCentreZ + Along)
                : Base;
        }

        public static readonly Placement[] Placements =
        {
            // On the floor at the foot of the storage shelving, clear of the aisle.
            new Placement { Name = "PaintTin", Base = new Vector3(-1.62f, 0.000f, 0.35f) },
            // Beside the bench leg, out of the walking line.
            new Placement { Name = "Toolbox", Base = new Vector3(1.34f, 0.000f, -0.95f) },
            // On the bench top, where somebody would have set it down.
            new Placement { Name = "FixingsJar", Base = new Vector3(1.70f, Dim.BenchHeight, 0.62f) },
            // Storage corner, by the garden things.
            new Placement { Name = "WateringCan", Base = new Vector3(-1.55f, 0.000f, 1.45f) },
            // Just inside the door, where a bucket actually ends up.
            new Placement { Name = "OffcutBlock", Base = new Vector3(-0.30f, 0.000f, -2.05f) },
            // On the bench beyond the vice, where you would leave a plane after
            // using it - on its side, which is how anybody who owns one puts it down.
            new Placement { Name = "HandPlane", Base = new Vector3(1.62f, Dim.BenchHeight, -0.52f) },
            // Utility shelf, by the consumer unit. Where the torch lives, so it can
            // be found when the light is the thing that has failed.
            new Placement { Name = "Torch",
                            Base = new Vector3(-1.10f, Dim.UtilityShelfTopY, Dim.HalfLength - 0.125f) },
            // In the cupboard under the bench, with the rest of the odds and ends.
            // You have to open the doors to find them, which is the point of doors.
            new Placement { Name = "NailTin",
                            Base = new Vector3(1.70f, 0f, 0f), InCupboard = true, Along = 0.20f },
            new Placement { Name = "Paintbrush",
                            Base = new Vector3(1.62f, 0f, 0f), InCupboard = true, Along = -0.20f },
        };

        public static void Build(BuildContext ctx, Transform parent)
        {
            GameObject group = ctx.CreateGroup("Carryables", parent);
            BuildContext.MarkMovable(group);

            foreach (Placement p in Placements)
            {
                switch (p.Name)
                {
                    case "PaintTin": BuildPaintTin(ctx, group.transform, p.Position); break;
                    case "Toolbox": BuildToolbox(ctx, group.transform, p.Position); break;
                    case "FixingsJar": BuildFixingsJar(ctx, group.transform, p.Position); break;
                    case "WateringCan": BuildWateringCan(ctx, group.transform, p.Position); break;
                    case "OffcutBlock": BuildOffcut(ctx, group.transform, p.Position); break;
                    case "HandPlane": BuildHandPlane(ctx, group.transform, p.Position); break;
                    case "Torch": BuildTorch(ctx, group.transform, p.Position); break;
                    case "NailTin": BuildNailTin(ctx, group.transform, p.Position); break;
                    case "Paintbrush": BuildPaintbrush(ctx, group.transform, p.Position); break;
                }
            }
        }

        private static void BuildPaintTin(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_PaintTin", 2);
            PropLibrary.PaintTin(mb, Vector3.zero, 0.175f, 0.088f, 0, 1);

            Finish(ctx, mb, "Carry_PaintTin", new[] { Keys.PaintedGreen, Keys.Hardware }, parent, at,
                   "paint tin", 2.4f, new Vector3(0.26f, -0.24f, 0.46f), Vector3.zero);
        }

        private static void BuildToolbox(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_Toolbox", 2);
            PropLibrary.Toolbox(mb, Vector3.zero, 0, 1, yaw: 14f);

            Finish(ctx, mb, "Carry_Toolbox", new[] { Keys.PaintedRed, Keys.Hardware }, parent, at,
                   "toolbox", 6.0f, new Vector3(0.30f, -0.30f, 0.56f), Vector3.zero);
        }

        private static void BuildFixingsJar(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_FixingsJar", 2);
            PropLibrary.FixingsJar(mb, Vector3.zero, 0.115f, 0.042f, 0, 1);

            Finish(ctx, mb, "Carry_FixingsJar", new[] { Keys.Glass, Keys.Hardware }, parent, at,
                   "jar of fixings", 0.7f, new Vector3(0.22f, -0.18f, 0.38f), Vector3.zero);
        }

        private static void BuildWateringCan(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_WateringCan", 1);
            PropLibrary.WateringCan(mb, Vector3.zero, 0, yaw: -30f);

            Finish(ctx, mb, "Carry_WateringCan", new[] { Keys.Galvanised }, parent, at,
                   "watering can", 1.8f, new Vector3(0.30f, -0.26f, 0.50f), Vector3.zero);
        }

        private static void BuildOffcut(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_Offcut", 1);
            // A 400 mm length of 90 x 45, the sort of thing that never gets thrown out.
            PropLibrary.Timber(mb, new Vector3(0f, 0.0225f, 0f),
                               new Vector3(0.400f, 0.045f, 0.090f), Quaternion.Euler(0f, 22f, 0f), 0);

            Finish(ctx, mb, "Carry_Offcut", new[] { Keys.StructuralPine }, parent, at,
                   "timber offcut", 1.2f, new Vector3(0.28f, -0.22f, 0.46f), new Vector3(0f, 0f, 8f));
        }

        /// <summary>
        /// A smoothing plane: cast body, timber tote and knob, and the lever cap.
        /// The single most recognisable object that belongs on a bench after the
        /// vice, and the one that says somebody works here rather than stores here.
        /// </summary>
        private static void BuildHandPlane(BuildContext ctx, Transform parent, Vector3 at)
        {
            // 0 dark steel, 1 timber
            MeshBuilder mb = new MeshBuilder("Carry_HandPlane", 2);

            const float Len = 0.240f;
            const float Wid = 0.060f;

            // Sole and sides, as a shallow open box.
            mb.AddBox(new Vector3(0f, 0.010f, 0f), new Vector3(Len, 0.020f, Wid), 0, 0.003f);
            foreach (int sz in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(0f, 0.042f, sz * ((Wid * 0.5f) - 0.006f)),
                          new Vector3(Len, 0.044f, 0.012f), 0, 0.002f);
            }

            // Frog and blade, raked back the way a bench plane's is.
            mb.AddBox(new Vector3(0.004f, 0.052f, 0f), new Vector3(0.075f, 0.055f, 0.040f),
                      Quaternion.Euler(0f, 0f, 45f), 0, 0.002f);

            // Tote at the back, knob at the front.
            mb.AddBox(new Vector3(-0.078f, 0.062f, 0f), new Vector3(0.022f, 0.085f, 0.030f),
                      Quaternion.Euler(0f, 0f, -16f), 1, 0.006f);
            mb.AddCylinder(new Vector3(0.086f, 0.048f, 0f), 0.014f, 0.024f, 0.046f, 12, 1,
                           Quaternion.identity);

            Finish(ctx, mb, "Carry_HandPlane", new[] { Keys.DarkSteel, Keys.StructuralPine },
                   parent, at, "hand plane", 1.6f,
                   new Vector3(0.26f, -0.20f, 0.44f), new Vector3(0f, 24f, 0f));
        }

        /// <summary>
        /// A rubber-bodied torch. It has no beam and it does not switch on: this shed
        /// is lit, and a working torch would be a light source somebody has to
        /// balance. It is here because a shed has one, and because it is the right
        /// weight and shape to want to pick up.
        /// </summary>
        private static void BuildTorch(BuildContext ctx, Transform parent, Vector3 at)
        {
            // 0 rubber body, 1 lens and bezel
            MeshBuilder mb = new MeshBuilder("Carry_Torch", 2);

            Quaternion lying = Quaternion.Euler(0f, 0f, 90f);
            mb.AddCylinder(new Vector3(-0.020f, 0.026f, 0f), 0.026f, 0.024f, 0.130f, 12, 0, lying);
            mb.AddCylinder(new Vector3(0.055f, 0.026f, 0f), 0.032f, 0.026f, 0.030f, 12, 1, lying);
            mb.AddCylinder(new Vector3(0.070f, 0.026f, 0f), 0.030f, 0.030f, 0.004f, 12, 1, lying);

            // Sprung hanging loop at the tail.
            mb.AddCylinder(new Vector3(-0.090f, 0.026f, 0f), 0.010f, 0.010f, 0.014f, 8, 1, lying);

            Finish(ctx, mb, "Carry_Torch", new[] { Keys.ElectricalPlastic, Keys.Glass },
                   parent, at, "torch", 0.4f,
                   new Vector3(0.24f, -0.18f, 0.38f), new Vector3(0f, 0f, -6f));
        }

        /// <summary>An old tobacco tin of nails, which is where nails live.</summary>
        private static void BuildNailTin(BuildContext ctx, Transform parent, Vector3 at)
        {
            MeshBuilder mb = new MeshBuilder("Carry_NailTin", 2);

            mb.AddBox(new Vector3(0f, 0.026f, 0f), new Vector3(0.110f, 0.052f, 0.078f), 0, 0.006f);
            // Lid, sitting slightly proud and a little out of true.
            mb.AddBox(new Vector3(0.004f, 0.055f, 0.002f), new Vector3(0.112f, 0.010f, 0.080f),
                      Quaternion.Euler(0f, 3f, 0f), 1, 0.003f);

            Finish(ctx, mb, "Carry_NailTin", new[] { Keys.PaintedGreen, Keys.Hardware },
                   parent, at, "tin of nails", 0.9f,
                   new Vector3(0.22f, -0.19f, 0.40f), Vector3.zero);
        }

        /// <summary>A 50 mm brush, gone hard, because they always have.</summary>
        private static void BuildPaintbrush(BuildContext ctx, Transform parent, Vector3 at)
        {
            // 0 timber handle, 1 ferrule, 2 bristle
            MeshBuilder mb = new MeshBuilder("Carry_Paintbrush", 3);

            Quaternion lying = Quaternion.Euler(0f, 0f, 90f);
            mb.AddBox(new Vector3(-0.062f, 0.008f, 0f), new Vector3(0.110f, 0.016f, 0.026f),
                      0, 0.004f);
            mb.AddBox(new Vector3(0.005f, 0.009f, 0f), new Vector3(0.036f, 0.018f, 0.048f),
                      1, 0.002f);
            mb.AddBox(new Vector3(0.048f, 0.009f, 0f), new Vector3(0.055f, 0.016f, 0.048f),
                      2, 0.002f);

            Finish(ctx, mb, "Carry_Paintbrush",
                   new[] { Keys.StructuralPine, Keys.Hardware, Keys.PaintedGreen },
                   parent, at, "paintbrush", 0.2f,
                   new Vector3(0.22f, -0.18f, 0.36f), Vector3.zero);
        }

        private static void Finish(BuildContext ctx, MeshBuilder mb, string name, string[] materials,
                                   Transform parent, Vector3 at, string displayName, float mass,
                                   Vector3 holdOffset, Vector3 holdEuler)
        {
            GameObject go = ctx.CreateObject(name, mb, materials, parent, at,
                                             Quaternion.identity,
                                             BuildContext.ColliderKind.Box, isStatic: false);
            if (go == null)
            {
                return;
            }

            Rigidbody body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            // Continuous, because a light object dropped from chest height at 60 fps
            // can otherwise tunnel through a 19 mm floorboard.
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            Carryable carry = go.AddComponent<Carryable>();
            carry.Configure(displayName, holdOffset, holdEuler);
        }
    }
}
