// Working implementations of UnityEngine's math types.
//
// Unlike the rest of the harness these are not empty stubs. Vector, quaternion,
// matrix and Mathf semantics are well-defined and documented, so implementing
// them faithfully is not circular - and it is what lets the geometry and
// dimension tests actually execute rather than merely compile.
//
// Conventions matched to Unity deliberately:
//   - left-handed, Y up, Z forward
//   - Quaternion.Euler applies Z, then X, then Y (composed as qy * qx * qz)
//   - Matrix4x4.TRS composes as T * R * S
//   - MultiplyPoint3x4 skips the perspective divide; MultiplyVector uses the
//     rotation/scale 3x3 only

using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public float this[int index]
        {
            get { return index == 0 ? x : y; }
            set { if (index == 0) x = value; else y = value; }
        }

        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 one { get { return new Vector2(1f, 1f); } }
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public static Vector2 down { get { return new Vector2(0f, -1f); } }
        public static Vector2 left { get { return new Vector2(-1f, 0f); } }
        public static Vector2 right { get { return new Vector2(1f, 0f); } }

        public float magnitude { get { return (float)Math.Sqrt((x * x) + (y * y)); } }
        public float sqrMagnitude { get { return (x * x) + (y * y); } }

        public Vector2 normalized
        {
            get
            {
                float m = magnitude;
                return m > 1e-05f ? new Vector2(x / m, y / m) : zero;
            }
        }

        public void Normalize() { this = normalized; }
        public void Set(float newX, float newY) { x = newX; y = newY; }

        public static float Dot(Vector2 a, Vector2 b) { return (a.x * b.x) + (a.y * b.y); }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) { return new Vector2(a.x + ((b.x - a.x) * t), a.y + ((b.y - a.y) * t)); }
        public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 Min(Vector2 a, Vector2 b) { return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
        public static Vector2 Max(Vector2 a, Vector2 b) { return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }
        public static Vector2 Perpendicular(Vector2 d) { return new Vector2(-d.y, d.x); }

        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
        {
            return vector.sqrMagnitude > maxLength * maxLength ? vector.normalized * maxLength : vector;
        }

        public static float Angle(Vector2 from, Vector2 to)
        {
            float denom = (float)Math.Sqrt(from.sqrMagnitude * (double)to.sqrMagnitude);
            if (denom < 1e-15f) return 0f;
            return (float)Math.Acos(Mathf.Clamp(Dot(from, to) / denom, -1f, 1f)) * Mathf.Rad2Deg;
        }

        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            return Angle(from, to) * Mathf.Sign((from.x * to.y) - (from.y * to.x));
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
        public static bool operator ==(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }

        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }

        public override bool Equals(object other) { return other is Vector2 v && this == v; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2); }
        public override string ToString() { return $"({x:F2}, {y:F2})"; }
        public string ToString(string format) { return $"({x.ToString(format)}, {y.ToString(format)})"; }
    }

    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
        public static Vector2Int one { get { return new Vector2Int(1, 1); } }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
        public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }

        public override bool Equals(object other) { return other is Vector2Int v && this == v; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2); }
        public override string ToString() { return $"({x}, {y})"; }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }

        public float this[int index]
        {
            get { return index == 0 ? x : index == 1 ? y : z; }
            set { if (index == 0) x = value; else if (index == 1) y = value; else z = value; }
        }

        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }
        public static Vector3 up { get { return new Vector3(0f, 1f, 0f); } }
        public static Vector3 down { get { return new Vector3(0f, -1f, 0f); } }
        public static Vector3 left { get { return new Vector3(-1f, 0f, 0f); } }
        public static Vector3 right { get { return new Vector3(1f, 0f, 0f); } }
        public static Vector3 forward { get { return new Vector3(0f, 0f, 1f); } }
        public static Vector3 back { get { return new Vector3(0f, 0f, -1f); } }
        public static Vector3 positiveInfinity { get { return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity); } }
        public static Vector3 negativeInfinity { get { return new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); } }

        public float magnitude { get { return (float)Math.Sqrt((x * x) + (y * y) + (z * z)); } }
        public float sqrMagnitude { get { return (x * x) + (y * y) + (z * z); } }

        public Vector3 normalized
        {
            get
            {
                float m = magnitude;
                return m > 1e-05f ? new Vector3(x / m, y / m, z / m) : zero;
            }
        }

        public void Normalize() { this = normalized; }
        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }
        public void Scale(Vector3 s) { x *= s.x; y *= s.y; z *= s.z; }

        public static float Dot(Vector3 a, Vector3 b) { return (a.x * b.x) + (a.y * b.y) + (a.z * b.z); }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3((a.y * b.z) - (a.z * b.y),
                               (a.z * b.x) - (a.x * b.z),
                               (a.x * b.y) - (a.y * b.x));
        }

        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public static float Magnitude(Vector3 v) { return v.magnitude; }
        public static float SqrMagnitude(Vector3 v) { return v.sqrMagnitude; }
        public static Vector3 Normalize(Vector3 v) { return v.normalized; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) { return new Vector3(a.x + ((b.x - a.x) * t), a.y + ((b.y - a.y) * t), a.z + ((b.z - a.z) * t)); }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
        public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
        public static Vector3 Project(Vector3 v, Vector3 onNormal)
        {
            float d = Dot(onNormal, onNormal);
            return d < Mathf.Epsilon ? zero : onNormal * (Dot(v, onNormal) / d);
        }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal) { return v - Project(v, planeNormal); }
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) { return (-2f * Dot(inNormal, inDirection) * inNormal) + inDirection; }

        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
        {
            return vector.sqrMagnitude > maxLength * maxLength ? vector.normalized * maxLength : vector;
        }

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            Vector3 delta = target - current;
            float dist = delta.magnitude;
            if (dist <= maxDistanceDelta || dist < 1e-06f) return target;
            return current + (delta / dist * maxDistanceDelta);
        }

        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            float ma = a.magnitude;
            float mb = b.magnitude;
            if (ma < 1e-06f || mb < 1e-06f) return Lerp(a, b, t);
            Vector3 na = a / ma;
            Vector3 nb = b / mb;
            float dot = Mathf.Clamp(Dot(na, nb), -1f, 1f);
            float theta = (float)Math.Acos(dot) * t;
            Vector3 relative = (nb - (na * dot)).normalized;
            return (((na * (float)Math.Cos(theta)) + (relative * (float)Math.Sin(theta))) * Mathf.Lerp(ma, mb, t));
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, Time.deltaTime);
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float xt = omega * deltaTime;
            float exp = 1f / (1f + xt + (0.48f * xt * xt) + (0.235f * xt * xt * xt));
            Vector3 change = ClampMagnitude(current - target, maxSpeed * smoothTime);
            Vector3 dest = current - change;
            Vector3 temp = (currentVelocity + (omega * change)) * deltaTime;
            currentVelocity = (currentVelocity - (omega * temp)) * exp;
            return dest + ((change + temp) * exp);
        }

        public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta)
        {
            // Adequate for the harness: magnitude moves linearly, direction slerps.
            float m = Mathf.MoveTowards(current.magnitude, target.magnitude, maxMagnitudeDelta);
            float angle = Angle(current, target) * Mathf.Deg2Rad;
            float t = angle < 1e-06f ? 1f : Mathf.Clamp01(maxRadiansDelta / angle);
            return Slerp(current.normalized, target.normalized, t) * m;
        }

        public static float Angle(Vector3 from, Vector3 to)
        {
            float denom = (float)Math.Sqrt(from.sqrMagnitude * (double)to.sqrMagnitude);
            if (denom < 1e-15f) return 0f;
            return (float)Math.Acos(Mathf.Clamp(Dot(from, to) / denom, -1f, 1f)) * Mathf.Rad2Deg;
        }

        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            return Angle(from, to) * Mathf.Sign(Dot(axis, Cross(from, to)));
        }

        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
        {
            normal = normal.normalized;
            tangent = (tangent - Project(tangent, normal)).normalized;
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }

        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }

        public override bool Equals(object other) { return other is Vector3 v && this == v; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
        public override string ToString() { return $"({x:F2}, {y:F2}, {z:F2})"; }
        public string ToString(string format) { return $"({x.ToString(format)}, {y.ToString(format)}, {z.ToString(format)})"; }
    }

    public struct Vector3Int
    {
        public int x;
        public int y;
        public int z;

        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3Int zero { get { return new Vector3Int(0, 0, 0); } }
        public static Vector3Int one { get { return new Vector3Int(1, 1, 1); } }

        public static Vector3Int operator +(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static bool operator ==(Vector3Int a, Vector3Int b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
        public static bool operator !=(Vector3Int a, Vector3Int b) { return !(a == b); }

        public override bool Equals(object other) { return other is Vector3Int v && this == v; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
        public override string ToString() { return $"({x}, {y}, {z})"; }
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 0f; }
        public Vector4(float x, float y) { this.x = x; this.y = y; this.z = 0f; this.w = 0f; }

        public float this[int index]
        {
            get { return index == 0 ? x : index == 1 ? y : index == 2 ? z : w; }
            set { if (index == 0) x = value; else if (index == 1) y = value; else if (index == 2) z = value; else w = value; }
        }

        public static Vector4 zero { get { return new Vector4(0f, 0f, 0f, 0f); } }
        public static Vector4 one { get { return new Vector4(1f, 1f, 1f, 1f); } }

        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public float sqrMagnitude { get { return (x * x) + (y * y) + (z * z) + (w * w); } }

        public Vector4 normalized
        {
            get
            {
                float m = magnitude;
                return m > 1e-05f ? new Vector4(x / m, y / m, z / m, w / m) : zero;
            }
        }

        public void Normalize() { this = normalized; }
        public void Set(float newX, float newY, float newZ, float newW) { x = newX; y = newY; z = newZ; w = newW; }

        public static float Dot(Vector4 a, Vector4 b) { return (a.x * b.x) + (a.y * b.y) + (a.z * b.z) + (a.w * b.w); }
        public static float Distance(Vector4 a, Vector4 b) { return (a - b).magnitude; }
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) { t = Mathf.Clamp01(t); return new Vector4(a.x + ((b.x - a.x) * t), a.y + ((b.y - a.y) * t), a.z + ((b.z - a.z) * t), a.w + ((b.w - a.w) * t)); }
        public static Vector4 Scale(Vector4 a, Vector4 b) { return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w); }

        public static Vector4 operator +(Vector4 a, Vector4 b) { return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); }
        public static Vector4 operator -(Vector4 a, Vector4 b) { return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); }
        public static Vector4 operator -(Vector4 a) { return new Vector4(-a.x, -a.y, -a.z, -a.w); }
        public static Vector4 operator *(Vector4 a, float d) { return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d); }
        public static Vector4 operator *(float d, Vector4 a) { return a * d; }
        public static Vector4 operator /(Vector4 a, float d) { return new Vector4(a.x / d, a.y / d, a.z / d, a.w / d); }
        public static bool operator ==(Vector4 a, Vector4 b) { return (a - b).sqrMagnitude < 9.99999944E-11f; }
        public static bool operator !=(Vector4 a, Vector4 b) { return !(a == b); }

        public static implicit operator Vector4(Vector3 v) { return new Vector4(v.x, v.y, v.z, 0f); }
        public static implicit operator Vector3(Vector4 v) { return new Vector3(v.x, v.y, v.z); }
        public static implicit operator Vector4(Vector2 v) { return new Vector4(v.x, v.y, 0f, 0f); }
        public static implicit operator Vector2(Vector4 v) { return new Vector2(v.x, v.y); }

        public override bool Equals(object other) { return other is Vector4 v && this == v; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1); }
        public override string ToString() { return $"({x:F2}, {y:F2}, {z:F2}, {w:F2})"; }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public float this[int index]
        {
            get { return index == 0 ? x : index == 1 ? y : index == 2 ? z : w; }
            set { if (index == 0) x = value; else if (index == 1) y = value; else if (index == 2) z = value; else w = value; }
        }

        public static Quaternion identity { get { return new Quaternion(0f, 0f, 0f, 1f); } }

        public Quaternion normalized
        {
            get
            {
                float m = (float)Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
                return m < 1e-06f ? identity : new Quaternion(x / m, y / m, z / m, w / m);
            }
        }

        public Vector3 eulerAngles
        {
            get { return ToEuler(this); }
            set { this = Euler(value); }
        }

        public void Set(float newX, float newY, float newZ, float newW) { x = newX; y = newY; z = newZ; w = newW; }
        public void SetLookRotation(Vector3 view) { this = LookRotation(view); }
        public void SetLookRotation(Vector3 view, Vector3 up) { this = LookRotation(view, up); }

        public void ToAngleAxis(out float angle, out Vector3 axis)
        {
            Quaternion q = normalized;
            angle = 2f * (float)Math.Acos(Mathf.Clamp(q.w, -1f, 1f)) * Mathf.Rad2Deg;
            float s = (float)Math.Sqrt(1f - (q.w * q.w));
            axis = s < 1e-04f ? Vector3.right : new Vector3(q.x / s, q.y / s, q.z / s);
        }

        public static Quaternion Euler(float x, float y, float z)
        {
            // Unity applies Z first, then X, then Y, which composes as qy * qx * qz.
            float hx = x * Mathf.Deg2Rad * 0.5f;
            float hy = y * Mathf.Deg2Rad * 0.5f;
            float hz = z * Mathf.Deg2Rad * 0.5f;

            float sx = (float)Math.Sin(hx), cx = (float)Math.Cos(hx);
            float sy = (float)Math.Sin(hy), cy = (float)Math.Cos(hy);
            float sz = (float)Math.Sin(hz), cz = (float)Math.Cos(hz);

            Quaternion qx = new Quaternion(sx, 0f, 0f, cx);
            Quaternion qy = new Quaternion(0f, sy, 0f, cy);
            Quaternion qz = new Quaternion(0f, 0f, sz, cz);

            return qy * qx * qz;
        }

        public static Quaternion Euler(Vector3 euler) { return Euler(euler.x, euler.y, euler.z); }

        private static Vector3 ToEuler(Quaternion q)
        {
            q = q.normalized;

            // Inverse of the Y-X-Z composition above.
            float sinX = 2f * ((q.w * q.x) - (q.y * q.z));
            sinX = Mathf.Clamp(sinX, -1f, 1f);
            float ex = (float)Math.Asin(sinX);

            float ey, ez;
            if (Math.Abs(sinX) > 0.9999f)
            {
                // Gimbal lock: fold the Z rotation into Y.
                ey = (float)Math.Atan2(2f * ((q.w * q.y) + (q.x * q.z)), 1f - (2f * ((q.x * q.x) + (q.y * q.y))));
                ez = 0f;
            }
            else
            {
                ey = (float)Math.Atan2(2f * ((q.w * q.y) + (q.x * q.z)), 1f - (2f * ((q.x * q.x) + (q.y * q.y))));
                ez = (float)Math.Atan2(2f * ((q.w * q.z) + (q.x * q.y)), 1f - (2f * ((q.x * q.x) + (q.z * q.z))));
            }

            Vector3 deg = new Vector3(ex, ey, ez) * Mathf.Rad2Deg;
            return new Vector3(Normalise360(deg.x), Normalise360(deg.y), Normalise360(deg.z));
        }

        private static float Normalise360(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            Vector3 a = axis.normalized;
            float half = angle * Mathf.Deg2Rad * 0.5f;
            float s = (float)Math.Sin(half);
            return new Quaternion(a.x * s, a.y * s, a.z * s, (float)Math.Cos(half));
        }

        public static Quaternion LookRotation(Vector3 forward) { return LookRotation(forward, Vector3.up); }

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            Vector3 f = forward.normalized;
            if (f.sqrMagnitude < 1e-10f) return identity;

            // Left-handed basis, matching Unity: right = up x forward.
            Vector3 r = Vector3.Cross(upwards, f).normalized;
            if (r.sqrMagnitude < 1e-10f)
            {
                r = Vector3.Cross(Math.Abs(f.y) > 0.99f ? Vector3.forward : Vector3.up, f).normalized;
            }
            Vector3 u = Vector3.Cross(f, r);

            float trace = r.x + u.y + f.z;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                return new Quaternion((u.z - f.y) / s, (f.x - r.z) / s, (r.y - u.x) / s, 0.25f * s);
            }
            if (r.x > u.y && r.x > f.z)
            {
                float s = (float)Math.Sqrt(1f + r.x - u.y - f.z) * 2f;
                return new Quaternion(0.25f * s, (u.x + r.y) / s, (f.x + r.z) / s, (u.z - f.y) / s);
            }
            if (u.y > f.z)
            {
                float s = (float)Math.Sqrt(1f + u.y - r.x - f.z) * 2f;
                return new Quaternion((u.x + r.y) / s, 0.25f * s, (f.y + u.z) / s, (f.x - r.z) / s);
            }
            {
                float s = (float)Math.Sqrt(1f + f.z - r.x - u.y) * 2f;
                return new Quaternion((f.x + r.z) / s, (f.y + u.z) / s, 0.25f * s, (r.y - u.x) / s);
            }
        }

        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
        {
            Vector3 a = fromDirection.normalized;
            Vector3 b = toDirection.normalized;
            float d = Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f);
            if (d > 0.999999f) return identity;
            if (d < -0.999999f)
            {
                Vector3 axis = Vector3.Cross(Vector3.right, a);
                if (axis.sqrMagnitude < 1e-06f) axis = Vector3.Cross(Vector3.up, a);
                return AngleAxis(180f, axis.normalized);
            }
            return AngleAxis((float)Math.Acos(d) * Mathf.Rad2Deg, Vector3.Cross(a, b));
        }

        public static Quaternion Inverse(Quaternion r)
        {
            float n = (r.x * r.x) + (r.y * r.y) + (r.z * r.z) + (r.w * r.w);
            if (n < 1e-10f) return identity;
            return new Quaternion(-r.x / n, -r.y / n, -r.z / n, r.w / n);
        }

        public static Quaternion Normalize(Quaternion q) { return q.normalized; }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }

        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            if (Dot(a, b) < 0f) b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
            return new Quaternion(a.x + ((b.x - a.x) * t), a.y + ((b.y - a.y) * t),
                                  a.z + ((b.z - a.z) * t), a.w + ((b.w - a.w) * t)).normalized;
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            float dot = Dot(a, b);
            if (dot < 0f) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); dot = -dot; }
            if (dot > 0.9995f) return LerpUnclamped(a, b, t);

            float theta = (float)Math.Acos(Mathf.Clamp(dot, -1f, 1f));
            float sinTheta = (float)Math.Sin(theta);
            float wa = (float)Math.Sin((1f - t) * theta) / sinTheta;
            float wb = (float)Math.Sin(t * theta) / sinTheta;
            return new Quaternion((a.x * wa) + (b.x * wb), (a.y * wa) + (b.y * wb),
                                  (a.z * wa) + (b.z * wb), (a.w * wa) + (b.w * wb)).normalized;
        }

        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta)
        {
            float angle = Angle(from, to);
            if (angle < 1e-06f) return to;
            return Slerp(from, to, Mathf.Min(1f, maxDegreesDelta / angle));
        }

        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = Mathf.Min(Math.Abs(Dot(a, b)), 1f);
            return dot > 0.999999f ? 0f : (float)Math.Acos(dot) * 2f * Mathf.Rad2Deg;
        }

        public static float Dot(Quaternion a, Quaternion b) { return (a.x * b.x) + (a.y * b.y) + (a.z * b.z) + (a.w * b.w); }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                (a.w * b.x) + (a.x * b.w) + (a.y * b.z) - (a.z * b.y),
                (a.w * b.y) + (a.y * b.w) + (a.z * b.x) - (a.x * b.z),
                (a.w * b.z) + (a.z * b.w) + (a.x * b.y) - (a.y * b.x),
                (a.w * b.w) - (a.x * b.x) - (a.y * b.y) - (a.z * b.z));
        }

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            float x2 = rotation.x * 2f, y2 = rotation.y * 2f, z2 = rotation.z * 2f;
            float xx = rotation.x * x2, yy = rotation.y * y2, zz = rotation.z * z2;
            float xy = rotation.x * y2, xz = rotation.x * z2, yz = rotation.y * z2;
            float wx = rotation.w * x2, wy = rotation.w * y2, wz = rotation.w * z2;

            return new Vector3(
                ((1f - (yy + zz)) * point.x) + ((xy - wz) * point.y) + ((xz + wy) * point.z),
                ((xy + wz) * point.x) + ((1f - (xx + zz)) * point.y) + ((yz - wx) * point.z),
                ((xz - wy) * point.x) + ((yz + wx) * point.y) + ((1f - (xx + yy)) * point.z));
        }

        public static bool operator ==(Quaternion a, Quaternion b) { return Dot(a, b) > 0.999999f; }
        public static bool operator !=(Quaternion a, Quaternion b) { return !(a == b); }

        public override bool Equals(object other) { return other is Quaternion q && this == q; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1); }
        public override string ToString() { return $"({x:F5}, {y:F5}, {z:F5}, {w:F5})"; }
    }

    public struct Matrix4x4
    {
        // Unity's field naming is mRC - row R, column C - and the storage is
        // column-major. Keeping both conventions identical to Unity matters
        // because the generators index columns directly.
        public float m00, m10, m20, m30;
        public float m01, m11, m21, m31;
        public float m02, m12, m22, m32;
        public float m03, m13, m23, m33;

        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3)
        {
            m00 = column0.x; m10 = column0.y; m20 = column0.z; m30 = column0.w;
            m01 = column1.x; m11 = column1.y; m21 = column1.z; m31 = column1.w;
            m02 = column2.x; m12 = column2.y; m22 = column2.z; m32 = column2.w;
            m03 = column3.x; m13 = column3.y; m23 = column3.z; m33 = column3.w;
        }

        public float this[int row, int column]
        {
            get { return this[row + (column * 4)]; }
            set { this[row + (column * 4)] = value; }
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m00; case 1: return m10; case 2: return m20; case 3: return m30;
                    case 4: return m01; case 5: return m11; case 6: return m21; case 7: return m31;
                    case 8: return m02; case 9: return m12; case 10: return m22; case 11: return m32;
                    case 12: return m03; case 13: return m13; case 14: return m23; case 15: return m33;
                    default: throw new IndexOutOfRangeException("Invalid matrix index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: m00 = value; break; case 1: m10 = value; break; case 2: m20 = value; break; case 3: m30 = value; break;
                    case 4: m01 = value; break; case 5: m11 = value; break; case 6: m21 = value; break; case 7: m31 = value; break;
                    case 8: m02 = value; break; case 9: m12 = value; break; case 10: m22 = value; break; case 11: m32 = value; break;
                    case 12: m03 = value; break; case 13: m13 = value; break; case 14: m23 = value; break; case 15: m33 = value; break;
                    default: throw new IndexOutOfRangeException("Invalid matrix index");
                }
            }
        }

        public static Matrix4x4 zero { get { return default(Matrix4x4); } }

        public static Matrix4x4 identity
        {
            get
            {
                Matrix4x4 m = default(Matrix4x4);
                m.m00 = 1f; m.m11 = 1f; m.m22 = 1f; m.m33 = 1f;
                return m;
            }
        }

        public Matrix4x4 transpose
        {
            get
            {
                Matrix4x4 r = default(Matrix4x4);
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        r[col, row] = this[row, col];
                    }
                }
                return r;
            }
        }

        public Vector3 lossyScale
        {
            get
            {
                return new Vector3(new Vector3(m00, m10, m20).magnitude,
                                   new Vector3(m01, m11, m21).magnitude,
                                   new Vector3(m02, m12, m22).magnitude);
            }
        }

        public Quaternion rotation
        {
            get
            {
                Vector3 s = lossyScale;
                if (s.x < 1e-08f || s.y < 1e-08f || s.z < 1e-08f) return Quaternion.identity;
                Vector3 fwd = new Vector3(m02 / s.z, m12 / s.z, m22 / s.z);
                Vector3 up = new Vector3(m01 / s.y, m11 / s.y, m21 / s.y);
                return Quaternion.LookRotation(fwd, up);
            }
        }

        public float determinant
        {
            get
            {
                return (m03 * m12 * m21 * m30) - (m02 * m13 * m21 * m30) - (m03 * m11 * m22 * m30) + (m01 * m13 * m22 * m30)
                     + (m02 * m11 * m23 * m30) - (m01 * m12 * m23 * m30) - (m03 * m12 * m20 * m31) + (m02 * m13 * m20 * m31)
                     + (m03 * m10 * m22 * m31) - (m00 * m13 * m22 * m31) - (m02 * m10 * m23 * m31) + (m00 * m12 * m23 * m31)
                     + (m03 * m11 * m20 * m32) - (m01 * m13 * m20 * m32) - (m03 * m10 * m21 * m32) + (m00 * m13 * m21 * m32)
                     + (m01 * m10 * m23 * m32) - (m00 * m11 * m23 * m32) - (m02 * m11 * m20 * m33) + (m01 * m12 * m20 * m33)
                     + (m02 * m10 * m21 * m33) - (m00 * m12 * m21 * m33) - (m01 * m10 * m22 * m33) + (m00 * m11 * m22 * m33);
            }
        }

        public Matrix4x4 inverse { get { return Inverse(this); } }

        public Vector3 MultiplyPoint(Vector3 point)
        {
            Vector3 r = MultiplyPoint3x4(point);
            float w = (m30 * point.x) + (m31 * point.y) + (m32 * point.z) + m33;
            if (Math.Abs(w) > 1e-08f && Math.Abs(w - 1f) > 1e-08f) r /= w;
            return r;
        }

        public Vector3 MultiplyPoint3x4(Vector3 point)
        {
            return new Vector3(
                (m00 * point.x) + (m01 * point.y) + (m02 * point.z) + m03,
                (m10 * point.x) + (m11 * point.y) + (m12 * point.z) + m13,
                (m20 * point.x) + (m21 * point.y) + (m22 * point.z) + m23);
        }

        public Vector3 MultiplyVector(Vector3 vector)
        {
            return new Vector3(
                (m00 * vector.x) + (m01 * vector.y) + (m02 * vector.z),
                (m10 * vector.x) + (m11 * vector.y) + (m12 * vector.z),
                (m20 * vector.x) + (m21 * vector.y) + (m22 * vector.z));
        }

        public Vector4 GetColumn(int index) { return new Vector4(this[0, index], this[1, index], this[2, index], this[3, index]); }
        public Vector4 GetRow(int index) { return new Vector4(this[index, 0], this[index, 1], this[index, 2], this[index, 3]); }

        public void SetColumn(int index, Vector4 column)
        {
            this[0, index] = column.x; this[1, index] = column.y;
            this[2, index] = column.z; this[3, index] = column.w;
        }

        public void SetRow(int index, Vector4 row)
        {
            this[index, 0] = row.x; this[index, 1] = row.y;
            this[index, 2] = row.z; this[index, 3] = row.w;
        }

        public void SetTRS(Vector3 pos, Quaternion q, Vector3 s) { this = TRS(pos, q, s); }

        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
        {
            return Translate(pos) * Rotate(q) * Scale(s);
        }

        public static Matrix4x4 Translate(Vector3 v)
        {
            Matrix4x4 m = identity;
            m.m03 = v.x; m.m13 = v.y; m.m23 = v.z;
            return m;
        }

        public static Matrix4x4 Scale(Vector3 v)
        {
            Matrix4x4 m = default(Matrix4x4);
            m.m00 = v.x; m.m11 = v.y; m.m22 = v.z; m.m33 = 1f;
            return m;
        }

        public static Matrix4x4 Rotate(Quaternion q)
        {
            float x2 = q.x * 2f, y2 = q.y * 2f, z2 = q.z * 2f;
            float xx = q.x * x2, yy = q.y * y2, zz = q.z * z2;
            float xy = q.x * y2, xz = q.x * z2, yz = q.y * z2;
            float wx = q.w * x2, wy = q.w * y2, wz = q.w * z2;

            Matrix4x4 m = default(Matrix4x4);
            m.m00 = 1f - (yy + zz); m.m01 = xy - wz;         m.m02 = xz + wy;
            m.m10 = xy + wz;        m.m11 = 1f - (xx + zz);  m.m12 = yz - wx;
            m.m20 = xz - wy;        m.m21 = yz + wx;         m.m22 = 1f - (xx + yy);
            m.m33 = 1f;
            return m;
        }

        public static Matrix4x4 Inverse(Matrix4x4 m)
        {
            // Cofactor expansion. General enough for the affine transforms the
            // generators build, and correct for anything invertible.
            float[] a = new float[16];
            for (int i = 0; i < 16; i++) a[i] = m[i];

            float[] inv = new float[16];

            inv[0] = a[5] * a[10] * a[15] - a[5] * a[11] * a[14] - a[9] * a[6] * a[15] + a[9] * a[7] * a[14] + a[13] * a[6] * a[11] - a[13] * a[7] * a[10];
            inv[4] = -a[4] * a[10] * a[15] + a[4] * a[11] * a[14] + a[8] * a[6] * a[15] - a[8] * a[7] * a[14] - a[12] * a[6] * a[11] + a[12] * a[7] * a[10];
            inv[8] = a[4] * a[9] * a[15] - a[4] * a[11] * a[13] - a[8] * a[5] * a[15] + a[8] * a[7] * a[13] + a[12] * a[5] * a[11] - a[12] * a[7] * a[9];
            inv[12] = -a[4] * a[9] * a[14] + a[4] * a[10] * a[13] + a[8] * a[5] * a[14] - a[8] * a[6] * a[13] - a[12] * a[5] * a[10] + a[12] * a[6] * a[9];
            inv[1] = -a[1] * a[10] * a[15] + a[1] * a[11] * a[14] + a[9] * a[2] * a[15] - a[9] * a[3] * a[14] - a[13] * a[2] * a[11] + a[13] * a[3] * a[10];
            inv[5] = a[0] * a[10] * a[15] - a[0] * a[11] * a[14] - a[8] * a[2] * a[15] + a[8] * a[3] * a[14] + a[12] * a[2] * a[11] - a[12] * a[3] * a[10];
            inv[9] = -a[0] * a[9] * a[15] + a[0] * a[11] * a[13] + a[8] * a[1] * a[15] - a[8] * a[3] * a[13] - a[12] * a[1] * a[11] + a[12] * a[3] * a[9];
            inv[13] = a[0] * a[9] * a[14] - a[0] * a[10] * a[13] - a[8] * a[1] * a[14] + a[8] * a[2] * a[13] + a[12] * a[1] * a[10] - a[12] * a[2] * a[9];
            inv[2] = a[1] * a[6] * a[15] - a[1] * a[7] * a[14] - a[5] * a[2] * a[15] + a[5] * a[3] * a[14] + a[13] * a[2] * a[7] - a[13] * a[3] * a[6];
            inv[6] = -a[0] * a[6] * a[15] + a[0] * a[7] * a[14] + a[4] * a[2] * a[15] - a[4] * a[3] * a[14] - a[12] * a[2] * a[7] + a[12] * a[3] * a[6];
            inv[10] = a[0] * a[5] * a[15] - a[0] * a[7] * a[13] - a[4] * a[1] * a[15] + a[4] * a[3] * a[13] + a[12] * a[1] * a[7] - a[12] * a[3] * a[5];
            inv[14] = -a[0] * a[5] * a[14] + a[0] * a[6] * a[13] + a[4] * a[1] * a[14] - a[4] * a[2] * a[13] - a[12] * a[1] * a[6] + a[12] * a[2] * a[5];
            inv[3] = -a[1] * a[6] * a[11] + a[1] * a[7] * a[10] + a[5] * a[2] * a[11] - a[5] * a[3] * a[10] - a[9] * a[2] * a[7] + a[9] * a[3] * a[6];
            inv[7] = a[0] * a[6] * a[11] - a[0] * a[7] * a[10] - a[4] * a[2] * a[11] + a[4] * a[3] * a[10] + a[8] * a[2] * a[7] - a[8] * a[3] * a[6];
            inv[11] = -a[0] * a[5] * a[11] + a[0] * a[7] * a[9] + a[4] * a[1] * a[11] - a[4] * a[3] * a[9] - a[8] * a[1] * a[7] + a[8] * a[3] * a[5];
            inv[15] = a[0] * a[5] * a[10] - a[0] * a[6] * a[9] - a[4] * a[1] * a[10] + a[4] * a[2] * a[9] + a[8] * a[1] * a[6] - a[8] * a[2] * a[5];

            float det = (a[0] * inv[0]) + (a[1] * inv[4]) + (a[2] * inv[8]) + (a[3] * inv[12]);
            if (Math.Abs(det) < 1e-12f) return zero;

            Matrix4x4 result = default(Matrix4x4);
            for (int i = 0; i < 16; i++) result[i] = inv[i] / det;
            return result;
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 r = default(Matrix4x4);
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float sum = 0f;
                    for (int k = 0; k < 4; k++) sum += a[row, k] * b[k, col];
                    r[row, col] = sum;
                }
            }
            return r;
        }

        public static Vector4 operator *(Matrix4x4 lhs, Vector4 v)
        {
            return new Vector4(
                (lhs.m00 * v.x) + (lhs.m01 * v.y) + (lhs.m02 * v.z) + (lhs.m03 * v.w),
                (lhs.m10 * v.x) + (lhs.m11 * v.y) + (lhs.m12 * v.z) + (lhs.m13 * v.w),
                (lhs.m20 * v.x) + (lhs.m21 * v.y) + (lhs.m22 * v.z) + (lhs.m23 * v.w),
                (lhs.m30 * v.x) + (lhs.m31 * v.y) + (lhs.m32 * v.z) + (lhs.m33 * v.w));
        }

        public static bool operator ==(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Math.Abs(a[i] - b[i]) > 1e-05f) return false;
            }
            return true;
        }

        public static bool operator !=(Matrix4x4 a, Matrix4x4 b) { return !(a == b); }

        public override bool Equals(object other) { return other is Matrix4x4 m && this == m; }
        public override int GetHashCode() { return GetColumn(0).GetHashCode() ^ (GetColumn(1).GetHashCode() << 2); }
        public override string ToString() { return $"{m00:F5}\t{m01:F5}\t{m02:F5}\t{m03:F5}"; }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }

        public float this[int index]
        {
            get { return index == 0 ? r : index == 1 ? g : index == 2 ? b : a; }
            set { if (index == 0) r = value; else if (index == 1) g = value; else if (index == 2) b = value; else a = value; }
        }

        public static Color red { get { return new Color(1f, 0f, 0f, 1f); } }
        public static Color green { get { return new Color(0f, 1f, 0f, 1f); } }
        public static Color blue { get { return new Color(0f, 0f, 1f, 1f); } }
        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
        public static Color black { get { return new Color(0f, 0f, 0f, 1f); } }
        public static Color yellow { get { return new Color(1f, 0.9215686f, 0.01568628f, 1f); } }
        public static Color cyan { get { return new Color(0f, 1f, 1f, 1f); } }
        public static Color magenta { get { return new Color(1f, 0f, 1f, 1f); } }
        public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f, 1f); } }
        public static Color grey { get { return gray; } }
        public static Color clear { get { return new Color(0f, 0f, 0f, 0f); } }

        public float grayscale { get { return (0.299f * r) + (0.587f * g) + (0.114f * b); } }
        public float maxColorComponent { get { return Mathf.Max(Mathf.Max(r, g), b); } }

        public Color linear { get { return new Color(GammaToLinear(r), GammaToLinear(g), GammaToLinear(b), a); } }
        public Color gamma { get { return new Color(LinearToGamma(r), LinearToGamma(g), LinearToGamma(b), a); } }

        private static float GammaToLinear(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
        }

        private static float LinearToGamma(float c)
        {
            if (c <= 0.0031308f) return c * 12.92f;
            return (1.055f * (float)Math.Pow(c, 1.0 / 2.4)) - 0.055f;
        }

        public static Color Lerp(Color a, Color b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }

        public static Color LerpUnclamped(Color x, Color y, float t)
        {
            return new Color(x.r + ((y.r - x.r) * t), x.g + ((y.g - x.g) * t),
                             x.b + ((y.b - x.b) * t), x.a + ((y.a - x.a) * t));
        }

        public static void RGBToHSV(Color rgb, out float h, out float s, out float v)
        {
            float max = Mathf.Max(rgb.r, Mathf.Max(rgb.g, rgb.b));
            float min = Mathf.Min(rgb.r, Mathf.Min(rgb.g, rgb.b));
            float delta = max - min;

            v = max;
            s = max <= 0f ? 0f : delta / max;

            if (delta <= 0f) { h = 0f; return; }

            if (Math.Abs(max - rgb.r) < 1e-06f) h = (rgb.g - rgb.b) / delta;
            else if (Math.Abs(max - rgb.g) < 1e-06f) h = 2f + ((rgb.b - rgb.r) / delta);
            else h = 4f + ((rgb.r - rgb.g) / delta);

            h /= 6f;
            if (h < 0f) h += 1f;
        }

        public static Color HSVToRGB(float h, float s, float v) { return HSVToRGB(h, s, v, true); }

        public static Color HSVToRGB(float h, float s, float v, bool hdr)
        {
            if (s <= 0f) return new Color(v, v, v, 1f);

            float sector = (h - (float)Math.Floor(h)) * 6f;
            int i = (int)Math.Floor(sector);
            float f = sector - i;
            float p = v * (1f - s);
            float q = v * (1f - (s * f));
            float t = v * (1f - (s * (1f - f)));

            switch (i)
            {
                case 0: return new Color(v, t, p, 1f);
                case 1: return new Color(q, v, p, 1f);
                case 2: return new Color(p, v, t, 1f);
                case 3: return new Color(p, q, v, 1f);
                case 4: return new Color(t, p, v, 1f);
                default: return new Color(v, p, q, 1f);
            }
        }

        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator *(Color a, float b) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }
        public static Color operator *(float b, Color a) { return a * b; }
        public static Color operator /(Color a, float b) { return new Color(a.r / b, a.g / b, a.b / b, a.a / b); }
        public static bool operator ==(Color a, Color b) { return (Vector4)a == (Vector4)b; }
        public static bool operator !=(Color a, Color b) { return !(a == b); }

        public static implicit operator Vector4(Color c) { return new Vector4(c.r, c.g, c.b, c.a); }
        public static implicit operator Color(Vector4 v) { return new Color(v.x, v.y, v.z, v.w); }

        public override bool Equals(object other) { return other is Color c && this == c; }
        public override int GetHashCode() { return ((Vector4)this).GetHashCode(); }
        public override string ToString() { return $"RGBA({r:F3}, {g:F3}, {b:F3}, {a:F3})"; }
    }

    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static implicit operator Color32(Color c)
        {
            return new Color32(
                (byte)(Mathf.Clamp01(c.r) * 255f),
                (byte)(Mathf.Clamp01(c.g) * 255f),
                (byte)(Mathf.Clamp01(c.b) * 255f),
                (byte)(Mathf.Clamp01(c.a) * 255f));
        }

        public static implicit operator Color(Color32 c)
        {
            return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        }

        public override string ToString() { return $"RGBA({r}, {g}, {b}, {a})"; }
    }

    public struct Rect
    {
        private float _x, _y, _w, _h;

        public Rect(float x, float y, float width, float height) { _x = x; _y = y; _w = width; _h = height; }
        public Rect(Vector2 position, Vector2 size) { _x = position.x; _y = position.y; _w = size.x; _h = size.y; }

        public float x { get { return _x; } set { _x = value; } }
        public float y { get { return _y; } set { _y = value; } }
        public float width { get { return _w; } set { _w = value; } }
        public float height { get { return _h; } set { _h = value; } }
        public float xMin { get { return Mathf.Min(_x, _x + _w); } set { float xm = xMax; _x = value; _w = xm - _x; } }
        public float yMin { get { return Mathf.Min(_y, _y + _h); } set { float ym = yMax; _y = value; _h = ym - _y; } }
        public float xMax { get { return Mathf.Max(_x, _x + _w); } set { _w = value - _x; } }
        public float yMax { get { return Mathf.Max(_y, _y + _h); } set { _h = value - _y; } }
        public Vector2 position { get { return new Vector2(_x, _y); } set { _x = value.x; _y = value.y; } }
        public Vector2 size { get { return new Vector2(_w, _h); } set { _w = value.x; _h = value.y; } }
        public Vector2 center { get { return new Vector2(_x + (_w / 2f), _y + (_h / 2f)); } set { _x = value.x - (_w / 2f); _y = value.y - (_h / 2f); } }
        public Vector2 min { get { return new Vector2(xMin, yMin); } set { xMin = value.x; yMin = value.y; } }
        public Vector2 max { get { return new Vector2(xMax, yMax); } set { xMax = value.x; yMax = value.y; } }

        public static Rect zero { get { return new Rect(0f, 0f, 0f, 0f); } }

        public bool Contains(Vector2 point)
        {
            return point.x >= xMin && point.x < xMax && point.y >= yMin && point.y < yMax;
        }

        public bool Overlaps(Rect other)
        {
            return other.xMax > xMin && other.xMin < xMax && other.yMax > yMin && other.yMin < yMax;
        }

        public static bool operator ==(Rect a, Rect b) { return a._x == b._x && a._y == b._y && a._w == b._w && a._h == b._h; }
        public static bool operator !=(Rect a, Rect b) { return !(a == b); }

        public override bool Equals(object other) { return other is Rect r && this == r; }
        public override int GetHashCode() { return _x.GetHashCode() ^ (_w.GetHashCode() << 2) ^ (_y.GetHashCode() >> 2) ^ (_h.GetHashCode() >> 1); }
        public override string ToString() { return $"(x:{_x:F2}, y:{_y:F2}, width:{_w:F2}, height:{_h:F2})"; }
    }

    public struct RectInt
    {
        public RectInt(int xMin, int yMin, int width, int height) { x = xMin; y = yMin; this.width = width; this.height = height; }
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public struct Bounds
    {
        private Vector3 _center;
        private Vector3 _extents;

        public Bounds(Vector3 center, Vector3 size) { _center = center; _extents = size * 0.5f; }

        public Vector3 center { get { return _center; } set { _center = value; } }
        public Vector3 extents { get { return _extents; } set { _extents = value; } }
        public Vector3 size { get { return _extents * 2f; } set { _extents = value * 0.5f; } }
        public Vector3 min { get { return _center - _extents; } set { SetMinMax(value, max); } }
        public Vector3 max { get { return _center + _extents; } set { SetMinMax(min, value); } }

        public void SetMinMax(Vector3 minimum, Vector3 maximum)
        {
            _extents = (maximum - minimum) * 0.5f;
            _center = minimum + _extents;
        }

        public void Encapsulate(Vector3 point) { SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point)); }
        public void Encapsulate(Bounds bounds) { Encapsulate(bounds.min); Encapsulate(bounds.max); }
        public void Expand(float amount) { _extents += new Vector3(amount, amount, amount) * 0.5f; }
        public void Expand(Vector3 amount) { _extents += amount * 0.5f; }

        public bool Contains(Vector3 point)
        {
            Vector3 lo = min, hi = max;
            return point.x >= lo.x && point.x <= hi.x
                && point.y >= lo.y && point.y <= hi.y
                && point.z >= lo.z && point.z <= hi.z;
        }

        public bool Intersects(Bounds b)
        {
            return min.x <= b.max.x && max.x >= b.min.x
                && min.y <= b.max.y && max.y >= b.min.y
                && min.z <= b.max.z && max.z >= b.min.z;
        }

        public Vector3 ClosestPoint(Vector3 point) { return Vector3.Min(Vector3.Max(point, min), max); }
        public float SqrDistance(Vector3 point) { return (ClosestPoint(point) - point).sqrMagnitude; }

        public static bool operator ==(Bounds a, Bounds b) { return a._center == b._center && a._extents == b._extents; }
        public static bool operator !=(Bounds a, Bounds b) { return !(a == b); }

        public override bool Equals(object other) { return other is Bounds b && this == b; }
        public override int GetHashCode() { return _center.GetHashCode() ^ (_extents.GetHashCode() << 2); }
        public override string ToString() { return $"Center: {_center}, Extents: {_extents}"; }
    }

    public struct Plane
    {
        private Vector3 _normal;
        private float _distance;

        public Plane(Vector3 inNormal, Vector3 inPoint)
        {
            _normal = inNormal.normalized;
            _distance = -Vector3.Dot(_normal, inPoint);
        }

        public Plane(Vector3 inNormal, float d) { _normal = inNormal.normalized; _distance = d; }

        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            _normal = Vector3.Cross(b - a, c - a).normalized;
            _distance = -Vector3.Dot(_normal, a);
        }

        public Vector3 normal { get { return _normal; } set { _normal = value; } }
        public float distance { get { return _distance; } set { _distance = value; } }

        public float GetDistanceToPoint(Vector3 point) { return Vector3.Dot(_normal, point) + _distance; }
        public bool GetSide(Vector3 point) { return GetDistanceToPoint(point) > 0f; }
        public Vector3 ClosestPointOnPlane(Vector3 point) { return point - (_normal * GetDistanceToPoint(point)); }

        public bool Raycast(Ray ray, out float enter)
        {
            float denom = Vector3.Dot(ray.direction, _normal);
            float num = -Vector3.Dot(ray.origin, _normal) - _distance;
            if (Math.Abs(denom) < 1e-06f) { enter = 0f; return false; }
            enter = num / denom;
            return enter > 0f;
        }
    }

    public struct Ray
    {
        private Vector3 _origin;
        private Vector3 _direction;

        public Ray(Vector3 origin, Vector3 direction) { _origin = origin; _direction = direction.normalized; }

        public Vector3 origin { get { return _origin; } set { _origin = value; } }
        public Vector3 direction { get { return _direction; } set { _direction = value.normalized; } }

        public Vector3 GetPoint(float distance) { return _origin + (_direction * distance); }
        public override string ToString() { return $"Origin: {_origin}, Dir: {_direction}"; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public const float Epsilon = 1.401298E-45f;

        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Tan(float f) { return (float)Math.Tan(f); }
        public static float Asin(float f) { return (float)Math.Asin(f); }
        public static float Acos(float f) { return (float)Math.Acos(f); }
        public static float Atan(float f) { return (float)Math.Atan(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int value) { return Math.Abs(value); }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }

        public static float Min(params float[] values)
        {
            if (values == null || values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }

        public static int Min(params int[] values)
        {
            if (values == null || values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }

        public static float Max(params float[] values)
        {
            if (values == null || values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }

        public static int Max(params int[] values)
        {
            if (values == null || values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }

        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Exp(float power) { return (float)Math.Exp(power); }
        public static float Log(float f, float p) { return (float)Math.Log(f, p); }
        public static float Log(float f) { return (float)Math.Log(f); }
        public static float Log10(float f) { return (float)Math.Log10(f); }
        public static float Ceil(float f) { return (float)Math.Ceiling(f); }
        public static float Floor(float f) { return (float)Math.Floor(f); }
        public static float Round(float f) { return (float)Math.Round(f, MidpointRounding.ToEven); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static int RoundToInt(float f) { return (int)Math.Round(f, MidpointRounding.ToEven); }
        public static float Sign(float f) { return f >= 0f ? 1f : -1f; }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp01(float value) { return Clamp(value, 0f, 1f); }

        public static float Lerp(float a, float b, float t) { return a + ((b - a) * Clamp01(t)); }
        public static float LerpUnclamped(float a, float b, float t) { return a + ((b - a) * t); }

        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat(b - a, 360f);
            if (delta > 180f) delta -= 360f;
            return a + (delta * Clamp01(t));
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + (Sign(target - current) * maxDelta);
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = DeltaAngle(current, target);
            if (-maxDelta < delta && delta < maxDelta) return target;
            return MoveTowards(current, current + delta, maxDelta);
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = (-2f * t * t * t) + (3f * t * t);
            return (to * t) + (from * (1f - t));
        }

        public static float Gamma(float value, float absmax, float gamma)
        {
            bool negative = value < 0f;
            float abs = Math.Abs(value);
            if (abs > absmax) return negative ? -abs : abs;
            float result = Pow(abs / absmax, gamma) * absmax;
            return negative ? -result : result;
        }

        public static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Max(1E-06f * Max(Math.Abs(a), Math.Abs(b)), Epsilon * 8f);
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, Time.deltaTime);
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed)
        {
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + (0.48f * x * x) + (0.235f * x * x * x));
            float change = Clamp(current - target, -maxSpeed * smoothTime, maxSpeed * smoothTime);
            float dest = current - change;
            float temp = (currentVelocity + (omega * change)) * deltaTime;
            currentVelocity = (currentVelocity - (omega * temp)) * exp;
            return dest + ((change + temp) * exp);
        }

        public static float Repeat(float t, float length) { return Clamp(t - (Floor(t / length) * length), 0f, length); }

        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - Math.Abs(t - length);
        }

        public static float InverseLerp(float a, float b, float value)
        {
            return Math.Abs(b - a) < 1e-09f ? 0f : Clamp01((value - a) / (b - a));
        }

        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }

        /// <summary>
        /// Not Unity's Perlin implementation and not expected to match it
        /// numerically. Nothing in this project uses it - TilingNoise exists
        /// precisely because Mathf.PerlinNoise does not tile - so this is here
        /// only to keep the surface complete.
        /// </summary>
        public static float PerlinNoise(float x, float y)
        {
            return 0.5f;
        }

        public static bool IsPowerOfTwo(int value) { return value > 0 && (value & (value - 1)) == 0; }

        public static int NextPowerOfTwo(int value)
        {
            if (value <= 0) return 0;
            value--;
            value |= value >> 1; value |= value >> 2; value |= value >> 4;
            value |= value >> 8; value |= value >> 16;
            return value + 1;
        }

        public static int ClosestPowerOfTwo(int value)
        {
            int next = NextPowerOfTwo(value);
            int prev = next >> 1;
            return (value - prev) < (next - value) ? prev : next;
        }
    }

    /// <summary>
    /// Deterministic stand-in for UnityEngine.Random. Seeded from a fixed value
    /// so a harness run is reproducible; the project does not rely on Unity's
    /// exact sequence anywhere.
    /// </summary>
    public static class Random
    {
        private static System.Random _rng = new System.Random(0);

        public static float value { get { return (float)_rng.NextDouble(); } }
        public static Vector3 insideUnitSphere { get { return onUnitSphere * (float)Math.Pow(_rng.NextDouble(), 1.0 / 3.0); } }
        public static Vector2 insideUnitCircle { get { float a = value * 2f * Mathf.PI; float r = (float)Math.Sqrt(_rng.NextDouble()); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; } }

        public static Vector3 onUnitSphere
        {
            get
            {
                float z = (value * 2f) - 1f;
                float a = value * 2f * Mathf.PI;
                float r = Mathf.Sqrt(1f - (z * z));
                return new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), z);
            }
        }

        public static Quaternion rotation { get { return Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f)); } }
        public static Quaternion rotationUniform { get { return rotation; } }

        public static State state
        {
            get { return default(State); }
            set { }
        }

        public static float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive + ((maxInclusive - minInclusive) * value);
        }

        public static int Range(int minInclusive, int maxExclusive)
        {
            return maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
        }

        public static void InitState(int seed) { _rng = new System.Random(seed); }

        public static Color ColorHSV() { return Color.HSVToRGB(value, value, value); }

        public struct State { }
    }
}
