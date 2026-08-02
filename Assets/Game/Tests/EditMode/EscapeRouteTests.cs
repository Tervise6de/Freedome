using NUnit.Framework;
using UnityEngine;
using Freedome.Environment;
using Freedome.EditorTools.Generation;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// The way out, checked as geometry rather than as a story.
    ///
    /// A puzzle chain can be logically correct and physically impossible - a crawl
    /// space too shallow to crawl through, an exit inside a wall, a skirt board
    /// wider than the shed. None of that shows up in a diff.
    /// </summary>
    public sealed class EscapeRouteTests
    {
        [Test]
        public void TheCrawlSpaceIsDeepEnoughToCrawlThrough()
        {
            float depth = EscapeRouteBuilder.CrawlSpaceTop - EscapeRouteBuilder.CrawlSpaceFloor;

            // 230 mm is tight but real - it is what a shed on 140 mm bearers gives
            // you. Below about 200 mm nobody is getting through on their front.
            Assert.Greater(depth, 0.20f,
                $"the crawl space is only {depth * 1000f:0} mm deep, which is not a crawl space");
        }

        [Test]
        public void TheSkirtBoardIsUnderTheFloorNotInTheRoom()
        {
            Assert.Less(EscapeRouteBuilder.SkirtCentre.y, 0f,
                "the skirt board is above floor level, so it is in the room");
            Assert.Greater(EscapeRouteBuilder.SkirtCentre.y, -ShedDimensions.FloorStructureDepth,
                "the skirt board is below the underside of the bearers");
        }

        [Test]
        public void TheSkirtBoardIsInTheEntranceWallLine()
        {
            float wallLine = -(ShedDimensions.HalfLength + (ShedDimensions.WallThickness * 0.5f));

            Assert.AreEqual(wallLine, EscapeRouteBuilder.SkirtCentre.z, 0.001f,
                "the skirt board is not in the perimeter it is supposed to close");
        }

        [Test]
        public void TheSkirtBoardIsWideEnoughToGetThroughAndNarrowerThanTheWall()
        {
            Assert.Greater(EscapeRouteBuilder.SkirtWidth, 0.55f,
                "the gap left by the board is too narrow for shoulders");
            Assert.Less(EscapeRouteBuilder.SkirtWidth, ShedDimensions.InteriorWidth,
                "the skirt board is wider than the wall it sits in");
        }

        [Test]
        public void TheRouteStartsAtTheServicePanel()
        {
            // The player goes down through the panel, so the board has to be
            // reachable from it - the same crawl space, not a different bay.
            float run = Mathf.Abs(EscapeRouteBuilder.SkirtCentre.z - ShedDimensions.ServicePanelCentreZ);

            Assert.Less(run, 2.5f,
                $"it is {run:0.00} m from the hatch to the skirt board, which is a long way on your front");
            Assert.AreEqual(ShedDimensions.ServicePanelCentreX, EscapeRouteBuilder.SkirtCentre.x, 0.001f,
                "the board is not in line with the hatch");
        }

        [Test]
        public void TheScrewdriverAndTheOffcutAreBothInTheRoom()
        {
            // Every step of the chain needs its tool to exist somewhere reachable.
            // The offcut is a placed carryable; the screwdriver is in a drawer.
            bool offcut = false;
            foreach (CarryablesBuilder.Placement p in CarryablesBuilder.Placements)
            {
                if (p.Name == "OffcutBlock")
                {
                    offcut = true;
                }
            }

            Assert.IsTrue(offcut, "the timber offcut is not placed, so the skirt cannot be levered");
        }
    }
}
