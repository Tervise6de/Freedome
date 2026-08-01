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
        };

        public static void Build(BuildContext ctx, Transform parent)
        {
            GameObject group = ctx.CreateGroup("Carryables", parent);
            BuildContext.MarkMovable(group);

            foreach (Placement p in Placements)
            {
                switch (p.Name)
                {
                    case "PaintTin": BuildPaintTin(ctx, group.transform, p.Base); break;
                    case "Toolbox": BuildToolbox(ctx, group.transform, p.Base); break;
                    case "FixingsJar": BuildFixingsJar(ctx, group.transform, p.Base); break;
                    case "WateringCan": BuildWateringCan(ctx, group.transform, p.Base); break;
                    case "OffcutBlock": BuildOffcut(ctx, group.transform, p.Base); break;
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
