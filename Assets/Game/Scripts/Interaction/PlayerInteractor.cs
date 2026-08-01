using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Looks for something to use, and carries one thing at a time.
    ///
    /// Reach is short on purpose. At 2.2 m you have to walk up to the door to open
    /// it, which is what makes the room read as a space you are standing in rather
    /// than a menu of objects. It also means the prompt is almost never ambiguous:
    /// at that range, one thing fills the centre of the screen.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float reach = 2.2f;
        [SerializeField] private KeyCode useKey = KeyCode.E;
        [SerializeField] private float dropClearance = 0.55f;

        private Transform _carryAnchor;
        private Carryable _carried;
        private Interactable _focus;

        public bool InputEnabled { get; set; } = true;

        /// <summary>What the prompt should say, or null when there is nothing to say.</summary>
        public string CurrentPrompt { get; private set; }

        public Carryable Carried
        {
            get { return _carried; }
        }

        private void Awake()
        {
            if (cameraPivot == null)
            {
                Camera child = GetComponentInChildren<Camera>();
                if (child != null)
                {
                    cameraPivot = child.transform;
                }
            }

            if (cameraPivot != null)
            {
                // A child of the camera so a held object tracks head movement without
                // any per-frame follow code, and so it inherits head bob for free.
                GameObject anchor = new GameObject("CarryAnchor");
                anchor.transform.SetParent(cameraPivot, false);
                _carryAnchor = anchor.transform;
            }
        }

        private void Update()
        {
            if (!InputEnabled)
            {
                CurrentPrompt = null;
                return;
            }

            _focus = FindFocus();
            CurrentPrompt = BuildPrompt();

            if (!Input.GetKeyDown(useKey))
            {
                return;
            }

            if (_carried != null)
            {
                Drop();
            }
            else if (_focus != null && _focus.CanInteract)
            {
                _focus.Interact(this);
            }
        }

        private string BuildPrompt()
        {
            if (_carried != null)
            {
                return $"[{useKey}] Put down {_carried.DisplayName}";
            }

            if (_focus != null && _focus.CanInteract)
            {
                return $"[{useKey}] {_focus.Prompt}";
            }

            return null;
        }

        private Interactable FindFocus()
        {
            if (cameraPivot == null)
            {
                return null;
            }

            if (!Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit,
                                 reach, ~0, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            // GetComponentInParent, not GetComponent: the collider is usually on the
            // leaf mesh while the behaviour sits on the hinge above it.
            return hit.collider.GetComponentInParent<Interactable>();
        }

        /// <summary>Picks up an item, swapping it for whatever is already held.</summary>
        public void TryCarry(Carryable item)
        {
            if (item == null || _carryAnchor == null || item == _carried)
            {
                return;
            }

            if (_carried != null)
            {
                Drop();
            }

            _carried = item;
            item.AttachTo(_carryAnchor);
        }

        public void Drop()
        {
            if (_carried == null)
            {
                return;
            }

            // Put it down in front of the player's feet rather than at head height,
            // so it settles instead of falling past them.
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            Vector3 target = transform.position + (forward * dropClearance) + (Vector3.up * 0.12f);

            // If that spot is inside something, drop it at the player's own feet
            // instead. Crude, but it cannot wedge an object into a wall.
            if (Physics.CheckSphere(target, 0.12f, ~0, QueryTriggerInteraction.Ignore))
            {
                target = transform.position + (Vector3.up * 0.12f);
            }

            _carried.Release(target);
            _carried = null;
        }
    }
}
