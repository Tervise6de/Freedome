using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The ground outside the door.
    ///
    /// A trigger rather than a prompt: you get out by walking out, not by looking
    /// at the doorway and pressing a key. The last beat of an escape is the wrong
    /// place to ask for one more keypress.
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
            if (_state == null || !_state.LockRemoved)
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
