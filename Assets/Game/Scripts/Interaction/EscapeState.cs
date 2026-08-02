using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The one place that knows how far along the way out the player is.
    ///
    /// This is the thing the environment milestone spent its whole life refusing
    /// to add - a flag that one object sets and another object reads. It exists
    /// now because the project is an escape game, and an escape needs a state that
    /// survives the object you are touching.
    ///
    /// Kept deliberately small: three booleans and an event. No score, no timer, no
    /// fail state, no branching. If this class starts growing a progression system,
    /// that is a new decision, not an extension of this one.
    /// </summary>
    public sealed class EscapeState : MonoBehaviour
    {
        /// <summary>The four screws holding the service panel down are out.</summary>
        public bool PanelUnscrewed { get; private set; }

        /// <summary>The skirt board under the floor has been levered off.</summary>
        public bool SkirtRemoved { get; private set; }

        /// <summary>The player has crawled out.</summary>
        public bool Escaped { get; private set; }

        /// <summary>Fires whenever any of the above changes, for the HUD.</summary>
        public event System.Action Changed;

        public static EscapeState Find()
        {
            return FindAnyObjectByType<EscapeState>();
        }

        public void SetPanelUnscrewed()
        {
            if (PanelUnscrewed)
            {
                return;
            }

            PanelUnscrewed = true;
            Changed?.Invoke();
        }

        public void SetSkirtRemoved()
        {
            if (SkirtRemoved)
            {
                return;
            }

            SkirtRemoved = true;
            Changed?.Invoke();
        }

        public void SetEscaped()
        {
            if (Escaped || !SkirtRemoved)
            {
                return;
            }

            Escaped = true;
            Changed?.Invoke();
        }

        /// <summary>Puts everything back. Used by the pause menu's restart.</summary>
        public void Reset()
        {
            PanelUnscrewed = false;
            SkirtRemoved = false;
            Escaped = false;
            Changed?.Invoke();
        }
    }
}
