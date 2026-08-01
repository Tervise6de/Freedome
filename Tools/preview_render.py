#!/usr/bin/env python3
"""Render preview images of the shed from the eight review viewpoints.

    python3 Tools/preview_render.py [--width 1280] [--height 720] [--only 3]

Output goes to docs/previews/.

WHAT THIS IS NOT
----------------
These are not Unity screenshots and they do not satisfy the milestone's
screenshot requirement. There is no Unity and no GPU in this environment, so
this script reconstructs the same geometry from the same dimension table
(ShedDimensions.cs) and rasterises it with a small software renderer, using
flat shading, one directional sun with an aperture-based shadow test, a window
fill term and the two practical lights.

What it is good for: confirming scale, proportion, sightlines, composition,
prop placement and whether the room reads as an ordinary shed. What it cannot
show: HDRP materials, real global illumination, the procedural textures,
shadow softness, or anything about performance.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))

from generate_diagrams import parse_dimensions            # noqa: E402
from preview_raster import (Camera, Mesh, render, rot_euler,  # noqa: E402
                            rot_from_to, tonemap)

OUT_DIR = Path(__file__).resolve().parent.parent / "docs/previews"

D = parse_dimensions()
RNG = np.random.default_rng(20240731)

# ----------------------------------------------------------------------------
# Palette. Approximate linear albedo per material family.
# Values above 1.2 in any channel mark a surface as emissive (glass, bulbs).
# ----------------------------------------------------------------------------

PINE = (0.430, 0.336, 0.222)
FLOOR = (0.395, 0.315, 0.228)
WEATHER = (0.300, 0.320, 0.282)
PLY = (0.440, 0.340, 0.222)
SHELF = (0.376, 0.340, 0.272)
PEG = (0.320, 0.232, 0.160)
GALV = (0.520, 0.535, 0.550)
STEEL = (0.170, 0.178, 0.190)
ZINC = (0.500, 0.512, 0.522)
GREEN = (0.110, 0.240, 0.132)
RED = (0.370, 0.100, 0.080)
CREAM = (0.620, 0.598, 0.540)
EPLAST = (0.600, 0.592, 0.570)
BLUE = (0.130, 0.240, 0.370)
RUBBER = (0.058, 0.058, 0.060)
TARP = (0.135, 0.235, 0.335)
CONCRETE = (0.420, 0.412, 0.400)
GRASS = (0.205, 0.265, 0.140)
GRAVEL = (0.360, 0.350, 0.330)
CARD = (0.450, 0.360, 0.260)
SOIL = (0.130, 0.150, 0.118)
SKY_COL = np.array([0.50, 0.63, 0.84])
GLASS = tuple(SKY_COL * 2.6)
BULB = (3.4, 2.7, 1.8)


def jitter(base, amount=0.10):
    """Small per-piece tonal variation so flat shading does not read as plastic."""
    k = 1.0 + (RNG.random() - 0.5) * 2.0 * amount
    return (base[0] * k, base[1] * k, base[2] * k)


# ----------------------------------------------------------------------------
# Geometry - mirrors the C# builders closely enough to judge the room
# ----------------------------------------------------------------------------

HW = D["HalfWidth"]
HL = D["HalfLength"]
WALL_H = D["WallHeight"]
WALL_T = D["WallThickness"]
STUD_D = D["StudDepth"]
STUD_W = D["StudWidth"]
PLATE = D["PlateThickness"]
BOARD = D["BoardThickness"]
DECK_W = HW + STUD_D
DECK_L = HL + STUD_D
PITCH = math.radians(D["RoofPitchDegrees"])
K = math.tan(PITCH)
HALF_SPAN = D["RoofHalfSpan"]
RIDGE_Y = WALL_H + HALF_SPAN * K
EAVE_X = HALF_SPAN + D["EaveOverhang"]
ROOF_HL = HL + WALL_T + D["GableOverhang"]
RAFTER_D = D["RafterDepth"]
PURLIN_T = 0.045


def roof_underside(x):
    return WALL_H + (HALF_SPAN - abs(x)) * K


def trough_y(x):
    """Underside of the corrugation troughs - what the gable and eaves close up to."""
    return roof_underside(x) + (RAFTER_D + PURLIN_T) / math.cos(PITCH) - 0.008


def slope_rot(side):
    """Local X along the slope, local Y perpendicular, local Z along the ridge."""
    up = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
    fwd = np.array([0.0, 0.0, 1.0])
    right = np.cross(up, fwd)
    right /= np.linalg.norm(right)
    return np.stack([right, up, fwd], axis=1)


def panel_with_openings(m, length, height, thickness, zoff, openings, color, cavity=1.0):
    """Sheathing behind the studs. `cavity` stands in for the ambient occlusion a
    real renderer computes: the panel sits 90 mm back between studs and noggins,
    so it genuinely receives less sky and bounce than the framing in front of it.
    """
    color = tuple(c * cavity for c in color)
    xs = sorted({0.0, length} | {o[0] for o in openings} | {o[1] for o in openings})
    ys = sorted({0.0, height} | {o[2] for o in openings} | {o[3] for o in openings})
    for i in range(len(xs) - 1):
        for j in range(len(ys) - 1):
            x0, x1, y0, y1 = xs[i], xs[i + 1], ys[j], ys[j + 1]
            if x1 - x0 < 1e-3 or y1 - y0 < 1e-3:
                continue
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            if any(o[0] < cx < o[1] and o[2] < cy < o[3] for o in openings):
                continue
            m.box((cx, cy, zoff + thickness / 2), (x1 - x0, y1 - y0, thickness), jitter(color, 0.06))


def stud_wall(m, length, height, openings, color):
    top_bottom = height - PLATE * 2
    floor_holes = [(o[0], o[1]) for o in openings if o[2] <= 1e-3]

    segs = [(0.0, length)]
    for h0, h1 in floor_holes:
        nxt = []
        for s0, s1 in segs:
            if h1 <= s0 or h0 >= s1:
                nxt.append((s0, s1))
                continue
            if h0 > s0:
                nxt.append((s0, h0))
            if h1 < s1:
                nxt.append((h1, s1))
        segs = nxt
    for s0, s1 in segs:
        if s1 - s0 > 2e-3:
            m.box(((s0 + s1) / 2, PLATE / 2, STUD_D / 2), (s1 - s0, PLATE, STUD_D), jitter(color))

    m.box((length / 2, top_bottom + PLATE / 2, STUD_D / 2), (length, PLATE, STUD_D), jitter(color))
    m.box((length / 2, top_bottom + PLATE * 1.5, STUD_D / 2), (length, PLATE, STUD_D), jitter(color))

    centres = [STUD_W / 2]
    n = int((length + 1e-3) / D["StudSpacing"])
    for i in range(1, n + 1):
        c = i * D["StudSpacing"]
        if STUD_W < c < length - STUD_W:
            centres.append(c)
    centres.append(length - STUD_W / 2)

    def blocked(c):
        return any(c + STUD_W / 2 > o[0] - 1e-3 and c - STUD_W / 2 < o[1] + 1e-3 for o in openings)

    for c in centres:
        if blocked(c):
            continue
        m.box((c, PLATE + (top_bottom - PLATE) / 2, STUD_D / 2),
              (STUD_W, top_bottom - PLATE, STUD_D), jitter(color))

    # Opening trimmers.
    for o in openings:
        u0, u1, y0, y1 = o
        lintel_top = min(y1 + 0.140, top_bottom)
        m.box(((u0 + u1) / 2, (y1 + lintel_top) / 2, STUD_D / 2),
              (u1 - u0 + STUD_W * 2, lintel_top - y1, STUD_D), jitter(color))
        for edge, d in ((u0, -1), (u1, 1)):
            jb = 0.0 if y0 <= 1e-3 else PLATE
            m.box((edge + d * STUD_W / 2, (jb + y1) / 2, STUD_D / 2),
                  (STUD_W, y1 - jb, STUD_D), jitter(color))
            m.box((edge + d * STUD_W * 1.5, PLATE + (top_bottom - PLATE) / 2, STUD_D / 2),
                  (STUD_W, top_bottom - PLATE, STUD_D), jitter(color))
        if y0 > 1e-3:
            m.box(((u0 + u1) / 2, y0 - PLATE / 2, STUD_D / 2), (u1 - u0, PLATE, STUD_D), jitter(color))
            ncr = max(1, round((u1 - u0) / D["StudSpacing"]))
            for i in range(ncr + 1):
                t = i / ncr
                x = u0 + STUD_W / 2 + t * (u1 - u0 - STUD_W)
                m.box((x, PLATE + (y0 - PLATE - PLATE) / 2, STUD_D / 2),
                      (STUD_W, y0 - PLATE * 2, STUD_D), jitter(color))
        if lintel_top < top_bottom - 0.03:
            ncr = max(1, round((u1 - u0) / D["StudSpacing"]))
            for i in range(ncr + 1):
                t = i / ncr
                x = u0 + STUD_W / 2 + t * (u1 - u0 - STUD_W)
                m.box((x, (lintel_top + top_bottom) / 2, STUD_D / 2),
                      (STUD_W, top_bottom - lintel_top, STUD_D), jitter(color))

    # Noggins.
    centres.sort()
    high = False
    for i in range(len(centres) - 1):
        a = centres[i] + STUD_W / 2
        b = centres[i + 1] - STUD_W / 2
        if b - a < 0.15:
            continue
        y = height / 2 + (0.05 if high else -0.05)
        high = not high
        if any(o[0] < (a + b) / 2 < o[1] and o[2] < y < o[3] for o in openings):
            continue
        m.box(((a + b) / 2, y, STUD_D / 2), (b - a, PLATE, STUD_D), jitter(color))


def gable_infill(m, length):
    half = length / 2
    ext = WALL_T
    apex = trough_y(0.0)
    edge = trough_y(half + ext)

    for u in np.arange(D["StudSpacing"], length - 0.05, D["StudSpacing"]):
        top = roof_underside(u - half)
        if top - WALL_H < 0.05:
            continue
        m.box((u, WALL_H + (top - WALL_H) / 2, STUD_D / 2),
              (STUD_W, top - WALL_H, STUD_D), jitter(PINE))
    m.box((half, WALL_H + (apex - WALL_H) / 2, STUD_D / 2),
          (STUD_W, apex - WALL_H, STUD_D), jitter(PINE))

    for zoff, thick, col in ((STUD_D, BOARD, PINE), (STUD_D + BOARD, D["CladdingThickness"], WEATHER)):
        z0, z1 = zoff, zoff + thick
        x0, x1 = -ext, length + ext
        pts = [(x0, WALL_H), (x1, WALL_H), (x1, edge), (half, apex), (x0, edge)]
        for zz in (z0, z1):
            for i in range(1, len(pts) - 1):
                m.tri((pts[0][0], pts[0][1], zz), (pts[i][0], pts[i][1], zz),
                      (pts[i + 1][0], pts[i + 1][1], zz), col)
        for i in range(len(pts)):
            a, b = pts[i], pts[(i + 1) % len(pts)]
            m.quad((a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1), col)


def build_shell(m):
    # --- floor structure -------------------------------------------------
    joist_top = -D["FloorBoardThickness"]
    joist_bot = joist_top - D["FloorJoistDepth"]
    bearer_bot = joist_bot - D["BearerDepth"]

    for x in (-1.75, 0.0, 1.75):
        m.box((x, (joist_bot + bearer_bot) / 2, 0.0), (0.09, D["BearerDepth"], DECK_L * 2), PINE)
    z = -DECK_L + D["FloorJoistWidth"] / 2
    while z <= DECK_L:
        m.box((0.0, (joist_top + joist_bot) / 2, z), (DECK_W * 2, D["FloorJoistDepth"], 0.045), PINE)
        z += D["FloorJoistSpacing"]
    for x in (-1.75, 0.0, 1.75):
        for zz in (-2.6, 0.0, 2.6):
            m.box((x, bearer_bot - 0.125, zz), (0.2, 0.25, 0.2), CONCRETE)

    # --- floorboards ------------------------------------------------------
    px0 = D["ServicePanelCentreX"] - D["ServicePanelWidth"] / 2
    px1 = D["ServicePanelCentreX"] + D["ServicePanelWidth"] / 2
    pz0 = D["ServicePanelCentreZ"] - D["ServicePanelLength"] / 2
    pz1 = D["ServicePanelCentreZ"] + D["ServicePanelLength"] / 2

    bw = D["FloorBoardWidth"]
    x = -DECK_W
    while x < DECK_W - 1e-3:
        w = min(bw, DECK_W - x)
        cx = x + w / 2
        col = jitter(FLOOR, 0.13)
        if px0 < cx < px1:
            m.box((cx, -D["FloorBoardThickness"] / 2, (-DECK_L + pz0) / 2),
                  (w - 0.004, D["FloorBoardThickness"], pz0 + DECK_L), col)
            m.box((cx, -D["FloorBoardThickness"] / 2, (pz1 + DECK_L) / 2),
                  (w - 0.004, D["FloorBoardThickness"], DECK_L - pz1), col)
        else:
            m.box((cx, -D["FloorBoardThickness"] / 2, 0.0),
                  (w - 0.004, D["FloorBoardThickness"], DECK_L * 2), col)
        x += w

    # service panel
    m.push((D["ServicePanelCentreX"], 0.0, D["ServicePanelCentreZ"]))
    pw = D["ServicePanelWidth"] - 0.006
    pl = D["ServicePanelLength"] - 0.006
    for i in range(5):
        cx = -pw / 2 + pw / 5 * (i + 0.5)
        m.box((cx, -D["FloorBoardThickness"] / 2, 0.0),
              (pw / 5 - 0.003, D["FloorBoardThickness"], pl), jitter(FLOOR, 0.13))
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.cyl((sx * (pw / 2 - 0.045), -0.002, sz * (pl / 2 - 0.07)), 0.0055, 0.004, 0.004, 8, ZINC)
    m.box((0.0, -0.012, pl / 2 - 0.055), (0.08, 0.014, 0.022), (0.05, 0.045, 0.04))
    m.pop()

    # --- walls -------------------------------------------------------------
    door_half = D["DoorRoughWidth"] / 2
    walls = [
        ("utility", (-HW, 0.0, HL), 0.0, D["InteriorWidth"],
         [(D["VentCentreX"] - D["VentWidth"] / 2 + HW, D["VentCentreX"] + D["VentWidth"] / 2 + HW,
           D["VentCentreY"] - D["VentHeight"] / 2, D["VentCentreY"] + D["VentHeight"] / 2)], True),
        ("entrance", (HW, 0.0, -HL), 180.0, D["InteriorWidth"],
         [(HW - (D["DoorCentreX"] + door_half), HW - (D["DoorCentreX"] - door_half),
           0.0, D["DoorRoughHeight"])], True),
        ("storage", (-HW, 0.0, -DECK_L), -90.0, DECK_L * 2, [], False),
        ("workbench", (HW, 0.0, DECK_L), 90.0, DECK_L * 2,
         [(DECK_L - (D["WindowCentreZ"] + D["WindowWidth"] / 2),
           DECK_L - (D["WindowCentreZ"] - D["WindowWidth"] / 2),
           D["WindowSillHeight"], D["WindowHeadHeight"])], False),
    ]

    for _name, origin, yaw, length, openings, is_gable in walls:
        m.push(origin, rot_euler(0, yaw, 0))
        stud_wall(m, length, WALL_H, openings, PINE)
        panel_with_openings(m, length, WALL_H, BOARD, STUD_D, openings, PINE, cavity=0.72)
        panel_with_openings(m, length, WALL_H, D["CladdingThickness"], STUD_D + BOARD,
                            openings, WEATHER)
        if is_gable:
            gable_infill(m, length)
        m.pop()

    # corner boards
    ox, oz = HW + WALL_T, HL + WALL_T
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.box((sx * (ox + 0.0095), WALL_H / 2, sz * (oz - 0.045)),
                  (0.019, WALL_H, 0.09), WEATHER)
            m.box((sx * (ox + 0.019 - 0.045), WALL_H / 2, sz * (oz + 0.0095)),
                  (0.09, WALL_H, 0.019), WEATHER)


def build_roof(m):
    rafter_z = [round(-HL + i * D["RafterSpacing"], 3) for i in range(11)]
    rafter_z = [-(ROOF_HL - 0.075)] + rafter_z + [ROOF_HL - 0.075]

    ridge_off = D["RidgeBoardThickness"] / 2
    length = (EAVE_X - ridge_off) / math.cos(PITCH)
    mid_x = (ridge_off + EAVE_X) / 2
    mid_y = roof_underside(mid_x)

    m.box((0.0, RIDGE_Y - D["RidgeBoardHeight"] * 0.35, 0.0),
          (D["RidgeBoardThickness"], D["RidgeBoardHeight"], ROOF_HL * 2), PINE)

    for z in rafter_z:
        for side in (-1, 1):
            r = slope_rot(side)
            n = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
            c = np.array([side * mid_x, mid_y, z]) + n * (RAFTER_D / 2)
            m.box(tuple(c), (length, RAFTER_D, D["RafterWidth"]), jitter(PINE, 0.08), r)

    collar_half = HALF_SPAN - (D["CollarTieHeight"] - WALL_H) / K + 0.09
    for z in (-2.4, -1.2, 0.0, 1.2, 2.4):
        m.box((0.0, D["CollarTieHeight"] + 0.045, z + 0.04),
              (collar_half * 2, 0.09, D["CollarTieThickness"]), jitter(PINE, 0.08))

    for t in (0.30, 1.10, 1.90, 2.45):
        if t > length:
            continue
        for side in (-1, 1):
            r = slope_rot(side)
            n = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
            along = np.array([side * math.cos(PITCH), -math.sin(PITCH), 0.0])
            start = np.array([side * ridge_off, roof_underside(ridge_off), 0.0])
            c = start + along * t + n * (RAFTER_D + PURLIN_T / 2)
            m.box(tuple(c), (0.07, PURLIN_T, ROOF_HL * 2), jitter(PINE, 0.08), r)

    # corrugated sheeting, sampled across the slope
    samples = 34
    for side in (-1, 1):
        r = slope_rot(side)
        n = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
        along = np.array([side * math.cos(PITCH), -math.sin(PITCH), 0.0])
        start = np.array([side * ridge_off, roof_underside(ridge_off), 0.0]) \
            + n * (RAFTER_D + PURLIN_T)
        prev = None
        for i in range(samples + 1):
            t = length * i / samples
            phase = t / D.get("CorrPitch", 0.076) * math.pi * 2
            off = math.sin(phase) * 0.016 * 0.5
            p = start + along * t + n * off
            if prev is not None:
                shade = 0.80 + 0.30 * (0.5 + 0.5 * math.cos(phase))
                col = tuple(np.array(GALV) * shade)
                a = prev + np.array([0, 0, -ROOF_HL])
                b = p + np.array([0, 0, -ROOF_HL])
                c = p + np.array([0, 0, ROOF_HL])
                d = prev + np.array([0, 0, ROOF_HL])
                m.quad(tuple(a), tuple(b), tuple(c), tuple(d), col)
            prev = p

    # ridge capping, folded over the apex
    for side in (-1, 1):
        r = slope_rot(side)
        n = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
        along = np.array([side * math.cos(PITCH), -math.sin(PITCH), 0.0])
        start = np.array([side * ridge_off, roof_underside(ridge_off), 0.0])
        c = start + along * 0.14 + n * (RAFTER_D + PURLIN_T + 0.016 + 0.004)
        m.box(tuple(c), (0.30, 0.006, ROOF_HL * 2), GALV, r)

    # fascia and barge boards
    eave_y = roof_underside(EAVE_X)
    for side in (-1, 1):
        m.box((side * (EAVE_X + 0.0095), eave_y + 0.05, 0.0), (0.019, 0.14, ROOF_HL * 2), WEATHER)
        for endz in (-1, 1):
            r = slope_rot(side)
            n = np.array([side * math.sin(PITCH), math.cos(PITCH), 0.0])
            c = np.array([side * mid_x, mid_y, endz * (ROOF_HL - 0.01)]) + n * (RAFTER_D + PURLIN_T / 2)
            m.box(tuple(c), (length, 0.14, 0.019), WEATHER, r)

    # eaves blocking between rafters
    for i in range(len(rafter_z) - 1):
        z0 = rafter_z[i] + D["RafterWidth"] / 2
        z1 = rafter_z[i + 1] - D["RafterWidth"] / 2
        if z1 <= -HL or z0 >= HL or z1 - z0 < 0.05:
            continue
        z0, z1 = max(z0, -HL), min(z1, HL)
        for side in (-1, 1):
            h_in = roof_underside(HW) + (RAFTER_D + PURLIN_T) / math.cos(PITCH) - 0.008 - WALL_H
            h_out = roof_underside(HW + STUD_D) + (RAFTER_D + PURLIN_T) / math.cos(PITCH) - 0.008 - WALL_H
            xa, xb = side * HW, side * (HW + STUD_D)
            m.quad((xa, WALL_H, z0), (xb, WALL_H, z0), (xb, WALL_H + h_out, z0), (xa, WALL_H + h_in, z0), PINE)
            m.quad((xa, WALL_H, z1), (xa, WALL_H + h_in, z1), (xb, WALL_H + h_out, z1), (xb, WALL_H, z1), PINE)
            m.quad((xa, WALL_H + h_in, z0), (xb, WALL_H + h_out, z0),
                   (xb, WALL_H + h_out, z1), (xa, WALL_H + h_in, z1), PINE)


def build_door(m):
    half_rough = D["DoorRoughWidth"] / 2
    jamb = D["DoorJambThickness"]
    clear_h = D["DoorRoughHeight"] - jamb

    m.push((D["DoorCentreX"], 0.0, -HL))
    for s in (-1, 1):
        m.box((s * (half_rough - jamb / 2), clear_h / 2, -WALL_T / 2), (jamb, clear_h, WALL_T), PINE)
    m.box((0.0, clear_h + jamb / 2, -WALL_T / 2), (D["DoorRoughWidth"], jamb, WALL_T), PINE)
    m.box((0.0, 0.009, -WALL_T / 2), (D["DoorRoughWidth"], 0.018, WALL_T), jitter(PINE))
    for s in (-1, 1):
        m.box((s * (half_rough - jamb - 0.006), clear_h / 2, -0.0515), (0.012, clear_h, 0.012), PINE)

    # leaf
    leaf_z = -WALL_T + 0.0225 + 0.025
    m.push((0.0, 0.0, leaf_z))
    w, h = D["DoorLeafWidth"], D["DoorLeafHeight"]
    for i in range(6):
        cx = -w / 2 + w / 6 * (i + 0.5)
        m.box((cx, h / 2 + 0.006, -0.0125), (w / 6 - 0.004, h, 0.025), jitter(PINE, 0.12))
    for ly in (0.22, 1.03, 1.84):
        m.box((0.0, ly + 0.006, 0.01), (w - 0.02, 0.095, 0.02), jitter(PINE, 0.08))
        for i in range(6):
            cx = -w / 2 + w / 6 * (i + 0.5)
            m.cyl((cx, ly + 0.006, 0.024), 0.0075, 0.0075, 0.008, 8, ZINC, rot_euler(90, 0, 0))
    for i, (y0, y1) in enumerate(((0.2675, 0.9825), (1.0775, 1.7925))):
        x0, x1 = -w / 2 + 0.06, w / 2 - 0.06
        ln = math.hypot(x1 - x0, y1 - y0)
        ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
        m.box(((x0 + x1) / 2, (y0 + y1) / 2 + 0.006, 0.01), (ln, 0.09, 0.02),
              jitter(PINE, 0.08), rot_euler(0, 0, ang))
    # ironmongery
    lx = w / 2 - 0.075
    y = D["DoorHandleHeight"] + 0.006
    m.box((lx, y, 0.039), (0.115, 0.145, 0.038), ZINC)
    m.cyl((lx, y, 0.062), 0.0165, 0.0165, 0.014, 14, ZINC, rot_euler(90, 0, 0))
    m.box((lx - 0.048, y, 0.072), (0.105, 0.02, 0.018), ZINC)
    m.cyl((lx, y - 0.052, 0.060), 0.011, 0.011, 0.005, 12, STEEL, rot_euler(90, 0, 0))
    by = 1.726
    m.box((lx, by, 0.032), (0.13, 0.038, 0.014), ZINC)
    m.cyl((lx + 0.045, by, 0.040), 0.008, 0.008, 0.09, 10, ZINC, rot_euler(0, 0, 90))
    m.pop()
    m.pop()


def build_window(m):
    m.push((HW, 0.0, D["WindowCentreZ"]), rot_euler(0, 90, 0))
    half_w = D["WindowWidth"] / 2
    sill, head = D["WindowSillHeight"], D["WindowHeadHeight"]
    fw, fd = D["WindowFrameWidth"], D["WindowFrameDepth"]
    frame_z = 0.06

    for s in (-1, 1):
        m.box((s * (half_w - 0.01), (sill + head) / 2, WALL_T / 2), (0.02, head - sill, WALL_T), PINE)
    m.box((0.0, head - 0.01, WALL_T / 2), (D["WindowWidth"], 0.02, WALL_T), PINE)

    for s in (-1, 1):
        m.box((s * (half_w - fw / 2 - 0.02), (sill + head) / 2, frame_z + fd / 2),
              (fw, head - sill - 0.04, fd), PINE)
    m.box((0.0, head - fw / 2 - 0.02, frame_z + fd / 2), (D["WindowWidth"] - 0.04, fw, fd), PINE)
    m.box((0.0, sill + fw / 2 + 0.01, frame_z + fd / 2), (D["WindowWidth"] - 0.04, fw, fd), PINE)

    gt, gb = head - fw - 0.02, sill + fw + 0.01
    m.box((0.0, (gt + gb) / 2, frame_z + fd / 2), (0.03, gt - gb, fd), PINE)
    # No glass in the preview: the rasteriser has no transparency, so a pane
    # would render as an opaque bright rectangle and hide the view outside.
    m.box((0.0, sill + 0.0125, 0.165 / 2 - D["WindowSillProjection"]),
          (D["WindowWidth"] + 0.1, 0.025, 0.165), jitter(PINE))
    m.pop()


def build_vent(m):
    m.push((D["VentCentreX"], D["VentCentreY"], HL))
    hw2, hh = D["VentWidth"] / 2, D["VentHeight"] / 2
    for s in (-1, 1):
        m.box((s * (hw2 - 0.009), 0.0, WALL_T / 2), (0.018, D["VentHeight"], WALL_T), PINE)
        m.box((0.0, s * (hh - 0.009), WALL_T / 2), (D["VentWidth"] - 0.036, 0.018, WALL_T), PINE)
    m.box((0.0, 0.0, WALL_T - 0.03), (D["VentWidth"] - 0.03, D["VentHeight"] - 0.03, 0.002),
          (0.09, 0.09, 0.085))
    for i in range(5):
        t = (i + 0.5) / 5
        y = hh - 0.012 - t * (D["VentHeight"] - 0.024)
        m.box((0.0, y, WALL_T + 0.01), (D["VentWidth"] - 0.01, 0.03, 0.004), GALV, rot_euler(-35, 0, 0))
    m.pop()


def build_bench(m):
    d, l, h = D["BenchDepth"], D["BenchLength"], D["BenchHeight"]
    tt, leg = D["BenchTopThickness"], D["BenchLegSize"]
    m.push((D["BenchFrontX"] + d / 2, 0.0, D["BenchStartZ"] + l / 2))
    m.box((0.0, h - tt / 2, 0.0), (d, tt, l), PLY)
    m.box((-d / 2 - 0.012, h - 0.03, 0.0), (0.024, 0.06, l), jitter(PINE))
    leg_top = h - tt
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.box((sx * (d / 2 - leg / 2 - 0.02), leg_top / 2, sz * (l / 2 - leg / 2 - 0.04)),
                  (leg, leg_top, leg), jitter(PINE))
    for sx in (-1, 1):
        m.box((sx * (d / 2 - 0.037), leg_top - 0.055, 0.0), (0.035, 0.09, l - 0.16), PINE)
    for sz in (-1, 1):
        m.box((0.0, leg_top - 0.055, sz * (l / 2 - 0.058)), (d - 0.14, 0.09, 0.035), PINE)
    m.box((0.0, 0.2, 0.0), (d - 0.1, BOARD, l - 0.2), jitter(PINE))

    # drawer bank
    m.push((0.0, 0.0, l / 2 - 0.34))
    ct, cb = leg_top - 0.10, 0.26
    for sz in (-1, 1):
        m.box((0.0, cb + (ct - cb) / 2, sz * 0.31), (d - 0.09, ct - cb, BOARD), PINE)
    for i in range(2):
        fh = (ct - cb - 0.014) / 2
        fy = cb + fh / 2 + i * (fh + 0.01) + 0.002
        m.box((-(d - 0.09) / 2 - 0.009, fy, 0.0), (0.018, fh - 0.006, 0.608), jitter(PINE))
        m.cyl((-(d - 0.09) / 2 - 0.03, fy, 0.0), 0.016, 0.02, 0.03, 10, PINE, rot_euler(0, 0, 90))
    m.pop()

    # cupboard
    m.push((0.0, 0.0, -l / 2 + 0.36))
    for sz in (-1, 1):
        m.box((0.0, cb + (ct - cb) / 2, sz * 0.33), (d - 0.09, ct - cb, BOARD), PINE)
    for i, sz in enumerate((-1, 1)):
        dw = 0.33 - 0.008
        ajar = 6.0 if i == 1 else 0.0
        m.push((-(d - 0.09) / 2 - 0.009, cb + (ct - cb) / 2, sz * (0.33 - 0.004)),
               rot_euler(0, sz * ajar, 0))
        m.box((0.0, 0.0, -sz * dw / 2), (0.018, ct - cb - 0.012, dw), jitter(PINE))
        m.cyl((-0.02, 0.0, -sz * (dw - 0.045)), 0.014, 0.017, 0.026, 10, PINE, rot_euler(0, 0, 90))
        m.pop()
    m.pop()

    # vice
    m.push((-d / 2 + 0.01, h, -l / 2 + 0.42))
    m.box((0.075, 0.014, 0.0), (0.19, 0.028, 0.14), STEEL)
    m.box((0.085, 0.07, 0.0), (0.13, 0.09, 0.11), STEEL)
    m.box((0.012, 0.088, 0.0), (0.04, 0.126, 0.13), STEEL)
    m.box((-0.06, 0.088, 0.0), (0.04, 0.126, 0.13), STEEL)
    m.box((-0.055, 0.048, 0.0), (0.07, 0.036, 0.07), STEEL)
    m.cyl((-0.09, 0.088, 0.0), 0.014, 0.014, 0.11, 10, STEEL, rot_euler(0, 0, 90))
    m.cyl((-0.148, 0.088, 0.0), 0.009, 0.009, 0.23, 8, ZINC, rot_euler(90, 0, 0))
    m.pop()
    m.pop()


def build_pegboard(m):
    m.push((HW - 0.001, D["PegboardBottomY"] + D["PegboardHeight"] / 2, -0.25), rot_euler(0, 90, 0))
    for sz in (-1, 1):
        m.box((0.008, 0.0, sz * (0.35 - 0.025)), (0.016, D["PegboardHeight"], 0.045), PINE)
    m.box((0.016 + D["PegboardThickness"] / 2, 0.0, 0.0),
          (D["PegboardThickness"], D["PegboardHeight"], 0.70), PEG)
    fx = 0.016 + D["PegboardThickness"]

    m.push((fx, 0.23, -0.23), rot_euler(0, 0, -4))
    m.cyl((0.055, 0.0, 0.0), 0.017, 0.017, 0.115, 10, PINE, rot_euler(0, 0, 90))
    m.box((0.035, 0.085, 0.0), (0.032, 0.036, 0.115), STEEL)
    m.box((0.035, 0.085, -0.075), (0.028, 0.026, 0.048), STEEL)
    m.pop()

    m.push((fx, 0.14, 0.14), rot_euler(0, 0, 3))
    m.box((0.028, 0.14, 0.0), (0.016, 0.016, 0.30), STEEL)
    m.box((0.020, 0.06, 0.0), (0.006, 0.022, 0.29), STEEL)
    m.box((0.028, 0.10, -0.17), (0.024, 0.11, 0.05), PINE)
    m.pop()

    for i, (dz, dl) in enumerate(((-0.29, 0.19), (-0.238, 0.23), (-0.185, 0.165))):
        m.push((fx, -0.29, dz), rot_euler(0, 0, (i - 1) * 3))
        m.cyl((0.02, -0.045, 0.0), 0.014, 0.011, 0.09, 10, RED)
        m.cyl((0.02, -0.045 - dl / 2, 0.0), 0.004, 0.004, dl, 8, ZINC)
        m.pop()

    m.push((fx, -0.25, 0.23), rot_euler(0, 0, -6))
    m.box((0.018, -0.055, 0.0), (0.014, 0.11, 0.026), STEEL)
    m.box((0.018, -0.135, 0.012), (0.014, 0.07, 0.014), STEEL)
    m.box((0.018, -0.135, -0.012), (0.014, 0.07, 0.014), STEEL)
    m.pop()

    m.box((fx + 0.007, 0.33, 0.29), (0.004, 0.18, 0.032), ZINC)
    m.box((fx + 0.008, 0.42, 0.32), (0.018, 0.026, 0.09), PINE)
    m.pop()

    # tool rack past the window
    m.push((HW - 0.001, 1.96, 1.42), rot_euler(0, 90, 0))
    m.box((0.1, 0.0, 0.0), (0.2, 0.019, 0.6), PINE)
    for sz in (-1, 1):
        m.box((0.055, -0.075, sz * 0.23), (0.15, 0.019, 0.035), PINE, rot_euler(0, 0, 42))
    m.pop()


def build_shelving(m):
    ln, dp, ht = D["ShelfUnitLength"], D["ShelfUnitDepth"], D["ShelfUnitHeight"]
    up = D["ShelfUprightSize"]
    m.push((-HW + dp / 2, 0.0, D["ShelfUnitStartZ"] + ln / 2))
    for z in (-ln / 2 + up / 2, 0.0, ln / 2 - up / 2):
        for sx in (-1, 1):
            m.box((sx * (dp / 2 - up / 2), ht / 2, z), (up, ht, 0.035), jitter(SHELF))
    for sh in (0.25, 0.75, 1.25, 1.75):
        for b in range(2):
            bd = dp / 2 - 0.012
            cx = -dp / 2 + bd / 2 + 0.006 + b * (bd + 0.01)
            m.box((cx, sh - BOARD / 2, 0.0), (bd, BOARD, ln - 0.01), jitter(SHELF, 0.08))
        for sx in (-1, 1):
            m.box((sx * (dp / 2 - 0.03), sh - 0.031, 0.0), (0.03, 0.038, ln - 0.15), SHELF)
    bl = math.hypot(ln, ht)
    m.box((dp / 2 - 0.02, ht / 2, 0.0), (0.016, 0.075, bl), SHELF,
          rot_euler(math.degrees(math.atan2(ht, ln)), 0, 0))
    m.pop()


def bucket(m, base, h, r, col):
    m.cyl((base[0], base[1] + h / 2, base[2]), r * 0.76, r, h, 12, col, cap1=False)
    m.cyl((base[0], base[1] + h - 0.006, base[2]), r + 0.006, r + 0.006, 0.014, 12, col)


def tin(m, base, h, r, col):
    m.cyl((base[0], base[1] + h / 2, base[2]), r, r, h, 12, col)
    m.cyl((base[0], base[1] + h - 0.004, base[2]), r + 0.004, r + 0.004, 0.01, 12, ZINC)


def build_shelf_contents(m):
    front = -HW + 0.22
    back = -HW + 0.15
    y = [0.25, 0.75, 1.25, 1.75]

    bucket(m, (front, y[0], -0.92), 0.29, 0.135, BLUE)
    bucket(m, (front - 0.02, y[0], -0.55), 0.27, 0.128, BLUE)
    m.box((front - 0.01, y[0] + 0.085, 0.18), (0.19, 0.17, 0.42), RED, rot_euler(0, 6, 0))
    m.box((front - 0.01, y[0] + 0.182, 0.18), (0.196, 0.024, 0.426), RED, rot_euler(0, 6, 0))
    for i in range(6):
        m.cyl((front, y[0] + 0.08 + i * 0.022, 0.86), 0.055, 0.082, 0.16, 10, (0.14, 0.14, 0.13),
              cap1=False)

    for i, (tz, th) in enumerate(zip((-1.02, -0.86, -0.70, -0.52), (0.175, 0.175, 0.13, 0.175))):
        tin(m, (back + (0.02 if i % 2 == 0 else 0.06), y[1], tz), th, 0.072, CREAM)
    m.box((front - 0.01, y[1] + 0.11, 0.05), (0.30, 0.22, 0.26), CARD, rot_euler(0, -8, 0))
    m.cyl((front - 0.03, y[1] + 0.12, 0.78), 0.105, 0.099, 0.24, 12, GALV)
    m.cyl((front - 0.03, y[1] + 0.252, 0.78), 0.058, 0.052, 0.03, 10, GALV)
    m.cyl((front + 0.10, y[1] + 0.15, 0.78), 0.022, 0.016, 0.33, 8, GALV, rot_euler(0, 0, -62))

    for i in range(4):
        m.box((front - 0.02, y[2] + 0.018 + i * 0.035, -0.82), (0.36 - i * 0.01, 0.033, 0.48 - i * 0.014),
              jitter(TARP, 0.08))
    for i in range(3):
        m.cyl((back + 0.04 + i * 0.03, y[2] + 0.058, -0.18 + i * 0.10), 0.045, 0.045, 0.115, 10,
              (0.55, 0.55, 0.52))
        m.cyl((back + 0.04 + i * 0.03, y[2] + 0.124, -0.18 + i * 0.10), 0.042, 0.042, 0.018, 10, ZINC)
    m.box((front, y[2] + 0.095, 0.42), (0.26, 0.19, 0.30), CARD, rot_euler(0, 11, 0))
    for i in range(4):
        m.box((back + 0.05, y[2] + 0.021 + i * 0.02, 0.95), (0.18, 0.019, 0.42 - i * 0.05),
              jitter(PINE, 0.10), rot_euler(0, i * 2.5, 0))

    for i, (sy, sx, sz) in enumerate(((0.036, 0.23, 0.40), (0.104, 0.216, 0.368), (0.155, 0.184, 0.312))):
        m.box((front - 0.02, y[3] + sy, -0.78), (sx, 0.072, sz), jitter(CARD, 0.08))
    tin(m, (back + 0.05, y[3], -0.20), 0.175, 0.072, CREAM)
    tin(m, (back + 0.08, y[3], -0.04), 0.13, 0.06, CREAM)
    for i in range(14):
        a0 = i / 14 * math.tau
        a1 = (i + 1) / 14 * math.tau
        p0 = np.array([front - 0.02 + math.cos(a0) * 0.105, y[3] + 0.03 + (i % 2) * 0.006,
                       0.62 + math.sin(a0) * 0.105])
        p1 = np.array([front - 0.02 + math.cos(a1) * 0.105, y[3] + 0.03 + ((i + 1) % 2) * 0.006,
                       0.62 + math.sin(a1) * 0.105])
        d = p1 - p0
        m.cyl(tuple((p0 + p1) / 2), 0.009, 0.009, float(np.linalg.norm(d)), 6, (0.42, 0.36, 0.24),
              rot_from_to(np.array([0.0, 1.0, 0.0]), d / np.linalg.norm(d)))


def build_bench_top(m):
    top = D["BenchHeight"]
    x = D["BenchFrontX"] + 0.30

    m.box((HW - 0.13, top + 0.115, 1.52), (0.18, 0.23, 0.31), CREAM)
    for row in range(3):
        for col in range(3):
            dy = top + 0.036 + row * 0.07
            dz = 1.52 - 0.098 + col * 0.098
            proud = 0.022 if (row == 1 and col == 2) else 0.0
            m.box((HW - 0.13 - 0.09 - 0.006 - proud / 2, dy, dz), (0.012 + proud, 0.058, 0.086),
                  (0.55, 0.55, 0.52))
    for (jz, jh, jr) in ((1.18, 0.12, 0.048), (1.12, 0.095, 0.04)):
        m.cyl((HW - 0.15 - (0.0 if jz > 1.15 else 0.08), top + jh / 2, jz), jr, jr, jh, 10,
              (0.55, 0.55, 0.52))
        m.cyl((HW - 0.15 - (0.0 if jz > 1.15 else 0.08), top + jh + 0.008, jz), jr * 0.94, jr * 0.94,
              0.018, 10, ZINC)
    tin(m, (HW - 0.17, top, 0.98), 0.10, 0.052, CREAM)

    m.push((x - 0.05, top, 0.64), rot_euler(0, -16, 0))
    m.box((0.0, 0.032, 0.0), (0.062, 0.064, 0.245), STEEL)
    m.box((0.0, 0.072, -0.02), (0.03, 0.026, 0.11), PINE)
    m.box((0.0, 0.086, 0.075), (0.024, 0.07, 0.03), PINE, rot_euler(-22, 0, 0))
    m.pop()

    m.box((x - 0.03, top + 0.01, 0.13), (0.185, 0.019, 0.56), jitter(PINE, 0.10), rot_euler(0, 6, 0))
    m.cyl((x + 0.10, top + 0.024, 0.02), 0.004, 0.004, 0.15, 6, (0.6, 0.45, 0.15), rot_euler(0, 0, 90))
    m.cyl((x + 0.13, top + 0.048, -0.18), 0.04, 0.043, 0.096, 12, (0.72, 0.70, 0.66))
    m.cyl((x + 0.185, top + 0.055, -0.18), 0.01, 0.01, 0.052, 8, (0.72, 0.70, 0.66), rot_euler(0, 0, 90))
    m.cyl((x - 0.12, top + 0.038, -0.42), 0.038, 0.038, 0.115, 10, CARD, rot_euler(0, 0, 90))
    m.push((x + 0.06, top, -0.47), rot_euler(0, 28, 0))
    m.box((0.0, 0.014, 0.0), (0.048, 0.028, 0.18), PINE)
    m.box((0.0, -0.006, -0.02), (0.042, 0.032, 0.11), (0.28, 0.22, 0.14))
    m.pop()


def build_utility(m):
    x, y = D["BreakerBoxCentreX"], D["BreakerBoxCentreY"]
    w, h, dpt = D["BreakerBoxWidth"], D["BreakerBoxHeight"], D["BreakerBoxDepth"]
    zf = HL - dpt

    m.box((x, y, HL - dpt / 2), (w, h, dpt), EPLAST)
    for i in range(6):
        tx = x - 0.082 + i * 0.03
        m.box((tx, y + 0.01, zf + 0.014), (0.024, 0.062, 0.028), EPLAST)
        m.box((tx, y + 0.03, zf + 0.004), (0.012, 0.02, 0.014), STEEL)
    m.box((x + 0.104, y + 0.01, zf + 0.014), (0.03, 0.062, 0.028), EPLAST)
    m.box((x + 0.104, y - 0.008, zf + 0.004), (0.016, 0.024, 0.014), RED)
    lz = zf - 0.005
    m.box((x, y + h * 0.25 + 0.02, lz), (w, h * 0.5 - 0.04, 0.01), EPLAST)
    m.box((x, y - h * 0.25 - 0.02, lz), (w, h * 0.5 + 0.02, 0.01), EPLAST)
    for s in (-1, 1):
        m.box((x + s * (w / 2 - (w - 0.23) / 4), y + 0.01, lz), ((w - 0.23) / 2, 0.08, 0.01), EPLAST)

    m.box((x, D["SocketCentreY"], HL - 0.02), (0.15, 0.09, 0.04), EPLAST)
    m.box((x, D["SocketCentreY"], HL - 0.043), (0.146, 0.086, 0.008), EPLAST)
    for s in (-1, 1):
        m.box((x + s * 0.036, D["SocketCentreY"] + 0.008, HL - 0.048), (0.009, 0.02, 0.004), STEEL)

    sx, sy = D["LightSwitchX"], D["LightSwitchY"]
    m.box((sx, sy, -HL + 0.018), (0.078, 0.078, 0.036), EPLAST)
    m.box((sx, sy, -HL + 0.04), (0.088, 0.088, 0.008), EPLAST)
    m.box((sx, sy + 0.004, -HL + 0.047), (0.03, 0.046, 0.008), EPLAST)

    def conduit(a, b):
        a, b = np.array(a, dtype=float), np.array(b, dtype=float)
        d = b - a
        ln = float(np.linalg.norm(d))
        if ln < 1e-4:
            return
        m.cyl(tuple((a + b) / 2), 0.01, 0.01, ln, 8, GALV,
              rot_from_to(np.array([0.0, 1.0, 0.0]), d / ln))
        for i in range(1, int(ln / 0.6) + 1):
            p = a + d * (i / (int(ln / 0.6) + 1))
            m.box(tuple(p), (0.03, 0.03, 0.014), GALV)

    zu, ze, xs = HL - 0.014, -HL + 0.014, -HW + 0.014
    high = 2.30
    conduit((x, y - h / 2, zu), (x, D["SocketCentreY"] + 0.045, zu))
    path = [(x, y + h / 2, zu), (x, high, zu), (xs + 0.06, high, zu),
            (xs, high, HL - 0.08), (xs, high, -HL + 0.08), (xs + 0.06, high, ze),
            (sx, high, ze), (sx, sy + 0.05, ze)]
    for i in range(len(path) - 1):
        conduit(path[i], path[i + 1])
    for p in path[1:-1]:
        m.box(p, (0.052, 0.052, 0.052), GALV)
    m.box((0.0, high, zu), (0.075, 0.075, 0.045), GALV)

    cable = [(0.0, 2.34, HL - 0.02), (0.0, 2.92, HL - 0.02), (0.0, 2.96, HL - 0.30),
             (0.0, 2.96, D["CeilingLightZ"] + 0.10), (0.0, 2.90, D["CeilingLightZ"] + 0.02)]
    for i in range(len(cable) - 1):
        a, b = np.array(cable[i]), np.array(cable[i + 1])
        d = b - a
        ln = float(np.linalg.norm(d))
        m.cyl(tuple((a + b) / 2), 0.0045, 0.0045, ln, 6, (0.10, 0.10, 0.10),
              rot_from_to(np.array([0.0, 1.0, 0.0]), d / ln))

    # utility shelf and radio
    m.push((D["UtilityShelfCentreX"], D["UtilityShelfHeight"], HL))
    m.box((0.0, 0.0, -D["UtilityShelfDepth"] / 2),
          (D["UtilityShelfLength"], BOARD, D["UtilityShelfDepth"]), jitter(PINE))
    for s in (-1, 1):
        bx = s * (D["UtilityShelfLength"] / 2 - 0.15)
        m.box((bx, -0.09, -0.008), (0.028, 0.17, 0.005), ZINC)
        m.box((bx, -0.062, -0.075), (0.024, 0.005, 0.17), ZINC, rot_euler(45, 0, 0))
    m.push((0.15, BOARD / 2, -0.115))
    m.box((0.0, 0.0725, 0.0), (0.23, 0.145, 0.095), (0.20, 0.20, 0.19))
    m.box((-0.045, 0.08, -0.0495), (0.105, 0.078, 0.006), ZINC)
    m.box((0.058, 0.104, -0.0495), (0.078, 0.03, 0.005), ZINC)
    for dx in (0.036, 0.08):
        m.cyl((dx, 0.0435, -0.0555), 0.016, 0.014, 0.016, 10, (0.14, 0.14, 0.13), rot_euler(90, 0, 0))
    m.box((0.0, 0.175, 0.0), (0.16, 0.01, 0.014), ZINC)
    for s in (-1, 1):
        m.box((s * 0.08, 0.159, 0.0), (0.01, 0.038, 0.014), ZINC)
    m.cyl((0.095, 0.295, 0.02), 0.0035, 0.002, 0.30, 6, ZINC, rot_euler(14, 0, -10))
    m.pop()
    m.cyl((-0.40, 0.055, -0.11), 0.038, 0.038, 0.10, 10, (0.55, 0.55, 0.52))
    m.cyl((-0.40, 0.112, -0.11), 0.036, 0.036, 0.016, 10, ZINC)
    m.pop()


def build_entrance_fittings(m):
    z = -HL
    rx, ry = 0.95, 1.65
    m.box((rx, ry, z + 0.011), (0.80, 0.14, 0.022), jitter(PINE))
    for i in range(4):
        hx = rx - 0.30 + i * 0.20
        m.box((hx, ry, z + 0.026), (0.024, 0.07, 0.008), ZINC)
        m.cyl((hx, ry - 0.03, z + 0.052), 0.005, 0.005, 0.055, 6, ZINC, rot_euler(90, 0, 0))
        m.cyl((hx, ry - 0.052, z + 0.072), 0.005, 0.005, 0.045, 6, ZINC)
    for i in range(12):
        a0, a1 = i / 12 * math.tau, (i + 1) / 12 * math.tau
        c = np.array([rx - 0.30, ry - 0.14, z + 0.075])
        p0 = c + np.array([math.cos(a0) * 0.115, math.sin(a0) * 0.132, 0.0])
        p1 = c + np.array([math.cos(a1) * 0.115, math.sin(a1) * 0.132, 0.006])
        d = p1 - p0
        m.cyl(tuple((p0 + p1) / 2), 0.006, 0.006, float(np.linalg.norm(d)), 5, (0.16, 0.16, 0.16),
              rot_from_to(np.array([0.0, 1.0, 0.0]), d / np.linalg.norm(d)))
    m.box((rx + 0.10, ry - 0.19, z + 0.115), (0.23, 0.28, 0.15), (0.30, 0.27, 0.20))

    tx, tz = 1.15, z + 0.33
    m.box((tx, 0.014, tz), (0.62, 0.028, 0.42), RUBBER)
    for i in range(2):
        bx = tx - 0.11 + i * 0.22
        m.push((bx, 0.028, tz + 0.01), rot_euler(0, i * 12, -5 + i * 9))
        m.cyl((0.0, 0.17, 0.0), 0.062, 0.058, 0.34, 10, RUBBER)
        m.box((0.0, 0.03, -0.055), (0.105, 0.06, 0.185), RUBBER)
        m.pop()


def build_props(m):
    # lawnmower
    m.push((1.10, 0.0, -1.55), rot_euler(0, 8, 0))
    wr = 0.09
    dy = wr + 0.045
    m.box((0.0, dy, 0.0), (0.50, 0.115, 0.44), GREEN)
    for sx in (-1, 1):
        for sz in (-1, 1):
            hub = (sx * 0.27, wr, sz * 0.167)
            m.cyl(hub, wr, wr, 0.045, 12, RUBBER, rot_euler(0, 0, 90))
            m.cyl(hub, wr * 0.42, wr * 0.42, 0.05, 8, EPLAST, rot_euler(0, 0, 90))
    m.box((0.0, dy + 0.135, 0.015), (0.235, 0.155, 0.23), STEEL)
    m.box((0.0, dy + 0.235, 0.01), (0.215, 0.07, 0.20), GREEN)
    m.cyl((0.135, dy + 0.14, 0.02), 0.062, 0.062, 0.055, 12, STEEL, rot_euler(0, 0, 90))
    m.box((-0.14, dy + 0.15, 0.02), (0.06, 0.085, 0.12), EPLAST)
    m.box((0.23, dy - 0.01, -0.13), (0.12, 0.09, 0.15), GREEN, rot_euler(0, 34, 0))
    for sx in (-1, 1):
        m.cyl((sx * 0.215, dy + 0.29, -0.47), 0.014, 0.014, 0.98, 8, STEEL, rot_euler(52, 0, 0))
    m.cyl((0.0, 1.01, -0.76), 0.014, 0.014, 0.43, 8, STEEL, rot_euler(0, 0, 90))
    for sx in (-1, 1):
        m.cyl((sx * 0.17, 1.01, -0.76), 0.019, 0.019, 0.10, 8, EPLAST, rot_euler(0, 0, 90))
    m.pop()

    # wheelbarrow
    m.push((-0.90, 0.0, 2.55), rot_euler(0, -90, 0))
    ty = 0.52
    m.box((0.0, ty - 0.09, 0.0), (0.432, 0.03, 0.688), GREEN)
    for sx in (-1, 1):
        m.box((sx * 0.24, ty - 0.02, 0.0), (0.026, 0.19, 0.74), GREEN, rot_euler(0, 0, sx * 20))
    for sz in (-1, 1):
        m.box((0.0, ty - 0.02, sz * 0.378), (0.48, 0.19, 0.026), GREEN, rot_euler(-sz * 18, 0, 0))
    for sx in (-1, 1):
        m.box((sx * 0.185, ty - 0.125, 0.18), (0.04, 0.035, 1.48), PINE, rot_euler(-9, 0, 0))
        m.cyl((sx * 0.185, ty + 0.005, 0.86), 0.021, 0.021, 0.13, 8, RUBBER, rot_euler(81, 0, 0))
    m.cyl((0.0, 0.16, -0.64), 0.16, 0.16, 0.085, 14, RUBBER, rot_euler(0, 0, 90))
    m.cyl((0.0, 0.16, -0.64), 0.055, 0.055, 0.095, 10, STEEL, rot_euler(0, 0, 90))
    for sx in (-1, 1):
        m.box((sx * 0.07, 0.27, -0.61), (0.02, 0.26, 0.03), STEEL, rot_euler(18, 0, sx * 7))
        m.box((sx * 0.19, 0.20, 0.33), (0.028, 0.40, 0.028), STEEL, rot_euler(-6, 0, 0))
    m.box((0.0, 0.01, 0.345), (0.42, 0.024, 0.028), STEEL)
    m.pop()

    # sawhorse
    m.push((-0.60, 0.0, 1.35), rot_euler(0, 12, 0))
    m.box((0.0, 0.595, 0.0), (0.09, 0.09, 0.90), jitter(PINE))
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.box((sx * 0.105, 0.275, sz * 0.36), (0.07, 0.64, 0.035), jitter(PINE),
                  rot_euler(0, 0, sx * 16))
    for sz in (-1, 1):
        m.box((0.0, 0.24, sz * 0.36), (0.30, 0.14, 0.019), PINE)
    m.pop()

    # stool
    m.push((1.12, 0.0, 2.10), rot_euler(0, 26, 0))
    m.box((0.0, 0.456, 0.0), (0.32, 0.028, 0.30), jitter(PINE))
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.box((sx * 0.118, 0.221, sz * 0.108), (0.038, 0.442, 0.038), PINE,
                  rot_euler(-sz * 6, 0, sx * 6))
    for sz in (-1, 1):
        m.box((0.0, 0.15, sz * 0.125), (0.25, 0.028, 0.022), PINE)
    m.pop()

    # compost sack
    m.push((-1.48, 0.0, -2.15), rot_euler(0, 14, 0))
    for (sy, sxx, szz) in ((0.052, 0.78, 0.44), (0.151, 0.733, 0.405), (0.224, 0.624, 0.343)):
        m.box((0.0, sy, 0.0), (sxx, 0.104 if sy < 0.2 else 0.068, szz), SOIL)
    m.pop()

    # leaning offcuts
    m.push((-1.68, 0.0, -2.76), rot_euler(0, 28, 0))
    for i in range(7):
        w = 0.045 + (i % 3) * 0.045
        ln = 1.50 + (i % 4) * 0.22
        tilt = 9 + i * 0.9
        m.box((i * 0.052 - 0.15, ln * 0.48, (i % 2) * 0.028),
              (w, ln, 0.026 + (i % 2) * 0.019), jitter(PINE, 0.12),
              rot_euler(-tilt * 0.35, i * 3, tilt))
    m.pop()

    # loose boards
    m.push((-1.26, 0.0, 0.98), rot_euler(0, 8, 0))
    m.box((0.0, 0.01, 0.0), (0.19, 0.02, 1.35), jitter(PINE, 0.10), rot_euler(0, 5, 0))
    m.box((0.075, 0.03, -0.08), (0.14, 0.02, 1.15), jitter(PINE, 0.10), rot_euler(0, -3, 0))
    m.pop()

    # galvanised bucket by the shelving
    bucket(m, (-1.78, 0.0, -1.15), 0.30, 0.145, GALV)
    m.cyl((-1.74, 0.33, -1.13), 0.015, 0.015, 0.32, 6, PINE, rot_euler(12, 0, 9))


def build_lights_geometry(m):
    z = D["CeilingLightZ"]
    rose = D["CollarTieHeight"]
    m.box((0.0, rose - 0.01, z), (0.14, 0.02, 0.14), PINE)
    m.cyl((0.0, rose - 0.035, z), 0.048, 0.048, 0.032, 12, GALV)
    drop = rose - 0.052 - (D["CeilingLightHeight"] + 0.055)
    m.cyl((0.0, D["CeilingLightHeight"] + 0.055 + drop / 2, z), 0.0045, 0.0045, drop, 6,
          (0.1, 0.1, 0.1))
    m.cyl((0.0, D["CeilingLightHeight"] + 0.058, z), 0.024, 0.026, 0.06, 12, GALV)
    m.cyl((0.0, D["CeilingLightHeight"] + 0.030, z), 0.03, 0.13, 0.105, 16, GALV,
          cap0=False, cap1=False)
    m.cyl((0.0, D["CeilingLightHeight"] - 0.008, z), 0.03, 0.022, 0.075, 10, BULB)

    mount = np.array([HW - 0.035, D["TaskLightHeight"], D["TaskLightZ"]])
    m.push(tuple(mount), rot_euler(0, 90, 0))
    m.box((0.02, 0.0, 0.0), (0.055, 0.07, 0.045), EPLAST)
    m.box((-0.02, -0.03, 0.0), (0.045, 0.014, 0.04), EPLAST)
    m.cyl((-0.075, -0.03, 0.0), 0.008, 0.008, 0.13, 8, EPLAST, rot_euler(0, 0, 72))
    m.cyl((-0.185, -0.095, 0.0), 0.035, 0.115, 0.115, 14, GALV, rot_euler(0, 0, 128),
          cap0=False, cap1=False)
    m.cyl((-0.170, -0.075, 0.0), 0.026, 0.022, 0.06, 10, BULB, rot_euler(0, 0, 128))
    m.pop()


def build_exterior(m):
    gy = -D["FloorStructureDepth"] - 0.20
    m.box((0.0, gy - 0.25, 0.0), (60.0, 0.5, 60.0), GRASS)
    apron_z = -(HL + WALL_T) - 0.9
    m.box((D["DoorCentreX"], gy + 0.02, apron_z), (2.2, 0.06, 1.8), GRAVEL)
    step_h = -0.02 - gy
    m.box((D["DoorCentreX"], gy + step_h / 2, -(HL + WALL_T + 0.225)), (1.10, step_h, 0.45), CONCRETE)
    # A hedge line, so the view out of the window is not an empty plane.
    for i in range(-6, 7):
        m.box((HW + 6.5, gy + 0.55, i * 1.2), (0.9, 1.1, 1.15), (0.13, 0.19, 0.10))


def build_scene() -> Mesh:
    m = Mesh()
    build_shell(m)
    build_roof(m)
    build_door(m)
    build_window(m)
    build_vent(m)
    build_bench(m)
    build_pegboard(m)
    build_shelving(m)
    build_shelf_contents(m)
    build_bench_top(m)
    build_utility(m)
    build_entrance_fittings(m)
    build_props(m)
    build_lights_geometry(m)
    build_exterior(m)
    return m


# ----------------------------------------------------------------------------
# Shading
# ----------------------------------------------------------------------------

SUN_DIR = rot_euler(46.0, -122.0, 0.0) @ np.array([0.0, 0.0, 1.0])
SUN_L = -SUN_DIR / np.linalg.norm(SUN_DIR)
SUN_COL = np.array([1.00, 0.955, 0.875]) * 3.1

WIN_CENTRE = np.array([HW, (D["WindowSillHeight"] + D["WindowHeadHeight"]) / 2, D["WindowCentreZ"]])
VENT_CENTRE = np.array([D["VentCentreX"], D["VentCentreY"], HL])

CEIL_POS = np.array([0.0, D["CeilingLightHeight"] - 0.01, D["CeilingLightZ"]])
CEIL_COL = np.array([1.0, 0.74, 0.48]) * 1.35
TASK_POS = np.array([HW - 0.20, D["TaskLightHeight"] - 0.09, D["TaskLightZ"] + 0.02])
TASK_COL = np.array([1.0, 0.80, 0.60]) * 0.85


def is_inside(p: np.ndarray) -> np.ndarray:
    """Interior classification for the shading model.

    The upper bound has to clear the ridge, not sit on it: the roof sheeting,
    the ridge capping and the upper half of every rafter all live above
    RIDGE_Y, and classifying them as exterior lights their undersides as if
    they were facing open sky.
    """
    return ((np.abs(p[:, 0]) < HW + 0.03) & (np.abs(p[:, 2]) < HL + 0.03)
            & (p[:, 1] > -0.05) & (p[:, 1] < RIDGE_Y + 0.45))


def sun_visible(p: np.ndarray) -> np.ndarray:
    """True where a ray from p toward the sun leaves through a real opening."""
    inside = is_inside(p)
    lit = np.ones(len(p), dtype=bool)

    lx, ly, lz = SUN_L
    with np.errstate(divide='ignore', invalid='ignore'):
        t_win = (HW - p[:, 0]) / lx if abs(lx) > 1e-6 else np.full(len(p), -1.0)
        t_z = (HL - p[:, 2]) / lz if abs(lz) > 1e-6 else np.full(len(p), 1e9)
        t_roof = (WALL_H + HALF_SPAN * K - K * p[:, 0] - p[:, 1]) / (ly + K * lx)

    t_win = np.where(np.isfinite(t_win), t_win, -1.0)
    t_z = np.where(np.isfinite(t_z), t_z, 1e9)
    t_roof = np.where(np.isfinite(t_roof), t_roof, 1e9)

    qz = p[:, 2] + t_win * lz
    qy = p[:, 1] + t_win * ly
    through_window = ((t_win > 0)
                      & (t_win <= t_z + 1e-6) & (t_win <= t_roof + 1e-6)
                      & (np.abs(qz - D["WindowCentreZ"]) < D["WindowWidth"] / 2)
                      & (qy > D["WindowSillHeight"]) & (qy < D["WindowHeadHeight"]))

    vx = p[:, 0] + t_z * lx
    vy = p[:, 1] + t_z * ly
    through_vent = ((t_z > 0) & (t_z <= t_roof + 1e-6)
                    & (np.abs(vx - D["VentCentreX"]) < D["VentWidth"] / 2)
                    & (np.abs(vy - D["VentCentreY"]) < D["VentHeight"] / 2))

    lit[inside] = (through_window | through_vent)[inside]
    return lit


def aperture_fill(p, n, centre, strength, size):
    d = centre - p
    dist = np.linalg.norm(d, axis=1)
    dhat = d / np.maximum(dist, 1e-6)[:, None]
    facing = np.clip(np.einsum('ij,ij->i', n, dhat), 0.0, 1.0)
    return strength * facing * size / (size + dist ** 2)


def point_light(p, n, pos, col, power, cone_dir=None, cone_cos=None):
    d = pos - p
    dist = np.linalg.norm(d, axis=1)
    dhat = d / np.maximum(dist, 1e-6)[:, None]
    ndl = np.clip(np.einsum('ij,ij->i', n, dhat), 0.0, 1.0)
    atten = power / (0.35 + dist ** 2)
    if cone_dir is not None:
        c = np.clip(np.einsum('ij,j->i', -dhat, cone_dir), 0.0, 1.0)
        atten = atten * np.clip((c - cone_cos) / (1.0 - cone_cos), 0.0, 1.0) ** 0.7
    return (ndl * atten)[:, None] * col


def shade(centroids, normals, albedo):
    emissive = albedo.max(axis=1) > 1.2
    out = np.zeros_like(albedo)

    inside = is_inside(centroids)

    ndl = np.clip(normals @ SUN_L, 0.0, 1.0)
    lit = sun_visible(centroids)
    out += albedo * (ndl * lit)[:, None] * SUN_COL

    up = np.clip(normals[:, 1] * 0.5 + 0.5, 0.0, 1.0)
    sky = np.array([0.42, 0.53, 0.72])
    bounce = np.array([0.24, 0.23, 0.18])
    hemi = bounce[None, :] * (1 - up)[:, None] + sky[None, :] * up[:, None]
    ambient_strength = np.where(inside, 0.16, 1.15)[:, None]
    out += albedo * hemi * ambient_strength

    win = aperture_fill(centroids, normals, WIN_CENTRE, 1.55, 1.1)
    vent = aperture_fill(centroids, normals, VENT_CENTRE, 0.35, 0.5)
    fill_col = np.array([0.62, 0.68, 0.80])
    out += albedo * ((win + vent) * inside)[:, None] * fill_col

    out += albedo * point_light(centroids, normals, CEIL_POS, CEIL_COL, 1.0) * inside[:, None]
    task_dir = rot_euler(52.0, -118.0, 0.0) @ np.array([0.0, 0.0, 1.0])
    task_dir /= np.linalg.norm(task_dir)
    out += (albedo * point_light(centroids, normals, TASK_POS, TASK_COL, 0.55,
                                 task_dir, math.cos(math.radians(50)))) * inside[:, None]

    out[emissive] = albedo[emissive]
    return out


def background(camera: Camera) -> np.ndarray:
    H, W = camera.height, camera.width
    ys = np.linspace(0.0, 1.0, H)[:, None]
    top = np.array([0.34, 0.50, 0.80])
    bot = np.array([0.72, 0.78, 0.86])
    grad = top[None, None, :] * (1 - ys)[:, :, None] + bot[None, None, :] * ys[:, :, None]
    return np.repeat(grad, W, axis=1) * 2.4


# ----------------------------------------------------------------------------
# Viewpoints
# ----------------------------------------------------------------------------

EYE = D["PlayerEyeHeight"]

VIEWS = [
    ("01_entrance_to_workbench", (D["DoorCentreX"], EYE, -2.55), (1.55, 1.10, 0.70), 70),
    ("02_workbench_to_entrance", (1.05, EYE, 0.60), (-0.85, 1.25, -2.95), 70),
    ("03_rear_corner_overview", (-1.50, EYE, 2.45), (0.65, 0.95, -1.60), 78),
    ("04_workbench_closeup", (0.95, 1.38, 0.25), (1.85, 0.94, 0.55), 55),
    ("05_electrical_utility_area", (0.85, 1.55, 1.75), (0.85, 1.45, 3.00), 58),
    ("06_ceiling_and_roof_structure", (0.00, 1.60, -0.60), (0.00, 3.05, 0.90), 74),
    ("07_floor_and_object_contact", (-0.62, 1.30, -0.30), (-0.62, 0.02, -1.55), 62),
    ("08_window_and_exterior", (1.05, 1.45, 0.60), (2.60, 1.32, 0.60), 60),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--width", type=int, default=1280)
    ap.add_argument("--height", type=int, default=720)
    ap.add_argument("--exposure", type=float, default=0.62)
    ap.add_argument("--only", type=int, default=0, help="render only view N (1-8)")
    args = ap.parse_args()

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    print("building geometry...")
    mesh = build_scene()
    print(f"  {len(mesh.tris):,} triangles")

    views = VIEWS if args.only == 0 else [VIEWS[args.only - 1]]

    for name, eye, target, fov in views:
        cam = Camera(eye, target, fov, args.width, args.height)
        print(f"rendering {name} ...", flush=True)
        frame = render(mesh, cam, shade, background)
        img = (tonemap(frame, args.exposure) * 255).astype(np.uint8)
        path = OUT_DIR / f"{name}.png"
        Image.fromarray(img).save(path)
        print(f"  wrote {path.relative_to(OUT_DIR.parent.parent)}")


if __name__ == "__main__":
    main()
