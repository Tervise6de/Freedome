using System.Collections.Generic;
using UnityEngine;
using Freedome.Environment;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Builds timber stud walls the way they are actually framed, so the interior
    /// of the shed shows a coherent structure rather than a decorated slab.
    ///
    /// Wall local space:
    ///   +X  along the wall, 0 .. length
    ///   +Y  up, 0 at floor level
    ///   +Z  into the wall, 0 at the interior face
    /// </summary>
    public static class FramingUtility
    {
        /// <summary>A rectangular hole in a wall, in wall-local coordinates.</summary>
        public struct Opening
        {
            public float Start;  // along the wall
            public float End;
            public float Bottom; // from the floor
            public float Top;

            public Opening(float start, float end, float bottom, float top)
            {
                Start = start;
                End = end;
                Bottom = bottom;
                Top = top;
            }

            public bool ReachesFloor => Bottom <= 0.001f;

            public float Centre => (Start + End) * 0.5f;

            public float Width => End - Start;
        }

        private const float Bevel = 0.0025f; // 2.5 mm arris on framing timber

        /// <summary>
        /// Emits bottom plate, double top plate, common studs at 600 centres, the
        /// king/jack/cripple studs and lintels around every opening, and staggered
        /// noggins between the studs.
        /// </summary>
        public static void BuildStudWall(MeshBuilder mb, float length, float height,
                                         IList<Opening> openings, int submesh)
        {
            float plate = ShedDimensions.PlateThickness; // 45
            float depth = ShedDimensions.StudDepth;      // 90
            float studW = ShedDimensions.StudWidth;      // 45
            float halfDepth = depth * 0.5f;

            float topPlateBottom = height - (plate * 2f);
            float studTop = topPlateBottom;
            float studBottom = plate;

            openings = openings ?? new List<Opening>();

            // --- bottom plate, interrupted by any opening that reaches the floor ---
            List<Vector2> floorHoles = new List<Vector2>();
            foreach (Opening o in openings)
            {
                if (o.ReachesFloor)
                {
                    floorHoles.Add(new Vector2(o.Start, o.End));
                }
            }

            foreach (Vector2 seg in Subtract(0f, length, floorHoles))
            {
                mb.AddBox(new Vector3((seg.x + seg.y) * 0.5f, plate * 0.5f, halfDepth),
                          new Vector3(seg.y - seg.x, plate, depth), submesh, Bevel);
            }

            // --- double top plate, continuous ---
            mb.AddBox(new Vector3(length * 0.5f, topPlateBottom + (plate * 0.5f), halfDepth),
                      new Vector3(length, plate, depth), submesh, Bevel);
            mb.AddBox(new Vector3(length * 0.5f, topPlateBottom + plate + (plate * 0.5f), halfDepth),
                      new Vector3(length, plate, depth), submesh, Bevel);

            // --- common studs -------------------------------------------------
            List<float> studCentres = new List<float>();
            float spacing = ShedDimensions.StudSpacing;
            int count = Mathf.FloorToInt((length + 0.001f) / spacing);

            studCentres.Add(studW * 0.5f);                    // start corner stud
            for (int i = 1; i <= count; i++)
            {
                float c = i * spacing;
                if (c > studW && c < length - studW)
                {
                    studCentres.Add(c);
                }
            }
            studCentres.Add(length - (studW * 0.5f));          // end corner stud

            foreach (float c in studCentres)
            {
                if (IsBlocked(c, studW, openings))
                {
                    continue;
                }

                mb.AddBox(new Vector3(c, studBottom + ((studTop - studBottom) * 0.5f), halfDepth),
                          new Vector3(studW, studTop - studBottom, depth), submesh, Bevel);
            }

            // --- opening trimmers ---------------------------------------------
            foreach (Opening o in openings)
            {
                BuildOpeningTrimmers(mb, o, height, submesh);
            }

            // --- noggins, staggered so the ends can be nailed ------------------
            studCentres.Sort();
            float noggingHeight = height * 0.5f;
            bool high = false;
            for (int i = 0; i < studCentres.Count - 1; i++)
            {
                float a = studCentres[i] + (studW * 0.5f);
                float b = studCentres[i + 1] - (studW * 0.5f);
                if (b - a < 0.15f)
                {
                    continue;
                }

                float y = noggingHeight + (high ? 0.05f : -0.05f);
                high = !high;

                if (OverlapsOpening((a + b) * 0.5f, y, openings) || OverlapsOpening(a + 0.02f, y, openings))
                {
                    continue;
                }

                mb.AddBox(new Vector3((a + b) * 0.5f, y, halfDepth),
                          new Vector3(b - a, plate, depth), submesh, Bevel);
            }
        }

        private static void BuildOpeningTrimmers(MeshBuilder mb, Opening o, float wallHeight, int submesh)
        {
            float plate = ShedDimensions.PlateThickness;
            float depth = ShedDimensions.StudDepth;
            float studW = ShedDimensions.StudWidth;
            float halfDepth = depth * 0.5f;
            float topPlateBottom = wallHeight - (plate * 2f);

            // Lintel over the opening. 140 x 45 on edge - the ordinary size for a
            // 0.9 m opening in a shed wall.
            const float LintelDepth = 0.140f;
            float lintelBottom = o.Top;
            float lintelTop = Mathf.Min(o.Top + LintelDepth, topPlateBottom);

            mb.AddBox(new Vector3(o.Centre, (lintelBottom + lintelTop) * 0.5f, halfDepth),
                      new Vector3(o.Width + (studW * 2f), lintelTop - lintelBottom, depth), submesh, Bevel);

            for (int side = 0; side < 2; side++)
            {
                float edge = side == 0 ? o.Start : o.End;
                float dir = side == 0 ? -1f : 1f;

                // Jack stud: carries the lintel, stops at its underside.
                float jackBottom = o.ReachesFloor ? 0f : plate;
                mb.AddBox(new Vector3(edge + (dir * studW * 0.5f), (jackBottom + lintelBottom) * 0.5f, halfDepth),
                          new Vector3(studW, lintelBottom - jackBottom, depth), submesh, Bevel);

                // King stud: full height, immediately outboard of the jack.
                float kingCentre = edge + (dir * studW * 1.5f);
                mb.AddBox(new Vector3(kingCentre, plate + ((topPlateBottom - plate) * 0.5f), halfDepth),
                          new Vector3(studW, topPlateBottom - plate, depth), submesh, Bevel);
            }

            // Sill trimmer and cripple studs under a window.
            if (!o.ReachesFloor)
            {
                mb.AddBox(new Vector3(o.Centre, o.Bottom - (plate * 0.5f), halfDepth),
                          new Vector3(o.Width, plate, depth), submesh, Bevel);

                float sillUnder = o.Bottom - plate;
                int cripples = Mathf.Max(1, Mathf.RoundToInt(o.Width / ShedDimensions.StudSpacing));
                for (int i = 0; i <= cripples; i++)
                {
                    float t = cripples == 0 ? 0.5f : (float)i / cripples;
                    float x = Mathf.Lerp(o.Start + (studW * 0.5f), o.End - (studW * 0.5f), t);
                    mb.AddBox(new Vector3(x, plate + ((sillUnder - plate) * 0.5f), halfDepth),
                              new Vector3(studW, sillUnder - plate, depth), submesh, Bevel);
                }
            }

            // Cripple studs between the lintel and the top plate.
            if (lintelTop < topPlateBottom - 0.03f)
            {
                int cripples = Mathf.Max(1, Mathf.RoundToInt(o.Width / ShedDimensions.StudSpacing));
                for (int i = 0; i <= cripples; i++)
                {
                    float t = cripples == 0 ? 0.5f : (float)i / cripples;
                    float x = Mathf.Lerp(o.Start + (studW * 0.5f), o.End - (studW * 0.5f), t);
                    mb.AddBox(new Vector3(x, (lintelTop + topPlateBottom) * 0.5f, halfDepth),
                              new Vector3(studW, topPlateBottom - lintelTop, depth), submesh, Bevel);
                }
            }
        }

        /// <summary>
        /// A flat panel - sheathing or cladding - with rectangular holes cut for the
        /// openings. Split on every opening edge, then emit the cells that survive.
        /// </summary>
        public static void AddPanelWithOpenings(MeshBuilder mb, float length, float height, float thickness,
                                                float zOffset, IList<Opening> openings, int submesh,
                                                float bevel = 0f)
        {
            List<float> xs = new List<float> { 0f, length };
            List<float> ys = new List<float> { 0f, height };

            if (openings != null)
            {
                foreach (Opening o in openings)
                {
                    AddSorted(xs, Mathf.Clamp(o.Start, 0f, length));
                    AddSorted(xs, Mathf.Clamp(o.End, 0f, length));
                    AddSorted(ys, Mathf.Clamp(o.Bottom, 0f, height));
                    AddSorted(ys, Mathf.Clamp(o.Top, 0f, height));
                }
            }

            xs.Sort();
            ys.Sort();

            for (int xi = 0; xi < xs.Count - 1; xi++)
            {
                for (int yi = 0; yi < ys.Count - 1; yi++)
                {
                    float x0 = xs[xi];
                    float x1 = xs[xi + 1];
                    float y0 = ys[yi];
                    float y1 = ys[yi + 1];

                    if (x1 - x0 < 0.001f || y1 - y0 < 0.001f)
                    {
                        continue;
                    }

                    float cx = (x0 + x1) * 0.5f;
                    float cy = (y0 + y1) * 0.5f;

                    if (OverlapsOpening(cx, cy, openings))
                    {
                        continue;
                    }

                    mb.AddBox(new Vector3(cx, cy, zOffset + (thickness * 0.5f)),
                              new Vector3(x1 - x0, y1 - y0, thickness), submesh, bevel);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static void AddSorted(List<float> list, float value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (Mathf.Abs(list[i] - value) < 0.0005f)
                {
                    return;
                }
            }
            list.Add(value);
        }

        private static bool OverlapsOpening(float x, float y, IList<Opening> openings)
        {
            if (openings == null)
            {
                return false;
            }

            foreach (Opening o in openings)
            {
                if (x > o.Start && x < o.End && y > o.Bottom && y < o.Top)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>True when a stud at this centre would land inside an opening.</summary>
        private static bool IsBlocked(float centre, float studWidth, IList<Opening> openings)
        {
            float half = studWidth * 0.5f;
            foreach (Opening o in openings)
            {
                if (centre + half > o.Start - 0.001f && centre - half < o.End + 0.001f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns the parts of [min,max] not covered by any hole interval.</summary>
        private static List<Vector2> Subtract(float min, float max, List<Vector2> holes)
        {
            List<Vector2> result = new List<Vector2> { new Vector2(min, max) };
            if (holes == null || holes.Count == 0)
            {
                return result;
            }

            foreach (Vector2 hole in holes)
            {
                List<Vector2> next = new List<Vector2>();
                foreach (Vector2 seg in result)
                {
                    if (hole.y <= seg.x || hole.x >= seg.y)
                    {
                        next.Add(seg);
                        continue;
                    }

                    if (hole.x > seg.x)
                    {
                        next.Add(new Vector2(seg.x, hole.x));
                    }
                    if (hole.y < seg.y)
                    {
                        next.Add(new Vector2(hole.y, seg.y));
                    }
                }
                result = next;
            }

            result.RemoveAll(s => s.y - s.x < 0.002f);
            return result;
        }
    }
}
