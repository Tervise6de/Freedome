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
        [SerializeField] private KeyCode dropKey = KeyCode.G;
        [SerializeField] private float dropClearance = 0.55f;

        private Transform _carryAnchor;
        private Transform _stowPoint;
        private PlayerInventory _inventory;
        private Carryable _carried;
        private Interactable _focus;

        public bool InputEnabled { get; set; } = true;

        /// <summary>What the prompt should say, or null when there is nothing to say.</summary>
        public string CurrentPrompt { get; private set; }

        public Carryable Carried
        {
            get { return _carried; }
        }

        public PlayerInventory Inventory
        {
            get { return _inventory; }
        }

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            if (_inventory == null)
            {
                _inventory = gameObject.AddComponent<PlayerInventory>();
            }
            _inventory.Changed += OnInventoryChanged;

            // Stowed objects park here, deactivated. Keeping them parented to the
            // player rather than leaving them where they were picked up means the
            // scene hierarchy still says who has what.
            GameObject stow = new GameObject("Stowed");
            stow.transform.SetParent(transform, false);
            _stowPoint = stow.transform;

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

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= OnInventoryChanged;
            }
        }

        private void Update()
        {
            if (!InputEnabled)
            {
                CurrentPrompt = null;
                return;
            }

            ReadSlotKeys();

            _focus = FindFocus();
            CurrentPrompt = BuildPrompt();

            if (Input.GetKeyDown(dropKey))
            {
                Drop();
                return;
            }

            if (Input.GetKeyDown(useKey) && _focus != null && _focus.CanInteract)
            {
                _focus.Interact(this);
            }
        }

        private void ReadSlotKeys()
        {
            for (int i = 0; i < PlayerInventory.Capacity; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _inventory.Select(i);
                }
            }
        }

        /// <summary>
        /// Puts whatever the inventory has selected into the player's hand, and
        /// stows whatever was there before.
        /// </summary>
        private void OnInventoryChanged()
        {
            Carryable wanted = _inventory.Selected;
            if (wanted == _carried)
            {
                return;
            }

            if (_carried != null)
            {
                _carried.Stow(_stowPoint);
                _carried = null;
            }

            if (wanted != null && _carryAnchor != null)
            {
                wanted.Unstow();
                wanted.AttachTo(_carryAnchor);
                _carried = wanted;
            }
        }

        private string BuildPrompt()
        {
            if (_focus != null && _focus.CanInteract)
            {
                if (_focus is Carryable && _inventory.IsFull)
                {
                    return "Your hands are full";
                }
                return $"[{useKey}] {_focus.Prompt}";
            }

            if (_carried != null)
            {
                return $"[{dropKey}] Put down {_carried.DisplayName}";
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

        /// <summary>
        /// Takes an item into the inventory and puts it straight into the hand, which
        /// is what "pick it up" should feel like. Silently does nothing when full -
        /// the prompt has already said so.
        /// </summary>
        public void TryCarry(Carryable item)
        {
            if (item == null || _carryAnchor == null || item == _carried)
            {
                return;
            }

            if (!_inventory.Add(item))
            {
                return;
            }

            // Stow it first so the selection change below has something consistent to
            // pick up, whatever was previously in hand.
            item.Stow(_stowPoint);
            _inventory.Select(_inventory.Count - 1);
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

            Carryable dropped = _carried;
            _carried = null;

            _inventory.Remove(dropped);
            dropped.Unstow();
            dropped.Release(target);
        }
    }
}
