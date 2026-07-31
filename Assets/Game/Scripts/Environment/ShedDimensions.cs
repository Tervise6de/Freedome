using UnityEngine;

namespace Freedome.Environment
{
    /// <summary>
    /// The single source of truth for every measurement in the shed.
    ///
    /// Both the editor generators and the automated validation tests read from this
    /// class, so the built scene and the documentation can never drift apart. All
    /// values are in metres and follow real timber-framing conventions (90x45 studs
    /// at 600 mm centres, 19 mm boards, 2040 mm door leaf).
    ///
    /// Coordinate convention
    ///   origin  centre of the floor, with the walking surface at y = 0
    ///   +X      width  (interior spans -2.0 .. +2.0, so 4.0 m)
    ///   +Z      length (interior spans -3.0 .. +3.0, so 6.0 m)
    ///   +Y      up     (top of the wall plate at 2.4 m)
    ///
    /// Wall naming used throughout the project and the design docs:
    ///   Entrance wall  z = -3.0  (gable end, contains the door)
    ///   Utility wall   z = +3.0  (gable end, breaker/vent/radio)
    ///   Workbench wall x = +2.0  (long wall, contains the window)
    ///   Storage wall   x = -2.0  (long wall, shelving)
    /// </summary>
    public static class ShedDimensions
    {
        // ---------------------------------------------------------------------
        // Interior envelope
        // ---------------------------------------------------------------------

        /// <summary>Interior clear width, wall lining face to wall lining face.</summary>
        public const float InteriorWidth = 4.0f;

        /// <summary>Interior clear length, wall lining face to wall lining face.</summary>
        public const float InteriorLength = 6.0f;

        /// <summary>Floor to top of the wall plate. Rafters bear on top of this.</summary>
        public const float WallHeight = 2.4f;

        public const float HalfWidth = InteriorWidth * 0.5f;   // 2.0
        public const float HalfLength = InteriorLength * 0.5f;  // 3.0

        // ---------------------------------------------------------------------
        // Timber stock. Nominal 90x45 framing, 19 mm boards - ordinary shed sizes.
        // ---------------------------------------------------------------------

        public const float StudDepth = 0.090f;   // 90 mm, wall thickness of the frame
        public const float StudWidth = 0.045f;   // 45 mm, the face you see from inside
        public const float StudSpacing = 0.600f; // 600 mm centres
        public const float BoardThickness = 0.019f; // 19 mm sheathing / floor / shelf boards
        public const float CladdingThickness = 0.016f; // 16 mm painted weatherboard skin
        public const float CladdingBoardHeight = 0.140f; // exposed face of one weatherboard

        /// <summary>Frame + sheathing + weatherboard. Used for exterior face positions.</summary>
        public const float WallThickness = StudDepth + BoardThickness + CladdingThickness; // 0.125

        /// <summary>Plate stock is laid flat: 90 wide x 45 thick.</summary>
        public const float PlateThickness = StudWidth; // 45 mm

        /// <summary>Bottom plate + double top plate are subtracted from stud length.</summary>
        public const float StudLength = WallHeight - PlateThickness - (PlateThickness * 2f); // 2.265

        // ---------------------------------------------------------------------
        // Floor build-up. The walking surface is y = 0; structure hangs below it.
        // ---------------------------------------------------------------------

        public const float FloorBoardThickness = 0.019f;
        public const float FloorBoardWidth = 0.140f;
        public const float FloorJoistDepth = 0.090f;
        public const float FloorJoistWidth = 0.045f;
        public const float FloorJoistSpacing = 0.450f;
        public const float BearerDepth = 0.140f;

        /// <summary>Underside of the bearers, i.e. how far the shed sits off the ground.</summary>
        public const float FloorStructureDepth =
            FloorBoardThickness + FloorJoistDepth + BearerDepth; // 0.249

        // ---------------------------------------------------------------------
        // Roof. Gable roof with the ridge running along Z (over the long axis).
        // ---------------------------------------------------------------------

        public const float RoofPitchDegrees = 22.0f;
        public const float RafterDepth = 0.090f;
        public const float RafterWidth = 0.045f;
        public const float RafterSpacing = 0.600f;
        public const float RidgeBoardHeight = 0.150f;
        public const float RidgeBoardThickness = 0.025f;
        public const float SarkingThickness = 0.019f;   // boards over the rafters
        public const float RoofSheetThickness = 0.006f; // corrugated galvanised profile
        public const float EaveOverhang = 0.300f;       // beyond the weatherboard face
        public const float GableOverhang = 0.250f;
        public const float CollarTieHeight = 2.85f;     // underside of the collar ties
        public const float CollarTieThickness = 0.035f;

        /// <summary>Horizontal run from the ridge to the outside face of the wall.</summary>
        public const float RoofHalfSpan = HalfWidth + WallThickness; // 2.125

        /// <summary>Height of the ridge line above the top plate.</summary>
        public static float RidgeRise =>
            RoofHalfSpan * Mathf.Tan(RoofPitchDegrees * Mathf.Deg2Rad); // ~0.858

        /// <summary>Absolute Y of the ridge line (top of rafter at x = 0).</summary>
        public static float RidgeHeight => WallHeight + RidgeRise; // ~3.258

        // ---------------------------------------------------------------------
        // Door. 820 x 2040 leaf, the ordinary exterior door size. Entrance wall.
        // ---------------------------------------------------------------------

        public const float DoorLeafWidth = 0.820f;
        public const float DoorLeafHeight = 2.040f;
        public const float DoorLeafThickness = 0.045f;
        public const float DoorJambThickness = 0.032f;
        public const float DoorClearance = 0.004f;
        public const float DoorTrimWidth = 0.070f;
        public const float DoorTrimThickness = 0.018f;

        /// <summary>Centre of the door opening along X on the entrance wall.</summary>
        public const float DoorCentreX = -0.85f;

        public const float DoorRoughWidth = DoorLeafWidth + (DoorJambThickness * 2f) + (DoorClearance * 2f);
        public const float DoorRoughHeight = DoorLeafHeight + DoorJambThickness + 0.010f;

        /// <summary>Handle centre height. 1.02 m is the normal set-out for a door lever.</summary>
        public const float DoorHandleHeight = 1.020f;

        // ---------------------------------------------------------------------
        // Window. One small practical window over the workbench.
        // ---------------------------------------------------------------------

        public const float WindowWidth = 0.900f;  // along Z
        public const float WindowHeight = 0.600f;
        public const float WindowSillHeight = 1.200f; // clears the 0.90 m bench top
        public const float WindowFrameWidth = 0.045f;
        public const float WindowFrameDepth = 0.060f;
        public const float GlassThickness = 0.004f;
        public const float WindowSillProjection = 0.040f;

        /// <summary>Centre of the window opening along Z on the workbench wall.</summary>
        public const float WindowCentreZ = 0.600f;

        public static float WindowHeadHeight => WindowSillHeight + WindowHeight; // 1.8

        // ---------------------------------------------------------------------
        // Workbench, on the +X wall under the window.
        // ---------------------------------------------------------------------

        public const float BenchLength = 2.400f; // along Z
        public const float BenchDepth = 0.600f;  // out from the wall
        public const float BenchHeight = 0.900f; // top surface
        public const float BenchTopThickness = 0.038f; // two laminated 19 mm plies
        public const float BenchLegSize = 0.070f;
        public const float BenchStartZ = -0.600f;
        public static float BenchEndZ => BenchStartZ + BenchLength; // +1.8
        public static float BenchFrontX => HalfWidth - BenchDepth;  // +1.4

        public const float PegboardHeight = 0.900f;
        public const float PegboardBottomY = 1.000f;
        public const float PegboardThickness = 0.006f;

        // ---------------------------------------------------------------------
        // Shelving, on the -X wall.
        // ---------------------------------------------------------------------

        public const float ShelfUnitLength = 2.400f; // along Z
        public const float ShelfUnitDepth = 0.450f;
        public const float ShelfUnitHeight = 1.950f;
        public const float ShelfUnitStartZ = -1.200f;
        public const float ShelfUprightSize = 0.070f;

        /// <summary>Height of each shelf board's top surface.</summary>
        public static readonly float[] ShelfHeights = { 0.250f, 0.750f, 1.250f, 1.750f };

        // ---------------------------------------------------------------------
        // Utility wall fittings, on the +Z gable end.
        // ---------------------------------------------------------------------

        public const float BreakerBoxWidth = 0.300f;
        public const float BreakerBoxHeight = 0.400f;
        public const float BreakerBoxDepth = 0.095f;
        public const float BreakerBoxCentreX = 0.850f;
        public const float BreakerBoxCentreY = 1.600f;

        public const float ConduitDiameter = 0.020f;

        public const float VentWidth = 0.300f;
        public const float VentHeight = 0.200f;
        public const float VentCentreX = -1.100f;
        public const float VentCentreY = 2.050f;

        public const float SocketCentreY = 0.350f;
        public const float UtilityShelfLength = 1.200f;
        public const float UtilityShelfDepth = 0.250f;
        public const float UtilityShelfHeight = 1.100f;
        public const float UtilityShelfCentreX = -1.000f;

        public const float LightSwitchX = -0.300f;
        public const float LightSwitchY = 1.150f;

        // ---------------------------------------------------------------------
        // Removable service panel in the floor. Non-functional in this milestone;
        // it is dressed to read as an ordinary inspection hatch.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Five floorboards wide. The hatch is deliberately aligned to whole boards -
        /// that is how a real one would be cut, and it stops the panel from reading
        /// as an object dropped on top of the floor.
        /// </summary>
        public const float ServicePanelWidth = 0.700f;  // along X, 5 x 140 mm boards
        public const float ServicePanelLength = 0.900f; // along Z
        public const float ServicePanelCentreX = -0.620f;
        public const float ServicePanelCentreZ = -1.400f;

        // ---------------------------------------------------------------------
        // Lighting fixtures.
        // ---------------------------------------------------------------------

        /// <summary>The fixture hangs from the collar tie that sits at z = 0.</summary>
        public const float CeilingLightZ = 0.000f;
        public const float CeilingLightHeight = 2.300f; // bulb centre, hung from a collar tie
        public const float TaskLightZ = -0.250f;
        public const float TaskLightHeight = 1.850f;

        // ---------------------------------------------------------------------
        // Player.
        // ---------------------------------------------------------------------

        public static readonly Vector3 PlayerSpawnPosition = new Vector3(-0.85f, 0.0f, -2.55f);

        /// <summary>Facing +Z, i.e. looking from the door down the length of the shed.</summary>
        public const float PlayerSpawnYaw = 0.0f;

        public const float PlayerStandingHeight = 1.800f;
        public const float PlayerEyeHeight = 1.700f;
        public const float PlayerCrouchHeight = 1.250f;
        public const float PlayerCrouchEyeHeight = 1.150f;
        public const float PlayerRadius = 0.300f;

        /// <summary>Human-height reference marker used to confirm scale during blockout.</summary>
        public const float ScaleReferenceHeight = 1.800f;

        // ---------------------------------------------------------------------
        // Derived helpers used by builders and tests.
        // ---------------------------------------------------------------------

        /// <summary>Interior face X of a long wall. sign -1 = storage wall, +1 = workbench wall.</summary>
        public static float LongWallInnerX(int sign) => sign * HalfWidth;

        /// <summary>Interior face Z of a gable wall. sign -1 = entrance, +1 = utility.</summary>
        public static float GableWallInnerZ(int sign) => sign * HalfLength;

        /// <summary>Outside face of the weatherboards on a long wall.</summary>
        public static float LongWallOuterX(int sign) => sign * (HalfWidth + WallThickness);

        /// <summary>Outside face of the weatherboards on a gable wall.</summary>
        public static float GableWallOuterZ(int sign) => sign * (HalfLength + WallThickness);

        /// <summary>Height of the underside of the rafter line at a given X.</summary>
        public static float RoofUndersideAt(float x)
        {
            float run = RoofHalfSpan - Mathf.Abs(x);
            float pitched = WallHeight + (run * Mathf.Tan(RoofPitchDegrees * Mathf.Deg2Rad));
            return pitched;
        }

        /// <summary>Interior floor area in square metres.</summary>
        public static float FloorArea => InteriorWidth * InteriorLength; // 24 m^2
    }
}
