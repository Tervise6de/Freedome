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
    ///
    /// The area is the shed plus a small apron outside the entrance. The apron
    /// exists because the door opens: before that, stepping outside was by
    /// definition a collision fault, and the backstop treated it as one. Walking
    /// out of an open door is now ordinary, so the boundary has to allow it - and
    /// it is still the thing that stops anyone wandering off across the ground
    /// plane.
    /// </summary>
    public sealed class PlayAreaBoundary : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private float margin = 0.35f;
        [SerializeField] private float apronDepth = 2.20f;
        [SerializeField] private float apronHalfWidth = 1.60f;
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
            if (position.y <= -floorTolerance ||
                position.y >= ShedDimensions.RidgeHeight + ceilingTolerance)
            {
                return false;
            }

            float halfWidth = ShedDimensions.HalfWidth + margin;
            float halfLength = ShedDimensions.HalfLength + margin;

            bool inShed = position.x > -halfWidth && position.x < halfWidth
                          && position.z > -halfLength && position.z < halfLength;

            return inShed || IsOnEntranceApron(position);
        }

        /// <summary>
        /// The patch of ground immediately outside the door. Sized so somebody who
        /// walks out can turn round and look back at the shed, and no further.
        /// </summary>
        public bool IsOnEntranceApron(Vector3 position)
        {
            float nearZ = -(ShedDimensions.HalfLength + margin);

            return position.z <= nearZ
                   && position.z > nearZ - apronDepth
                   && position.x > ShedDimensions.DoorCentreX - apronHalfWidth
                   && position.x < ShedDimensions.DoorCentreX + apronHalfWidth;
        }

        /// <summary>Centre of the apron, so the light probes can be put over it.</summary>
        public static Vector3 ApronCentre(float depth = 2.20f) =>
            new Vector3(ShedDimensions.DoorCentreX, 0f,
                        -(ShedDimensions.HalfLength + 0.35f) - (depth * 0.5f));
    }
}
