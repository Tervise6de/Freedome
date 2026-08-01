#!/usr/bin/env python3
"""A tiny software rasteriser used only to preview the shed.

This is NOT the Unity renderer and its output is NOT a substitute for the review
screenshots. It exists because the authoring environment has no Unity and no GPU,
and a room that nobody has ever seen is a room nobody can review. Given the same
dimension table the scene generator reads, it produces a flat-shaded perspective
image from an arbitrary camera - enough to check scale, composition, sightlines
and whether anything is obviously in the wrong place.

Deliberately simple: flat per-triangle normals (which is what the generated
meshes actually have), one bounced-free directional sun with an aperture-based
shadow test, a crude window fill term and two point lights.
"""

from __future__ import annotations

import math
import numpy as np


# ----------------------------------------------------------------------------
# Transforms
# ----------------------------------------------------------------------------

def rot_euler(x: float = 0.0, y: float = 0.0, z: float = 0.0) -> np.ndarray:
    """Unity's Euler convention: applied as Ry * Rx * Rz."""
    rx, ry, rz = math.radians(x), math.radians(y), math.radians(z)
    cx, sx = math.cos(rx), math.sin(rx)
    cy, sy = math.cos(ry), math.sin(ry)
    cz, sz = math.cos(rz), math.sin(rz)

    mx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]], dtype=np.float64)
    my = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]], dtype=np.float64)
    mz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]], dtype=np.float64)
    return my @ mx @ mz


def rot_from_to(a: np.ndarray, b: np.ndarray) -> np.ndarray:
    """Rotation taking unit vector a onto unit vector b."""
    a = a / np.linalg.norm(a)
    b = b / np.linalg.norm(b)
    v = np.cross(a, b)
    c = float(np.dot(a, b))
    if c < -0.999999:
        # Antiparallel: rotate 180 degrees about any perpendicular axis.
        axis = np.cross(a, np.array([1.0, 0.0, 0.0]))
        if np.linalg.norm(axis) < 1e-6:
            axis = np.cross(a, np.array([0.0, 1.0, 0.0]))
        axis = axis / np.linalg.norm(axis)
        x, y, z = axis
        return np.array([
            [2 * x * x - 1, 2 * x * y, 2 * x * z],
            [2 * x * y, 2 * y * y - 1, 2 * y * z],
            [2 * x * z, 2 * y * z, 2 * z * z - 1],
        ])
    vx = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + vx + (vx @ vx) * (1.0 / (1.0 + c))


# ----------------------------------------------------------------------------
# Mesh accumulation
# ----------------------------------------------------------------------------

class Mesh:
    """Triangle soup with a per-triangle colour."""

    def __init__(self):
        self.verts: list[list[float]] = []
        self.tris: list[tuple[int, int, int]] = []
        self.colors: list[tuple[float, float, float]] = []
        self._stack: list[np.ndarray] = []
        self._m = np.eye(4)

    # -- transform stack ----------------------------------------------------

    def push(self, position=(0.0, 0.0, 0.0), rotation: np.ndarray | None = None):
        m = np.eye(4)
        if rotation is not None:
            m[:3, :3] = rotation
        m[:3, 3] = position
        self._stack.append(self._m)
        self._m = self._m @ m

    def pop(self):
        self._m = self._stack.pop() if self._stack else np.eye(4)

    def _xf(self, p) -> list[float]:
        v = self._m @ np.array([p[0], p[1], p[2], 1.0])
        return [float(v[0]), float(v[1]), float(v[2])]

    # -- primitives ---------------------------------------------------------

    def tri(self, a, b, c, color):
        i = len(self.verts)
        self.verts.extend([self._xf(a), self._xf(b), self._xf(c)])
        self.tris.append((i, i + 1, i + 2))
        self.colors.append(color)

    def quad(self, a, b, c, d, color):
        self.tri(a, b, c, color)
        self.tri(a, c, d, color)

    def box(self, centre, size, color, rotation: np.ndarray | None = None, inset=0.0):
        hx, hy, hz = size[0] / 2 - inset, size[1] / 2 - inset, size[2] / 2 - inset
        hx, hy, hz = max(hx, 1e-4), max(hy, 1e-4), max(hz, 1e-4)
        self.push(centre, rotation)
        p = [
            (-hx, -hy, -hz), (hx, -hy, -hz), (hx, hy, -hz), (-hx, hy, -hz),
            (-hx, -hy, hz), (hx, -hy, hz), (hx, hy, hz), (-hx, hy, hz),
        ]
        # Counter-clockwise seen from outside.
        self.quad(p[4], p[5], p[6], p[7], color)   # +Z
        self.quad(p[1], p[0], p[3], p[2], color)   # -Z
        self.quad(p[5], p[1], p[2], p[6], color)   # +X
        self.quad(p[0], p[4], p[7], p[3], color)   # -X
        self.quad(p[3], p[7], p[6], p[2], color)   # +Y
        self.quad(p[0], p[1], p[5], p[4], color)   # -Y
        self.pop()

    def cyl(self, centre, r0, r1, h, seg, color, rotation: np.ndarray | None = None,
            cap0=True, cap1=True):
        seg = max(3, seg)
        self.push(centre, rotation)
        half = h / 2
        ring0 = [(math.cos(2 * math.pi * i / seg) * r0, -half,
                  math.sin(2 * math.pi * i / seg) * r0) for i in range(seg)]
        ring1 = [(math.cos(2 * math.pi * i / seg) * r1, half,
                  math.sin(2 * math.pi * i / seg) * r1) for i in range(seg)]
        for i in range(seg):
            j = (i + 1) % seg
            self.quad(ring0[i], ring0[j], ring1[j], ring1[i], color)
        if cap1 and r1 > 1e-5:
            for i in range(1, seg - 1):
                self.tri(ring1[0], ring1[i], ring1[i + 1], color)
        if cap0 and r0 > 1e-5:
            for i in range(1, seg - 1):
                self.tri(ring0[0], ring0[i + 1], ring0[i], color)
        self.pop()

    def arrays(self):
        v = np.array(self.verts, dtype=np.float64)
        f = np.array(self.tris, dtype=np.int64)
        c = np.array(self.colors, dtype=np.float64)
        return v, f, c


# ----------------------------------------------------------------------------
# Rasteriser
# ----------------------------------------------------------------------------

class Camera:
    def __init__(self, eye, target, fov_y_deg, width, height):
        self.eye = np.array(eye, dtype=np.float64)
        self.target = np.array(target, dtype=np.float64)
        self.fov = math.radians(fov_y_deg)
        self.width = width
        self.height = height

        fwd = self.target - self.eye
        fwd /= np.linalg.norm(fwd)
        world_up = np.array([0.0, 1.0, 0.0])
        # Unity is left-handed: with forward +Z and up +Y, right is +X.
        # Taking cross(forward, up) instead mirrors the whole image.
        right = np.cross(world_up, fwd)
        if np.linalg.norm(right) < 1e-6:
            right = np.cross(np.array([0.0, 0.0, 1.0]), fwd)
        right /= np.linalg.norm(right)
        up = np.cross(fwd, right)

        self.basis = np.stack([right, up, fwd])  # rows

    def to_view(self, points: np.ndarray) -> np.ndarray:
        return (points - self.eye) @ self.basis.T


def _clip_near(poly_view, poly_shade, near):
    """Clip a view-space polygon against z >= near, carrying per-vertex payload."""
    out_v, out_s = [], []
    n = len(poly_view)
    for i in range(n):
        cur_v, nxt_v = poly_view[i], poly_view[(i + 1) % n]
        cur_s, nxt_s = poly_shade[i], poly_shade[(i + 1) % n]
        cur_in, nxt_in = cur_v[2] >= near, nxt_v[2] >= near
        if cur_in:
            out_v.append(cur_v)
            out_s.append(cur_s)
        if cur_in != nxt_in:
            t = (near - cur_v[2]) / (nxt_v[2] - cur_v[2])
            out_v.append(cur_v + (nxt_v - cur_v) * t)
            out_s.append(cur_s + (nxt_s - cur_s) * t)
    return out_v, out_s


def tessellate(a, b, c, colors, max_edge, limit_fn=None):
    """Split triangles until no edge is longer than max_edge.

    Lighting is evaluated per vertex, so a six-metre floorboard drawn as one
    quad can only ever have one brightness across its whole length: the sun
    pool from the window, and the falloff of the lamp, simply cannot appear.
    Subdividing first is what lets them.
    """
    while True:
        e0 = np.linalg.norm(b - a, axis=1)
        e1 = np.linalg.norm(c - b, axis=1)
        e2 = np.linalg.norm(a - c, axis=1)
        longest = np.maximum(np.maximum(e0, e1), e2)
        # The limit varies with position: the 60 m ground plane and the hedge do
        # not need a 300 mm mesh, and subdividing them costs three quarters of a
        # million triangles for scenery seen through one small window.
        limit = max_edge if limit_fn is None else limit_fn((a + b + c) / 3.0)
        # Area guard. Without it, the 19 mm edge of a six-metre floorboard gets
        # subdivided fifteen times along its length for no visible benefit, and
        # slivers like that dominate the triangle budget.
        area = 0.5 * np.linalg.norm(np.cross(b - a, c - a), axis=1)
        split = (longest > limit) & (area > 0.012)
        if not split.any():
            return a, b, c, colors

        keep = ~split
        ka, kb, kc, kcol = a[keep], b[keep], c[keep], colors[keep]

        sa, sb, sc, scol = a[split], b[split], c[split], colors[split]
        se0, se1, se2 = e0[split], e1[split], e2[split]

        # Split the longest edge of each triangle at its midpoint.
        pick0 = (se0 >= se1) & (se0 >= se2)
        pick1 = (se1 > se0) & (se1 >= se2)
        pick2 = ~(pick0 | pick1)

        out_a, out_b, out_c, out_col = [ka], [kb], [kc], [kcol]
        for mask, (p, q, r) in ((pick0, (0, 1, 2)), (pick1, (1, 2, 0)), (pick2, (2, 0, 1))):
            if not mask.any():
                continue
            tri = np.stack([sa[mask], sb[mask], sc[mask]], axis=1)
            v0, v1, v2 = tri[:, p], tri[:, q], tri[:, r]
            mid = (v0 + v1) * 0.5
            col = scol[mask]
            out_a.extend([v0, mid])
            out_b.extend([mid, v1])
            out_c.extend([v2, v2])
            out_col.extend([col, col])

        a = np.concatenate(out_a)
        b = np.concatenate(out_b)
        c = np.concatenate(out_c)
        colors = np.concatenate(out_col)


def prepare(mesh: Mesh, camera_eye, shade_fn, max_edge=0.35, limit_fn=None):
    """Tessellate, then shade every vertex. Returns (a, b, c, ca, cb, cc)."""
    verts, tris, colors = mesh.arrays()

    a = verts[tris[:, 0]]
    b = verts[tris[:, 1]]
    c = verts[tris[:, 2]]

    normals = np.cross(b - a, c - a)
    lengths = np.linalg.norm(normals, axis=1)
    keep = lengths > 1e-12
    a, b, c, colors = a[keep], b[keep], c[keep], colors[keep]

    if max_edge > 0:
        a, b, c, colors = tessellate(a, b, c, colors, max_edge, limit_fn)

    normals = np.cross(b - a, c - a)
    lengths = np.linalg.norm(normals, axis=1)
    keep = lengths > 1e-12
    a, b, c, colors = a[keep], b[keep], c[keep], colors[keep]
    normals = normals[keep] / lengths[keep, None]

    centroids = (a + b + c) / 3.0
    to_cam = np.asarray(camera_eye) - centroids
    flip = np.einsum('ij,ij->i', normals, to_cam) < 0
    normals[flip] *= -1.0

    # Flat normals, but lighting sampled at each corner, so gradients are smooth
    # across a face while the facet edges stay crisp.
    ca = shade_fn(a, normals, colors)
    cb = shade_fn(b, normals, colors)
    cc = shade_fn(c, normals, colors)
    return a, b, c, ca, cb, cc


def render(mesh: Mesh, camera: Camera, shade_fn, background_fn, near=0.04, max_edge=0.35,
           prepared=None):
    """Rasterise the mesh with per-vertex interpolated lighting."""
    if prepared is None:
        prepared = prepare(mesh, camera.eye, shade_fn, max_edge)
    a, b, c, ca, cb, cc = prepared

    va = camera.to_view(a)
    vb = camera.to_view(b)
    vc = camera.to_view(c)

    # Discard anything entirely behind the camera.
    visible = (va[:, 2] > near) | (vb[:, 2] > near) | (vc[:, 2] > near)

    W, H = camera.width, camera.height
    aspect = W / H
    f = 1.0 / math.tan(camera.fov / 2.0)

    frame = background_fn(camera)
    depth = np.zeros((H, W), dtype=np.float64)  # storing 1/z, larger is nearer

    idx = np.nonzero(visible)[0]
    # Painter-ish ordering is unnecessary with a z-buffer, but drawing near
    # triangles first lets early rejection do a little work.
    order = idx[np.argsort(-np.minimum(np.minimum(va[idx, 2], vb[idx, 2]), vc[idx, 2]))]

    xs = np.arange(W)
    ys = np.arange(H)

    for t in order:
        poly_v = [va[t], vb[t], vc[t]]
        poly_s = [ca[t], cb[t], cc[t]]
        pv, ps = _clip_near(poly_v, poly_s, near)
        if len(pv) < 3:
            continue

        pv = np.array(pv)
        ps = np.array(ps)
        z = pv[:, 2]
        sx = (pv[:, 0] * f / aspect / z * 0.5 + 0.5) * W
        sy = (0.5 - pv[:, 1] * f / z * 0.5) * H
        inv_z = 1.0 / z

        for k in range(1, len(pv) - 1):
            tri_x = np.array([sx[0], sx[k], sx[k + 1]])
            tri_y = np.array([sy[0], sy[k], sy[k + 1]])
            tri_w = np.array([inv_z[0], inv_z[k], inv_z[k + 1]])

            x0 = max(int(np.floor(tri_x.min())), 0)
            x1 = min(int(np.ceil(tri_x.max())) + 1, W)
            y0 = max(int(np.floor(tri_y.min())), 0)
            y1 = min(int(np.ceil(tri_y.max())) + 1, H)
            if x1 <= x0 or y1 <= y0:
                continue

            px = xs[x0:x1] + 0.5
            py = ys[y0:y1] + 0.5
            gx, gy = np.meshgrid(px, py)

            d = ((tri_y[1] - tri_y[2]) * (tri_x[0] - tri_x[2])
                 + (tri_x[2] - tri_x[1]) * (tri_y[0] - tri_y[2]))
            if abs(d) < 1e-12:
                continue

            w0 = ((tri_y[1] - tri_y[2]) * (gx - tri_x[2])
                  + (tri_x[2] - tri_x[1]) * (gy - tri_y[2])) / d
            w1 = ((tri_y[2] - tri_y[0]) * (gx - tri_x[2])
                  + (tri_x[0] - tri_x[2]) * (gy - tri_y[2])) / d
            w2 = 1.0 - w0 - w1

            inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
            if not inside.any():
                continue

            pixel_w = w0 * tri_w[0] + w1 * tri_w[1] + w2 * tri_w[2]
            sub = depth[y0:y1, x0:x1]
            nearer = inside & (pixel_w > sub)
            if not nearer.any():
                continue

            sub[nearer] = pixel_w[nearer]
            col = (w0[..., None] * ps[0] + w1[..., None] * ps[k]
                   + w2[..., None] * ps[k + 1])
            frame[y0:y1, x0:x1][nearer] = col[nearer]

    return frame


def tonemap(linear: np.ndarray, exposure=1.0) -> np.ndarray:
    """Reinhard plus gamma. Keeps the window bright without clipping to white."""
    x = np.clip(linear * exposure, 0.0, None)
    mapped = x / (1.0 + x)
    return np.clip(mapped, 0.0, 1.0) ** (1.0 / 2.2)
