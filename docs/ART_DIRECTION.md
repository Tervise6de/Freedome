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

Two constraints shape all of them:

1. **Each map covers exactly one square metre.** Paired with UVs measured in
   metres, that means a board, a wall and a bench top all get identical grain
   density with no per-object tiling values to get wrong.
2. **Each map tiles seamlessly.** The noise library (`TilingNoise`) is periodic
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
