#!/usr/bin/env python3
"""Generate scale drawings of the shed into docs/diagrams/.

The dimensions are parsed straight out of Assets/Game/Scripts/Environment/
ShedDimensions.cs rather than being retyped here. That is the whole point: the
drawings, the generated scene and the design documents all read from one table,
so a drawing can never quietly disagree with the room.

Usage:
    python3 Tools/generate_diagrams.py
"""

from __future__ import annotations

import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DIMENSIONS_CS = ROOT / "Assets/Game/Scripts/Environment/ShedDimensions.cs"
OUT_DIR = ROOT / "docs/diagrams"

PX_PER_M = 110.0
MARGIN = 90.0

INK = "#22262b"
LIGHT = "#9aa3ad"
FILL_WALL = "#c8cdd3"
FILL_TIMBER = "#d8c9a8"
FILL_ROOF = "#b9c0c7"
FILL_PROP = "#e6e2d8"
ACCENT = "#4a6fa5"
PATH_COLOR = "#7aa06a"


def parse_dimensions() -> dict[str, float]:
    """Pull `public const float Name = <expr>;` out of the C# table."""
    text = DIMENSIONS_CS.read_text()
    raw: dict[str, str] = {}

    pattern = re.compile(
        r"public\s+const\s+float\s+(\w+)\s*=\s*([^;]+);", re.MULTILINE
    )
    for name, expr in pattern.findall(text):
        raw[name] = expr.strip()

    resolved: dict[str, float] = {}

    def resolve(name: str, seen: set[str]) -> float:
        if name in resolved:
            return resolved[name]
        if name in seen:
            raise ValueError(f"circular constant: {name}")
        expr = raw[name]
        seen.add(name)

        # Strip C# float suffixes and comments, then substitute other constants.
        expr = re.sub(r"//.*", "", expr)
        expr = re.sub(r"(\d)[fF]\b", r"\1", expr)

        def sub(match: re.Match) -> str:
            token = match.group(0)
            if token in raw:
                return repr(resolve(token, seen))
            return token

        expr = re.sub(r"[A-Za-z_]\w*", sub, expr)
        value = float(eval(expr, {"__builtins__": {}}, {}))  # noqa: S307 - local constants only
        resolved[name] = value
        seen.discard(name)
        return value

    for name in raw:
        try:
            resolve(name, set())
        except Exception:
            # Constants that reference things we do not model (none today) are skipped.
            continue

    # Values ShedDimensions exposes as computed properties rather than consts.
    # Mirrored here with the same formulas so the drawings stay in step.
    resolved["BenchFrontX"] = resolved["HalfWidth"] - resolved["BenchDepth"]
    resolved["BenchEndZ"] = resolved["BenchStartZ"] + resolved["BenchLength"]
    resolved["WindowHeadHeight"] = resolved["WindowSillHeight"] + resolved["WindowHeight"]
    resolved["RidgeRise"] = resolved["RoofHalfSpan"] * math.tan(
        math.radians(resolved["RoofPitchDegrees"])
    )
    resolved["RidgeHeight"] = resolved["WallHeight"] + resolved["RidgeRise"]

    return resolved


class Svg:
    def __init__(self, width: float, height: float, title: str):
        self.width = width
        self.height = height
        self.title = title
        self.parts: list[str] = []

    def rect(self, x, y, w, h, fill=FILL_WALL, stroke=INK, sw=1.2, opacity=1.0):
        self.parts.append(
            f'<rect x="{x:.2f}" y="{y:.2f}" width="{w:.2f}" height="{h:.2f}" '
            f'fill="{fill}" fill-opacity="{opacity}" stroke="{stroke}" stroke-width="{sw}"/>'
        )

    def poly(self, points, fill=FILL_ROOF, stroke=INK, sw=1.2):
        pts = " ".join(f"{x:.2f},{y:.2f}" for x, y in points)
        self.parts.append(
            f'<polygon points="{pts}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'
        )

    def line(self, x1, y1, x2, y2, stroke=INK, sw=1.0, dash=None):
        d = f' stroke-dasharray="{dash}"' if dash else ""
        self.parts.append(
            f'<line x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" '
            f'stroke="{stroke}" stroke-width="{sw}"{d}/>'
        )

    def path(self, d, stroke=INK, fill="none", sw=1.0, dash=None):
        da = f' stroke-dasharray="{dash}"' if dash else ""
        self.parts.append(
            f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"{da}/>'
        )

    def text(self, x, y, content, size=13, anchor="start", fill=INK, weight="normal"):
        safe = content.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        self.parts.append(
            f'<text x="{x:.2f}" y="{y:.2f}" font-family="Helvetica,Arial,sans-serif" '
            f'font-size="{size}" text-anchor="{anchor}" fill="{fill}" '
            f'font-weight="{weight}">{safe}</text>'
        )

    def circle(self, cx, cy, r, fill=ACCENT, stroke="none", sw=1.0):
        self.parts.append(
            f'<circle cx="{cx:.2f}" cy="{cy:.2f}" r="{r:.2f}" fill="{fill}" '
            f'stroke="{stroke}" stroke-width="{sw}"/>'
        )

    def dimension(self, x1, y1, x2, y2, label, offset=0):
        """A dimension line with ticks and a centred label."""
        self.line(x1, y1, x2, y2, stroke=LIGHT, sw=1.0)
        tick = 5
        if abs(y2 - y1) < 0.5:
            self.line(x1, y1 - tick, x1, y1 + tick, stroke=LIGHT)
            self.line(x2, y2 - tick, x2, y2 + tick, stroke=LIGHT)
            self.text((x1 + x2) / 2, y1 - 7 + offset, label, size=12,
                      anchor="middle", fill=LIGHT)
        else:
            self.line(x1 - tick, y1, x1 + tick, y1, stroke=LIGHT)
            self.line(x2 - tick, y2, x2 + tick, y2, stroke=LIGHT)
            self.text(x1 + 9 + offset, (y1 + y2) / 2 + 4, label, size=12,
                      anchor="start", fill=LIGHT)

    def render(self) -> str:
        body = "\n  ".join(self.parts)
        return (
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.width:.0f}" '
            f'height="{self.height:.0f}" viewBox="0 0 {self.width:.0f} {self.height:.0f}">\n'
            f'  <title>{self.title}</title>\n'
            f'  <rect width="100%" height="100%" fill="#f6f4ef"/>\n'
            f'  {body}\n'
            f'</svg>\n'
        )


def build_floor_plan(d: dict[str, float]) -> Svg:
    hw, hl = d["HalfWidth"], d["HalfLength"]
    wall = d["WallThickness"]

    inner_w = hw * 2 * PX_PER_M
    inner_l = hl * 2 * PX_PER_M
    wall_px = wall * PX_PER_M

    width = inner_w + (wall_px * 2) + (MARGIN * 2) + 300
    height = inner_l + (wall_px * 2) + (MARGIN * 2) + 40

    svg = Svg(width, height, "Shed floor plan")

    # World (x, z) -> screen. +z (utility wall) is drawn at the top.
    def sx(x: float) -> float:
        return MARGIN + wall_px + ((x + hw) * PX_PER_M)

    def sz(z: float) -> float:
        return MARGIN + wall_px + ((hl - z) * PX_PER_M)

    svg.text(MARGIN, 40, "Shed room - floor plan", size=20, weight="bold")
    svg.text(MARGIN, 60,
             f"Interior {d['InteriorWidth']:.1f} m x {d['InteriorLength']:.1f} m "
             f"= {d['InteriorWidth'] * d['InteriorLength']:.0f} m2"
             f"   |   walls {wall * 1000:.0f} mm"
             f"   |   1 m = {PX_PER_M:.0f} px",
             size=12, fill=LIGHT)

    # Wall envelope drawn as a thick frame.
    svg.rect(sx(-hw) - wall_px, sz(hl) - wall_px,
             inner_w + (wall_px * 2), inner_l + (wall_px * 2), fill=FILL_WALL)
    svg.rect(sx(-hw), sz(hl), inner_w, inner_l, fill="#fbfaf6")

    # --- door opening on the entrance wall (z = -hl) ---
    door_half = d["DoorRoughWidth"] / 2
    dx0, dx1 = sx(d["DoorCentreX"] - door_half), sx(d["DoorCentreX"] + door_half)
    svg.rect(dx0, sz(-hl), dx1 - dx0, wall_px, fill="#fbfaf6", stroke="none")
    svg.line(dx0, sz(-hl), dx0, sz(-hl) + wall_px, sw=1.4)
    svg.line(dx1, sz(-hl), dx1, sz(-hl) + wall_px, sw=1.4)
    # Leaf and swing (opens outward).
    leaf = d["DoorLeafWidth"] * PX_PER_M
    svg.line(dx0, sz(-hl) + wall_px, dx0, sz(-hl) + wall_px + leaf, stroke=ACCENT, sw=2)
    svg.path(f"M {dx0 + leaf:.2f} {sz(-hl) + wall_px:.2f} "
             f"A {leaf:.2f} {leaf:.2f} 0 0 1 {dx0:.2f} {sz(-hl) + wall_px + leaf:.2f}",
             stroke=ACCENT, sw=1.0, dash="4 3")
    svg.text((dx0 + dx1) / 2, sz(-hl) + wall_px + leaf + 18,
             f"door {d['DoorLeafWidth'] * 1000:.0f} x {d['DoorLeafHeight'] * 1000:.0f}",
             size=11, anchor="middle", fill=ACCENT)

    # --- window on the workbench wall (x = +hw) ---
    win_half = d["WindowWidth"] / 2
    wz0, wz1 = sz(d["WindowCentreZ"] + win_half), sz(d["WindowCentreZ"] - win_half)
    svg.rect(sx(hw), wz0, wall_px, wz1 - wz0, fill="#dceaf2", stroke=ACCENT, sw=1.4)
    svg.text(sx(hw) + wall_px + 8, (wz0 + wz1) / 2 + 4,
             f"window {d['WindowWidth'] * 1000:.0f} x {d['WindowHeight'] * 1000:.0f}"
             f", sill {d['WindowSillHeight'] * 1000:.0f}",
             size=11, fill=ACCENT)

    # --- vent on the utility wall (z = +hl) ---
    vx0 = sx(d["VentCentreX"] - d["VentWidth"] / 2)
    vx1 = sx(d["VentCentreX"] + d["VentWidth"] / 2)
    svg.rect(vx0, sz(hl) - wall_px, vx1 - vx0, wall_px, fill="#dceaf2", stroke=ACCENT, sw=1.2)
    svg.text((vx0 + vx1) / 2, sz(hl) - wall_px - 8, "vent", size=11,
             anchor="middle", fill=ACCENT)

    # --- fixtures ---
    def prop(x0, z0, x1, z1, label, fill=FILL_PROP, size=11):
        a, b = sx(min(x0, x1)), sz(max(z0, z1))
        w, h = abs(sx(x1) - sx(x0)), abs(sz(z1) - sz(z0))
        svg.rect(a, b, w, h, fill=fill, stroke=INK, sw=0.9)
        if label:
            svg.text(a + w / 2, b + h / 2 + 4, label, size=size, anchor="middle")

    prop(d["BenchFrontX"], d["BenchStartZ"], hw, d["BenchStartZ"] + d["BenchLength"],
         "workbench", FILL_TIMBER)
    prop(-hw, d["ShelfUnitStartZ"], -hw + d["ShelfUnitDepth"],
         d["ShelfUnitStartZ"] + d["ShelfUnitLength"], "shelving", FILL_TIMBER)
    prop(d["UtilityShelfCentreX"] - d["UtilityShelfLength"] / 2, hl - d["UtilityShelfDepth"],
         d["UtilityShelfCentreX"] + d["UtilityShelfLength"] / 2, hl, "utility shelf", FILL_TIMBER)
    prop(d["BreakerBoxCentreX"] - d["BreakerBoxWidth"] / 2, hl - d["BreakerBoxDepth"],
         d["BreakerBoxCentreX"] + d["BreakerBoxWidth"] / 2, hl, "", "#cfd6dd")
    svg.text(sx(d["BreakerBoxCentreX"]), sz(hl) + 24, "breaker", size=10, anchor="middle")

    # Service hatch.
    prop(d["ServicePanelCentreX"] - d["ServicePanelWidth"] / 2,
         d["ServicePanelCentreZ"] - d["ServicePanelLength"] / 2,
         d["ServicePanelCentreX"] + d["ServicePanelWidth"] / 2,
         d["ServicePanelCentreZ"] + d["ServicePanelLength"] / 2,
         "hatch", "#eae3d2", 10)

    # Floor props, drawn from the placements in PropsBuilder.
    for x, z, w, l, name in [
        (1.10, -1.55, 0.58, 1.10, "mower"),
        (-0.90, 2.55, 1.73, 0.42, "barrow"),
        (-0.60, 1.35, 0.32, 0.90, "sawhorse"),
        (1.12, 2.10, 0.32, 0.30, "stool"),
        (-1.48, -2.15, 0.78, 0.44, "compost"),
        (-1.68, -2.76, 0.45, 0.30, "offcuts"),
        (-1.26, 0.98, 0.35, 1.35, ""),
        (1.15, -2.67, 0.62, 0.42, "boots"),
    ]:
        prop(x - w / 2, z - l / 2, x + w / 2, z + l / 2, name, "#e9e4d6", 10)

    # Circulation route.
    route = [(-0.85, -2.55), (-0.45, -1.60), (0.10, -0.40), (0.25, 1.10), (0.35, 2.35)]
    dpath = "M " + " L ".join(f"{sx(x):.1f} {sz(z):.1f}" for x, z in route)
    svg.path(dpath, stroke=PATH_COLOR, sw=3.0, dash="9 6")
    svg.circle(sx(-0.85), sz(-2.55), 6, fill=PATH_COLOR)
    svg.text(sx(-0.85) - 12, sz(-2.55) + 20, "spawn", size=11, anchor="middle", fill=PATH_COLOR)

    # Dimensions.
    top = sz(hl) - wall_px - 34
    svg.dimension(sx(-hw), top, sx(hw), top, f"{d['InteriorWidth']:.2f} m interior")
    right = sx(hw) + wall_px + 46
    svg.dimension(right, sz(hl), right, sz(-hl), f"{d['InteriorLength']:.2f} m interior")

    legend_y = height - 30
    svg.text(MARGIN, legend_y, "N", size=12, fill=LIGHT)
    svg.line(MARGIN + 14, legend_y - 4, MARGIN + 44, legend_y - 4, stroke=LIGHT, sw=1.2)
    svg.text(MARGIN + 52, legend_y,
             "+Z is the utility wall (top). +X is the workbench wall (right).",
             size=11, fill=LIGHT)
    svg.text(MARGIN, legend_y + 18,
             "Dashed green: intended circulation route. Dashed blue: door swing.",
             size=11, fill=LIGHT)

    return svg


def build_cross_section(d: dict[str, float]) -> Svg:
    """Section across the width, showing the roof pitch and both wall fit-outs."""
    hw = d["HalfWidth"]
    wall = d["WallThickness"]
    span = d["RoofHalfSpan"] + d["EaveOverhang"]
    ridge = d["WallHeight"] + (d["RoofHalfSpan"] * math.tan(math.radians(d["RoofPitchDegrees"])))

    width = (span * 2 * PX_PER_M) + (MARGIN * 2) + 120
    height = (ridge + 1.3) * PX_PER_M + (MARGIN * 2)

    svg = Svg(width, height, "Shed cross section")
    ground_y = height - MARGIN

    def sx(x: float) -> float:
        return (width / 2) + (x * PX_PER_M) - 30

    def sy(y: float) -> float:
        return ground_y - (y * PX_PER_M)

    svg.text(MARGIN, 40, "Shed room - cross section looking toward the utility wall",
             size=20, weight="bold")
    svg.text(MARGIN, 60,
             f"Wall height {d['WallHeight']:.2f} m   |   roof pitch "
             f"{d['RoofPitchDegrees']:.0f} deg   |   ridge {ridge:.2f} m",
             size=12, fill=LIGHT)

    # Ground and floor structure.
    svg.line(sx(-span - 0.4), sy(-d["FloorStructureDepth"] - 0.2),
             sx(span + 0.4), sy(-d["FloorStructureDepth"] - 0.2), stroke=LIGHT, sw=2)
    svg.rect(sx(-hw - d["StudDepth"]), sy(0),
             (hw + d["StudDepth"]) * 2 * PX_PER_M, d["FloorStructureDepth"] * PX_PER_M,
             fill=FILL_TIMBER)
    svg.text(sx(0), sy(-d["FloorStructureDepth"] / 2) + 4,
             "floor: 19 mm boards / 90x45 joists / bearers on piers",
             size=10, anchor="middle")

    # Walls.
    for sign in (-1, 1):
        x0 = sign * hw
        x1 = sign * (hw + wall)
        svg.rect(sx(min(x0, x1)), sy(d["WallHeight"]),
                 wall * PX_PER_M, d["WallHeight"] * PX_PER_M, fill=FILL_WALL)

    # Roof planes.
    for sign in (-1, 1):
        eave_x = sign * span
        eave_y = d["WallHeight"] - (d["EaveOverhang"] * math.tan(math.radians(d["RoofPitchDegrees"])))
        svg.poly([
            (sx(0), sy(ridge)),
            (sx(eave_x), sy(eave_y)),
            (sx(eave_x), sy(eave_y - 0.135)),
            (sx(0), sy(ridge - 0.135)),
        ], fill=FILL_ROOF)

    svg.text(sx(0), sy(ridge) - 10, "corrugated steel on purlins over 90x45 rafters at 600",
             size=10, anchor="middle")

    # Collar tie.
    collar_half = d["RoofHalfSpan"] - (
        (d["CollarTieHeight"] - d["WallHeight"]) / math.tan(math.radians(d["RoofPitchDegrees"]))
    ) + 0.09
    svg.rect(sx(-collar_half), sy(d["CollarTieHeight"] + 0.09),
             collar_half * 2 * PX_PER_M, 0.09 * PX_PER_M, fill=FILL_TIMBER)
    svg.text(sx(collar_half) + 6, sy(d["CollarTieHeight"]) + 4, "collar tie", size=10)

    # Workbench (right) and shelving (left).
    bench_x0 = d["BenchFrontX"]
    svg.rect(sx(bench_x0), sy(d["BenchHeight"]),
             (hw - bench_x0) * PX_PER_M, d["BenchHeight"] * PX_PER_M,
             fill=FILL_TIMBER, opacity=0.65)
    svg.text(sx(bench_x0) - 6, sy(d["BenchHeight"]) - 8,
             f"bench {d['BenchHeight'] * 1000:.0f}", size=10, anchor="end")

    svg.rect(sx(-hw), sy(d["ShelfUnitHeight"]),
             d["ShelfUnitDepth"] * PX_PER_M, d["ShelfUnitHeight"] * PX_PER_M,
             fill=FILL_TIMBER, opacity=0.65)
    for shelf in (0.25, 0.75, 1.25, 1.75):
        svg.line(sx(-hw), sy(shelf), sx(-hw + d["ShelfUnitDepth"]), sy(shelf), stroke=INK, sw=0.8)
    svg.text(sx(-hw + d["ShelfUnitDepth"]) + 6, sy(d["ShelfUnitHeight"]) - 8,
             "shelving 1950", size=10)

    # Window in section.
    svg.rect(sx(hw), sy(d["WindowHeadHeight"]), wall * PX_PER_M,
             d["WindowHeight"] * PX_PER_M, fill="#dceaf2", stroke=ACCENT, sw=1.4)

    # Human reference.
    fx = sx(0.2)
    svg.line(fx, sy(0), fx, sy(1.45), stroke="#5c6672", sw=2)
    svg.circle(fx, sy(1.62), 0.15 * PX_PER_M, fill="none", stroke="#5c6672", sw=2)
    svg.line(fx - 22, sy(1.25), fx + 22, sy(1.25), stroke="#5c6672", sw=2)
    svg.line(fx, sy(0.95), fx - 16, sy(0), stroke="#5c6672", sw=2)
    svg.line(fx, sy(0.95), fx + 16, sy(0), stroke="#5c6672", sw=2)
    svg.text(fx + 26, sy(1.80), "1.80 m", size=11, fill="#5c6672")
    svg.line(fx - 40, sy(d["PlayerEyeHeight"]), fx + 60, sy(d["PlayerEyeHeight"]),
             stroke="#5c6672", sw=0.8, dash="4 3")
    svg.text(fx + 64, sy(d["PlayerEyeHeight"]) + 4,
             f"eye {d['PlayerEyeHeight']:.2f} m", size=10, fill="#5c6672")

    # Dimensions.
    dim_x = sx(-span) - 34
    svg.dimension(dim_x, sy(0), dim_x, sy(d["WallHeight"]), f"{d['WallHeight']:.2f} m")
    svg.dimension(dim_x - 34, sy(0), dim_x - 34, sy(ridge), f"{ridge:.2f} m to ridge")
    base = sy(-d["FloorStructureDepth"]) + 30
    svg.dimension(sx(-hw), base, sx(hw), base, f"{d['InteriorWidth']:.2f} m interior")

    return svg


def build_long_section(d: dict[str, float]) -> Svg:
    """Section along the length, looking at the workbench wall."""
    hl = d["HalfLength"]
    wall = d["WallThickness"]
    ridge = d["WallHeight"] + (d["RoofHalfSpan"] * math.tan(math.radians(d["RoofPitchDegrees"])))
    over = d["GableOverhang"] + wall

    width = ((hl + over) * 2 * PX_PER_M) + (MARGIN * 2)
    height = (ridge + 1.0) * PX_PER_M + (MARGIN * 2)

    svg = Svg(width, height, "Shed long section")
    ground_y = height - MARGIN

    def sz(z: float) -> float:
        return (width / 2) - (z * PX_PER_M)

    def sy(y: float) -> float:
        return ground_y - (y * PX_PER_M)

    svg.text(MARGIN, 40, "Shed room - long section looking at the workbench wall",
             size=20, weight="bold")
    svg.text(MARGIN, 60,
             "Entrance wall on the right, utility wall on the left. "
             "Rafters at 600 mm centres.", size=12, fill=LIGHT)

    # Floor and gable walls.
    svg.rect(sz(hl + d["StudDepth"]), sy(0),
             (hl + d["StudDepth"]) * 2 * PX_PER_M, d["FloorStructureDepth"] * PX_PER_M,
             fill=FILL_TIMBER)

    for sign in (-1, 1):
        z0, z1 = sign * hl, sign * (hl + wall)
        svg.rect(sz(max(z0, z1)), sy(d["WallHeight"]),
                 wall * PX_PER_M, d["WallHeight"] * PX_PER_M, fill=FILL_WALL)

    # Roof line seen in elevation: a flat band at the ridge.
    svg.rect(sz(hl + over), sy(ridge), (hl + over) * 2 * PX_PER_M, 0.14 * PX_PER_M,
             fill=FILL_ROOF)
    svg.text(sz(0), sy(ridge) - 10, f"ridge {ridge:.2f} m", size=11, anchor="middle")

    # Rafters, drawn as ticks along the ridge.
    z = -hl
    while z <= hl + 0.001:
        svg.line(sz(z), sy(ridge), sz(z), sy(d["WallHeight"]), stroke=LIGHT, sw=0.9)
        z += d["RafterSpacing"]

    # Top plate line.
    svg.line(sz(hl + over), sy(d["WallHeight"]), sz(-hl - over), sy(d["WallHeight"]),
             stroke=INK, sw=1.4)

    # Door on the entrance wall (right).
    svg.rect(sz(-hl) - 4, sy(d["DoorLeafHeight"]), 8, d["DoorLeafHeight"] * PX_PER_M,
             fill="#fbfaf6", stroke=ACCENT, sw=1.4)
    svg.text(sz(-hl) - 12, sy(d["DoorLeafHeight"]) - 8,
             f"door head {d['DoorLeafHeight']:.2f} m", size=10, anchor="end", fill=ACCENT)

    # Workbench and shelving in elevation.
    svg.rect(sz(d["BenchStartZ"] + d["BenchLength"]), sy(d["BenchHeight"]),
             d["BenchLength"] * PX_PER_M, d["BenchHeight"] * PX_PER_M,
             fill=FILL_TIMBER, opacity=0.7)
    svg.text(sz(d["BenchStartZ"] + d["BenchLength"] / 2), sy(d["BenchHeight"]) - 8,
             f"workbench {d['BenchLength']:.1f} m", size=10, anchor="middle")

    # Window elevation.
    svg.rect(sz(d["WindowCentreZ"] + d["WindowWidth"] / 2), sy(d["WindowHeadHeight"]),
             d["WindowWidth"] * PX_PER_M, d["WindowHeight"] * PX_PER_M,
             fill="#dceaf2", stroke=ACCENT, sw=1.4)
    svg.line(sz(d["WindowCentreZ"]), sy(d["WindowHeadHeight"]),
             sz(d["WindowCentreZ"]), sy(d["WindowSillHeight"]), stroke=ACCENT, sw=1.0)

    # Pegboard.
    svg.rect(sz(-0.25 + 0.35), sy(d["PegboardBottomY"] + d["PegboardHeight"]),
             0.70 * PX_PER_M, d["PegboardHeight"] * PX_PER_M,
             fill="#d9c7ae", opacity=0.75)
    svg.text(sz(-0.25), sy(d["PegboardBottomY"] + d["PegboardHeight"]) - 8,
             "pegboard", size=10, anchor="middle")

    # Ceiling light.
    svg.line(sz(d["CeilingLightZ"]), sy(d["CollarTieHeight"]),
             sz(d["CeilingLightZ"]), sy(d["CeilingLightHeight"]), stroke=INK, sw=1.0)
    svg.circle(sz(d["CeilingLightZ"]), sy(d["CeilingLightHeight"]), 8, fill="#f0d9a0",
               stroke=INK, sw=1.0)

    # Dimensions.
    dim_z = sz(hl + over) - 34
    svg.dimension(dim_z, sy(0), dim_z, sy(d["WallHeight"]), f"{d['WallHeight']:.2f} m")
    base = sy(0) + 34
    svg.dimension(sz(hl), base, sz(-hl), base, f"{d['InteriorLength']:.2f} m interior")

    return svg


def main() -> None:
    dims = parse_dimensions()
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    drawings = {
        "floor_plan.svg": build_floor_plan(dims),
        "cross_section.svg": build_cross_section(dims),
        "long_section.svg": build_long_section(dims),
    }

    for name, svg in drawings.items():
        (OUT_DIR / name).write_text(svg.render())
        print(f"wrote docs/diagrams/{name}")

    print(f"\nParsed {len(dims)} constants from ShedDimensions.cs")
    for key in ("InteriorWidth", "InteriorLength", "WallHeight", "WallThickness",
                "DoorLeafWidth", "WindowSillHeight", "BenchHeight"):
        if key in dims:
            print(f"  {key:<20} {dims[key]:.3f}")


if __name__ == "__main__":
    main()
