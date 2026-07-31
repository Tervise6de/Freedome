# Future gameplay hooks

The shed reserves six locations that a later milestone could turn into escape
routes. This document records where they are and how each one was made an
ordinary part of the building.

**Nothing in this document is implemented.** None of these objects has a
collider trigger, an interaction script, a highlight, an outline, a symbol, a
marking or any other cue. Read from inside the room they are a door, a window, a
vent, a fuse box, a floor hatch and a radio - which is what they are.

## The rule this document exists to enforce

An affordance that has been *added* to a room reads differently from one the room
would have had anyway. Players notice the odd thing out long before they notice
the useful thing. So the test applied to each of these six was not "can this be
used later?" but:

> If somebody built this shed with no intention of anyone ever escaping from it,
> would this still be here, in this place, looking like this?

All six pass. Four of them (door, window, vent, electrics) are required by the
building. One (the floor hatch) follows from the shed standing on piers. One (the
radio) is a thing people put in sheds.

The second rule: **no clustering**. If all six sat on one wall the room would
read as a puzzle board. They are spread across all four walls, the floor and a
shelf.

---

## 1. Main door and mechanical locking area

**Where** Entrance wall (z = -3.0), leaf centred at x = -0.85. Rim lock case at
1.020 m on the latch stile, with a lever, spindle and keyhole escutcheon; barrel
bolt at 1.720 m; three tee hinges on the outside face; a striking plate visible
in the leaf edge.

**How it is integrated** This is just the door, hung and locked the way an
outbuilding door is. The choice worth noting is that the leaf is
ledged-and-braced rather than a flush panel: six vertical boards, three ledges,
two diagonal braces, coach bolts through into the ledges. That is the cheap
traditional way to build a shed door, and it means the mechanism side of the
lock, the bolt and the bolt heads are all naturally visible from inside without
anything being exposed for the player's benefit. A flush door would have needed
something contrived to show its workings.

The 6 mm gap under the leaf is standard clearance. It also puts a thin line of
daylight on the threshold, which is a lighting detail first and an affordance
second.

**What a later milestone might do** Any of: picking or forcing the lock,
unscrewing the hinges from the leaf side, removing the rim lock case, working the
bolt, or attacking the boards. The geometry supports all of them as separate
parts already.

**What is deliberately absent** Scratches around the keyhole, a conspicuously
new lock on an old door, wear polish on the bolt, or a lock that is visibly
different in quality from the rest of the building.

---

## 2. Window and surrounding frame

**Where** Workbench wall (x = +2.0), 900 x 600 mm, sill at 1.200 m, centred at
z = +0.60 - directly over the bench.

**How it is integrated** The window is where a window goes: over the work
surface, so you can see what you are doing. The sill at 1.200 m is set by the
900 mm bench top plus clearance for things standing on it, not by anything else.
It is a fixed light split by a central glazing bar into two panes, with a timber
frame, glazing beads on the room side, a projecting interior sill board, exterior
architrave and a galvanised head flashing.

The interior sill board projects 40 mm into the room and is 165 mm deep. This is
authentic - shed window sills always become shelves - and it is also, incidentally,
a ledge.

**What a later milestone might do** Breaking a pane, prising the beads and
removing the glazing intact, unscrewing the frame from the reveal, or working on
the trimmer studs around the opening. The frame, beads, glass and lining are
already separate pieces of geometry.

**What is deliberately absent** Bars, boards, mesh, a suspiciously new fixing,
or a window that is the only clean thing in the room.

---

## 3. Ventilation opening

**Where** Utility wall (z = +3.0), 300 x 200 mm louvre centred at x = -1.10,
2.050 m up.

**How it is integrated** A shed with a timber floor needs cross-ventilation or
it rots, and the vent is at high level on a gable end because that is where you
put one. It is a genuine hole through the 125 mm wall - timber lining to the
reveal, insect mesh set 30 mm in, a galvanised louvre frame with five angled
blades on the outside face. Daylight actually comes through it, which is the
main reason it is there from a lighting point of view.

Its position was chosen to be *away* from the consumer unit on the same wall, so
the utility wall does not read as a row of interesting objects.

**What a later milestone might do** Removing the mesh, unscrewing the louvre,
enlarging the opening, or passing something through it. The louvre, mesh and
lining are separate already.

**What is deliberately absent** A vent large enough for a person, a loose or
damaged grille, or fresh tool marks.

---

## 4. Electrical breaker and wiring system

**Where** Consumer unit on the utility wall at x = 0.85, centred 1.600 m up.
Double socket directly below at 350 mm. Surface conduit from the unit down to
the socket, and up to 2.300 m then around the storage wall and along the
entrance wall to a drop feeding the light switch beside the door. A junction box
on the high-level run at x = 0, and twin-and-earth from it up into the roof and
along to the ceiling rose.

**How it is integrated** This is a whole circuit, not a prop. It was routed by
asking where the supply enters, what it has to feed, and how you would clip it to
an unlined stud wall - which is why it goes the long way round two walls to reach
the switch, why every corner has an inspection box, why saddles appear every
600 mm, and why the lighting drop sags slightly between clips.

Building it as a system rather than as three separate objects is what stops the
consumer unit from looking placed. It is the end of a run that goes somewhere.

**What a later milestone might do** Killing the lighting circuit, pulling cable
for its copper, shorting something, using the conduit as a route to somewhere, or
tracing the circuit to work out what else is on it. The unit, its lid, the
breakers, the conduit, the boxes and the cable are all separate geometry.

**What is deliberately absent** Exposed live conductors, scorch marks, taped
repairs, a hand-written warning, or a breaker labelled anything other than what a
real one would say.

---

## 5. Removable floor service panel

**Where** Floor, centred at (-0.62, -1.40) - on the walking route between the
door and the middle of the room. 700 x 900 mm.

**How it is integrated** The shed stands 249 mm off the ground on piers, so
there is a real void under the floor, and a hatch to reach it is the ordinary
solution for getting at a stop-cock, a cable run or a rodent problem.

Two details do the work of making it disappear:

- It is **exactly five floorboards wide** and aligned to the board joints. A
  hatch cut by a builder follows the boards; a hatch added by a level designer
  usually does not, and the mismatched joints are what gives it away.
- It is framed properly. Trimmer joists run round the opening under the floor and
  are visible in the 3 mm reveal, and the panel itself is five board offcuts on
  two ledger battens with four countersunk screws and a shallow finger slot.

It sits on the circulation route deliberately. Something the player walks over
every time they cross the room stops being notable.

**What a later milestone might do** Unscrewing the panel, lifting it, and going
under the floor. The panel is already a separate object with its own collider
and pivot.

**What is deliberately absent** A recessed ring pull, a hinge, a different
timber, a different finish, dust disturbed around the edges, or a reveal wide
enough to notice from standing height.

---

## 6. Radio or communications device

**Where** Utility shelf on the utility wall, at x = -1.00, 1.100 m up.

**How it is integrated** A mains radio on a shelf at the far end of the shed,
positioned so you could hear it from the bench - which is why it is on that wall
and at that height. It is modelled as an ordinary portable: moulded case, speaker
grille, tuning scale, two dials, a carry handle, a part-extended telescopic aerial
leaning as they always do, and a flex disappearing behind the shelf toward the
socket.

The shelf carries a jar of fixings as well, so the radio is one of the things on
a shelf rather than the thing on a shelf.

**What a later milestone might do** Powering it, tuning it, stripping it for
parts, using the aerial or the speaker magnet, or hearing something through it.
The case, grille, dials, handle and aerial are separate already.

**What is deliberately absent** A CB or emergency radio, a microphone, an
antenna lead going anywhere unusual, or a radio that is conspicuously more modern
or more valuable than everything around it.

---

## Distribution check

| # | Affordance | Surface | Height |
| --- | --- | --- | --- |
| 1 | Door and lock | Entrance wall, z = -3.0 | 0 - 2.04 m |
| 2 | Window and frame | Workbench wall, x = +2.0 | 1.20 - 1.80 m |
| 3 | Vent | Utility wall, z = +3.0 | 2.05 m |
| 4 | Breaker and circuit | Utility wall + two more walls | 0.35 - 2.30 m |
| 5 | Floor panel | Floor, centre-left | 0 m |
| 6 | Radio | Utility shelf | 1.10 m |

Four walls, one floor, one shelf; heights from the floor to just under the plate.
No two of them share a sightline in a way that groups them.

## Rules for whoever implements these

1. Do not add a highlight, outline, glint, sound cue or particle to any of them.
   If a later design needs the player to find these, the answer is level design
   and lighting, not decoration.
2. Do not move them to be more convenient. Their positions are load-bearing for
   the room reading as a shed.
3. If an affordance needs a part that does not exist yet, add it as a thing the
   building would have - not as a thing the puzzle needs.
4. Keep the geometry split as it is. Every one of these is already broken into
   the parts an interaction would need.
