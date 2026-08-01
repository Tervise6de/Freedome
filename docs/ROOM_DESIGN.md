# Room design

The shed is a 6 m x 4 m timber outbuilding, framed and dressed as one that gets
used. This document records the dimensions, the layout reasoning, the prop
groups, the circulation route and the performance shape of the room.

Every number here is parsed from or mirrors
`Assets/Game/Scripts/Environment/ShedDimensions.cs`. The drawings in
`docs/diagrams/` are generated from that same file by
`Tools/generate_diagrams.py`, so if a number below disagrees with the room, the
drawing will show it.

---

## 1. Dimensions

### Interior envelope

| Measurement | Value |
| --- | --- |
| Interior width (X) | 4.000 m |
| Interior length (Z) | 6.000 m |
| Floor area | 24.0 m2 |
| Wall height to top plate | 2.400 m |
| Ridge height | 3.258 m |
| Headroom at the wall line | 2.451 m |
| Roof pitch | 22 degrees |

### Construction

| Element | Specification |
| --- | --- |
| Wall frame | 90 x 45 studs at 600 mm centres, single bottom plate, double top plate, staggered noggins at mid height |
| Wall build-up | 90 mm frame + 19 mm sheathing + 16 mm painted weatherboard = 125 mm total |
| Floor | 19 mm boards on 90 x 45 joists at 450 mm centres, on 140 mm bearers, on nine concrete piers |
| Floor build-up depth | 249 mm below the walking surface |
| Roof | 90 x 45 rafters at 600 mm centres, 150 x 25 ridge board, 70 x 45 purlins, corrugated steel |
| Collar ties | 90 x 35 at every third rafter pair, underside at 2.850 m |
| Eaves | 300 mm overhang, fascia, tapered blocking in each rafter bay |
| Gable | 250 mm overhang, barge boards |

### Openings

| Opening | Size | Position |
| --- | --- | --- |
| Door leaf | 820 x 2040 x 45 mm, ledged and braced | Entrance wall, centred at x = -0.85 |
| Door rough opening | 892 x 2082 mm | 6 mm gap under the leaf |
| Window | 900 x 600 mm, sill at 1.200 m | Workbench wall, centred at z = +0.60 |
| Vent | 300 x 200 mm louvre | Utility wall, centred at x = -1.10, y = 2.05 |

### Coordinate convention

Origin at the centre of the floor, walking surface at y = 0.

```
              +Z  utility wall (gable, breaker / vent / radio)
               |
storage wall   |   workbench wall
   -X  --------+--------  +X   (window over the bench)
               |
              -Z  entrance wall (gable, door)
```

---

## 2. Floor plan

See `docs/diagrams/floor_plan.svg`, `cross_section.svg` and `long_section.svg`.

### Entrance wall (z = -3.0)

The door sits off-centre at x = -0.85, which leaves a usable stretch of wall to
its right rather than splitting the wall into two useless halves. Going
clockwise from the door:

- Ledged-and-braced door with a rim lock, lever handle, keyhole escutcheon and a
  barrel bolt above it. Three tee hinges on the outside face.
- Light switch at x = -0.30, 1.150 m up - beside the door on the latch side,
  where you reach for it walking in.
- Coat rack at x = 0.95, 1.650 m up: a 800 mm board with four hooks, carrying a
  coiled extension lead and a canvas tool bag.
- Boot area at x = 1.15 against the wall: a rubber tray with a pair of
  wellingtons standing in it.

### Workbench wall (x = +2.0)

- Workbench, 2.400 m long x 600 mm deep, top at 900 mm. Laminated ply top with a
  hardwood front lipping, four 70 mm legs, aprons, a lower shelf at 200 mm.
- Drawer bank at the far end, small cupboard at the near end with one door left
  slightly open.
- Engineer's vice bolted through the top at the near end.
- Window directly over the bench, sill at 1.200 m so it clears the bench top and
  whatever is standing on it.
- Pegboard from z = -0.60 to +0.10, 1.000 to 1.900 m up, on battens: hammer,
  hacksaw, three screwdrivers, pliers, a try square.
- Small tool rack shelf on the far side of the window with three cup hooks.
- Clamp task light on the pegboard batten, aimed down across the bench.

### Storage wall (x = -2.0)

- Shelving unit, 2.400 m long x 450 mm deep x 1.950 m high, four shelves at 250,
  750, 1250 and 1750 mm. Site-built: three pairs of uprights, two boards per
  shelf on cleats, a diagonal brace across the back.
- Shelf contents from the bottom up: two buckets, a toolbox and a stack of plant
  pots; four paint tins (one with its lid leaning against it), an open cardboard
  box, a watering can; a folded tarpaulin, three jars of fixings, a box and a
  small stack of offcuts; a sack, two more tins and a coil of rope.
- Below and beside it: a bag of compost, a galvanised bucket with a hand brush,
  and a stack of timber offcuts leaning in the corner behind the door.

### Utility wall (z = +3.0)

- Consumer unit at x = 0.85, centred 1.600 m up: enclosure, hinged lid with a
  window, six breakers and a main switch, a label strip, glands top and bottom.
- Double socket directly below it at 350 mm.
- Surface conduit: down from the unit to the socket, and up from the unit to
  2.300 m, then around two walls at high level to a drop feeding the light
  switch by the door. Saddled every 600 mm, with inspection boxes at every
  corner and a junction box where the lighting spur leaves.
- Twin-and-earth from that junction box up into the roof and along to the
  ceiling rose, clipped to the timber with a little sag between clips.
- Ventilation louvre at x = -1.10, 2.050 m up - a real hole through the wall.
- Utility shelf, 1.200 m x 250 mm at 1.100 m on pressed-steel brackets, carrying
  a mains radio and a jar of assorted fixings.

### Central and floor area

| Object | Position | Notes |
| --- | --- | --- |
| Petrol lawnmower | (1.10, -1.55) | Deck, four wheels, engine, tank, chute, handle to 1.01 m |
| Wheelbarrow | (-0.90, 2.55) | Parked parallel to the utility wall |
| Sawhorse | (-0.60, 1.35) | With saw cuts across the top rail |
| Stool | (1.12, 2.10) | Just past the end of the bench |
| Compost bag | (-1.48, -2.15) | Slumped against the storage wall |
| Timber offcuts | (-1.68, -2.76) | Leaning in the corner |
| Loose boards | (-1.26, 0.98) | Two boards left flat on the floor |
| Service hatch | (-0.62, -1.40) | 700 x 900 mm, five floorboards wide |

---

## 3. Architectural assumptions

These are the decisions the geometry takes as given. They are worth stating
because they are what makes the room read as a specific building rather than a
generic box.

1. **Platform-framed timber on piers, not a slab.** The floor is a sprung timber
   deck 249 mm off the ground on nine concrete piers. This is why there is a
   step outside the door, why the floorboards have visible arrises, and why a
   service hatch through the floor is a plausible thing to find.
2. **Unlined interior.** The studs, noggins, rafters and the back of the
   sheathing are all visible. A shed of this size is rarely lined, and leaving it
   open means the player looks at real structure everywhere instead of at flat
   panels.
3. **Purlins and bare sheeting, no ceiling.** The roof is corrugated steel on
   purlins over the rafters. That is both what a shed of this budget has and the
   reason the room has an honest daylight leak (see LIGHTING.md).
4. **Door opens outward.** Standard for an outbuilding, saves interior space,
   and it puts the tee hinges on the outside where they belong.
5. **Surface-mounted electrics.** Nothing is chased into anything, because there
   is nothing to chase into. Conduit is clipped to the face of the framing.
6. **The building is maintained but used.** Paint is intact with some chalking,
   timber is dry, metal is galvanised rather than rusted. Wear is from use, not
   neglect.

---

## 4. Main prop groups

| Group | Contents | Reads as |
| --- | --- | --- |
| Bench group | Bench, vice, pegboard, tools, task light, parts organiser, jars, hand plane, offcut, pencil, mug, abrasive roll, brush | Somebody works here |
| Storage group | Shelving, buckets, tins, tarpaulin, pots, watering can, boxes, rope, sack, toolbox | Things get kept here |
| Utility group | Consumer unit, conduit, socket, switch, vent, utility shelf, radio | The building has services |
| Entrance group | Door and ironmongery, coat rack, lead, bag, boot tray, boots | People come in and out here |
| Floor group | Mower, wheelbarrow, sawhorse, stool, compost, offcuts, loose boards | Bigger things live here |

Objects are set down at slightly different angles and are not aligned to each
other or to the walls. One cupboard door is ajar; one drawer of the parts
organiser is pulled out; the paint tins have different heights and one has lost
its lid. A shed where everything squares up looks like a shop display.

---

## 5. Player circulation

The player spawns just inside the door at (-0.85, 0, -2.55) facing down the
length of the shed, standing, eye height 1.700 m.

The intended route, drawn in green on the floor plan:

```
spawn (-0.85, -2.55)
  -> (-0.45, -1.60)   past the service hatch
  -> ( 0.10, -0.40)   centre of the room, bench on the right
  -> ( 0.25,  1.10)   between shelving and bench
  -> ( 0.35,  2.35)   utility wall, breaker at arm's length
```

The clear aisle between the bench face (x = +1.40) and the shelving face
(x = -1.55) is **2.95 m**, which an EditMode test asserts stays above 2.0 m. No
floor object narrows the walking route below about 1.2 m at any point. Walking
the full 6 m length at 1.5 m/s takes about four seconds.

The player cannot leave: all four walls, the roof and the closed door leaf have
colliders, the door has an additional dedicated blocker, and `PlayAreaBoundary`
returns the player to the spawn point if they ever end up outside the interior
volume. That backstop firing at all is treated as a bug - a PlayMode test
asserts its recovery count stays at zero.

---

## 6. Lighting strategy

Summarised here; the detail is in [LIGHTING.md](LIGHTING.md).

A mid-morning sun at 38 klux comes in across the workbench wall at an angle, so
light rakes down the bench rather than stopping at the sill. Three secondary
daylight sources keep the room from having one bright end and one black end: the
vent on the utility wall, the 6 mm gap under the door, and a row of thin
crescents along the top of both long walls where the corrugation profile does not
sit flat on the eaves blocking.

Two practicals: an 820 lm warm ceiling lamp hung from the collar tie at the
centre of the room, and a 430 lm clamp light over the bench. Exposure is
automatic but clamped to a narrow range so the room does not pump as the player
turns from the window to the dim corner.

No fog, no vignette, no film grain, no coloured gels, no flicker.

---

## 7. Future gameplay locations

Six affordances are reserved. All of them are ordinary parts of the shed, none is
interactive in this milestone, and none is visually marked. Full reasoning in
[FUTURE_GAMEPLAY_HOOKS.md](FUTURE_GAMEPLAY_HOOKS.md).

1. Door and its mechanical locking area - entrance wall
2. Window and its frame - workbench wall, over the bench
3. Ventilation opening - utility wall, 2.05 m up
4. Consumer unit and the conduit circuit - utility wall, running round two walls
5. Removable floor service panel - centre-left of the walking route
6. Radio - utility shelf

---

## 8. Performance considerations

The room is built to hit 60 fps at 1920 x 1080 on a mid-to-high-end PC. The
choices that matter:

- **Assemblies, not objects.** Each logical assembly - a wall, the roof
  structure, the workbench, everything on the shelves - is one mesh with one
  submesh per material, built through a single `MeshBuilder`. The shelf contents
  are roughly twenty separate containers rendered as one renderer.
- **A small material palette.** 23 materials for the whole room. Draw calls
  scale with distinct materials per renderer, not with the number of things.
- **Chamfers where they are seen.** Architecture and hero props get 2-4 mm
  chamfers on every edge; small background items pass `bevel = 0` and cost 12
  triangles per box instead of 44.
- **Everything static.** All geometry is marked contribute-GI, batching,
  occluder, occludee and reflection-probe static.
- **Mixed lighting.** Two practicals and the sun are Mixed with shadowmask, so
  indirect light is baked and only direct shadows cost anything at runtime.
- **Two reflection probes**, one for the room and a tighter higher-importance one
  over the bench where the metal is.
- **Modest shadow resolutions**: 2048 for the sun, 512 for each practical.
- **No transparency except the window glass**, and no volumetrics.

The generator logs its own renderer and triangle totals when it runs. The
in-game overlay (F3) reports frame rate, CPU and GPU frame time, triangles,
draw calls and batches so the numbers in the performance review come from the
shipped build rather than from the editor.

**None of these figures have been measured.** The scene has never been
generated or run - see [KNOWN_ISSUES.md](KNOWN_ISSUES.md).


## Window, revised

The window is two lights, 900 mm overall. The left one is fixed, glazed into the
frame behind beads. The right one is an opening casement - stiles and rails
around its own pane, a lever catch, hung on the outer stile and swinging 72
degrees outward.

It was originally a single fixed light with a glazing bar across it. The
casement was added when the interaction milestone reached the window and found
there was nothing there to open.
