using NUnit.Framework;
using UnityEngine;
using Freedome.Environment;

namespace Freedome.Tests.EditMode
{
    /// <summary>
    /// Guards the numbers the whole project is built from.
    ///
    /// These are not tautologies. The dimension table is edited by hand and read by
    /// a dozen builders; a slip that turns the shed into a 6 x 4 m room with a 3 m
    /// ceiling would still generate happily and would only be caught by eye. These
    /// tests state the architectural intent so that kind of change fails loudly.
    /// </summary>
    public sealed class ShedDimensionsTests
    {
        [Test]
        public void InteriorMatchesTheBriefedSize()
        {
            Assert.AreEqual(4.0f, ShedDimensions.InteriorWidth, 0.001f, "interior width");
            Assert.AreEqual(6.0f, ShedDimensions.InteriorLength, 0.001f, "interior length");
            Assert.AreEqual(2.4f, ShedDimensions.WallHeight, 0.001f, "wall height");
            Assert.AreEqual(24.0f, ShedDimensions.FloorArea, 0.01f, "floor area");
        }

        [Test]
        public void WallsHaveRealisticThickness()
        {
            // Frame plus sheathing plus cladding should land between 100 and 150 mm.
            Assert.Greater(ShedDimensions.WallThickness, 0.100f);
            Assert.Less(ShedDimensions.WallThickness, 0.150f);
            Assert.AreEqual(0.090f, ShedDimensions.StudDepth, 0.0001f);
            Assert.AreEqual(0.600f, ShedDimensions.StudSpacing, 0.0001f);
        }

        [Test]
        public void DoorIsAStandardExteriorSize()
        {
            Assert.AreEqual(0.820f, ShedDimensions.DoorLeafWidth, 0.001f);
            Assert.AreEqual(2.040f, ShedDimensions.DoorLeafHeight, 0.001f);

            // A door that is wider than a metre is the classic scale error.
            Assert.Less(ShedDimensions.DoorRoughWidth, 1.000f,
                "the door opening should not approach a metre wide");
            Assert.Greater(ShedDimensions.DoorLeafThickness, 0.030f, "the leaf needs real thickness");
        }

        [Test]
        public void DoorOpeningFitsWithinTheEntranceWall()
        {
            float half = ShedDimensions.DoorRoughWidth * 0.5f;
            float left = ShedDimensions.DoorCentreX - half;
            float right = ShedDimensions.DoorCentreX + half;

            Assert.Greater(left, -ShedDimensions.HalfWidth + 0.10f,
                "the door opening runs into the corner framing");
            Assert.Less(right, ShedDimensions.HalfWidth - 0.10f,
                "the door opening runs into the corner framing");
            Assert.Less(ShedDimensions.DoorRoughHeight, ShedDimensions.WallHeight - 0.15f,
                "the door head leaves no room for a lintel under the top plate");
        }

        [Test]
        public void WindowSitsAboveTheBenchAndBelowTheTopPlate()
        {
            Assert.Greater(ShedDimensions.WindowSillHeight, ShedDimensions.BenchHeight + 0.20f,
                "the window sill should clear the bench top and whatever is standing on it");
            Assert.Less(ShedDimensions.WindowHeadHeight, ShedDimensions.WallHeight - 0.30f,
                "the window head leaves no room for a lintel");
        }

        [Test]
        public void WindowOpeningFitsWithinTheWorkbenchWall()
        {
            float half = ShedDimensions.WindowWidth * 0.5f;
            Assert.Greater(ShedDimensions.WindowCentreZ - half, -ShedDimensions.HalfLength + 0.15f);
            Assert.Less(ShedDimensions.WindowCentreZ + half, ShedDimensions.HalfLength - 0.15f);
        }

        [Test]
        public void RoofGivesHeadroomWithoutBecomingAHall()
        {
            float ridge = ShedDimensions.RidgeHeight;

            Assert.Greater(ridge, ShedDimensions.WallHeight + 0.5f, "the roof pitch is too shallow to read");
            Assert.Less(ridge, 3.6f, "the ridge is too high for a domestic shed");

            // Headroom at the wall line must still clear a standing person.
            float atWall = ShedDimensions.RoofUndersideAt(ShedDimensions.HalfWidth);
            Assert.Greater(atWall, ShedDimensions.PlayerStandingHeight + 0.4f);
        }

        [Test]
        public void PlayerSpawnIsInsideTheRoomAndOnTheFloor()
        {
            Vector3 spawn = ShedDimensions.PlayerSpawnPosition;

            Assert.Less(Mathf.Abs(spawn.x), ShedDimensions.HalfWidth - ShedDimensions.PlayerRadius);
            Assert.Less(Mathf.Abs(spawn.z), ShedDimensions.HalfLength - ShedDimensions.PlayerRadius);
            Assert.AreEqual(0f, spawn.y, 0.001f, "the player should spawn on the floor surface");
        }

        [Test]
        public void PlayerProportionsAreHuman()
        {
            Assert.AreEqual(1.80f, ShedDimensions.PlayerStandingHeight, 0.001f);
            Assert.Less(ShedDimensions.PlayerEyeHeight, ShedDimensions.PlayerStandingHeight);
            Assert.Greater(ShedDimensions.PlayerEyeHeight, 1.55f);
            Assert.Less(ShedDimensions.PlayerCrouchHeight, ShedDimensions.PlayerStandingHeight);
        }

        [Test]
        public void ServicePanelSitsInsideTheRoomAndClearOfTheWalls()
        {
            float halfW = ShedDimensions.ServicePanelWidth * 0.5f;
            float halfL = ShedDimensions.ServicePanelLength * 0.5f;

            Assert.Less(Mathf.Abs(ShedDimensions.ServicePanelCentreX) + halfW,
                ShedDimensions.HalfWidth - 0.20f);
            Assert.Less(Mathf.Abs(ShedDimensions.ServicePanelCentreZ) + halfL,
                ShedDimensions.HalfLength - 0.20f);
        }

        [Test]
        public void BenchAndShelvingFitTheirWalls()
        {
            Assert.Less(ShedDimensions.BenchEndZ, ShedDimensions.HalfLength - 0.30f,
                "the bench runs into the utility wall");
            Assert.Greater(ShedDimensions.BenchStartZ, -ShedDimensions.HalfLength + 0.30f,
                "the bench runs into the entrance wall");

            float shelfEnd = ShedDimensions.ShelfUnitStartZ + ShedDimensions.ShelfUnitLength;
            Assert.Less(shelfEnd, ShedDimensions.HalfLength);
            Assert.Greater(ShedDimensions.ShelfUnitStartZ, -ShedDimensions.HalfLength);
            Assert.Less(ShedDimensions.ShelfUnitHeight, ShedDimensions.WallHeight - 0.30f);
        }

        [Test]
        public void CirculationAisleRemainsWalkable()
        {
            // Bench on one side, shelving on the other: what is left in the middle
            // has to be wide enough to walk down without sidling.
            float benchFace = ShedDimensions.BenchFrontX;                       // +1.40
            float shelfFace = -ShedDimensions.HalfWidth + ShedDimensions.ShelfUnitDepth; // -1.55
            float aisle = benchFace - shelfFace;

            Assert.Greater(aisle, 2.0f, $"the clear aisle is only {aisle:0.00} m wide");
        }

        [Test]
        public void FutureGameplayLocationsAreAllInsideTheRoom()
        {
            // The five architectural affordances have to be reachable and on a wall,
            // otherwise the design document is describing something that is not there.
            Assert.Less(Mathf.Abs(ShedDimensions.BreakerBoxCentreX), ShedDimensions.HalfWidth);
            Assert.Less(ShedDimensions.BreakerBoxCentreY + (ShedDimensions.BreakerBoxHeight * 0.5f),
                ShedDimensions.WallHeight);

            Assert.Less(Mathf.Abs(ShedDimensions.VentCentreX), ShedDimensions.HalfWidth);
            Assert.Less(ShedDimensions.VentCentreY + (ShedDimensions.VentHeight * 0.5f),
                ShedDimensions.WallHeight);

            Assert.Less(Mathf.Abs(ShedDimensions.UtilityShelfCentreX), ShedDimensions.HalfWidth);
            Assert.Less(ShedDimensions.LightSwitchY, ShedDimensions.WallHeight);
        }
    }
}
