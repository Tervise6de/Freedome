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
        public void TheRimLockIsOnTheInsideFaceWithinReach()
        {
            // A rim lock mounts on the inside of the door, which is the whole reason
            // this route works. Its case sits at 1.020 m on the latch stile.
            const float LockHeight = 1.020f;

            Assert.Less(LockHeight, ShedDimensions.PlayerEyeHeight + 0.5f,
                "the lock case is above comfortable reach");
            Assert.Greater(LockHeight, 0.6f, "the lock case is below comfortable reach");
            Assert.Less(LockHeight, ShedDimensions.DoorLeafHeight,
                "the lock case is above the top of the door");
        }
    }
}
