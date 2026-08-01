using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// A row of six slots along the bottom of the screen.
    ///
    /// Deliberately plain: numbered boxes and the object's name, no icons, no
    /// rarity colours, no weight, no counts. The shed has no economy and the
    /// inventory has no rules, so a UI that implied either would be lying about
    /// what the project is.
    ///
    /// Drawn in OnGUI for the same reason the other overlays are - it needs no
    /// canvas built and laid out by the scene generator.
    /// </summary>
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InventoryHud : MonoBehaviour
    {
        [SerializeField] private float slotWidth = 116f;
        [SerializeField] private float slotHeight = 30f;
        [SerializeField] private float margin = 18f;

        private PlayerInteractor _interactor;
        private GUIStyle _slotStyle;
        private Texture2D _fill;

        private void Awake()
        {
            _interactor = GetComponent<PlayerInteractor>();
        }

        private void OnDestroy()
        {
            if (_fill != null)
            {
                Destroy(_fill);
            }
        }

        private void OnGUI()
        {
            PlayerInventory inventory = _interactor.Inventory;
            if (inventory == null)
            {
                return;
            }

            if (_fill == null)
            {
                _fill = new Texture2D(1, 1);
                _fill.SetPixel(0, 0, Color.white);
                _fill.Apply();
            }

            _slotStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                normal = { textColor = new Color(0.94f, 0.94f, 0.90f) },
            };

            float totalWidth = (slotWidth * PlayerInventory.Capacity) +
                               (6f * (PlayerInventory.Capacity - 1));
            float x = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - slotHeight - margin;

            Color previous = GUI.color;

            for (int i = 0; i < PlayerInventory.Capacity; i++)
            {
                Rect slot = new Rect(x + (i * (slotWidth + 6f)), y, slotWidth, slotHeight);
                Carryable item = inventory.At(i);
                bool selected = i == inventory.SelectedIndex;

                GUI.color = selected
                    ? new Color(0.85f, 0.80f, 0.62f, 0.35f)
                    : new Color(0f, 0f, 0f, 0.38f);
                GUI.DrawTexture(slot, _fill);

                GUI.color = Color.white;
                string label = item == null ? $"{i + 1}" : $"{i + 1}  {item.DisplayName}";
                GUI.Label(slot, label, _slotStyle);
            }

            GUI.color = previous;
        }
    }
}
