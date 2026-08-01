using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The reticle and the interaction prompt.
    ///
    /// Drawn in OnGUI rather than on the pause menu's canvas, for the same reason
    /// the performance overlay is: it needs no scene wiring, so the generator does
    /// not have to build and lay out a second canvas that exists to show one line
    /// of text.
    ///
    /// The reticle is a dot, and it does not change shape, colour or size when
    /// something is in reach - the prompt appearing is the whole feedback. A
    /// reticle that reacts to objects is how a room starts telling the player what
    /// matters, which is the thing this milestone is meant not to do.
    /// </summary>
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InteractionHud : MonoBehaviour
    {
        [SerializeField] private float reticleSize = 3f;

        private PlayerInteractor _interactor;
        private GUIStyle _style;
        private Texture2D _dot;

        private void Awake()
        {
            _interactor = GetComponent<PlayerInteractor>();
        }

        private void OnDestroy()
        {
            if (_dot != null)
            {
                Destroy(_dot);
            }
        }

        private void OnGUI()
        {
            if (_dot == null)
            {
                _dot = new Texture2D(1, 1);
                _dot.SetPixel(0, 0, Color.white);
                _dot.Apply();
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(cx - (reticleSize * 0.5f), cy - (reticleSize * 0.5f),
                                     reticleSize, reticleSize), _dot);
            GUI.color = previous;

            string prompt = _interactor.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.96f, 0.93f) },
            };

            Vector2 size = _style.CalcSize(new GUIContent(prompt));
            Rect box = new Rect(cx - (size.x * 0.5f) - 10f, cy + 26f, size.x + 20f, size.y + 10f);

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(box, _dot);
            GUI.color = Color.white;
            GUI.Label(box, prompt, _style);
        }
    }
}
