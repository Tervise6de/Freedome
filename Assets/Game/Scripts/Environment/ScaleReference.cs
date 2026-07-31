using UnityEngine;

namespace Freedome.Environment
{
    /// <summary>
    /// A 1.8 m human-height reference used to confirm scale during the blockout.
    ///
    /// It is disabled in the shipped build. Its value is during authoring: if the
    /// door head is not a little above the marker's head and the bench top is not
    /// around its wrist height, the room is the wrong size, and that is far easier
    /// to see against a figure than against a set of numbers.
    /// </summary>
    public sealed class ScaleReference : MonoBehaviour
    {
        [SerializeField] private bool visibleInBuilds;

        public const float ReferenceHeight = ShedDimensions.ScaleReferenceHeight;

        private void Awake()
        {
            if (!visibleInBuilds && !Application.isEditor)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            // Height bands at the measurements that matter for a first-person room.
            DrawBand(0.90f, new Color(0.3f, 0.8f, 0.4f, 0.9f)); // bench top
            DrawBand(1.20f, new Color(0.3f, 0.6f, 0.9f, 0.9f)); // window sill
            DrawBand(1.70f, new Color(0.9f, 0.8f, 0.3f, 0.9f)); // standing eye height
            DrawBand(ReferenceHeight, new Color(0.9f, 0.9f, 0.9f, 0.9f));
        }

        private void DrawBand(float height, Color color)
        {
            Gizmos.color = color;
            Vector3 centre = transform.position + (Vector3.up * height);
            Gizmos.DrawWireCube(centre, new Vector3(0.55f, 0.004f, 0.35f));
        }
    }
}
