using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Marks an object the player can pick up and put down.
    ///
    /// One at a time, held in front of the camera, dropped where the player is
    /// standing. There is deliberately no inventory, no stacking, no combining and
    /// no throwing - "pick it up and put it down somewhere else" is the whole of it.
    /// A shed where you can move the paint tin off the bench is a shed you believe
    /// somebody uses; anything past that is a different project.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Carryable : Interactable
    {
        [SerializeField] private string displayName = "Object";
        [SerializeField] private Vector3 holdOffset = new Vector3(0.28f, -0.20f, 0.52f);
        [SerializeField] private Vector3 holdEuler = Vector3.zero;

        // Serialized, not a plain field: the generator sets this at build time and
        // it has to survive being written into the scene and loaded back.
        [SerializeField] private bool restingInContainer;

        private Rigidbody _body;
        private Transform _originalParent;

        public string DisplayName
        {
            get { return displayName; }
        }

        public Vector3 HoldOffset
        {
            get { return holdOffset; }
        }

        public Vector3 HoldEuler
        {
            get { return holdEuler; }
        }

        public bool IsHeld { get; private set; }

        public override string Prompt
        {
            get { return $"Pick up {displayName}"; }
        }

        public override bool CanInteract
        {
            get { return !IsHeld; }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _originalParent = transform.parent;

            if (restingInContainer)
            {
                // Kinematic so it rides the drawer rather than being dragged through
                // the drawer bottom by physics every time the drawer moves. It becomes
                // an ordinary dynamic body the first time it is put down.
                _body.isKinematic = true;
                _body.useGravity = false;
            }
        }

        /// <summary>
        /// Marks an object as sitting inside something that moves - a drawer. Set by
        /// the generator before the scene is saved.
        /// </summary>
        public void SetRestingInContainer(bool resting)
        {
            restingInContainer = resting;
        }

        public override void Interact(PlayerInteractor actor)
        {
            actor.TryCarry(this);
        }

        /// <summary>Called by the interactor. Not a general-purpose API.</summary>
        internal void AttachTo(Transform anchor)
        {
            gameObject.SetActive(true);
            IsHeld = true;

            // Kinematic while carried. A held rigidbody driven by physics fights the
            // character controller and ends up jittering or shoving the player through
            // walls; parenting it is duller and behaves.
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.detectCollisions = false;

            transform.SetParent(anchor, false);
            transform.localPosition = holdOffset;
            transform.localRotation = Quaternion.Euler(holdEuler);
        }

        /// <summary>
        /// Put out of sight in the inventory. The object is kept, not destroyed -
        /// it is the same tin of screws when it comes back out.
        /// </summary>
        internal void Stow(Transform holder)
        {
            IsHeld = false;
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.detectCollisions = false;

            transform.SetParent(holder, false);
            transform.localPosition = Vector3.zero;
            gameObject.SetActive(false);
        }

        internal void Unstow()
        {
            gameObject.SetActive(true);
        }

        internal void Release(Vector3 position)
        {
            // Once it has been handled it is loose, wherever it started.
            restingInContainer = false;

            transform.SetParent(_originalParent, true);
            transform.position = position;

            _body.isKinematic = false;
            _body.useGravity = true;
            _body.detectCollisions = true;
            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;

            IsHeld = false;
        }

        public void Configure(string name, Vector3 offset, Vector3 euler)
        {
            displayName = name;
            holdOffset = offset;
            holdEuler = euler;
        }
    }
}
