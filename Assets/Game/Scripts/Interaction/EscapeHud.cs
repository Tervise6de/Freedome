using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The one line the game says when you get out.
    ///
    /// No score, no time, no rating, no "chapter complete". The shed is behind
    /// you and that is the whole reward. Esc still opens the pause menu, which is
    /// where restarting lives - the same place it always was.
    /// </summary>
    public sealed class EscapeHud : MonoBehaviour
    {
        private EscapeState _state;
        private GUIStyle _style;
        private Texture2D _fill;
        private float _shownAt = -1f;

        private void Awake()
        {
            _state = EscapeState.Find();
            if (_state != null)
            {
                _state.Changed += OnChanged;
            }
        }

        private void OnDestroy()
        {
            if (_state != null)
            {
                _state.Changed -= OnChanged;
            }

            if (_fill != null)
            {
                Destroy(_fill);
            }
        }

        private void OnChanged()
        {
            _shownAt = _state != null && _state.Escaped ? Time.unscaledTime : -1f;
        }

        private void OnGUI()
        {
            if (_state == null || !_state.Escaped || _shownAt < 0f)
            {
                return;
            }

            if (_fill == null)
            {
                _fill = new Texture2D(1, 1);
                _fill.SetPixel(0, 0, Color.white);
                _fill.Apply();
            }

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.95f, 0.90f) },
            };

            // Fades up over a second and a half rather than snapping in.
            float t = Mathf.Clamp01((Time.unscaledTime - _shownAt) / 1.5f);

            Rect band = new Rect(0f, (Screen.height * 0.5f) - 44f, Screen.width, 88f);
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.45f * t);
            GUI.DrawTexture(band, _fill);

            GUI.color = new Color(1f, 1f, 1f, t);
            GUI.Label(band, "You are out.", _style);
            GUI.color = previous;
        }
    }
}
