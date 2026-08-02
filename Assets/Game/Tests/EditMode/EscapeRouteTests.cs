using NUnit.Framework;
using UnityEngine;
using Freedome.Environment;
using Freedome.EditorTools.Generation;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// The way out, checked as geometry rather than as a story.
    ///
    /// The first version of the route took the player under the floor and out past
    /// a skirt board. It read well and it was impossible: the platform leaves
    /// 230 mm of clear space and a person needs 350 to 400 mm to crawl on their
    /// front. Nothing in the puzzle logic could have caught that. These tests exist
    /// so the next such mistake fails a build instead of shipping.
    /// </summary>
    public sealed class EscapeRouteTests
    {
        /// <summary>Belly-crawl clearance for an adult, generously.</summary>
        private const float CrawlableHeight = 0.35f;

        [Test]
        public void NothingInTheRouteRequiresCrawlingUnderTheFloor()
        {
            float underfloor = ShedDimensions.FloorStructureDepth - ShedDimensions.FloorBoardThickness;

            // Stated as a fact about the building, not a wish. If somebody raises
            // the shed on taller piers this becomes false and the comment in
            // EscapeRouteBuilder about why the route changed stops being true.
            Assert.Less(underfloor, CrawlableHeight,
                $"the crawl space is {underfloor * 1000f:0} mm, which is now passable - " +
                "the reason the route avoids it no longer holds");
        }

        [Test]
        public void TheExitIsOutsideTheEntranceWall()
        {
            float wallOuter = -(ShedDimensions.HalfLength + ShedDimensions.WallThickness);

            Assert.Less(EscapeRouteBuilder.ExitCentre.z, wallOuter,
                "the exit trigger is inside the building");
            Assert.AreEqual(ShedDimensions.DoorCentreX, EscapeRouteBuilder.ExitCentre.x, 0.001f,
                "the exit is not in front of the door");
        }

        [Test]
        public void TheExitIsInsideThePlayArea()
        {
            // Walking out has to be possible without the boundary backstop firing.
            PlayAreaBoundary boundary = new PlayAreaBoundary();
            Vector3 onTheGround = new Vector3(EscapeRouteBuilder.ExitCentre.x, 0.1f,
                                              EscapeRouteBuilder.ExitCentre.z);

            Assert.IsTrue(boundary.IsInside(onTheGround),
                "the exit is outside the play area, so stepping through it teleports " +
                "the player back to spawn");
        }

        [Test]
        public void TheExitIsPastTheDoorsSwingNotInsideIt()
        {
            // The leaf swings outward 92 degrees on an 820 mm radius. An exit
            // trigger inside that arc would fire while the door is still moving.
            float reach = ShedDimensions.DoorLeafWidth;
            float standoff = Mathf.Abs(EscapeRouteBuilder.ExitCentre.z) -
                             (ShedDimensions.HalfLength + ShedDimensions.WallThickness);

            Assert.Greater(standoff + 0.40f, 0f, "the exit sits behind the wall face");
            Assert.Less(standoff, reach + 1.0f,
                "the exit is so far out the player leaves the apron before reaching it");
        }

        [Test]
        public void BothToolsExistAndAreDistinct()
        {
            // The chain needs a lever and a driver, and they must not be the same
            // object - a single tool that does everything is a key, not a puzzle.
            bool offcut = false;
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                if (p.Name == "OffcutBlock")
                {
                    offcut = true;
                }
            }

            Assert.IsTrue(offcut, "the timber offcut is not placed, so the drawer cannot be levered");
        }

        [Test]
        public void TheChainCannotDeadlock()
        {
            // Every tool must be reachable strictly before the step that needs it.
            // A tool freed by the step it is needed for - or by a later one - is an
            // unwinnable room, and it takes one careless move to create.
            EscapeRouteBuilder.Step[] chain = EscapeRouteBuilder.Chain;

            for (int i = 0; i < chain.Length; i++)
            {
                if (string.IsNullOrEmpty(chain[i].Tool))
                {
                    continue;
                }

                Assert.Less(chain[i].ToolFreedByStep, i,
                    $"step {i} ({chain[i].Name}) needs the {chain[i].Tool}, which is not " +
                    $"reachable until step {chain[i].ToolFreedByStep}. The room cannot be finished.");
            }
        }

        [Test]
        public void TheFirstStepNeedsNothingYouCannotAlreadyReach()
        {
            EscapeRouteBuilder.Step first = EscapeRouteBuilder.Chain[0];

            Assert.AreEqual(-1, first.ToolFreedByStep,
                "the first step needs a tool that is locked away, so the game cannot start");
        }

        [Test]
        public void TheChainEndsByLeavingTheShed()
        {
            EscapeRouteBuilder.Step[] chain = EscapeRouteBuilder.Chain;

            Assert.Greater(chain.Length, 2, "a two-step escape is a door with a key");
            Assert.IsTrue(chain[chain.Length - 1].Name.ToLowerInvariant().Contains("out"),
                "the chain does not end by getting out");
        }

        [Test]
        public void TheRimLockIsOnTheInsideFaceWithinReach()
        {
            // A rim lock mounts on the inside of the door, which is the whole reason
            // this route works. Read from the builder: an earlier version of this
            // test kept its own copy of the height, which is exactly how a collider
            // ends up somewhere the lock is not.
            float LockHeight = OpeningsBuilder.RimLockLocalCentre.y;

            Assert.Less(LockHeight, ShedDimensions.PlayerEyeHeight + 0.5f,
                "the lock case is above comfortable reach");
            Assert.Greater(LockHeight, 0.6f, "the lock case is below comfortable reach");
            Assert.Less(LockHeight, ShedDimensions.DoorLeafHeight,
                "the lock case is above the top of the door");

            // And the case has to sit proud of the leaf, on the room side, or the
            // screws are not reachable and the whole route is fiction.
            Assert.Greater(OpeningsBuilder.RimLockLocalCentre.z, 0f,
                "the lock case is on the outside face of the door");
        }
    }
}
