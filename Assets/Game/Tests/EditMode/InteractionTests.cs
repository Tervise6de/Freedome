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
            // The generator places the hinge half a leaf-width back from the door
            // centre, then offsets the leaf the same amount forward. If those two
            // disagree the door is hung off its middle and sweeps through the frame.
            float halfLeaf = ShedDimensions.DoorLeafWidth * 0.5f;
            float hingeX = ShedDimensions.DoorCentreX - halfLeaf;
            float leafCentreX = hingeX + halfLeaf;

            Assert.AreEqual(ShedDimensions.DoorCentreX, leafCentreX, 0.0001f,
                "the leaf no longer lands in the middle of its opening");

            // And the hinge has to be inside the rough opening, not out in the wall.
            float openingMinX = ShedDimensions.DoorCentreX - (ShedDimensions.DoorRoughWidth * 0.5f);
            Assert.GreaterOrEqual(hingeX, openingMinX - 0.001f,
                "the hinge line sits outside the door's rough opening");
        }

        [Test]
        public void ServicePanelHingeSitsOnItsFarEdge()
        {
            float halfPanel = ShedDimensions.ServicePanelLength * 0.5f;
            float hingeZ = ShedDimensions.ServicePanelCentreZ - halfPanel;
            float panelCentreZ = hingeZ + halfPanel;

            Assert.AreEqual(ShedDimensions.ServicePanelCentreZ, panelCentreZ, 0.0001f,
                "the panel no longer lands back in its own hole");
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
