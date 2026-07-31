using System.Collections.Generic;
using UnityEngine;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The electrical installation and the entrance-wall fittings.
    ///
    /// The conduit is routed as a real circuit rather than decorated onto the walls:
    /// it leaves the top of the consumer unit on the utility wall, runs at high level
    /// around two walls, and drops to the light switch beside the door, with a spur
    /// carrying a clipped cable up into the roof and along to the ceiling rose. Every
    /// run is saddled at sensible intervals and every change of direction has a box.
    /// </summary>
    public static class UtilityBuilder
    {
        private const float ConduitRadius = 0.010f;
        private const int ConduitSegments = 10;

        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildElectrical(ctx, parent);
            BuildUtilityShelf(ctx, parent);
            BuildEntranceFittings(ctx, parent);
        }

        // =====================================================================
        // Electrical
        // =====================================================================

        private static void BuildElectrical(BuildContext ctx, Transform parent)
        {
            // 0 grey plastic, 1 galvanised conduit, 2 dark steel toggles and cable
            MeshBuilder mb = new MeshBuilder("Electrical_Installation", 3);

            BuildConsumerUnit(mb);
            BuildSocket(mb);
            BuildSwitch(mb);
            BuildConduitCircuit(mb);
            BuildLightingCable(mb);

            ctx.CreateObject("Electrical_Installation", mb,
                new[] { Keys.ElectricalPlastic, Keys.Galvanised, Keys.DarkSteel },
                parent, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// A small consumer unit: enclosure, hinged lid with a window, a main switch
        /// and six breakers behind it, plus the circuit label strip.
        /// </summary>
        private static void BuildConsumerUnit(MeshBuilder mb)
        {
            float x = Dim.BreakerBoxCentreX;
            float y = Dim.BreakerBoxCentreY;
            float zWall = Dim.HalfLength;
            float w = Dim.BreakerBoxWidth;
            float h = Dim.BreakerBoxHeight;
            float d = Dim.BreakerBoxDepth;

            float zFront = zWall - d;

            // Enclosure.
            mb.AddBox(new Vector3(x, y, zWall - (d * 0.5f)), new Vector3(w, h, d), 0, 0.004f);

            // Breaker toggles sit on a DIN rail behind the lid window.
            for (int i = 0; i < 6; i++)
            {
                float tx = x - 0.082f + (i * 0.030f);
                mb.AddBox(new Vector3(tx, y + 0.010f, zFront + 0.014f),
                          new Vector3(0.024f, 0.062f, 0.028f), 0, 0.002f);
                mb.AddBox(new Vector3(tx, y + 0.030f, zFront + 0.004f),
                          new Vector3(0.012f, 0.020f, 0.014f), 2, 0.001f);
            }

            // Main switch, larger and set to one side.
            mb.AddBox(new Vector3(x + 0.104f, y + 0.010f, zFront + 0.014f),
                      new Vector3(0.030f, 0.062f, 0.028f), 0, 0.002f);
            mb.AddBox(new Vector3(x + 0.104f, y - 0.008f, zFront + 0.004f),
                      new Vector3(0.016f, 0.024f, 0.014f), 2, 0.001f);

            // Lid: four frame pieces around a window, so the breakers stay visible.
            const float LidThickness = 0.010f;
            float lidZ = zFront - (LidThickness * 0.5f);
            float windowW = 0.230f;
            float windowH = 0.080f;

            mb.AddBox(new Vector3(x, y + (h * 0.25f) + (windowH * 0.25f), lidZ),
                      new Vector3(w, (h * 0.5f) - (windowH * 0.5f), LidThickness), 0, 0.002f);
            mb.AddBox(new Vector3(x, y - (h * 0.25f) - (windowH * 0.25f) + 0.020f, lidZ),
                      new Vector3(w, (h * 0.5f) - (windowH * 0.5f) + 0.040f, LidThickness), 0, 0.002f);
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(x + (s * ((w * 0.5f) - ((w - windowW) * 0.25f))), y + 0.010f, lidZ),
                          new Vector3((w - windowW) * 0.5f, windowH, LidThickness), 0, 0.002f);
            }

            // Label strip and two lid screws.
            mb.AddBox(new Vector3(x, y - 0.052f, lidZ - 0.004f),
                      new Vector3(0.210f, 0.024f, 0.002f), 0);
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(x + (s * 0.120f), y - (h * 0.5f) + 0.024f, lidZ - 0.006f),
                               0.006f, 0.006f, 0.004f, 8, 1, Quaternion.Euler(90f, 0f, 0f));
            }

            // Conduit entry gland at the top and bottom of the enclosure.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(x, y + (s * ((h * 0.5f) + 0.010f)), zWall - 0.028f),
                               0.016f, 0.016f, 0.020f, 10, 1, Quaternion.identity);
            }
        }

        private static void BuildSocket(MeshBuilder mb)
        {
            float x = Dim.BreakerBoxCentreX;
            float y = Dim.SocketCentreY;
            float z = Dim.HalfLength;

            // Back box and face plate.
            mb.AddBox(new Vector3(x, y, z - 0.020f), new Vector3(0.150f, 0.090f, 0.040f), 0, 0.003f);
            mb.AddBox(new Vector3(x, y, z - 0.043f), new Vector3(0.146f, 0.086f, 0.008f), 0, 0.003f);

            // Two outlets and their switches.
            foreach (int s in new[] { -1, 1 })
            {
                float ox = x + (s * 0.036f);
                mb.AddBox(new Vector3(ox, y + 0.008f, z - 0.048f), new Vector3(0.009f, 0.020f, 0.004f), 2);
                mb.AddBox(new Vector3(ox - 0.012f, y - 0.010f, z - 0.048f),
                          new Vector3(0.009f, 0.016f, 0.004f), 2);
                mb.AddBox(new Vector3(ox + 0.012f, y - 0.010f, z - 0.048f),
                          new Vector3(0.009f, 0.016f, 0.004f), 2);
            }

            foreach (int s in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(x + (s * 0.060f), y, z - 0.048f), 0.005f, 0.005f, 0.004f, 8, 1,
                               Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private static void BuildSwitch(MeshBuilder mb)
        {
            float x = Dim.LightSwitchX;
            float y = Dim.LightSwitchY;
            float z = -Dim.HalfLength;

            mb.AddBox(new Vector3(x, y, z + 0.018f), new Vector3(0.078f, 0.078f, 0.036f), 0, 0.003f);
            mb.AddBox(new Vector3(x, y, z + 0.040f), new Vector3(0.088f, 0.088f, 0.008f), 0, 0.003f);
            mb.AddBox(new Vector3(x, y + 0.004f, z + 0.047f), new Vector3(0.030f, 0.046f, 0.008f), 0, 0.002f);

            foreach (int s in new[] { -1, 1 })
            {
                mb.AddCylinder(new Vector3(x + (s * 0.030f), y, z + 0.045f), 0.005f, 0.005f, 0.004f, 8, 1,
                               Quaternion.Euler(90f, 0f, 0f));
            }
        }

        /// <summary>
        /// The surface conduit run. Consumer unit to switch, the long way round.
        /// </summary>
        private static void BuildConduitCircuit(MeshBuilder mb)
        {
            float zUtility = Dim.HalfLength - 0.014f;
            float zEntrance = -Dim.HalfLength + 0.014f;
            float xStorage = -Dim.HalfWidth + 0.014f;
            const float High = 2.30f;

            float boxTop = Dim.BreakerBoxCentreY + (Dim.BreakerBoxHeight * 0.5f);
            float boxBottom = Dim.BreakerBoxCentreY - (Dim.BreakerBoxHeight * 0.5f);

            // Drop from the consumer unit to the socket.
            AddConduit(mb, new Vector3(Dim.BreakerBoxCentreX, boxBottom, zUtility),
                       new Vector3(Dim.BreakerBoxCentreX, Dim.SocketCentreY + 0.045f, zUtility));

            // Riser and the high-level run around the shed.
            List<Vector3> path = new List<Vector3>
            {
                new Vector3(Dim.BreakerBoxCentreX, boxTop, zUtility),
                new Vector3(Dim.BreakerBoxCentreX, High, zUtility),
                new Vector3(xStorage + 0.06f, High, zUtility),
                new Vector3(xStorage, High, Dim.HalfLength - 0.08f),
                new Vector3(xStorage, High, -Dim.HalfLength + 0.08f),
                new Vector3(xStorage + 0.06f, High, zEntrance),
                new Vector3(Dim.LightSwitchX, High, zEntrance),
                new Vector3(Dim.LightSwitchX, Dim.LightSwitchY + 0.050f, zEntrance),
            };

            for (int i = 0; i < path.Count - 1; i++)
            {
                AddConduit(mb, path[i], path[i + 1]);
            }

            // Inspection boxes where the run turns a corner.
            for (int i = 1; i < path.Count - 1; i++)
            {
                mb.AddBox(path[i], new Vector3(0.052f, 0.052f, 0.052f), 1, 0.004f);
            }

            // A junction box on the high-level run where the lighting spur leaves.
            mb.AddBox(new Vector3(0f, High, zUtility), new Vector3(0.075f, 0.075f, 0.045f), 1, 0.004f);
        }

        /// <summary>
        /// Twin-and-earth from the junction box up into the roof and along to the
        /// ceiling rose, clipped to the timber with a little sag between clips.
        /// </summary>
        private static void BuildLightingCable(MeshBuilder mb)
        {
            const float CableRadius = 0.0045f;
            float zUtility = Dim.HalfLength - 0.020f;

            List<Vector3> path = new List<Vector3>
            {
                new Vector3(0f, 2.34f, zUtility),
                new Vector3(0f, 2.92f, zUtility),
                new Vector3(0f, 2.96f, Dim.HalfLength - 0.30f),
                new Vector3(0f, 2.96f, Dim.CeilingLightZ + 0.10f),
                new Vector3(0f, 2.90f, Dim.CeilingLightZ + 0.02f),
            };

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];
                float len = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.RoundToInt(len / 0.45f));

                for (int s = 0; s < steps; s++)
                {
                    Vector3 p0 = Vector3.Lerp(a, b, (float)s / steps);
                    Vector3 p1 = Vector3.Lerp(a, b, (float)(s + 1) / steps);

                    // A little sag on the horizontal runs.
                    float sag = Mathf.Abs(b.y - a.y) < 0.05f ? 0.012f : 0f;
                    Vector3 mid = ((p0 + p1) * 0.5f) - new Vector3(0f, sag, 0f);

                    AddSegment(mb, p0, mid, CableRadius, 2, 6);
                    AddSegment(mb, mid, p1, CableRadius, 2, 6);

                    // Cable clip at the start of each span.
                    mb.AddBox(p0, new Vector3(0.018f, 0.010f, 0.010f), 1, 0.001f);
                }
            }
        }

        private static void AddConduit(MeshBuilder mb, Vector3 a, Vector3 b)
        {
            AddSegment(mb, a, b, ConduitRadius, 1, ConduitSegments);

            // Saddles roughly every 600 mm.
            float len = Vector3.Distance(a, b);
            int saddles = Mathf.FloorToInt(len / 0.6f);
            for (int i = 1; i <= saddles; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, (float)i / (saddles + 1));
                mb.AddBox(p, new Vector3(0.030f, 0.030f, 0.014f), 1, 0.002f);
            }
        }

        private static void AddSegment(MeshBuilder mb, Vector3 a, Vector3 b, float radius, int submesh,
                                       int segments)
        {
            Vector3 delta = b - a;
            float len = delta.magnitude;
            if (len < 0.001f)
            {
                return;
            }

            Quaternion rot = Quaternion.FromToRotation(Vector3.up, delta / len);
            mb.AddCylinder((a + b) * 0.5f, radius, radius, len, segments, submesh, rot);
        }

        // =====================================================================
        // Utility shelf and radio
        // =====================================================================

        private static void BuildUtilityShelf(BuildContext ctx, Transform parent)
        {
            // 0 pine, 1 zinc brackets, 2 dark plastic radio, 3 galvanised
            MeshBuilder mb = new MeshBuilder("UtilityShelf", 4);

            float len = Dim.UtilityShelfLength;
            float depth = Dim.UtilityShelfDepth;

            mb.AddBox(new Vector3(0f, 0f, -depth * 0.5f),
                      new Vector3(len, Dim.BoardThickness, depth), 0, 0.003f);

            // Pressed-steel shelf brackets.
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (len * 0.5f - 0.150f);
                mb.AddBox(new Vector3(x, -0.010f, -depth * 0.5f + 0.010f),
                          new Vector3(0.028f, 0.005f, depth - 0.020f), 1, 0.001f);
                mb.AddBox(new Vector3(x, -0.090f, -0.008f),
                          new Vector3(0.028f, 0.170f, 0.005f), 1, 0.001f);
                mb.AddBox(new Vector3(x, -0.062f, -0.075f),
                          new Vector3(0.024f, 0.005f, 0.170f),
                          Quaternion.Euler(45f, 0f, 0f), 1, 0.001f);
            }

            BuildRadio(mb, new Vector3(0.150f, Dim.BoardThickness * 0.5f, -0.115f));

            // A jam jar of assorted fixings, the way every shed shelf ends up.
            mb.AddCylinder(new Vector3(-0.400f, 0.055f, -0.110f), 0.038f, 0.038f, 0.100f, 14, 3,
                           Quaternion.identity);
            mb.AddCylinder(new Vector3(-0.400f, 0.112f, -0.110f), 0.036f, 0.036f, 0.016f, 14, 1,
                           Quaternion.identity);

            ctx.CreateObject("UtilityShelf", mb,
                new[] { Keys.StructuralPine, Keys.Hardware, Keys.ElectricalPlastic, Keys.Glass },
                parent,
                new Vector3(Dim.UtilityShelfCentreX, Dim.UtilityShelfHeight, Dim.HalfLength),
                Quaternion.identity, BuildContext.ColliderKind.Box);
        }

        /// <summary>
        /// A small mains radio with a carry handle, speaker grille and two dials. It
        /// is an ordinary shed radio, sitting where you would put one so you can hear
        /// it from the bench.
        /// </summary>
        private static void BuildRadio(MeshBuilder mb, Vector3 basePoint)
        {
            const int Body = 2;
            const int Metal = 1;

            mb.Push(basePoint);

            const float W = 0.230f;
            const float H = 0.145f;
            const float D = 0.095f;

            mb.AddBox(new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H, D), Body, 0.006f);

            // Speaker grille as a shallow recess with bars.
            mb.AddBox(new Vector3(-0.045f, H * 0.55f, -(D * 0.5f) - 0.002f),
                      new Vector3(0.105f, 0.078f, 0.006f), Metal, 0.002f);
            for (int i = 0; i < 5; i++)
            {
                mb.AddBox(new Vector3(-0.045f, (H * 0.55f) - 0.030f + (i * 0.015f), -(D * 0.5f) - 0.006f),
                          new Vector3(0.100f, 0.004f, 0.003f), Body);
            }

            // Tuning scale and two dials.
            mb.AddBox(new Vector3(0.058f, H * 0.72f, -(D * 0.5f) - 0.002f),
                      new Vector3(0.078f, 0.030f, 0.005f), Metal, 0.002f);
            foreach (float dx in new[] { 0.036f, 0.080f })
            {
                mb.AddCylinder(new Vector3(dx, H * 0.30f, -(D * 0.5f) - 0.008f),
                               0.016f, 0.014f, 0.016f, 12, Body, Quaternion.Euler(90f, 0f, 0f));
            }

            // Carry handle.
            mb.AddBox(new Vector3(0f, H + 0.030f, 0f), new Vector3(W - 0.070f, 0.010f, 0.014f), Metal, 0.003f);
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * ((W - 0.070f) * 0.5f), H + 0.014f, 0f),
                          new Vector3(0.010f, 0.038f, 0.014f), Metal, 0.002f);
            }

            // Telescopic aerial, part way up and leaning as they always are.
            mb.AddCylinder(new Vector3((W * 0.5f) - 0.020f, H + 0.150f, 0.020f),
                           0.0035f, 0.0020f, 0.300f, 8, Metal, Quaternion.Euler(14f, 0f, -10f));

            // Flex disappearing behind the shelf.
            mb.AddCylinder(new Vector3(0.020f, 0.010f, (D * 0.5f) + 0.030f),
                           0.004f, 0.004f, 0.080f, 6, Body, Quaternion.Euler(90f, 0f, 0f));

            mb.Pop();
        }

        // =====================================================================
        // Entrance wall: coat rack and boot area
        // =====================================================================

        private static void BuildEntranceFittings(BuildContext ctx, Transform parent)
        {
            // 0 pine, 1 zinc hooks, 2 rubber, 3 tarpaulin-style fabric
            MeshBuilder mb = new MeshBuilder("EntranceFittings", 4);

            float z = -Dim.HalfLength;

            // --- coat rack ----------------------------------------------------
            const float RackLength = 0.800f;
            float rackX = 0.950f;
            float rackY = 1.650f;

            mb.AddBox(new Vector3(rackX, rackY, z + 0.011f),
                      new Vector3(RackLength, 0.140f, 0.022f), 0, 0.004f);

            for (int i = 0; i < 4; i++)
            {
                float hx = rackX - 0.300f + (i * 0.200f);
                // Simple two-part hook: a plate and a bent arm.
                mb.AddBox(new Vector3(hx, rackY, z + 0.026f), new Vector3(0.024f, 0.070f, 0.008f), 1, 0.002f);
                mb.AddCylinder(new Vector3(hx, rackY - 0.030f, z + 0.052f), 0.005f, 0.005f, 0.055f, 8, 1,
                               Quaternion.Euler(90f, 0f, 0f));
                mb.AddCylinder(new Vector3(hx, rackY - 0.052f, z + 0.072f), 0.005f, 0.005f, 0.045f, 8, 1,
                               Quaternion.identity);
            }

            // An extension lead coiled over one hook.
            BuildCoiledLead(mb, new Vector3(rackX - 0.300f, rackY - 0.140f, z + 0.075f));

            // A canvas tool bag on another.
            mb.AddBox(new Vector3(rackX + 0.100f, rackY - 0.190f, z + 0.115f),
                      new Vector3(0.230f, 0.280f, 0.150f), 3, 0.020f);
            mb.AddBox(new Vector3(rackX + 0.100f, rackY - 0.040f, z + 0.115f),
                      new Vector3(0.020f, 0.130f, 0.014f), 3, 0.005f);

            // --- boot tray and boots -------------------------------------------
            float trayX = 1.150f;
            float trayZ = z + 0.330f;

            mb.AddBox(new Vector3(trayX, 0.014f, trayZ), new Vector3(0.620f, 0.028f, 0.420f), 2, 0.006f);
            mb.AddBox(new Vector3(trayX, 0.030f, trayZ), new Vector3(0.580f, 0.020f, 0.380f), 2, 0.004f);

            for (int i = 0; i < 2; i++)
            {
                float bx = trayX - 0.110f + (i * 0.220f);
                float lean = i == 0 ? -5f : 4f;
                mb.Push(new Vector3(bx, 0.028f, trayZ + 0.010f), Quaternion.Euler(0f, i * 12f, lean));
                mb.AddCylinder(new Vector3(0f, 0.170f, 0f), 0.062f, 0.058f, 0.340f, 12, 2,
                               Quaternion.identity);
                mb.AddBox(new Vector3(0f, 0.030f, -0.055f), new Vector3(0.105f, 0.060f, 0.185f), 2, 0.012f);
                mb.Pop();
            }

            ctx.CreateObject("EntranceFittings", mb,
                new[] { Keys.StructuralPine, Keys.Hardware, Keys.Rubber, Keys.Tarpaulin },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.None);
        }

        private static void BuildCoiledLead(MeshBuilder mb, Vector3 centre)
        {
            const int Turns = 14;
            const float Radius = 0.115f;

            mb.Push(centre);
            for (int i = 0; i < Turns; i++)
            {
                float a0 = (i / (float)Turns) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)Turns) * Mathf.PI * 2f;
                float wobble = Mathf.Sin(i * 2.3f) * 0.010f;

                Vector3 p0 = new Vector3(Mathf.Cos(a0) * (Radius + wobble), Mathf.Sin(a0) * Radius * 1.15f, 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * (Radius + wobble), Mathf.Sin(a1) * Radius * 1.15f,
                                         0.006f);

                Vector3 delta = p1 - p0;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, delta.normalized);
                mb.AddCylinder((p0 + p1) * 0.5f, 0.006f, 0.006f, delta.magnitude, 6, 2, rot);
            }
            mb.Pop();
        }
    }
}
