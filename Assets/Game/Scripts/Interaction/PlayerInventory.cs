using System.Collections.Generic;
using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Six slots you can put shed objects into and take them out of again.
    ///
    /// This is a container and nothing more. No item has an effect, none can be
    /// combined with another, none is required for anything, and nothing is
    /// tracked once it leaves. Adding an inventory reverses the milestone's
    /// original "no inventory" line - see DECISIONS.md - but it does not carry the
    /// rest of that boundary with it: there is still no objective in this project,
    /// and picking something up still changes nothing except where the object is.
    ///
    /// The selected slot is held in the player's hand, which is why selecting is
    /// separate from storing: an inventory you cannot see the contents of in the
    /// world would make the shed's objects feel like data rather than things.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int Capacity = 6;

        private readonly List<Carryable> _slots = new List<Carryable>(Capacity);
        private int _selected = -1;

        /// <summary>Fires when the contents or the selection change, for the HUD.</summary>
        public event System.Action Changed;

        public int Count
        {
            get { return _slots.Count; }
        }

        public bool IsFull
        {
            get { return _slots.Count >= Capacity; }
        }

        public int SelectedIndex
        {
            get { return _selected; }
        }

        public Carryable Selected
        {
            get { return _selected >= 0 && _selected < _slots.Count ? _slots[_selected] : null; }
        }

        public Carryable At(int index)
        {
            return index >= 0 && index < _slots.Count ? _slots[index] : null;
        }

        public IReadOnlyList<Carryable> Slots
        {
            get { return _slots; }
        }

        public bool Add(Carryable item)
        {
            if (item == null || IsFull || _slots.Contains(item))
            {
                return false;
            }

            _slots.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(Carryable item)
        {
            int index = _slots.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            _slots.RemoveAt(index);

            // Keep the selection pointing at something sensible rather than sliding
            // silently onto whatever moved up into the gap.
            if (_selected > index)
            {
                _selected--;
            }
            else if (_selected == index)
            {
                _selected = -1;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Selects a slot, or clears the selection when given the same one.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return;
            }

            _selected = _selected == index ? -1 : index;
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            if (_selected < 0)
            {
                return;
            }

            _selected = -1;
            Changed?.Invoke();
        }
    }
}
