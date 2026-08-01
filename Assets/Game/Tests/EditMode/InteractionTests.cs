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

        [Test]
        public void CarryablesStartInsideTheRoom()
        {
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                Assert.Less(Mathf.Abs(p.Base.x), ShedDimensions.HalfWidth - WallClearance,
                    $"{p.Name} at x={p.Base.x:0.00} is in or through a long wall");
                Assert.Less(Mathf.Abs(p.Base.z), ShedDimensions.HalfLength - WallClearance,
                    $"{p.Name} at z={p.Base.z:0.00} is in or through an end wall");
            }
        }

        [Test]
        public void CarryablesRestOnASurfaceNotInMidAir()
        {
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                bool onFloor = Mathf.Approximately(p.Base.y, 0f);
                bool onBench = Mathf.Approximately(p.Base.y, ShedDimensions.BenchHeight);

                Assert.IsTrue(onFloor || onBench,
                    $"{p.Name} starts at y={p.Base.y:0.000}, which is neither the floor nor the bench");
            }
        }

        [Test]
        public void CarryablesOnTheBenchAreActuallyOverIt()
        {
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                if (!Mathf.Approximately(p.Base.y, ShedDimensions.BenchHeight))
                {
                    continue;
                }

                Assert.Greater(p.Base.x, ShedDimensions.BenchFrontX,
                    $"{p.Name} sits at bench height but in front of the bench, so it would fall");
                Assert.Greater(p.Base.z, ShedDimensions.BenchStartZ,
                    $"{p.Name} is past the near end of the bench");
                Assert.Less(p.Base.z, ShedDimensions.BenchEndZ,
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
                if (!Mathf.Approximately(p.Base.y, 0f))
                {
                    continue;
                }

                // The entrance mat area is not part of the through-route; an object
                // just inside the door is normal and is walked around.
                if (p.Base.z < -1.8f)
                {
                    continue;
                }

                Assert.Greater(Mathf.Abs(p.Base.x), AisleHalfWidth,
                    $"{p.Name} at x={p.Base.x:0.00} sits in the middle of the walking aisle");
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
                    float distance = Vector3.Distance(all[i].Base, all[j].Base);
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
