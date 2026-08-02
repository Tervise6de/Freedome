using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The gap under the floor, once the skirt board is off.
    ///
    /// A trigger rather than a prompt: you get out by going there, not by looking
    /// at it and pressing a key. Crawling out of a hole should feel like moving,
    /// and the last beat of an escape is the wrong place to ask for one more
    /// keypress.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EscapeExit : MonoBehaviour
    {
        private EscapeState _state;

        private void Awake()
        {
            _state = EscapeState.Find();
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_state == null || !_state.SkirtRemoved)
            {
                return;
            }

            if (other.GetComponentInParent<Freedome.Player.FirstPersonController>() == null)
            {
                return;
            }

            _state.SetEscaped();
        }
    }
}
