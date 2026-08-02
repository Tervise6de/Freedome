using UnityEngine;
using Freedome.Interaction;
using Dim = Freedome.Environment.ShedDimensions;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The way out, which is the door.
    ///
    /// An earlier version of this took the player under the floor, through the
    /// service panel and out past a skirt board. It was checked arithmetically and
    /// it was wrong: the platform is floorboards on 90 mm joists on 140 mm bearers,
    /// which leaves 230 mm of clear space. A person needs 350 to 400 mm to crawl on
    /// their front. That route was impossible in the real world, not merely awkward
    /// in Unity - which is exactly why nobody escapes from under a shed.
    ///
    /// What is left is the honest route out of a locked outbuilding: a rim lock is
    /// mounted on the *inside* face of the door, and its case is held on by screws
    /// you can reach. Take the case off and the door opens.
    /// </summary>
    public static class EscapeRouteBuilder
    {
        /// <summary>How far out from the entrance wall the exit trigger sits.</summary>
        public const float ExitStandoff = 0.90f;

        public static Vector3 ExitCentre =>
            new Vector3(Dim.DoorCentreX, Dim.PlayerStandingHeight * 0.5f,
                        -(Dim.HalfLength + Dim.WallThickness) - ExitStandoff);

        public static void Build(BuildContext ctx, Transform parent)
        {
            GameObject group = ctx.CreateGroup("EscapeRoute", parent);
            BuildContext.MarkMovable(group);

            GameObject go = new GameObject("Escape_Exit");
            go.transform.SetParent(group.transform, false);
            go.transform.localPosition = ExitCentre;
            BuildContext.MarkMovable(go);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.80f, Dim.PlayerStandingHeight, 0.80f);

            go.AddComponent<EscapeExit>();
        }
    }
}
