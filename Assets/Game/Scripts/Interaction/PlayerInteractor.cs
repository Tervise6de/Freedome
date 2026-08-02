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

                // A tool-gated fixture describes itself differently depending on
                // what is in your hand, and only offers the key when you can act.
                if (_focus is ToolGatedFixture fixture)
                {
                    string text = fixture.PromptFor(_carried);
                    return text == fixture.Prompt ? text : $"[{useKey}] {text}";
                }

                // Same shape for the mower: the filler cap states its condition until
                // you are holding something that can change it.
                if (_focus is MowerControl control)
                {
                    string text = control.PromptFor(_carried);
                    return text == control.Prompt && text != "Pull the starter" &&
                           text != "Stop the engine"
                        ? text
                        : $"[{useKey}] {text}";
                }

                // A locked thing states its condition; there is nothing to press.
                if (_focus is HingedPart hinged && hinged.IsBlocked)
                {
                    return hinged.Prompt;
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

        /// <summary>
        /// Somewhere in front of the player's feet with room for the object.
        ///
        /// The old version tested one spot and, if it was occupied, fell back to the
        /// player's own position - which is inside the character capsule, so a bucket
        /// dropped facing a wall was spawned inside the person dropping it and then
        /// shoved out by whichever contact resolved first. It also counted the
        /// player's own collider as an obstruction, so that fallback fired far more
        /// often than it looked like it would. Now it walks in and keeps the last
        /// clear spot, and the shortest step it will accept still clears the capsule.
        /// </summary>
        private Vector3 FindDropSpot(Vector3 forward)
        {
            Collider self = GetComponent<Collider>();

            for (float d = dropClearance; d >= 0.35f; d -= 0.10f)
            {
                Vector3 candidate = transform.position + (forward * d) + (Vector3.up * 0.12f);
                if (IsClear(candidate, self))
                {
                    return candidate;
                }
            }

            // Nowhere in front is clear - pressed into a corner, say. Put it down
            // just above the floor at arm's length anyway rather than inside the
            // player: the physics will settle it, and it is still reachable.
            return transform.position + (forward * 0.35f) + (Vector3.up * 0.05f);
        }

        private static bool IsClear(Vector3 at, Collider self)
        {
            Collider[] hits = Physics.OverlapSphere(at, 0.12f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in hits)
            {
                if (c != self && !c.isTrigger)
                {
                    return false;
                }
            }

            return true;
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

            Vector3 target = FindDropSpot(forward);

            Carryable dropped = _carried;
            _carried = null;

            _inventory.Remove(dropped);
            dropped.Unstow();
            dropped.Release(target);
        }
    }
}
