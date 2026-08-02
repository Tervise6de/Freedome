using UnityEngine;
using Freedome.Interaction;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The way out, under the floor.
    ///
    /// The shed stands on piers with a 249 mm platform - floorboards on joists on
    /// bearers - so there is a real void under it, closed at the perimeter by a
    /// skirt board. That skirt is the last thing between the crawl space and the
    /// outside, and it is nailed rather than screwed, which is why it comes off
    /// with a lever instead of the screwdriver.
    ///
    /// None of this is new geometry invented for the escape. The platform, the
    /// piers and the service panel were all built for the environment milestone
    /// because that is how the building goes together. The only addition is the
    /// skirt board itself, which a shed on piers would have anyway.
    /// </summary>
    public static class EscapeRouteBuilder
    {
        /// <summary>Underside of the floorboards - the ceiling of the crawl space.</summary>
        public static float CrawlSpaceTop => -Dim.FloorBoardThickness;

        /// <summary>Ground level under the shed.</summary>
        public static float CrawlSpaceFloor => -Dim.FloorStructureDepth;

        /// <summary>Where the skirt board sits: the entrance end, below the sill.</summary>
        public static Vector3 SkirtCentre =>
            new Vector3(Dim.ServicePanelCentreX,
                        (CrawlSpaceTop + CrawlSpaceFloor) * 0.5f,
                        -(Dim.HalfLength + (Dim.WallThickness * 0.5f)));

        public const float SkirtWidth = 1.10f;

        public static void Build(BuildContext ctx, Transform parent)
        {
            GameObject group = ctx.CreateGroup("EscapeRoute", parent);
            BuildContext.MarkMovable(group);

            BuildSkirtBoard(ctx, group.transform);
            BuildExitTrigger(group.transform);
        }

        private static void BuildSkirtBoard(BuildContext ctx, Transform parent)
        {
            float height = CrawlSpaceTop - CrawlSpaceFloor;

            MeshBuilder mb = new MeshBuilder("Skirt_Board", 1);
            mb.AddBox(Vector3.zero, new Vector3(SkirtWidth, height, 0.019f), 0, 0.002f);

            GameObject board = ctx.CreateObject("Skirt_Board", mb,
                new[] { Keys.Weatherboard }, parent, SkirtCentre, Quaternion.identity,
                BuildContext.ColliderKind.Box, isStatic: false);

            if (board == null)
            {
                return;
            }

            ToolGatedFixture fixture = board.AddComponent<ToolGatedFixture>();
            fixture.Configure("offcut",
                              "A skirt board, nailed on from outside",
                              "Lever the board off",
                              "The board is off",
                              ToolGatedFixture.Effect.LeverSkirt);
        }

        /// <summary>
        /// The gap the board was covering. Sits just outside the skirt line so the
        /// player has to actually crawl through rather than brush past it.
        /// </summary>
        private static void BuildExitTrigger(Transform parent)
        {
            GameObject go = new GameObject("Escape_Exit");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = SkirtCentre + new Vector3(0f, 0f, -0.45f);
            BuildContext.MarkMovable(go);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(SkirtWidth, CrawlSpaceTop - CrawlSpaceFloor, 0.60f);

            go.AddComponent<EscapeExit>();
        }
    }
}
