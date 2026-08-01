using System.Collections.Generic;
using UnityEngine;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Accumulates procedural geometry into a single mesh with one submesh per
    /// material. Whole assemblies - a wall, the workbench, a shelving unit - are
    /// built through one MeshBuilder so they cost a single renderer and only as
    /// many draw calls as they use distinct materials.
    ///
    /// Two details matter for the look of the shed:
    ///
    /// Bevels. AddBox takes a chamfer size. A chamfered box catches a highlight
    /// along every visible edge, which is the main thing that stops procedural
    /// geometry reading as an untouched primitive blockout. Chamfers cost
    /// vertices, so background props pass bevel = 0.
    ///
    /// UVs. Every face is unwrapped planar against its dominant local axis with
    /// coordinates measured in metres. A 1 m x 1 m patch of any surface therefore
    /// receives exactly one tile of its material regardless of the object it
    /// belongs to, which keeps texture scale consistent across the whole scene.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<List<int>> _submeshes = new List<List<int>>();
        private readonly Stack<Matrix4x4> _transformStack = new Stack<Matrix4x4>();

        private Matrix4x4 _current = Matrix4x4.identity;

        public string Name { get; }

        /// <summary>Metres per texture tile. 1 means one tile per metre.</summary>
        public float UvScale { get; set; } = 1f;

        /// <summary>
        /// Forces the direction the material's V axis runs, in builder-local space.
        /// Leave it null and each piece uses its own longest axis instead.
        ///
        /// Set it where the material has a direction the geometry does not imply -
        /// weatherboards have to lap horizontally whichever way the wall is longer,
        /// and pegboard holes have to stay on their grid.
        /// </summary>
        public Vector3? GrainOverride { get; set; }

        /// <summary>
        /// Slides the whole map along the surface, in tiles.
        ///
        /// The maps tile every metre, so without this every floorboard samples the
        /// same square of texture and the floor reads as twenty-nine copies of one
        /// board. Giving each board its own offset costs nothing and is the single
        /// cheapest way to break that repeat.
        /// </summary>
        public Vector2 UvOffset { get; set; }

        /// <summary>Resolved grain for the primitive currently being emitted.</summary>
        private Vector3 _grain;

        public MeshBuilder(string name, int submeshCount = 1)
        {
            Name = name;
            for (int i = 0; i < Mathf.Max(1, submeshCount); i++)
            {
                _submeshes.Add(new List<int>());
            }
        }

        public int SubmeshCount => _submeshes.Count;

        public int VertexCount => _vertices.Count;

        public int TriangleCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _submeshes.Count; i++)
                {
                    total += _submeshes[i].Count / 3;
                }
                return total;
            }
        }

        /// <summary>Grows the submesh list so <paramref name="index"/> is valid.</summary>
        public void EnsureSubmesh(int index)
        {
            while (_submeshes.Count <= index)
            {
                _submeshes.Add(new List<int>());
            }
        }

        // ------------------------------------------------------------------
        // Transform stack
        // ------------------------------------------------------------------

        public void Push(Vector3 position, Quaternion rotation)
        {
            Push(Matrix4x4.TRS(position, rotation, Vector3.one));
        }

        public void Push(Vector3 position)
        {
            Push(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one));
        }

        public void Push(Matrix4x4 matrix)
        {
            _transformStack.Push(_current);
            _current = _current * matrix;
        }

        public void Pop()
        {
            _current = _transformStack.Count > 0 ? _transformStack.Pop() : Matrix4x4.identity;
        }

        // ------------------------------------------------------------------
        // Core primitives
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a quad with a flat normal. Vertices must be given counter-clockwise
        /// when viewed from the front face.
        /// </summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int submesh)
        {
            a = _current.MultiplyPoint3x4(a);
            b = _current.MultiplyPoint3x4(b);
            c = _current.MultiplyPoint3x4(c);
            d = _current.MultiplyPoint3x4(d);

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int baseIndex = _vertices.Count;

            AppendVertex(a, normal);
            AppendVertex(b, normal);
            AppendVertex(c, normal);
            AppendVertex(d, normal);

            EnsureSubmesh(submesh);
            List<int> tris = _submeshes[submesh];
            tris.Add(baseIndex);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }

        /// <summary>Adds a triangle with a flat normal.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, int submesh)
        {
            a = _current.MultiplyPoint3x4(a);
            b = _current.MultiplyPoint3x4(b);
            c = _current.MultiplyPoint3x4(c);

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int baseIndex = _vertices.Count;

            AppendVertex(a, normal);
            AppendVertex(b, normal);
            AppendVertex(c, normal);

            EnsureSubmesh(submesh);
            List<int> tris = _submeshes[submesh];
            tris.Add(baseIndex);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
        }

        // ------------------------------------------------------------------
        // Boxes
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds an axis-aligned box centred on <paramref name="centre"/>.
        /// A positive <paramref name="bevel"/> chamfers all twelve edges.
        /// </summary>
        public void AddBox(Vector3 centre, Vector3 size, int submesh, float bevel = 0f)
        {
            AddBox(centre, size, Quaternion.identity, submesh, bevel);
        }

        /// <summary>Adds a rotated box. Rotation is applied about the box centre.</summary>
        public void AddBox(Vector3 centre, Vector3 size, Quaternion rotation, int submesh, float bevel = 0f)
        {
            Vector3 half = size * 0.5f;
            half.x = Mathf.Max(half.x, 0.0001f);
            half.y = Mathf.Max(half.y, 0.0001f);
            half.z = Mathf.Max(half.z, 0.0001f);

            // Never let a chamfer eat more than a third of the smallest dimension;
            // that keeps thin boards (19 mm) from collapsing into a wedge.
            float maxBevel = Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.66f;
            float b = Mathf.Clamp(bevel, 0f, maxBevel);

            Push(Matrix4x4.TRS(centre, rotation, Vector3.one));

            // Sawn timber's grain runs along the length of the piece, so that is
            // what the V axis follows unless a caller has forced otherwise. Every
            // face of the box shares it, which is what makes the grain wrap round
            // a board continuously instead of turning a corner.
            SetGrain(LongestAxis(size));

            if (b <= 0.00005f)
            {
                AddPlainBox(half, submesh);
            }
            else
            {
                AddChamferedBox(half, b, submesh);
            }

            Pop();
            _grain = Vector3.zero;
        }

        /// <summary>The piece's own long axis, in its local space.</summary>
        private static Vector3 LongestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
            {
                return Vector3.right;
            }
            return size.y >= size.z ? Vector3.up : Vector3.forward;
        }

        /// <summary>
        /// Records the grain direction in builder space. An explicit override wins;
        /// otherwise the local axis is carried through the current transform, so a
        /// rafter tilted to the roof pitch keeps its grain along the rafter rather
        /// than along the world axis it happens to point closest to.
        /// </summary>
        private void SetGrain(Vector3 localAxis)
        {
            if (GrainOverride.HasValue)
            {
                Vector3 forced = GrainOverride.Value;
                _grain = forced.sqrMagnitude > 1e-8f ? forced.normalized : Vector3.zero;
                return;
            }

            Vector3 inBuilderSpace = _current.MultiplyVector(localAxis);
            _grain = inBuilderSpace.sqrMagnitude > 1e-8f ? inBuilderSpace.normalized : Vector3.zero;
        }

        private void AddPlainBox(Vector3 h, int submesh)
        {
            // +X / -X
            AddQuad(new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z),
                    new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), submesh);
            AddQuad(new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z),
                    new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z), submesh);
            // +Y / -Y
            AddQuad(new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z),
                    new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z), submesh);
            AddQuad(new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z),
                    new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z), submesh);
            // +Z / -Z
            AddQuad(new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z),
                    new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), submesh);
            AddQuad(new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z),
                    new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), submesh);
        }

        /// <summary>
        /// Chamfered box: six inset faces, twelve edge chamfers and eight corner
        /// triangles. 96 vertices and 44 triangles, spent only where an edge is
        /// close enough to the player to read.
        /// </summary>
        private void AddChamferedBox(Vector3 h, float b, int submesh)
        {
            Vector3 i = new Vector3(h.x - b, h.y - b, h.z - b);

            // Corner helper: for sign triple s, the three vertices pushed out to
            // each of the adjacent faces.
            Vector3 FaceX(int sx, int sy, int sz) => new Vector3(sx * h.x, sy * i.y, sz * i.z);
            Vector3 FaceY(int sx, int sy, int sz) => new Vector3(sx * i.x, sy * h.y, sz * i.z);
            Vector3 FaceZ(int sx, int sy, int sz) => new Vector3(sx * i.x, sy * i.y, sz * h.z);

            // --- six inset faces -------------------------------------------------
            AddQuad(FaceX(1, -1, 1), FaceX(1, -1, -1), FaceX(1, 1, -1), FaceX(1, 1, 1), submesh);
            AddQuad(FaceX(-1, -1, -1), FaceX(-1, -1, 1), FaceX(-1, 1, 1), FaceX(-1, 1, -1), submesh);
            AddQuad(FaceY(-1, 1, 1), FaceY(1, 1, 1), FaceY(1, 1, -1), FaceY(-1, 1, -1), submesh);
            AddQuad(FaceY(-1, -1, -1), FaceY(1, -1, -1), FaceY(1, -1, 1), FaceY(-1, -1, 1), submesh);
            AddQuad(FaceZ(-1, -1, 1), FaceZ(1, -1, 1), FaceZ(1, 1, 1), FaceZ(-1, 1, 1), submesh);
            AddQuad(FaceZ(1, -1, -1), FaceZ(-1, -1, -1), FaceZ(-1, 1, -1), FaceZ(1, 1, -1), submesh);

            // --- twelve edge chamfers -------------------------------------------
            // Edges running along Z (vary sx, sy).
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Vector3 xNear = FaceX(sx, sy, -1);
                    Vector3 xFar = FaceX(sx, sy, 1);
                    Vector3 yNear = FaceY(sx, sy, -1);
                    Vector3 yFar = FaceY(sx, sy, 1);
                    if (sx * sy > 0)
                    {
                        AddQuad(xNear, xFar, yFar, yNear, submesh);
                    }
                    else
                    {
                        AddQuad(yNear, yFar, xFar, xNear, submesh);
                    }
                }
            }

            // Edges running along X (vary sy, sz).
            for (int sy = -1; sy <= 1; sy += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 yNear = FaceY(-1, sy, sz);
                    Vector3 yFar = FaceY(1, sy, sz);
                    Vector3 zNear = FaceZ(-1, sy, sz);
                    Vector3 zFar = FaceZ(1, sy, sz);
                    if (sy * sz > 0)
                    {
                        AddQuad(yNear, yFar, zFar, zNear, submesh);
                    }
                    else
                    {
                        AddQuad(zNear, zFar, yFar, yNear, submesh);
                    }
                }
            }

            // Edges running along Y (vary sx, sz).
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 zLow = FaceZ(sx, -1, sz);
                    Vector3 zHigh = FaceZ(sx, 1, sz);
                    Vector3 xLow = FaceX(sx, -1, sz);
                    Vector3 xHigh = FaceX(sx, 1, sz);
                    if (sx * sz > 0)
                    {
                        AddQuad(zLow, zHigh, xHigh, xLow, submesh);
                    }
                    else
                    {
                        AddQuad(xLow, xHigh, zHigh, zLow, submesh);
                    }
                }
            }

            // --- eight corner triangles -----------------------------------------
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 vx = FaceX(sx, sy, sz);
                        Vector3 vy = FaceY(sx, sy, sz);
                        Vector3 vz = FaceZ(sx, sy, sz);
                        // Winding flips with the parity of the corner's signs.
                        if (sx * sy * sz > 0)
                        {
                            AddTriangle(vx, vy, vz, submesh);
                        }
                        else
                        {
                            AddTriangle(vx, vz, vy, submesh);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Cylinders and discs
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a cylinder or truncated cone about the local Y axis, centred on
        /// <paramref name="centre"/>. Used for conduit, handles, buckets, pots,
        /// paint tins, wheels and fastener heads.
        /// </summary>
        public void AddCylinder(Vector3 centre, float radiusBottom, float radiusTop, float height,
                                int segments, int submesh, Quaternion rotation, bool capBottom = true,
                                bool capTop = true, float arcDegrees = 360f)
        {
            segments = Mathf.Max(3, segments);
            float half = height * 0.5f;
            bool fullArc = arcDegrees >= 359.9f;

            Push(Matrix4x4.TRS(centre, rotation, Vector3.one));

            int sides = fullArc ? segments : segments;
            float step = (arcDegrees * Mathf.Deg2Rad) / segments;

            // Side wall with smooth radial normals.
            int baseIndex = _vertices.Count;
            for (int s = 0; s <= sides; s++)
            {
                float a = step * s;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);
                Vector3 dir = new Vector3(cos, 0f, sin);

                Vector3 pLow = _current.MultiplyPoint3x4(new Vector3(cos * radiusBottom, -half, sin * radiusBottom));
                Vector3 pHigh = _current.MultiplyPoint3x4(new Vector3(cos * radiusTop, half, sin * radiusTop));

                // Slope-corrected normal so cones light correctly.
                float slope = (radiusBottom - radiusTop) / Mathf.Max(height, 0.0001f);
                Vector3 n = _current.MultiplyVector(new Vector3(dir.x, slope, dir.z)).normalized;

                float u = (a * Mathf.Max(radiusBottom, radiusTop)) * UvScale;
                AppendVertexRaw(pLow, n, new Vector2(u, -half * UvScale));
                AppendVertexRaw(pHigh, n, new Vector2(u, half * UvScale));
            }

            EnsureSubmesh(submesh);
            List<int> tris = _submeshes[submesh];
            for (int s = 0; s < sides; s++)
            {
                int i0 = baseIndex + (s * 2);
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }

            if (capTop && radiusTop > 0.00001f)
            {
                AddDisc(new Vector3(0f, half, 0f), radiusTop, segments, submesh, true, arcDegrees);
            }
            if (capBottom && radiusBottom > 0.00001f)
            {
                AddDisc(new Vector3(0f, -half, 0f), radiusBottom, segments, submesh, false, arcDegrees);
            }

            Pop();
        }

        public void AddCylinder(Vector3 centre, float radius, float height, int segments, int submesh)
        {
            AddCylinder(centre, radius, radius, height, segments, submesh, Quaternion.identity);
        }

        private void AddDisc(Vector3 localCentre, float radius, int segments, int submesh, bool facingUp, float arcDegrees)
        {
            float step = (arcDegrees * Mathf.Deg2Rad) / segments;
            Vector3 worldCentre = _current.MultiplyPoint3x4(localCentre);
            Vector3 normal = _current.MultiplyVector(facingUp ? Vector3.up : Vector3.down).normalized;

            int centreIndex = _vertices.Count;
            AppendVertexRaw(worldCentre, normal, Vector2.zero);

            for (int s = 0; s <= segments; s++)
            {
                float a = step * s;
                Vector3 p = localCentre + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Vector3 wp = _current.MultiplyPoint3x4(p);
                AppendVertexRaw(wp, normal,
                    new Vector2(Mathf.Cos(a) * radius * UvScale, Mathf.Sin(a) * radius * UvScale));
            }

            EnsureSubmesh(submesh);
            List<int> tris = _submeshes[submesh];
            for (int s = 0; s < segments; s++)
            {
                int i1 = centreIndex + 1 + s;
                int i2 = centreIndex + 2 + s;
                if (facingUp)
                {
                    tris.Add(centreIndex); tris.Add(i2); tris.Add(i1);
                }
                else
                {
                    tris.Add(centreIndex); tris.Add(i1); tris.Add(i2);
                }
            }
        }

        // ------------------------------------------------------------------
        // Profile extrusion - used for trim, sills, weatherboards and angle iron
        // ------------------------------------------------------------------

        /// <summary>
        /// Extrudes a closed 2D profile (given in the local XY plane, counter-clockwise)
        /// along local +Z for <paramref name="length"/> metres, with flat side normals
        /// and capped ends. Moulded trim, window sills and L-section brackets are all
        /// built this way rather than as stacks of boxes.
        /// </summary>
        public void AddExtrusion(Vector2[] profile, float length, Vector3 origin, Quaternion rotation, int submesh)
        {
            if (profile == null || profile.Length < 3)
            {
                return;
            }

            Push(Matrix4x4.TRS(origin, rotation, Vector3.one));

            // An extrusion's length is its local +Z, which is the way the timber runs.
            SetGrain(Vector3.forward);

            float z0 = 0f;
            float z1 = length;

            for (int i = 0; i < profile.Length; i++)
            {
                Vector2 p0 = profile[i];
                Vector2 p1 = profile[(i + 1) % profile.Length];

                AddQuad(new Vector3(p0.x, p0.y, z0), new Vector3(p1.x, p1.y, z0),
                        new Vector3(p1.x, p1.y, z1), new Vector3(p0.x, p0.y, z1), submesh);
            }

            // Caps by fanning from the first vertex. Profiles used in this project are
            // convex or mildly L-shaped, which a fan handles correctly.
            for (int i = 1; i < profile.Length - 1; i++)
            {
                AddTriangle(new Vector3(profile[0].x, profile[0].y, z1),
                            new Vector3(profile[i].x, profile[i].y, z1),
                            new Vector3(profile[i + 1].x, profile[i + 1].y, z1), submesh);
                AddTriangle(new Vector3(profile[0].x, profile[0].y, z0),
                            new Vector3(profile[i + 1].x, profile[i + 1].y, z0),
                            new Vector3(profile[i].x, profile[i].y, z0), submesh);
            }

            Pop();
            _grain = Vector3.zero;
        }

        // ------------------------------------------------------------------
        // Corrugated sheet - the roof covering
        // ------------------------------------------------------------------

        /// <summary>
        /// A single sheet of corrugated steel lying in the local XZ plane, corrugations
        /// running along local Z. Smooth normals across the profile so it reads as
        /// rolled metal rather than faceted geometry.
        /// </summary>
        public void AddCorrugatedSheet(Vector3 origin, Quaternion rotation, float width, float length,
                                       float pitch, float amplitude, int submesh)
        {
            int ribs = Mathf.Max(2, Mathf.RoundToInt(width / pitch));
            int samplesPerRib = 6;
            int samples = ribs * samplesPerRib;

            Push(Matrix4x4.TRS(origin, rotation, Vector3.one));

            int baseIndex = _vertices.Count;
            for (int s = 0; s <= samples; s++)
            {
                float t = (float)s / samples;
                float x = t * width;
                float phase = (x / pitch) * Mathf.PI * 2f;
                float y = Mathf.Sin(phase) * amplitude * 0.5f;
                float dy = Mathf.Cos(phase) * amplitude * 0.5f * (Mathf.PI * 2f / pitch);

                Vector3 n = _current.MultiplyVector(new Vector3(-dy, 1f, 0f)).normalized;
                Vector3 pNear = _current.MultiplyPoint3x4(new Vector3(x, y, 0f));
                Vector3 pFar = _current.MultiplyPoint3x4(new Vector3(x, y, length));

                AppendVertexRaw(pNear, n, new Vector2(x * UvScale, 0f));
                AppendVertexRaw(pFar, n, new Vector2(x * UvScale, length * UvScale));
            }

            EnsureSubmesh(submesh);
            List<int> tris = _submeshes[submesh];
            for (int s = 0; s < samples; s++)
            {
                int i0 = baseIndex + (s * 2);
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i2); tris.Add(i1); tris.Add(i3);
            }

            Pop();
        }

        // ------------------------------------------------------------------
        // Vertex plumbing
        // ------------------------------------------------------------------

        private void AppendVertex(Vector3 worldPosition, Vector3 normal)
        {
            AppendVertexRaw(worldPosition, normal, PlanarUv(worldPosition, normal));
        }

        private void AppendVertexRaw(Vector3 position, Vector3 normal, Vector2 uv)
        {
            _vertices.Add(position);
            _normals.Add(normal);
            _uvs.Add(uv);
        }

        /// <summary>
        /// Planar projection in metres, which is what keeps texture scale identical
        /// on every surface in the shed.
        ///
        /// Where a grain direction is known the frame is built from it: V follows
        /// the grain projected onto the face and U runs across it. Projecting
        /// against the dominant world axis instead - the obvious approach - puts
        /// vertical grain on every horizontal member, because the side of a rafter
        /// faces sideways whichever way the rafter runs. Faces whose normal is
        /// parallel to the grain are the sawn ends, and fall through to the
        /// axis-aligned mapping so they read as end grain.
        /// </summary>
        private Vector2 PlanarUv(Vector3 p, Vector3 n)
        {
            // Raw quads and triangles never set a per-piece grain, so an override
            // still applies to them - that is how a hand-built shape like a gable
            // end gets the same treatment as a box.
            Vector3 grain = _grain.sqrMagnitude > 1e-8f
                ? _grain
                : (GrainOverride ?? Vector3.zero);

            if (grain.sqrMagnitude > 1e-8f)
            {
                Vector3 along = grain - (n * Vector3.Dot(grain, n));
                if (along.sqrMagnitude > 1e-4f)
                {
                    along.Normalize();
                    Vector3 across = Vector3.Cross(n, along);
                    return (new Vector2(Vector3.Dot(p, across), Vector3.Dot(p, along)) * UvScale)
                           + UvOffset;
                }
            }

            float ax = Mathf.Abs(n.x);
            float ay = Mathf.Abs(n.y);
            float az = Mathf.Abs(n.z);

            Vector2 uv;
            if (ax >= ay && ax >= az)
            {
                uv = new Vector2(p.z, p.y);
            }
            else if (ay >= ax && ay >= az)
            {
                uv = new Vector2(p.x, p.z);
            }
            else
            {
                uv = new Vector2(p.x, p.y);
            }

            return (uv * UvScale) + UvOffset;
        }

        // ------------------------------------------------------------------
        // Output
        // ------------------------------------------------------------------

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh { name = Name };
            if (_vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);

            // Lightmap UVs share channel 0's layout; because channel 0 is already a
            // non-overlapping metre-scale planar unwrap per face, the generator asks
            // Unity to build a proper packed UV2 at import time instead.
            mesh.subMeshCount = _submeshes.Count;
            for (int i = 0; i < _submeshes.Count; i++)
            {
                mesh.SetTriangles(_submeshes[i], i, true);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
