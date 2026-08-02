using NUnit.Framework;
using UnityEngine;
using Freedome.Environment;
using Freedome.EditorTools.Generation;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// The interaction milestone's geometry, checked arithmetically.
    ///
    /// None of this can be looked at yet, so the placements and hinge offsets are
    /// pinned by numbers instead. A carryable dropped inside a wall or a door hung
    /// off the wrong edge is exactly the kind of fault that is obvious on screen and
    /// invisible in a diff.
    /// </summary>
    public sealed class InteractionTests
    {
        // Interior half-extents, less the thickness a solid object needs to sit
        // clear of the wall lining.
        private const float WallClearance = 0.12f;

        /// <summary>
        /// Light probe interpolation is only defined inside the hull of the probes.
        /// Outside it, an object clamps to whatever the nearest outer tetrahedron
        /// holds and stays there - so the hull has to contain the whole space the
        /// player and the loose objects can occupy, not just the middle of it.
        ///
        /// The lattice used to run to x = +/-1.6 against walls at 2.0 and start at
        /// y = 0.25 over a floor at 0. Three of the five loose objects start outside
        /// it, and so does anything standing within 400 mm of a wall.
        /// </summary>
        [Test]
        public void TheProbeHullReachesTheWallsAndTheFloor()
        {
            Bounds hull = LightingBuilder.ProbeHull();

            Assert.LessOrEqual(hull.min.y, 0.10f,
                $"the lowest probe is at y = {hull.min.y:0.00}, so anything on the floor " +
                "is below the hull");

            Assert.GreaterOrEqual(hull.max.x, ShedDimensions.HalfWidth - 0.10f,
                "the lattice stops short of the long walls");
            Assert.GreaterOrEqual(hull.max.z, ShedDimensions.HalfLength - 0.10f,
                "the lattice stops short of the end walls");

            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                Vector3 at = p.Position;
                Assert.IsTrue(hull.Contains(new Vector3(at.x, Mathf.Max(at.y, hull.min.y), at.z)),
                    $"{p.Name} starts outside the probe hull, so it is lit by whatever " +
                    "tetrahedron it happens to clamp to");
            }
        }

        /// <summary>
        /// What is in a drawer has to be in the drawer: on the bottom board rather
        /// than through it, and inside the sides rather than sticking out of them.
        ///
        /// Both of these failed before this test existed. The screwdriver's handle
        /// was buried 7 mm into the drawer bottom and the folding rule floated 6 mm
        /// above it, and neither is visible until somebody pulls the drawer open -
        /// by which point they have already levered it with a piece of timber to get
        /// there.
        /// </summary>
        [Test]
        public void ThingsInDrawersSitOnTheDrawerBottom()
        {
            float boardTop = -(FixturesBuilder.DrawerBoxHeight * 0.5f) +
                             (FixturesBuilder.DrawerBottomThickness * 0.5f);

            foreach (FixturesBuilder.DrawerItem item in FixturesBuilder.DrawerContents)
            {
                float underside = item.Centre.y - item.HalfSize.y;

                Assert.AreEqual(boardTop, underside, 0.0005f,
                    $"the {item.Name} rests {(underside - boardTop) * 1000f:0.0} mm from the " +
                    "drawer bottom - negative is buried in it, positive is floating over it");
            }
        }

        [Test]
        public void ThingsInDrawersFitInsideThem()
        {
            foreach (FixturesBuilder.DrawerItem item in FixturesBuilder.DrawerContents)
            {
                Assert.Less(Mathf.Abs(item.Centre.z) + item.HalfSize.z,
                    FixturesBuilder.DrawerInnerWidth * 0.5f,
                    $"the {item.Name} passes through the side of the drawer");

                Assert.Greater(item.Centre.x - item.HalfSize.x, 0.012f,
                    $"the {item.Name} pokes out through the drawer front");

                Assert.Less(item.Centre.x + item.HalfSize.x,
                    FixturesBuilder.DrawerBoxMidX + (FixturesBuilder.DrawerBoxDepth * 0.5f),
                    $"the {item.Name} passes through the back of the drawer");

                Assert.Less(item.HalfSize.y * 2f, FixturesBuilder.DrawerBoxHeight,
                    $"the {item.Name} is taller than the drawer it is in");
            }
        }

        [Test]
        public void CarryablesStartInsideTheRoom()
        {
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                Assert.Less(Mathf.Abs(p.Position.x), ShedDimensions.HalfWidth - WallClearance,
                    $"{p.Name} at x={p.Position.x:0.00} is in or through a long wall");
                Assert.Less(Mathf.Abs(p.Position.z), ShedDimensions.HalfLength - WallClearance,
                    $"{p.Name} at z={p.Position.z:0.00} is in or through an end wall");
            }
        }

        [Test]
        public void CarryablesRestOnASurfaceNotInMidAir()
        {
            // Every height something is allowed to start at, and the thing that
            // holds it up. A loose object at any other height is hovering.
            float[] surfaces =
            {
                0f,
                ShedDimensions.BenchHeight,
                ShedDimensions.UtilityShelfTopY,
                FixturesBuilder.CupboardShelfY,
            };

            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                bool supported = false;
                foreach (float y in surfaces)
                {
                    supported |= Mathf.Abs(p.Position.y - y) < 0.001f;
                }

                Assert.IsTrue(supported,
                    $"{p.Name} starts at y={p.Position.y:0.000}, which is not a surface in this shed");
            }
        }

        [Test]
        public void CarryablesOnTheBenchAreActuallyOverIt()
        {
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                if (!Mathf.Approximately(p.Position.y, ShedDimensions.BenchHeight))
                {
                    continue;
                }

                Assert.Greater(p.Position.x, ShedDimensions.BenchFrontX,
                    $"{p.Name} sits at bench height but in front of the bench, so it would fall");
                Assert.Greater(p.Position.z, ShedDimensions.BenchStartZ,
                    $"{p.Name} is past the near end of the bench");
                Assert.Less(p.Position.z, ShedDimensions.BenchEndZ,
                    $"{p.Name} is past the far end of the bench");
            }
        }

        [Test]
        public void FloorCarryablesLeaveTheAisleClear()
        {
            // The walking line down the middle of the room. Objects may sit against
            // the bench or the shelving, but nothing may start in the aisle itself.
            const float AisleHalfWidth = 0.55f;

            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                if (!Mathf.Approximately(p.Position.y, 0f))
                {
                    continue;
                }

                // The entrance mat area is not part of the through-route; an object
                // just inside the door is normal and is walked around.
                if (p.Position.z < -1.8f)
                {
                    continue;
                }

                Assert.Greater(Mathf.Abs(p.Position.x), AisleHalfWidth,
                    $"{p.Name} at x={p.Position.x:0.00} sits in the middle of the walking aisle");
            }
        }

        [Test]
        public void CarryablesDoNotStartInsideEachOther()
        {
            CarryablesBuilder.Placement[] all = CarryablesBuilder.Placements;

            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    float distance = Vector3.Distance(all[i].Position, all[j].Position);
                    Assert.Greater(distance, 0.30f,
                        $"{all[i].Name} and {all[j].Name} are {distance:0.00} m apart and would intersect");
                }
            }
        }

        [Test]
        public void DoorHingeSitsOnTheLeafEdgeNotItsCentre()
        {
            // Read from the builder, not re-derived from ShedDimensions. An earlier
            // version of this test computed the hinge itself and then checked its own
            // arithmetic - (C - h) + h == C - which is true no matter what the
            // generator does, and passed happily with the door hung off its centre.
            float hingeX = OpeningsBuilder.DoorHingeX;
            float leafLocalX = OpeningsBuilder.DoorLeafLocalX;

            Assert.AreEqual(ShedDimensions.DoorCentreX, hingeX + leafLocalX, 0.0001f,
                "the leaf no longer lands in the middle of its opening");

            // The hinge must be at the leaf's edge. Off its centre the door sweeps
            // through its own frame, and half of it swings back into the room.
            Assert.AreEqual(ShedDimensions.DoorLeafWidth * 0.5f, leafLocalX, 0.0001f,
                $"the leaf is offset {leafLocalX:0.000} m from the hinge, so the hinge " +
                "is not on its edge");

            float openingMinX = ShedDimensions.DoorCentreX - (ShedDimensions.DoorRoughWidth * 0.5f);
            Assert.GreaterOrEqual(hingeX, openingMinX - 0.001f,
                "the hinge line sits outside the door's rough opening");
        }

        [Test]
        public void DoorSwingsOutwardThroughItsWholeArc()
        {
            // Unity rotates left-handed, so a point at +X moves toward -Z under a
            // positive angle about +Y. The door sits in the -Z wall, which makes -Z
            // away from the room. Getting that sign wrong swings an 820 mm leaf into
            // a 4 m room, and the comment beside the code claimed outward while the
            // code did the opposite for one commit.
            float hingeX = OpeningsBuilder.DoorHingeX;
            float hingeZ = -ShedDimensions.HalfLength + OpeningsBuilder.DoorLeafClosedZ;
            float interiorFaceZ = -ShedDimensions.HalfLength;

            for (int step = 0; step <= 24; step++)
            {
                float angle = OpeningsBuilder.DoorOpenAngleDegrees * (step / 24f);

                // Sample along the leaf, hinge to latch edge.
                for (int i = 0; i <= 8; i++)
                {
                    float r = ShedDimensions.DoorLeafWidth * (i / 8f);
                    Vector3 point = new Vector3(hingeX, 0f, hingeZ) +
                                    (Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(r, 0f, 0f));

                    Assert.LessOrEqual(point.z, interiorFaceZ + 0.001f,
                        $"at {angle:0} deg the leaf reaches z={point.z:0.000}, which is " +
                        $"{(point.z - interiorFaceZ) * 1000f:0} mm inside the room");
                }
            }
        }

        [Test]
        public void ClosedDoorLeafFitsItsRoughOpening()
        {
            float hingeX = OpeningsBuilder.DoorHingeX;
            float latchX = hingeX + ShedDimensions.DoorLeafWidth;

            float openingMinX = ShedDimensions.DoorCentreX - (ShedDimensions.DoorRoughWidth * 0.5f);
            float openingMaxX = ShedDimensions.DoorCentreX + (ShedDimensions.DoorRoughWidth * 0.5f);

            Assert.GreaterOrEqual(hingeX, openingMinX - 0.001f,
                "the hinge stile is buried in the wall beside the opening");
            Assert.LessOrEqual(latchX, openingMaxX + 0.001f,
                "the latch stile is buried in the wall beside the opening");
        }

        [Test]
        public void OpenDoorLeavesTheDoorwayWalkable()
        {
            // At full open the leaf must be close enough to perpendicular that it is
            // not still standing across its own opening.
            float across = Mathf.Abs(Mathf.Cos(OpeningsBuilder.DoorOpenAngleDegrees * Mathf.Deg2Rad)) *
                           ShedDimensions.DoorLeafWidth;

            Assert.Less(across, 0.12f,
                $"the open leaf still spans {across:0.00} m of its own doorway");
        }

        [Test]
        public void WindowCasementSwingsOutward()
        {
            // The window assembly is rotated 90 degrees about Y, so work in its own
            // frame: local +X runs along the wall, local +Z points out of the
            // building. The sash hangs at -X from its hinge, so a positive angle has
            // to carry it to positive local Z.
            for (int step = 1; step <= 12; step++)
            {
                float angle = OpeningsBuilder.CasementOpenAngleDegrees * (step / 12f);
                Vector3 tip = Quaternion.AngleAxis(angle, Vector3.up) *
                              new Vector3(-OpeningsBuilder.CasementSashHalfWidth, 0f, 0f);

                Assert.Greater(tip.z, 0f,
                    $"at {angle:0} deg the casement is at local z={tip.z:0.000}, " +
                    "which is back through the wall into the room");
            }
        }

        [Test]
        public void WindowCasementStaysInsideItsReveal()
        {
            // The sash must sit within the hole in the wall, or it fouls the lining
            // on the way past and the exterior architrave beyond that.
            float hingeX = OpeningsBuilder.CasementHingeLocalX;
            float halfW = OpeningsBuilder.CasementSashHalfWidth;
            float revealHalf = ShedDimensions.WindowWidth * 0.5f;

            Assert.LessOrEqual(hingeX, revealHalf - 0.005f,
                $"the casement hinge at x={hingeX:0.000} is outside the {revealHalf:0.000} m reveal");
            Assert.GreaterOrEqual(hingeX - (halfW * 2f), -revealHalf - 0.005f,
                "the closed casement overhangs the far side of the reveal");
        }

        [Test]
        public void ServicePanelHingeSitsOnItsFarEdge()
        {
            float hingeZ = ShellBuilder.ServicePanelHingeZ;
            float localZ = ShellBuilder.ServicePanelLocalZ;

            Assert.AreEqual(ShedDimensions.ServicePanelCentreZ, hingeZ + localZ, 0.0001f,
                "the panel no longer lands back in its own hole");

            Assert.AreEqual(ShedDimensions.ServicePanelLength * 0.5f, localZ, 0.0001f,
                "the panel is hinged through its middle rather than along an edge");
        }

        [Test]
        public void TheEntranceApronIsReachableThroughTheOpenDoor()
        {
            // The door opens now, so the boundary has to allow the ground just
            // outside it. Before that, being outside was by definition a collision
            // fault and the backstop teleported the player to spawn with a warning.
            PlayAreaBoundary boundary = new PlayAreaBoundary();

            Vector3 justOutside = new Vector3(ShedDimensions.DoorCentreX, 0.1f,
                                              -ShedDimensions.HalfLength - 0.8f);
            Assert.IsTrue(boundary.IsOnEntranceApron(justOutside),
                "the ground immediately outside the door is not in the play area");

            // And it must still stop somewhere.
            Vector3 wellAway = new Vector3(ShedDimensions.DoorCentreX, 0.1f,
                                           -ShedDimensions.HalfLength - 6f);
            Assert.IsFalse(boundary.IsOnEntranceApron(wellAway),
                "the apron does not end, so the player can walk off across the ground");

            // The apron is in front of the door, not wrapped round the building.
            Vector3 besideTheShed = new Vector3(ShedDimensions.HalfWidth + 1.5f, 0.1f, 0f);
            Assert.IsFalse(boundary.IsOnEntranceApron(besideTheShed),
                "the apron reaches round the side of the shed");
        }

        [Test]
        public void TheSwitchIsWithinReachOfAStandingPlayer()
        {
            // Reach is 2.2 m from the eye. The switch has to be usable from a spot a
            // player can actually stand in, which means inside the room and not
            // inside the wall it is mounted on.
            Assert.Less(ShedDimensions.LightSwitchY, ShedDimensions.PlayerEyeHeight + 0.6f,
                "the light switch is mounted above comfortable reach");
            Assert.Greater(ShedDimensions.LightSwitchY, 0.9f,
                "the light switch is mounted below comfortable reach");
            Assert.Less(Mathf.Abs(ShedDimensions.LightSwitchX), ShedDimensions.HalfWidth - 0.1f,
                "the light switch is off the end of the entrance wall");
        }
    }
}
