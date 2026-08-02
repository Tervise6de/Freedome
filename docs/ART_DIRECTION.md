# Art direction

The target is a shed on an ordinary weekday morning. Realistic, grounded,
understated, and specifically **not** frightening.

## The single test

> Would somebody store a lawnmower in here without thinking about it?

Everything below serves that. If a change makes the room more atmospheric but
less like a place you would keep a mower, it is the wrong change.

---

## What the room is

A timber shed belonging to somebody who does their own repairs and looks after
their garden. It is 10 or 15 years old. It has been painted once and could do
with another coat in a few years. It is swept but not clean. Things are put away
roughly where they belong.

It is not abandoned, not derelict, not a crime scene, and nothing has happened
in it.

---

## Materials

23 HDRP/Lit materials for the whole room. The palette is small on purpose - a
real shed is made of about six substances, and every extra material is a draw
call.

### Roughness targets

Smoothness values are picked from what the real surface does, not from what looks
good in a screenshot.

| Surface | Smoothness | Reasoning |
| --- | --- | --- |
| Sawn structural pine | 0.16 | Rough-sawn timber is one of the least reflective things in any room |
| Floorboards | 0.28, up to ~0.44 in patches | Traffic polishes the walking route; the map varies it broadly |
| Painted weatherboard | 0.35, down to 0.22 where worn | Exterior paint chalks with age |
| Bench ply | 0.34, 0.20-0.44 varying | Years of things set down on it |
| Shelf boards | 0.20 | Same timber as the framing, greyer and dustier |
| Pegboard | 0.30 | Hardboard with a sealed face |
| Galvanised steel | 0.52, metallic 1.0 | Spangled zinc, not chrome and not rust |
| Dark steel (tools, vice) | 0.38, metallic 1.0 | Oiled and handled |
| Zinc-plated hardware | 0.58, metallic 1.0 | Hinges, screws, latches |
| Painted metal | 0.44-0.55 | Mower, toolbox, tins |
| Electrical plastic | 0.42 | Moulded ABS |
| Bucket plastic | 0.46 | Injection-moulded polypropylene |
| Rubber | 0.22 | Tyres, boots, tray |
| Tarpaulin | 0.34 | Woven polyethylene |
| Cardboard | 0.14 | The flattest thing in the room |
| Concrete | 0.20 | Piers and the door step |
| Glass | 0.96 | Slightly dirty, not optically perfect |

### Texture generation

Every map is generated procedurally by `ShedTextureGenerator` into
`Assets/Game/Art/Textures/Generated/`. Ten families, three maps each:

- `_Albedo` sRGB base colour
- `_Normal` derived from the recipe's height field
- `_Mask` R metallic, G ambient occlusion, B detail, A smoothness

### The wood model

Timber is most of what the player looks at, so it gets a real model rather than
layered noise.

A tiling map has to be exactly periodic, and the figure a plainsawn board shows
comes from growth rings concentric about a pith outside the board. Concentric
circles are not periodic, so they cannot be used. Instead a periodic stripe
pattern is warped by periodic noise: the stripes wander into arches and flames
locally while the map still repeats exactly.

Three things make the difference between grain and noise:

- **Both warps stay well under one ring of displacement.** Push them past that
  and the rings stop being continuous lines and break into dashes.
- **Ring spacing varies slowly**, so bands of tight rings sit next to bands of
  wide ones. Evenly spaced rings read as a comb, not as timber.
- **The fibre is stretched about sixty to one.** The aspect ratio is what makes
  it read as fibre at all.

A sharpness control blends between the hard latewood band of sawn softwood and
the broad soft banding of rotary-cut veneer. Plywood needs the soft end: warping
the hard band closes it into a crazed network of loops.

Knots are sparse - two per square metre is generous for clean stock - and dark
with a slightly redder core.

Galvanised steel takes its spangle from cell *boundaries* (Worley F2 - F1)
rather than cell centres. Zinc crystallises into flat angular facets; the
nearest-point distance only ever gives round blobs, which read as dents.

### Breaking the repeat

Two mechanisms, because a one-metre tile on a six-metre floor is otherwise
obvious:

- **Per-piece UV offset.** Every floorboard samples a different square of the
  map, derived from its index so it stays reproducible. Costs nothing and turns
  twenty-nine copies of one board into twenty-nine boards.
- **Resolution where it counts.** Pine, floorboards and the bench ply generate
  at 2048 across one metre - half a millimetre per texel - because the player
  stands over them. The ground outside the window stays at 1024.

Two constraints shape all of them:

1. **Each map covers exactly one square metre.** Paired with UVs measured in
   metres, that means a board, a wall and a bench top all get identical grain
   density with no per-object tiling values to get wrong.
2. **Grain follows the piece.** The timber maps run their fibre along V, and
   `MeshBuilder` builds each face's UV frame from a grain direction rather than
   from the face's dominant world axis: V follows the piece's own longest axis,
   carried through whatever rotation it has, so a rafter tilted to the roof pitch
   is still grained along the rafter. Sawn ends, where the grain points out of
   the face, fall back to the axis-aligned mapping and read as end grain.
   `GrainOverride` forces the axis where the material has a direction the
   geometry does not imply - weatherboards lap horizontally whichever way round
   the wall is, and pegboard holes have to stay on their grid.
3. **Each map tiles seamlessly.** The noise library (`TilingNoise`) is periodic
   by construction rather than relying on `Mathf.PerlinNoise`, which does not
   tile. Where a pattern has to line up with the grid - pegboard holes at a 25 mm
   pitch (40/m), weatherboards at 143 mm (7/m) - the pitch was chosen to divide
   into a metre exactly.

### Wear and imperfection

Present, in small amounts:

- Timber grain with occasional knots and fine saw marks across the grain
- Broad traffic polish on the floorboards, varying smoothness rather than colour
- A light even settling of dust on the floor, and scattered drag scuffs
- Paint chalking and thinned patches on the weatherboards
- Ring marks and scratches on the bench top where tools have been set down
- Spangle and faint scratching on galvanised steel
- Saw cuts across the sawhorse rail from being used properly

Absent:

- Rust beyond a faint dulling
- Grime, stains, streaks, drips
- Mould, damp, rot
- Cracks, holes, breakage of any kind
- Anything organic

The distinction throughout: **wear from use, not damage from neglect.**

---

## Geometry and detailing

The room must not read as an untextured primitive blockout. The two systemic
things that prevent that:

### Bevelled edges

`MeshBuilder.AddBox` takes a chamfer size and generates a proper chamfered box -
six inset faces, twelve edge chamfers, eight corner triangles. Every architectural
edge catches a highlight instead of ending in a mathematically sharp line.

| Element | Chamfer |
| --- | --- |
| Floorboards | 1.5 mm |
| Framing, rafters, plates | 2.5 mm |
| Trim, jambs, door boards | 3 mm |
| Bench top, hardwood lipping | 4 mm |
| Concrete, soft goods | 6-30 mm |
| Small background props | 0 mm |

The chamfer is clamped to a third of the smallest dimension, so a 19 mm board
asked for a 50 mm chamfer stays a board. There is a test for that.

### Construction detail

Things exist because the building would have them, not because the surface
looked bare:

- Bottom plate, double top plate, staggered noggins
- King studs, jack studs, lintels, sill trimmers and cripple studs at every
  opening
- Ridge board, collar ties, purlins, fascia, barge boards, tapered eaves blocking
  in each rafter bay
- Corner boards lapping at all four corners
- Door jambs, stops, threshold, reveal trim
- Window lining, frame, glazing bar, glazing beads, sill board, architrave, head
  flashing
- Coach bolt heads through the door boards into the ledges
- Countersunk screws in the service panel, saddles on every conduit run,
  inspection boxes at every corner

### Polygon discipline

Detail goes where the player stands. Small background objects use plain boxes and
low-segment cylinders. Curved things use 6-18 segments depending on how close
they get, not a uniform number.

### Pivots

Each assembly is authored in its own local space and placed at a meaningful
pivot: the bench at its footprint centre, the door leaf at its hinge line height,
the service panel at the hatch centre. Nothing is authored at the world origin
and offset.

---

## Colour

Muted and desaturated, with one or two saturated objects to stop the room going
monochrome.

| | |
| --- | --- |
| Timber | Warm mid-browns, 0.35-0.51 luminance |
| Exterior paint | Muted sage-grey, deliberately unremarkable |
| Galvanised steel | Neutral grey, slight blue |
| Accents | Mower green, toolbox red, bucket blue, tarpaulin blue |
| Grade | Contrast +4, saturation -2, post-exposure +0.15 |

No colour grading pushes the room toward any temperature extreme. The grade is
close to neutral because the point is to see the materials.

---

## Explicitly banned

Not stylistic preferences - hard constraints on this milestone.

**Imagery**: blood, chains, occult symbols, victim photographs, threatening
writing or markings, restraints, anything suggesting violence or captivity, any
environmental storytelling about kidnapping.

**Condition**: extreme grime, heavy rust, broken furniture, smashed glass,
holes in walls, anything derelict.

**Effects**: supernatural effects of any kind, theatrical fog, volumetric god
rays used for mood, flickering or failing lights, aggressive vignette, heavy
film grain, chromatic aberration, dramatic red or green lighting, high-contrast
darkness used to hide unfinished work.

**Lighting**: single-source horror key lighting, deep unreadable shadow,
underlighting, coloured gels.

The volume stack contains **no vignette override and no film grain override at
all**. They were not added and set to zero - they are absent, so they cannot be
turned on by accident.

---

## Review criteria

The eight review screenshots (`Freedome > Capture Review Screenshots`) are checked
against:

| Check | Looking for |
| --- | --- |
| Scale | Door under a metre wide, bench at wrist height, ceiling not cathedral-like |
| Floating | Every object's contact shadow reaching the surface it sits on |
| Intersection | Nothing passing through anything it should not |
| Missing materials | No magenta, no default grey |
| Texture stretch | Grain density identical on every surface |
| Repetition | No visible one-metre grid in any material |
| Lighting | Every corner readable; window not blown out |
| Placement | Nothing arranged too neatly, nothing arranged nonsensically |
| Blockout | No untrimmed primitive shapes visible anywhere |
| Atmosphere | Does it look like a shed or does it look like a horror set |

The last one is the one that matters. If a reviewer's first reaction is
"something bad happened here", the milestone has failed regardless of the other
nine.

**The review has not been performed** - the scene has never been rendered. See
[KNOWN_ISSUES.md](KNOWN_ISSUES.md).


## Detail that exists so the game reads

Additions made for legibility rather than for looks, though they are all
improvements either way:

- **Fixing screws on the rim lock case.** Four countersunk heads near the
  corners. The case has always been a separate box on the inside face of the
  door; what was missing was any sign of how it is held there. It is now the
  thing the whole way out turns on, and a player who cannot see fixings has no
  reason to think it comes off.
- **A deeper chamfer between the door's boards** - 4.5 mm rather than 2.5, with
  a 3 mm gap rather than 1.5. A ledged-and-braced door is six separate boards
  and the shadow line between them is most of what says so. Too fine a chamfer
  and the leaf reads as one flat slab.

- **The rim lock case comes off.** It is its own object under the door leaf
  rather than part of the leaf mesh, so that unscrewing it can actually take it
  off the door. Watching a lock you had just unscrewed stay screwed on was the
  one place in the route where doing the right thing changed nothing you could
  see.
- **A keep on the latch jamb** - the staple the rim lock's bolt shoots into,
  with its two fixing screws. Without it the lock fastened to nothing, which is
  not the sort of thing you notice in a plan and is very much the sort of thing
  you notice standing in front of a door trying to get out.
- **The screwdriver is a screwdriver.** Turned handle waisted at the palm, brass
  ferrule, round shank, a flat tip spread wider than the bar. It was a square
  block of timber with a wire in the end of it, which does not read as a tool at
  any range you can pick it up from.
- **The folding rule is folded.** Two leaves splayed a few degrees about a brass
  hinge, one lying on the other's edge - a folded rule never quite shuts flat.
  It was a single lath, which reads as scrap.
- **Fixings through the roof sheeting.** Screws with sealing washers through
  every third crest, on the line of each purlin, both slopes. Sheet steel is
  fixed through the crest rather than the trough so the fixing sits above
  standing water; it also means they read as rows of small bright dots rather
  than as a seam.
- **The corrugation runs down the slope**, which it did not before. Ribs laid
  parallel to the ridge would have held water in every trough, and they gave the
  eaves blocking a sheet of constant height to meet - so the crescents of
  daylight LIGHTING.md describes did not exist as geometry at all.

All of these are mirrored in `Tools/preview_render.py`, since the previews are
the only place anyone can currently check whether they work. The two things
inside the drawer are the exception: nothing renders the inside of a closed
drawer, so their placement is held by tests instead.
