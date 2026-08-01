#!/usr/bin/env python3
"""Export the shed as a self-contained WebGL walkthrough page.

    python3 Tools/export_web_walkthrough.py [--tessellate 1.0] [--out docs/walkthrough.html]

Same caveat as the preview renders: this is built from ShedDimensions.cs by the
Python reconstruction, not from Unity. Lighting is baked into vertex colours by
the same shading model the preview renderer uses, so the page needs no lighting
maths at runtime - which is what keeps it to one draw call and makes it run on a
phone.

The geometry is quantised to millimetres as int16 and the colours to bytes, then
base64'd into the page. No external requests, no libraries.
"""

from __future__ import annotations

import argparse
import base64
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))

import preview_render as pr           # noqa: E402
from preview_raster import prepare    # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
POS_SCALE = 1000.0   # millimetres


def build_payload(max_edge: float):
    mesh = pr.build_scene()
    pr.TESSELLATION[0] = max_edge

    a, b, c, ca, cb, cc = prepare(
        mesh, np.array([0.0, 1.6, 0.0]), pr.shade, max_edge, pr.tessellation_limit)

    verts = np.empty((len(a) * 3, 3), dtype=np.float64)
    verts[0::3], verts[1::3], verts[2::3] = a, b, c

    cols = np.empty((len(a) * 3, 3), dtype=np.float64)
    cols[0::3], cols[1::3], cols[2::3] = ca, cb, cc

    # Bake the same tonemap the stills use, so the page is display-ready.
    cols = pr.tonemap(cols, 0.46)

    quantised = np.clip(np.round(verts * POS_SCALE), -32767, 32767).astype('<i2')
    colours = np.clip(np.round(cols * 255.0), 0, 255).astype(np.uint8)

    return len(a), quantised.tobytes(), colours.tobytes()


HTML = """<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no">
<title>Shed room - walkthrough</title>
<style>
  /* Palette taken from the building's own materials: sawn pine, galvanised
     zinc, and the muted sage the weatherboards are painted. Neutrals carry a
     warm bias so they read as chosen rather than inherited. */
  :root {{
    --ground: #EFEAE1;
    --scrim: 18, 16, 13;
    --ink: #211E19;
    --ink-soft: #6B655B;
    --edge: rgba(33, 30, 25, .18);
    --chip: rgba(247, 244, 238, .82);
    --chip-hover: rgba(255, 253, 249, .95);
    --pine: #8A7350;
    --sage: #5C6754;
    --on-sage: #F2F0E9;
  }}
  @media (prefers-color-scheme: dark) {{
    :root {{
      --ground: #16140F;
      --scrim: 8, 7, 5;
      --ink: #EDE8DE;
      --ink-soft: #9A9284;
      --edge: rgba(237, 232, 222, .20);
      --chip: rgba(30, 27, 22, .78);
      --chip-hover: rgba(48, 43, 35, .92);
      --pine: #C4A97C;
      --sage: #99A78C;
      --on-sage: #16140F;
    }}
  }}
  :root[data-theme="light"] {{
    --ground: #EFEAE1; --scrim: 18, 16, 13; --ink: #211E19; --ink-soft: #6B655B;
    --edge: rgba(33, 30, 25, .18); --chip: rgba(247, 244, 238, .82);
    --chip-hover: rgba(255, 253, 249, .95); --pine: #8A7350; --sage: #5C6754;
    --on-sage: #F2F0E9;
  }}
  :root[data-theme="dark"] {{
    --ground: #16140F; --scrim: 8, 7, 5; --ink: #EDE8DE; --ink-soft: #9A9284;
    --edge: rgba(237, 232, 222, .20); --chip: rgba(30, 27, 22, .78);
    --chip-hover: rgba(48, 43, 35, .92); --pine: #C4A97C; --sage: #99A78C;
    --on-sage: #16140F;
  }}

  * {{ box-sizing: border-box; }}
  html, body {{ margin: 0; height: 100%; }}
  body {{
    background: var(--ground); color: var(--ink);
    font-family: ui-sans-serif, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    overscroll-behavior: none; -webkit-font-smoothing: antialiased;
  }}
  .mono {{
    font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace;
    font-variant-numeric: tabular-nums;
  }}

  #wrap {{ position: relative; width: 100vw; height: 100vh; height: 100svh;
           overflow: hidden; touch-action: none; }}
  canvas {{ display: block; width: 100%; height: 100%; background: #6E86A8; }}

  .overlay {{ position: absolute; pointer-events: none; user-select: none; }}

  #rail {{
    top: 0; left: 0; right: 0; padding: 11px 15px 22px;
    display: flex; flex-wrap: wrap; align-items: baseline; gap: 4px 12px;
    background: linear-gradient(to bottom, rgba(var(--scrim), .72), rgba(var(--scrim), 0));
    color: #F2EEE6;
  }}
  #rail h1 {{ margin: 0; font-size: 14px; font-weight: 600; letter-spacing: .005em; }}
  #rail .spec {{ font-size: 11.5px; opacity: .78; }}
  #rail .caveat {{
    flex-basis: 100%; font-size: 10.5px; letter-spacing: .07em;
    text-transform: uppercase; opacity: .58; margin-top: 1px;
  }}

  #readout {{
    left: 15px; bottom: 172px; font-size: 11px; letter-spacing: .02em;
    color: #F2EEE6; opacity: .7; text-shadow: 0 1px 3px rgba(0,0,0,.55);
  }}
  @media (min-width: 780px) {{ #readout {{ bottom: 18px; }} }}

  /* Controls sit on a translucent chip so they stay legible over any part of
     the render without a heavy panel competing with it. */
  .chip {{
    pointer-events: auto; font: inherit; font-size: 12px; line-height: 1;
    padding: 9px 12px; border-radius: 7px;
    border: 1px solid var(--edge); background: var(--chip); color: var(--ink);
    cursor: pointer; -webkit-tap-highlight-color: transparent;
    backdrop-filter: blur(10px); -webkit-backdrop-filter: blur(10px);
  }}
  .chip:hover {{ background: var(--chip-hover); }}
  .chip:focus-visible {{ outline: 2px solid var(--pine); outline-offset: 2px; }}
  .chip[aria-pressed="true"] {{ background: var(--sage); color: var(--on-sage); border-color: transparent; }}

  #views {{
    position: absolute; right: 12px; bottom: 20px;
    display: flex; flex-direction: column; gap: 5px; align-items: stretch;
    max-height: 62svh; overflow-y: auto;
  }}
  #views .num {{
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
    color: var(--pine); margin-right: 7px; font-size: 11px;
  }}
  #views .chip {{ text-align: left; white-space: nowrap; }}

  #crouch {{ position: absolute; right: 12px; top: 74px; }}

  #stick {{
    position: absolute; left: 16px; bottom: 20px; width: 128px; height: 128px;
    border-radius: 50%; border: 1.5px solid rgba(242, 238, 230, .30);
    background: rgba(var(--scrim), .28); touch-action: none;
    backdrop-filter: blur(3px); -webkit-backdrop-filter: blur(3px);
  }}
  #knob {{
    position: absolute; left: 50%; top: 50%; width: 52px; height: 52px; margin: -26px 0 0 -26px;
    border-radius: 50%; background: rgba(242, 238, 230, .42);
    border: 1.5px solid rgba(242, 238, 230, .62);
    transition: transform .12s ease-out;
  }}
  @media (prefers-reduced-motion: reduce) {{ #knob {{ transition: none; }} }}
  @media (min-width: 780px) {{ #stick {{ display: none; }} }}

  #hint {{
    left: 0; right: 0; bottom: 0; padding: 26px 15px 9px; text-align: center;
    font-size: 11px; letter-spacing: .01em; color: #F2EEE6; opacity: .6;
    background: linear-gradient(to top, rgba(var(--scrim), .6), rgba(var(--scrim), 0));
  }}
  @media (max-width: 779px) {{ #hint {{ display: none; }} }}
</style>

<div id="wrap">
  <canvas id="gl" aria-label="Interactive walkthrough of the shed interior"></canvas>

  <div id="rail" class="overlay">
    <h1>Shed room</h1>
    <span class="spec mono">4.0 &times; 6.0 m &middot; walls 2.4 m &middot; ridge 3.26 m</span>
    <span class="caveat">Preview reconstruction &mdash; not a Unity render</span>
  </div>

  <div id="readout" class="overlay mono"></div>
  <div id="stick"><div id="knob"></div></div>
  <button id="crouch" class="chip" aria-pressed="false">Crouch</button>
  <div id="views"></div>
  <div id="hint" class="overlay">Click to capture the mouse &middot; WASD to walk &middot; C to crouch</div>
</div>

<script>
const TRI_COUNT = {tri_count};
const POS_SCALE = {pos_scale};
const POS_B64 = "{pos_b64}";
const COL_B64 = "{col_b64}";
const VIEWS = {views_json};
const BOUNDS = {bounds_json};

function unb64(s) {{
  const bin = atob(s);
  const out = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
  return out;
}}

const posBytes = unb64(POS_B64);
const positions = new Int16Array(posBytes.buffer, posBytes.byteOffset, posBytes.byteLength / 2);
const colors = unb64(COL_B64);

const canvas = document.getElementById('gl');
const gl = canvas.getContext('webgl', {{ antialias: true, alpha: false }});
if (!gl) {{
  document.getElementById('wrap').innerHTML =
    '<p style="padding:28px;font-size:15px;max-width:34em">This walkthrough needs WebGL, ' +
    'which this browser did not provide. The still renders in docs/previews/ show the same room.</p>';
  throw new Error('no webgl');
}}

function compile(type, src) {{
  const s = gl.createShader(type);
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
  return s;
}}

const prog = gl.createProgram();
gl.attachShader(prog, compile(gl.VERTEX_SHADER,
  'attribute vec3 aPos; attribute vec3 aCol; uniform mat4 uMVP; varying vec3 vCol;' +
  'void main() {{ vCol = aCol; gl_Position = uMVP * vec4(aPos, 1.0); }}'));
gl.attachShader(prog, compile(gl.FRAGMENT_SHADER,
  'precision mediump float; varying vec3 vCol;' +
  'void main() {{ gl_FragColor = vec4(vCol, 1.0); }}'));
gl.linkProgram(prog);
if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(prog));
gl.useProgram(prog);

const posBuf = gl.createBuffer();
gl.bindBuffer(gl.ARRAY_BUFFER, posBuf);
gl.bufferData(gl.ARRAY_BUFFER, positions, gl.STATIC_DRAW);
const aPos = gl.getAttribLocation(prog, 'aPos');
gl.enableVertexAttribArray(aPos);
gl.vertexAttribPointer(aPos, 3, gl.SHORT, false, 0, 0);

const colBuf = gl.createBuffer();
gl.bindBuffer(gl.ARRAY_BUFFER, colBuf);
gl.bufferData(gl.ARRAY_BUFFER, colors, gl.STATIC_DRAW);
const aCol = gl.getAttribLocation(prog, 'aCol');
gl.enableVertexAttribArray(aCol);
gl.vertexAttribPointer(aCol, 3, gl.UNSIGNED_BYTE, true, 0, 0);

const uMVP = gl.getUniformLocation(prog, 'uMVP');
gl.enable(gl.DEPTH_TEST);
gl.clearColor(0.43, 0.53, 0.66, 1.0);

// ---- camera ---------------------------------------------------------------
const cam = {{ x: -0.85, y: 1.70, z: -2.55, yaw: 0, pitch: 0, crouch: false }};
const EYE_STAND = 1.70, EYE_CROUCH = 1.15;

function setView(v) {{
  cam.x = v.pos[0]; cam.y = v.pos[1]; cam.z = v.pos[2];
  const dx = v.look[0] - v.pos[0], dy = v.look[1] - v.pos[1], dz = v.look[2] - v.pos[2];
  cam.yaw = Math.atan2(dx, dz);
  cam.pitch = Math.asin(dy / Math.hypot(dx, dy, dz));
}}

const viewsEl = document.getElementById('views');
VIEWS.forEach((v, i) => {{
  const b = document.createElement('button');
  b.className = 'chip';
  b.innerHTML = '<span class="num">' + (i + 1) + '</span>' + v.label;
  b.onclick = () => setView(v);
  viewsEl.appendChild(b);
}});

// ---- input ----------------------------------------------------------------
const keys = Object.create(null);
const crouchBtn = document.getElementById('crouch');
function setCrouch(on) {{
  cam.crouch = on;
  crouchBtn.setAttribute('aria-pressed', on ? 'true' : 'false');
}}
crouchBtn.onclick = () => setCrouch(!cam.crouch);

addEventListener('keydown', e => {{
  keys[e.key.toLowerCase()] = true;
  if (e.key === 'Control' || e.key.toLowerCase() === 'c') setCrouch(true);
}});
addEventListener('keyup', e => {{
  keys[e.key.toLowerCase()] = false;
  if (e.key === 'Control' || e.key.toLowerCase() === 'c') setCrouch(false);
}});

function look(dx, dy) {{
  cam.yaw += dx * 0.0032;
  cam.pitch = Math.max(-1.45, Math.min(1.45, cam.pitch - dy * 0.0032));
}}

const desktop = () => matchMedia('(min-width: 780px)').matches;
canvas.addEventListener('click', () => {{ if (desktop()) canvas.requestPointerLock?.(); }});

// Pointer lock when granted; plain drag when it is not.
let dragging = false, dragX = 0, dragY = 0;
canvas.addEventListener('mousedown', e => {{ dragging = true; dragX = e.clientX; dragY = e.clientY; }});
addEventListener('mouseup', () => {{ dragging = false; }});
addEventListener('mousemove', e => {{
  if (document.pointerLockElement === canvas) {{ look(e.movementX, e.movementY); return; }}
  if (dragging) {{ look(e.clientX - dragX, e.clientY - dragY); dragX = e.clientX; dragY = e.clientY; }}
}});

let lookId = null, lastX = 0, lastY = 0;
canvas.addEventListener('touchstart', e => {{
  const t = e.changedTouches[0];
  lookId = t.identifier; lastX = t.clientX; lastY = t.clientY;
}}, {{ passive: true }});
canvas.addEventListener('touchmove', e => {{
  for (const t of e.changedTouches) {{
    if (t.identifier !== lookId) continue;
    look((t.clientX - lastX) * 1.7, (t.clientY - lastY) * 1.7);
    lastX = t.clientX; lastY = t.clientY;
  }}
}}, {{ passive: true }});
canvas.addEventListener('touchend', e => {{
  for (const t of e.changedTouches) if (t.identifier === lookId) lookId = null;
}}, {{ passive: true }});

const stick = document.getElementById('stick'), knob = document.getElementById('knob');
let stickId = null, moveX = 0, moveY = 0;
function stickAt(t) {{
  const r = stick.getBoundingClientRect();
  let dx = (t.clientX - (r.left + r.width / 2)) / (r.width / 2);
  let dy = (t.clientY - (r.top + r.height / 2)) / (r.height / 2);
  const m = Math.hypot(dx, dy);
  if (m > 1) {{ dx /= m; dy /= m; }}
  moveX = dx; moveY = -dy;
  knob.style.transform = 'translate(' + (dx * 33) + 'px,' + (dy * 33) + 'px)';
}}
stick.addEventListener('touchstart', e => {{
  const t = e.changedTouches[0]; stickId = t.identifier; stickAt(t); e.preventDefault();
}});
stick.addEventListener('touchmove', e => {{
  for (const t of e.changedTouches) if (t.identifier === stickId) stickAt(t);
  e.preventDefault();
}});
stick.addEventListener('touchend', e => {{
  for (const t of e.changedTouches) if (t.identifier === stickId) {{
    stickId = null; moveX = moveY = 0; knob.style.transform = '';
  }}
}});

// ---- matrices -------------------------------------------------------------
function perspective(fovy, aspect, near, far) {{
  const f = 1 / Math.tan(fovy / 2), nf = 1 / (near - far);
  return [f / aspect,0,0,0, 0,f,0,0, 0,0,(far+near)*nf,-1, 0,0,2*far*near*nf,0];
}}
function multiply(a, b) {{
  const o = new Array(16);
  for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) {{
    o[i*4+j] = a[j]*b[i*4] + a[4+j]*b[i*4+1] + a[8+j]*b[i*4+2] + a[12+j]*b[i*4+3];
  }}
  return o;
}}
function viewMatrix() {{
  const cy = Math.cos(cam.yaw), sy = Math.sin(cam.yaw);
  const cp = Math.cos(cam.pitch), sp = Math.sin(cam.pitch);
  // Left-handed basis matching Unity: forward +Z, right +X at yaw 0.
  const f = [sy * cp, sp, cy * cp];
  const r = [cy, 0, -sy];
  const u = [-sy * sp, cp, -cy * sp];
  const e = [cam.x, cam.y, cam.z];
  const d = v => -(v[0]*e[0] + v[1]*e[1] + v[2]*e[2]);
  return [r[0], u[0], -f[0], 0,
          r[1], u[1], -f[1], 0,
          r[2], u[2], -f[2], 0,
          d(r),  d(u),  -d(f), 1];
}}

// ---- loop -----------------------------------------------------------------
const readout = document.getElementById('readout');
let last = 0;

function frame(now) {{
  const dt = Math.min((now - last) / 1000 || 0, 0.05);
  last = now;

  let fwd = moveY, str = moveX;
  if (keys['w'] || keys['arrowup']) fwd += 1;
  if (keys['s'] || keys['arrowdown']) fwd -= 1;
  if (keys['d'] || keys['arrowright']) str += 1;
  if (keys['a'] || keys['arrowleft']) str -= 1;
  const m = Math.hypot(fwd, str);
  if (m > 1) {{ fwd /= m; str /= m; }}

  const speed = cam.crouch ? 0.85 : 1.5;
  const cy = Math.cos(cam.yaw), sy = Math.sin(cam.yaw);
  cam.x += (sy * fwd + cy * str) * speed * dt;
  cam.z += (cy * fwd - sy * str) * speed * dt;

  // The real build is enclosed by wall colliders; a clamp does the same job here.
  cam.x = Math.max(BOUNDS.minX, Math.min(BOUNDS.maxX, cam.x));
  cam.z = Math.max(BOUNDS.minZ, Math.min(BOUNDS.maxZ, cam.z));

  const targetEye = cam.crouch ? EYE_CROUCH : EYE_STAND;
  cam.y += (targetEye - cam.y) * Math.min(1, dt * 9);

  const dpr = Math.min(devicePixelRatio || 1, 2);
  const w = Math.max(1, Math.floor(canvas.clientWidth * dpr));
  const h = Math.max(1, Math.floor(canvas.clientHeight * dpr));
  if (canvas.width !== w || canvas.height !== h) {{ canvas.width = w; canvas.height = h; }}
  gl.viewport(0, 0, w, h);
  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

  const mvp = multiply(perspective(1.2, w / h, 0.04, 200), viewMatrix());
  // Undo the millimetre quantisation in the linear part of the same matrix.
  const s = 1 / POS_SCALE;
  for (let i = 0; i < 12; i++) mvp[i] *= s;

  gl.uniformMatrix4fv(uMVP, false, new Float32Array(mvp));
  gl.drawArrays(gl.TRIANGLES, 0, TRI_COUNT * 3);

  readout.textContent =
    'x ' + cam.x.toFixed(2) + '   z ' + cam.z.toFixed(2) + '   eye ' + cam.y.toFixed(2) + ' m';

  requestAnimationFrame(frame);
}}

setView(VIEWS[0]);
requestAnimationFrame(frame);
</script>
"""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tessellate", type=float, default=1.0)
    ap.add_argument("--out", default="docs/walkthrough.html")
    args = ap.parse_args()

    print("building geometry ...")
    tri_count, pos_bytes, col_bytes = build_payload(args.tessellate)
    print(f"  {tri_count:,} triangles")
    print(f"  positions {len(pos_bytes)/1024:.0f} KB, colours {len(col_bytes)/1024:.0f} KB")

    labels = [
        "Entrance", "To the door", "Rear corner", "Workbench",
        "Utility wall", "Roof", "Floor", "Window",
    ]
    views = [
        {"label": labels[i], "pos": list(v[1]), "look": list(v[2])}
        for i, v in enumerate(pr.VIEWS)
    ]

    bounds = {
        "minX": -pr.HW + 0.35, "maxX": pr.HW - 0.35,
        "minZ": -pr.HL + 0.35, "maxZ": pr.HL - 0.35,
    }

    import json
    html = HTML.format(
        tri_count=tri_count,
        pos_scale=POS_SCALE,
        pos_b64=base64.b64encode(pos_bytes).decode(),
        col_b64=base64.b64encode(col_bytes).decode(),
        views_json=json.dumps(views),
        bounds_json=json.dumps(bounds),
    )

    out = ROOT / args.out
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(html)
    print(f"wrote {args.out}  ({len(html)/1024:.0f} KB)")


if __name__ == "__main__":
    main()
