#!/usr/bin/env python3
"""Render swatch sheets of the procedural material set.

    python3 Tools/preview_textures.py [--size 512] [--out docs/previews/materials]

This is a mirror of `ShedTextureGenerator.cs`, which is the source of truth. It
exists because the authoring environment has no Unity, so the only way to see
what the generator produces is to reimplement it. The two must be kept in step
by hand; if they drift, the C# is right and this is wrong.

The wood model is worth explaining because it is not obvious. A tiling texture
has to be exactly periodic, and the figure real timber shows - the cathedral
arches of a plainsawn board - comes from rings that are concentric about a pith
somewhere outside the board. Concentric circles are not periodic, so they cannot
be used directly. Instead a periodic stripe pattern is warped by periodic noise:
the stripes wander into arches and flames locally while the whole map still
repeats exactly.
"""

from __future__ import annotations

import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent


# ---------------------------------------------------------------------------
# Periodic noise, mirroring TilingNoise.cs
# ---------------------------------------------------------------------------

def _hash(x, y, seed):
    h = (x.astype(np.int64) * 374761393 + y.astype(np.int64) * 668265263
         + np.int64(seed) * 1442695040)
    h = (h ^ (h >> 13)) * 1274126177
    h ^= h >> 16
    return (h & 0x7fffffff) / float(0x7fffffff)


def _smooth(t):
    return t * t * (3.0 - 2.0 * t)


def value(x, y, period, seed):
    xi, yi = np.floor(x).astype(np.int64), np.floor(y).astype(np.int64)
    xf, yf = _smooth(x - xi), _smooth(y - yi)
    w = lambda v: np.mod(v, period)  # noqa: E731
    v00 = _hash(w(xi), w(yi), seed)
    v10 = _hash(w(xi + 1), w(yi), seed)
    v01 = _hash(w(xi), w(yi + 1), seed)
    v11 = _hash(w(xi + 1), w(yi + 1), seed)
    return (v00 * (1 - xf) + v10 * xf) * (1 - yf) + (v01 * (1 - xf) + v11 * xf) * yf


def fbm(x, y, period, seed, octaves=4, gain=0.5):
    total = np.zeros_like(x)
    amp, norm, freq, p = 1.0, 0.0, 1.0, period
    for o in range(octaves):
        total += value(x * freq, y * freq, p, seed + o * 71) * amp
        norm += amp
        amp *= gain
        freq *= 2.0
        p *= 2
    return total / norm


def cell(x, y, period, seed, second=False):
    """Periodic Worley. Returns distance to the nearest feature point, or to the
    cell boundary (F2 - F1) when `second` is set - which is what draws the
    angular crystal edges of galvanised spangle rather than round blobs."""
    xi, yi = np.floor(x).astype(np.int64), np.floor(y).astype(np.int64)
    f1 = np.full(x.shape, 10.0)
    f2 = np.full(x.shape, 10.0)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = xi + dx, yi + dy
            wx, wy = np.mod(cx, period), np.mod(cy, period)
            px = cx + _hash(wx, wy, seed)
            py = cy + _hash(wx, wy, seed + 977)
            d = (px - x) ** 2 + (py - y) ** 2
            f2 = np.minimum(f2, np.maximum(d, f1))
            f1 = np.minimum(f1, d)
    return (np.sqrt(f2) - np.sqrt(f1)) if second else np.sqrt(f1)


# ---------------------------------------------------------------------------
# The wood model
# ---------------------------------------------------------------------------

RINGS_PER_METRE = 60          # integer, so the stripe pattern stays periodic
LATEWOOD_FRACTION = 0.30      # dark band as a share of each ring


def wood_figure(u, v, seed, warp=1.35, ring_scale=1.0, sharpness=1.0):
    """Growth rings warped into plainsawn figure. Returns (ring, knot, fibre).

    `ring` is 0 in earlywood and 1 at the centre of a latewood band. The warp is
    two octaves at very different aspect ratios: a broad one that bends whole
    groups of rings into arches, and a finer one that gives each ring its own
    wobble.
    """
    # Both warps have to stay well under one ring of displacement per unit of
    # length, or the rings stop being continuous lines and become dashes.
    broad = fbm(u * 1.6, v * 0.5, 2, seed + 11, octaves=3) - 0.5
    fine = fbm(u * 5.0, v * 1.1, 5, seed + 29, octaves=2) - 0.5

    rings = RINGS_PER_METRE * ring_scale
    # Slow variation in ring spacing. Without it the grain is a comb.
    density = 0.72 + 0.56 * fbm(u * 1.1, v * 0.35, 2, seed + 71, octaves=2)
    s = np.cumsum(np.ones_like(u), axis=1) * 0.0  # keeps the shape, cost-free
    s = u * rings * density + broad * warp * 5.0 + fine * warp * 0.9

    # Sawtooth across each ring, then a sharp band at the late end. Real latewood
    # is a narrow hard band, not a sine.
    phase = s - np.floor(s)
    band = np.clip((phase - (1.0 - LATEWOOD_FRACTION)) / LATEWOOD_FRACTION, 0.0, 1.0)
    band = np.sin(band * math.pi * 0.5) ** 1.4
    soft = 0.5 - 0.5 * np.cos(phase * math.pi * 2.0)
    ring = band * sharpness + soft * (1.0 - sharpness)

    # Fibre: long thin streaks. The aspect ratio is what makes it read as fibre
    # rather than as noise, so it is stretched about sixty to one.
    fibre = fbm(u * 96.0, v * 1.6, 96, seed + 53, octaves=2)

    # Knots, sparse. Two per square metre is already generous for clean stock.
    kd = cell(u * 1.7, v * 1.1, 2, seed + 101)
    knot = np.clip(1.0 - kd * 7.5, 0.0, 1.0) ** 2.4

    return ring, knot, fibre


def sample_pine(u, v, seed=11, pale=(0.512, 0.408, 0.278), dark=(0.335, 0.243, 0.150)):
    ring, knot, fibre = wood_figure(u, v, seed)

    pale = np.array(pale)
    dark = np.array(dark)
    t = np.clip(ring * 0.62 + (fibre - 0.5) * 0.13 + 0.06, 0, 1)
    rgb = pale[None, None, :] * (1 - t)[..., None] + dark[None, None, :] * t[..., None]

    # Knots are dark, slightly redder, and ringed.
    knot_col = np.array([0.180, 0.116, 0.068])
    rgb = rgb * (1 - (knot * 0.88)[..., None]) + knot_col[None, None, :] * (knot * 0.88)[..., None]

    # Mill marks: a faint regular ripple across the grain, from the saw.
    saw = np.sin(v * 190.0 * math.pi * 2.0) * 0.5 + 0.5
    rgb *= (0.972 + 0.028 * saw)[..., None]

    height = 0.5 + (ring - 0.5) * 0.30 - knot * 0.22 + (saw - 0.5) * 0.04
    smooth = 0.235 - ring * 0.070 - knot * 0.05
    ao = 1.0 - ring * 0.11 - knot * 0.30

    return rgb, height, smooth, ao, np.zeros_like(u)


def sample_floorboard(u, v):
    rgb, height, smooth, ao, metal = sample_pine(u, v, seed=131,
                                                 pale=(0.470, 0.372, 0.256),
                                                 dark=(0.300, 0.216, 0.134))
    # Traffic polish: broad, and only in the smoothness.
    polish = fbm(u * 2.2, v * 2.2, 3, 211, octaves=3)
    smooth = smooth + polish * 0.20

    # A light even settling of dust. A used shed, not a neglected one.
    dust = fbm(u * 5.0, v * 5.0, 5, 223, octaves=3)
    amt = np.clip((dust - 0.46) * 1.15, 0, 1) * 0.17
    rgb = rgb * (1 - amt[..., None]) + np.array([0.472, 0.440, 0.396])[None, None, :] * amt[..., None]
    smooth = smooth - amt * 0.55

    # Drag scuffs where things have been pulled across the floor.
    scuff = 1.0 - np.abs(fbm(u * 16.0, v * 3.0, 16, 307, octaves=2) * 2 - 1)
    mask = np.clip((scuff - 0.87) * 9.0, 0, 1)
    rgb = rgb * (1 - (mask * 0.30)[..., None]) + \
        np.array([0.300, 0.235, 0.160])[None, None, :] * (mask * 0.30)[..., None]

    return rgb, height, smooth, ao, metal


def sample_weatherboard(u, v):
    boards = 7  # 143 mm boards; divides into a metre exactly
    bv = v * boards
    within = bv - np.floor(bv)

    lap = np.clip(1.0 - within / 0.085, 0, 1)
    taper = 0.35 + 0.65 * within

    # Paint over timber: the grain reads through as a height variation more than
    # as colour, which is what distinguishes painted wood from bare wood.
    ring, knot, fibre = wood_figure(u, v, seed=401, warp=0.55)
    paint = np.array([0.318, 0.340, 0.300])
    rgb = np.repeat(np.repeat(paint[None, None, :], u.shape[0], 0), u.shape[1], 1).copy()
    rgb *= (0.975 + 0.05 * fibre)[..., None]

    # Chalking, and worn patches where the paint has thinned to the timber.
    wear = fbm(u * 4.0, v * 4.0, 4, 433, octaves=3)
    worn = np.clip((wear - 0.60) * 3.2, 0, 1)
    rgb = rgb * (1 - (worn * 0.42)[..., None]) + \
        np.array([0.430, 0.368, 0.276])[None, None, :] * (worn * 0.42)[..., None]

    height = taper * 0.72 + ring * 0.10 - lap * 0.58
    smooth = 0.42 - worn * 0.20 - lap * 0.10
    ao = 1.0 - lap * 0.55
    return rgb, height, smooth, ao, np.zeros_like(u)


def sample_ply(u, v):
    # Rotary-cut veneer: very stretched figure, occasional patch.
    ring, knot, fibre = wood_figure(u, v, seed=601, warp=0.85, ring_scale=0.30,
                                    sharpness=0.15)
    pale = np.array([0.500, 0.392, 0.252])
    mid = np.array([0.352, 0.262, 0.162])
    t = np.clip(ring * 0.42 + (fibre - 0.5) * 0.16 + 0.10, 0, 1)
    rgb = pale[None, None, :] * (1 - t)[..., None] + mid[None, None, :] * t[..., None]

    use = fbm(u * 3.0, v * 3.0, 3, 617, octaves=3)
    rgb = rgb * (1 - (np.clip((use - 0.5) * 1.4, 0, 1) * 0.42)[..., None]) + \
        np.array([0.268, 0.205, 0.140])[None, None, :] * \
        (np.clip((use - 0.5) * 1.4, 0, 1) * 0.42)[..., None]

    scratch = 1.0 - np.abs(fbm(u * 44.0, v * 3.2, 44, 631, octaves=2) * 2 - 1)
    sm = np.clip((scratch - 0.93) * 13.0, 0, 1)
    rgb = rgb * (1 - (sm * 0.34)[..., None]) + \
        np.array([0.560, 0.452, 0.310])[None, None, :] * (sm * 0.34)[..., None]

    height = 0.5 + (ring - 0.5) * 0.18 - sm * 0.10
    smooth = 0.20 + use * 0.26 - sm * 0.10
    return rgb, height, smooth, 1.0 - sm * 0.2, np.zeros_like(u)


def sample_galvanised(u, v):
    edge = cell(u * 22.0, v * 22.0, 22, 701, second=True)
    crystal = np.clip(edge * 2.0, 0, 1)          # bright facets, dark boundaries
    facet = _hash(np.floor(u * 22.0).astype(np.int64),
                  np.floor(v * 22.0).astype(np.int64), 705)
    spangle = np.clip(crystal * (0.55 + 0.75 * facet), 0, 1)
    base = np.array([0.470, 0.486, 0.500])
    rgb = base[None, None, :] * (0.92 + 0.15 * spangle)[..., None]

    dirt = fbm(u * 5.0, v * 5.0, 5, 733, octaves=3)
    d = np.clip((dirt - 0.6) * 1.5, 0, 1) * 0.30
    rgb = rgb * (1 - d[..., None]) + np.array([0.430, 0.430, 0.420])[None, None, :] * d[..., None]

    scratch = 1.0 - np.abs(fbm(u * 44.0, v * 7.0, 44, 757, octaves=2) * 2 - 1)
    sm = np.clip((scratch - 0.92) * 11.0, 0, 1)
    return (rgb, 0.5 + (spangle - 0.5) * 0.10,
            0.44 + spangle * 0.18 + sm * 0.12 - dirt * 0.08,
            np.ones_like(u), np.ones_like(u))


def sample_pegboard(u, v):
    holes = 40  # 25 mm pitch
    gx = (u * holes) - np.floor(u * holes) - 0.5
    gy = (v * holes) - np.floor(v * holes) - 0.5
    d = np.sqrt(gx * gx + gy * gy)
    t = np.clip((d - 0.10) / 0.04, 0, 1)
    hole = 1.0 - (t * t * (3 - 2 * t))

    fib = fbm(u * 30.0, v * 30.0, 30, 811, octaves=3)
    board = np.array([0.352, 0.252, 0.170])[None, None, :] * (1 - fib)[..., None] + \
        np.array([0.278, 0.196, 0.128])[None, None, :] * fib[..., None]
    rgb = board * (1 - hole[..., None]) + \
        np.array([0.045, 0.038, 0.032])[None, None, :] * hole[..., None]

    return (rgb, 0.85 - hole * 0.85 + fib * 0.08,
            0.30 - hole * 0.25 - fib * 0.06, 1.0 - hole * 0.75, np.zeros_like(u))


FAMILIES = {
    "Pine": sample_pine,
    "PineFloorboard": sample_floorboard,
    "Weatherboard": sample_weatherboard,
    "PlyBench": sample_ply,
    "Galvanised": sample_galvanised,
    "Pegboard": sample_pegboard,
}


# ---------------------------------------------------------------------------
# Rendering the sheet
# ---------------------------------------------------------------------------

def render(fn, size):
    v, u = np.meshgrid((np.arange(size) + 0.5) / size,
                       (np.arange(size) + 0.5) / size, indexing='ij')
    rgb, height, smooth, ao, metal = fn(u, v)
    rgb = np.clip(rgb, 0, 1)
    height = np.clip(np.broadcast_to(np.asarray(height, dtype=float), u.shape), 0, 1)
    smooth = np.clip(np.broadcast_to(np.asarray(smooth, dtype=float), u.shape), 0, 1)
    ao = np.clip(np.broadcast_to(np.asarray(ao, dtype=float), u.shape), 0, 1)
    return rgb, height, smooth, ao


def lit(rgb, height, smooth, ao, size):
    """A quick relit view so the swatch shows what the normal map will do."""
    gy, gx = np.gradient(height)
    strength = size * 0.016
    nx, ny = -gx * strength, -gy * strength
    nz = np.ones_like(height)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny, nz = nx / ln, ny / ln, nz / ln

    lx, ly, lz = -0.42, -0.38, 0.82
    ndl = np.clip(nx * lx + ny * ly + nz * lz, 0, 1)
    # Restrained specular: enough to show the normal map, not enough to wash the
    # albedo out to white, which is what makes a swatch impossible to judge.
    spec = np.clip(nx * lx + ny * ly + nz * lz, 0, 1) ** (6.0 + smooth * 60.0) * smooth * 0.16
    shade = (0.42 + 0.62 * ndl) * (0.55 + 0.45 * ao)
    out = rgb * shade[..., None] + spec[..., None]
    return np.clip(out, 0, 1) ** (1 / 2.2)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--size", type=int, default=384)
    ap.add_argument("--out", default="docs/previews/materials")
    args = ap.parse_args()

    out = ROOT / args.out
    out.mkdir(parents=True, exist_ok=True)

    size = args.size
    pad, label_h = 10, 22
    cols, rows = 3, 2
    sheet_w = cols * (size + pad) + pad
    sheet_h = rows * (size + pad + label_h) + pad
    sheet = np.ones((sheet_h, sheet_w, 3)) * 0.09

    for i, (name, fn) in enumerate(FAMILIES.items()):
        rgb, height, smooth, ao = render(fn, size)
        tile = lit(rgb, height, smooth, ao, size)

        # Save the three maps at full size too.
        Image.fromarray((np.clip(rgb, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)) \
            .save(out / f"{name}_albedo.png")
        Image.fromarray((tile * 255).astype(np.uint8)).save(out / f"{name}_lit.png")

        r, c = divmod(i, cols)
        y = pad + r * (size + pad + label_h) + label_h
        x = pad + c * (size + pad)
        sheet[y:y + size, x:x + size] = tile
        print(f"  {name}")

    img = Image.fromarray((sheet * 255).astype(np.uint8))

    from PIL import ImageDraw
    draw = ImageDraw.Draw(img)
    for i, name in enumerate(FAMILIES):
        r, c = divmod(i, cols)
        y = pad + r * (size + pad + label_h)
        x = pad + c * (size + pad)
        draw.text((x + 2, y + 5), f"{name}   1 m x 1 m", fill=(214, 208, 196))

    path = out / "material_sheet.png"
    img.save(path)
    print(f"\nwrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
