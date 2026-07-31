using UnityEngine;
using Freedome.Player;

namespace Freedome.Environment
{
    /// <summary>
    /// Guarantees the player stays in the demonstration area.
    ///
    /// The walls, the closed door and the floor already do this with ordinary
    /// colliders. This is the backstop: if the player somehow ends up outside the
    /// interior volume - a physics glitch, a seam in the framing, a future change
    /// that opens a hole - they are put straight back at the spawn point rather than
    /// falling through the world.
    ///
    /// It is written to be silent in normal play. If it ever fires during testing,
    /// that is a bug in the collision, not a feature.
    /// </summary>
    public sealed class PlayAreaBoundary : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private float margin = 0.35f;
        [SerializeField] private float floorTolerance = 1.0f;
        [SerializeField] private float ceilingTolerance = 1.5f;

        private int _recoveries;

        /// <summary>How many times the backstop has had to fire. Tests assert on this.</summary>
        public int RecoveryCount => _recoveries;

        private void Awake()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<FirstPersonController>();
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            if (IsInside(player.transform.position))
            {
                return;
            }

            _recoveries++;
            Debug.LogWarning($"[Freedome] Player left the demonstration area at " +
                             $"{player.transform.position}. Returning to the spawn point. " +
                             $"This indicates a collision gap and should be investigated.");

            player.Teleport(ShedDimensions.PlayerSpawnPosition, ShedDimensions.PlayerSpawnYaw);
        }

        public bool IsInside(Vector3 position)
        {
            float halfWidth = ShedDimensions.HalfWidth + margin;
            float halfLength = ShedDimensions.HalfLength + margin;

            return position.x > -halfWidth && position.x < halfWidth
                   && position.z > -halfLength && position.z < halfLength
                   && position.y > -floorTolerance
                   && position.y < ShedDimensions.RidgeHeight + ceilingTolerance;
        }
    }
}
