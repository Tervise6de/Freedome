using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Anything that swings on a hinge: the door leaf, the window casement, the
    /// service panel in the floor.
    ///
    /// The transform this sits on is the hinge itself, not the leaf. The generator
    /// creates an empty at the hinge line and parents the leaf under it with a local
    /// offset, so this only ever has to set a local rotation - no rotating about an
    /// arbitrary pivot, and no drift from repeated Rotate calls.
    /// </summary>
    public sealed class HingedPart : Interactable
    {
        [SerializeField] private string closedVerb = "Open";
        [SerializeField] private string openVerb = "Close";
        [SerializeField] private Vector3 hingeAxis = Vector3.up;
        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float degreesPerSecond = 150f;
        [SerializeField] private bool startOpen;

        /// <summary>
        /// Gates. The door's lock never opens - it is the dead end that sends the
        /// player looking elsewhere - and the floor panel will not lift until its
        /// four screws are out.
        /// </summary>
        [SerializeField] private bool needsLockRemoved;
        [SerializeField] private bool needsPanelUnscrewed;
        [SerializeField] private string lockedPrompt = "It is locked";

        private EscapeState _escape;

        /// <summary>
        /// Disabled while the part is open, so the player can walk through a door
        /// that is standing open. Left null for parts nothing passes through.
        /// </summary>
        [SerializeField] private Collider blocker;

        private float _angle;
        private float _target;

        public bool IsOpen
        {
            get { return _target > 0.5f; }
        }

        /// <summary>Current swing in degrees. Exposed for the tests.</summary>
        public float Angle
        {
            get { return _angle; }
        }

        public bool IsBlocked
        {
            get
            {
                if (needsLockRemoved && (_escape == null || !_escape.LockRemoved))
                {
                    return true;
                }

                return needsPanelUnscrewed && (_escape == null || !_escape.PanelUnscrewed);
            }
        }

        public override string Prompt
        {
            get
            {
                if (IsBlocked)
                {
                    return lockedPrompt;
                }

                return IsOpen ? openVerb : closedVerb;
            }
        }

        private void Awake()
        {
            _escape = EscapeState.Find();
            _target = startOpen ? openAngle : 0f;
            _angle = _target;
            Apply();
        }

        public override void Interact(PlayerInteractor actor)
        {
            if (IsBlocked)
            {
                return;
            }

            _target = IsOpen ? 0f : openAngle;
        }

        private void Update()
        {
            if (Mathf.Approximately(_angle, _target))
            {
                return;
            }

            _angle = Mathf.MoveTowards(_angle, _target, degreesPerSecond * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            transform.localRotation = Quaternion.AngleAxis(_angle, hingeAxis.normalized);

            if (blocker != null)
            {
                // Off as soon as it starts moving, not when it finishes: the player
                // should not be able to walk into a door that is already swinging away.
                blocker.enabled = _angle < 1f;
            }
        }

        /// <summary>Holds it shut until the rim lock is off, or the panel screws are out.</summary>
        public void Gate(bool untilLockRemoved, bool untilPanelUnscrewed, string prompt)
        {
            needsLockRemoved = untilLockRemoved;
            needsPanelUnscrewed = untilPanelUnscrewed;
            lockedPrompt = prompt;
        }

        /// <summary>
        /// Configuration entry point for the scene generator, which has no inspector
        /// to fill these in from.
        /// </summary>
        public void Configure(string closed, string open, Vector3 axis, float angle,
                              float speed, Collider passageBlocker)
        {
            closedVerb = closed;
            openVerb = open;
            hingeAxis = axis;
            openAngle = angle;
            degreesPerSecond = speed;
            blocker = passageBlocker;
        }
    }
}
