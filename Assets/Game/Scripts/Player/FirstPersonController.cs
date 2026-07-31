using UnityEngine;

namespace Freedome.Player
{
    /// <summary>
    /// A plain first-person walker built on CharacterController.
    ///
    /// The movement values are chosen so the shed reads at the right size: 1.5 m/s
    /// is an unhurried indoor walking pace, and crossing the 6 m length takes about
    /// four seconds. A demo that moves at 6 m/s makes any room feel like a corridor.
    ///
    /// There is no jump. The environment has nothing to jump onto, and leaving it
    /// out removes the easiest way for a player to clip out of the building.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float crouchSpeed = 0.85f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Stance")]
        [SerializeField] private float standingHeight = 1.80f;
        [SerializeField] private float crouchHeight = 1.25f;
        [SerializeField] private float standingEyeHeight = 1.70f;
        [SerializeField] private float crouchEyeHeight = 1.15f;
        [SerializeField] private float stanceChangeSpeed = 8f;

        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _targetHeight;
        private float _currentEyeHeight;
        private bool _crouching;

        /// <summary>Set false by the pause menu to freeze the player.</summary>
        public bool InputEnabled { get; set; } = true;

        public bool IsCrouching => _crouching;

        /// <summary>Horizontal speed in m/s. Head bob reads this.</summary>
        public float CurrentSpeed => _horizontalVelocity.magnitude;

        public bool IsGrounded => _controller != null && _controller.isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.height = standingHeight;
            _controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);
            _controller.slopeLimit = 45f;
            _controller.stepOffset = 0.25f;
            _controller.skinWidth = 0.02f;
            _controller.minMoveDistance = 0f;

            _targetHeight = standingHeight;
            _currentEyeHeight = standingEyeHeight;

            if (cameraPivot == null)
            {
                Camera child = GetComponentInChildren<Camera>();
                if (child != null)
                {
                    cameraPivot = child.transform;
                }
            }
        }

        private void Update()
        {
            UpdateStance();
            UpdateMovement();
            UpdateCameraHeight();
        }

        private void UpdateStance()
        {
            bool wantsCrouch = InputEnabled &&
                               (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));

            if (_crouching && !wantsCrouch && !HasHeadroom())
            {
                // Something is directly overhead, so stay down until it is clear.
                wantsCrouch = true;
            }

            _crouching = wantsCrouch;
            _targetHeight = _crouching ? crouchHeight : standingHeight;

            float newHeight = Mathf.MoveTowards(_controller.height, _targetHeight,
                                                stanceChangeSpeed * Time.deltaTime);
            if (!Mathf.Approximately(newHeight, _controller.height))
            {
                _controller.height = newHeight;
                _controller.center = new Vector3(0f, newHeight * 0.5f, 0f);
            }
        }

        /// <summary>Sphere cast upward to see whether standing up would clip the roof.</summary>
        private bool HasHeadroom()
        {
            float radius = _controller.radius * 0.95f;
            Vector3 origin = transform.position + (Vector3.up * (crouchHeight - radius));
            float distance = standingHeight - crouchHeight;

            return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance,
                                       ~0, QueryTriggerInteraction.Ignore);
        }

        private void UpdateMovement()
        {
            Vector3 wish = Vector3.zero;

            if (InputEnabled)
            {
                float forward = 0f;
                float strafe = 0f;

                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) forward += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) forward -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) strafe += 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) strafe -= 1f;

                wish = (transform.forward * forward) + (transform.right * strafe);
                if (wish.sqrMagnitude > 1f)
                {
                    wish.Normalize();
                }
            }

            float targetSpeed = _crouching ? crouchSpeed : walkSpeed;
            Vector3 targetVelocity = wish * targetSpeed;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity,
                                                      acceleration * Time.deltaTime);

            if (_controller.isGrounded)
            {
                // A small downward bias keeps the controller stuck to the floorboards
                // and to the low threshold at the door rather than skipping.
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 motion = _horizontalVelocity + (Vector3.up * _verticalVelocity);
            _controller.Move(motion * Time.deltaTime);
        }

        private void UpdateCameraHeight()
        {
            if (cameraPivot == null)
            {
                return;
            }

            float targetEye = _crouching ? crouchEyeHeight : standingEyeHeight;
            _currentEyeHeight = Mathf.MoveTowards(_currentEyeHeight, targetEye,
                                                  stanceChangeSpeed * Time.deltaTime);

            Vector3 local = cameraPivot.localPosition;
            local.y = _currentEyeHeight;
            cameraPivot.localPosition = local;
        }

        /// <summary>Used by the boundary guard and the restart option.</summary>
        public void Teleport(Vector3 position, float yawDegrees)
        {
            _controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            _controller.enabled = true;

            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }
    }
}
