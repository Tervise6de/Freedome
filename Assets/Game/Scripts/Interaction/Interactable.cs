using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Base for anything the player can look at and use.
    ///
    /// The rule this milestone keeps: an interactable does something *physical* and
    /// nothing else. A door swings, a panel lifts, a switch switches. None of them
    /// report to a goal, unlock anything, or advance any state outside themselves.
    /// There is no objective in this project and adding one is a separate decision.
    ///
    /// Nothing here highlights itself either. The prompt appears only when the
    /// player is already looking directly at the object from arm's reach; there are
    /// no outlines, no glows and no world-space markers.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>Verb shown in the prompt, e.g. "Open" or "Light switch".</summary>
        public abstract string Prompt { get; }

        /// <summary>
        /// False while the object is mid-animation or otherwise busy. The prompt is
        /// hidden rather than shown greyed out - a prompt you cannot act on reads as
        /// a puzzle, which is exactly the impression this milestone avoids.
        /// </summary>
        public virtual bool CanInteract
        {
            get { return true; }
        }

        public abstract void Interact(PlayerInteractor actor);
    }
}
