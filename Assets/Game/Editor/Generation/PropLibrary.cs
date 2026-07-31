using UnityEngine;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Reusable prop shapes. Each method appends one object into a MeshBuilder at the
    /// current transform, so a shelf full of containers costs one renderer rather
    /// than fifteen.
    ///
    /// Everything here is deliberately low on triangles - these are background
    /// objects seen from a metre or more away, and the budget is better spent on the
    /// bevels of the architecture the player stands next to.
    /// </summary>
    public static class PropLibrary
    {
        /// <summary>A tapered builder's bucket with a rolled rim and a wire handle.</summary>
        public static void Bucket(MeshBuilder mb, Vector3 basePoint, float height, float topRadius,
                                  int bodySubmesh, int handleSubmesh, float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            float bottomRadius = topRadius * 0.76f;
            mb.AddCylinder(new Vector3(0f, height * 0.5f, 0f), bottomRadius, topRadius, height, 14,
                           bodySubmesh, Quaternion.identity, true, false);

            // Rolled rim.
            mb.AddCylinder(new Vector3(0f, height - 0.006f, 0f), topRadius + 0.006f, topRadius + 0.006f,
                           0.014f, 14, bodySubmesh, Quaternion.identity, false, true);

            // Wire handle, thrown over to one side.
            const int Steps = 6;
            for (int i = 0; i < Steps; i++)
            {
                float t0 = i / (float)Steps;
                float t1 = (i + 1) / (float)Steps;
                Vector3 p0 = HandlePoint(t0, topRadius, height);
                Vector3 p1 = HandlePoint(t1, topRadius, height);
                Vector3 d = p1 - p0;
                mb.AddCylinder((p0 + p1) * 0.5f, 0.0035f, 0.0035f, d.magnitude, 6, handleSubmesh,
                               Quaternion.FromToRotation(Vector3.up, d.normalized));
            }

            mb.Pop();
        }

        private static Vector3 HandlePoint(float t, float radius, float height)
        {
            float angle = Mathf.Lerp(-0.35f, Mathf.PI + 0.35f, t);
            float lift = Mathf.Sin(t * Mathf.PI) * radius * 0.85f;
            return new Vector3(Mathf.Cos(angle) * radius * 0.98f,
                               height - 0.030f + lift,
                               Mathf.Sin(angle) * radius * 0.30f);
        }

        /// <summary>A paint tin: body, lid ring, and a wire bail handle.</summary>
        public static void PaintTin(MeshBuilder mb, Vector3 basePoint, float height, float radius,
                                    int bodySubmesh, int metalSubmesh, float yaw = 0f, bool lidOff = false)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            mb.AddCylinder(new Vector3(0f, height * 0.5f, 0f), radius, radius, height, 14, bodySubmesh,
                           Quaternion.identity);

            // Rolled seams top and bottom.
            foreach (float y in new[] { 0.008f, height - 0.008f })
            {
                mb.AddCylinder(new Vector3(0f, y, 0f), radius + 0.004f, radius + 0.004f, 0.010f, 14,
                               metalSubmesh, Quaternion.identity, false, false);
            }

            if (lidOff)
            {
                // Lid leaning against the tin.
                mb.AddCylinder(new Vector3(radius + 0.035f, radius * 0.9f, 0f), radius, radius, 0.008f, 14,
                               metalSubmesh, Quaternion.Euler(0f, 0f, 78f));
            }

            // Bail handle.
            const int Steps = 5;
            for (int i = 0; i < Steps; i++)
            {
                float a0 = Mathf.Lerp(0f, Mathf.PI, i / (float)Steps);
                float a1 = Mathf.Lerp(0f, Mathf.PI, (i + 1) / (float)Steps);
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, height - 0.02f + (Mathf.Sin(a0) * 0.055f), 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, height - 0.02f + (Mathf.Sin(a1) * 0.055f), 0f);
                Vector3 d = p1 - p0;
                mb.AddCylinder((p0 + p1) * 0.5f, 0.0025f, 0.0025f, d.magnitude, 5, metalSubmesh,
                               Quaternion.FromToRotation(Vector3.up, d.normalized));
            }

            mb.Pop();
        }

        /// <summary>A pressed-steel toolbox with a cantilever tray lip and a handle.</summary>
        public static void Toolbox(MeshBuilder mb, Vector3 basePoint, int bodySubmesh, int metalSubmesh,
                                   float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            const float W = 0.420f;
            const float D = 0.190f;
            const float H = 0.170f;

            mb.AddBox(new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H, D), bodySubmesh, 0.008f);

            // Lid with a slight overhang and two catches.
            mb.AddBox(new Vector3(0f, H + 0.012f, 0f), new Vector3(W + 0.006f, 0.024f, D + 0.006f),
                      bodySubmesh, 0.006f);
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * (W * 0.32f), H - 0.010f, -(D * 0.5f) - 0.005f),
                          new Vector3(0.045f, 0.036f, 0.008f), metalSubmesh, 0.002f);
            }

            // Handle on top.
            mb.AddBox(new Vector3(0f, H + 0.062f, 0f), new Vector3(0.170f, 0.014f, 0.020f),
                      metalSubmesh, 0.004f);
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * 0.085f, H + 0.040f, 0f), new Vector3(0.012f, 0.056f, 0.020f),
                          metalSubmesh, 0.003f);
            }

            mb.Pop();
        }

        /// <summary>A cardboard box, optionally with its flaps open.</summary>
        public static void CardboardBox(MeshBuilder mb, Vector3 basePoint, Vector3 size, int submesh,
                                        float yaw = 0f, bool openFlaps = false)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            mb.AddBox(new Vector3(0f, size.y * 0.5f, 0f), size, submesh, 0.006f);

            if (openFlaps)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    mb.AddBox(new Vector3(0f, size.y + (size.z * 0.22f), s * (size.z * 0.34f)),
                              new Vector3(size.x, 0.004f, size.z * 0.5f),
                              Quaternion.Euler(s * -55f, 0f, 0f), submesh, 0.002f);
                }
            }

            mb.Pop();
        }

        /// <summary>A stack of nested plastic plant pots.</summary>
        public static void PlantPotStack(MeshBuilder mb, Vector3 basePoint, int count, int submesh,
                                         float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            for (int i = 0; i < count; i++)
            {
                float y = i * 0.022f;
                mb.AddCylinder(new Vector3(0f, y + 0.080f, 0f), 0.055f, 0.082f, 0.160f, 12, submesh,
                               Quaternion.Euler(0f, i * 17f, 0f), i == 0, false);
            }

            mb.Pop();
        }

        /// <summary>A galvanised watering can with a spout and rose.</summary>
        public static void WateringCan(MeshBuilder mb, Vector3 basePoint, int submesh, float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            const float R = 0.105f;
            const float H = 0.240f;

            mb.AddCylinder(new Vector3(0f, H * 0.5f, 0f), R, R * 0.94f, H, 14, submesh,
                           Quaternion.identity);
            mb.AddCylinder(new Vector3(0f, H + 0.012f, 0f), R * 0.55f, R * 0.50f, 0.030f, 12, submesh,
                           Quaternion.identity);

            // Spout rising from the base to above the rim.
            mb.AddCylinder(new Vector3(R + 0.115f, H * 0.62f, 0f), 0.022f, 0.016f, 0.330f, 10, submesh,
                           Quaternion.Euler(0f, 0f, -62f));
            mb.AddCylinder(new Vector3(R + 0.215f, H * 1.02f, 0f), 0.036f, 0.038f, 0.028f, 12, submesh,
                           Quaternion.Euler(0f, 0f, 28f));

            // Carry handle over the top.
            const int Steps = 5;
            for (int i = 0; i < Steps; i++)
            {
                float a0 = Mathf.Lerp(0.15f, Mathf.PI - 0.15f, i / (float)Steps);
                float a1 = Mathf.Lerp(0.15f, Mathf.PI - 0.15f, (i + 1) / (float)Steps);
                Vector3 p0 = new Vector3(-Mathf.Cos(a0) * R * 0.8f, H + (Mathf.Sin(a0) * 0.085f), 0f);
                Vector3 p1 = new Vector3(-Mathf.Cos(a1) * R * 0.8f, H + (Mathf.Sin(a1) * 0.085f), 0f);
                Vector3 d = p1 - p0;
                mb.AddCylinder((p0 + p1) * 0.5f, 0.008f, 0.008f, d.magnitude, 6, submesh,
                               Quaternion.FromToRotation(Vector3.up, d.normalized));
            }

            mb.Pop();
        }

        /// <summary>A length of sawn timber, chamfered so its edges read.</summary>
        public static void Timber(MeshBuilder mb, Vector3 centre, Vector3 size, Quaternion rotation,
                                  int submesh)
        {
            mb.AddBox(centre, size, rotation, submesh, 0.002f);
        }

        /// <summary>A screw-top jar, the universal shed container for loose fixings.</summary>
        public static void FixingsJar(MeshBuilder mb, Vector3 basePoint, float height, float radius,
                                      int glassSubmesh, int lidSubmesh)
        {
            mb.AddCylinder(basePoint + new Vector3(0f, height * 0.5f, 0f), radius, radius, height, 12,
                           glassSubmesh, Quaternion.identity);
            mb.AddCylinder(basePoint + new Vector3(0f, height + 0.008f, 0f), radius * 0.94f, radius * 0.94f,
                           0.018f, 12, lidSubmesh, Quaternion.identity);
        }

        /// <summary>A soft bag - compost, sand or fertiliser - slumped under its own weight.</summary>
        public static void Sack(MeshBuilder mb, Vector3 basePoint, Vector3 size, int submesh, float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            // Three stacked slabs of decreasing width give a slumped silhouette
            // without needing a simulated cloth mesh.
            mb.AddBox(new Vector3(0f, size.y * 0.20f, 0f),
                      new Vector3(size.x, size.y * 0.40f, size.z), submesh, 0.030f);
            mb.AddBox(new Vector3(0f, size.y * 0.58f, 0f),
                      new Vector3(size.x * 0.94f, size.y * 0.40f, size.z * 0.92f), submesh, 0.030f);
            mb.AddBox(new Vector3(0f, size.y * 0.86f, 0f),
                      new Vector3(size.x * 0.80f, size.y * 0.26f, size.z * 0.78f), submesh, 0.028f);

            // Folded and taped ends.
            foreach (int s in new[] { -1, 1 })
            {
                mb.AddBox(new Vector3(s * size.x * 0.48f, size.y * 0.30f, 0f),
                          new Vector3(size.x * 0.08f, size.y * 0.34f, size.z * 0.70f), submesh, 0.012f);
            }

            mb.Pop();
        }

        /// <summary>A folded tarpaulin - a squat slab with visible fold edges.</summary>
        public static void FoldedTarpaulin(MeshBuilder mb, Vector3 basePoint, Vector3 size, int submesh,
                                           float yaw = 0f)
        {
            mb.Push(basePoint, Quaternion.Euler(0f, yaw, 0f));

            const int Layers = 4;
            for (int i = 0; i < Layers; i++)
            {
                float t = i / (float)(Layers - 1);
                float h = size.y / Layers;
                mb.AddBox(new Vector3(Mathf.Lerp(-0.008f, 0.008f, t), (h * i) + (h * 0.5f), 0f),
                          new Vector3(size.x - (i * 0.010f), h * 0.94f, size.z - (i * 0.014f)),
                          submesh, 0.008f);
            }

            mb.Pop();
        }
    }
}
