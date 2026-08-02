using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The mower's engine: whether there is petrol in it, and whether it is running.
    ///
    /// Deliberately not part of <see cref="EscapeState"/>. Nothing about the way out
    /// of this shed goes through the mower, and the moment engine state lives in the
    /// same object as the door state, somebody will be tempted to make it. This is
    /// the same kind of thing as the service panel: it works, it rewards looking, and
    /// it leads nowhere.
    ///
    /// There is no audio in this project, so a running engine is a vibration and a
    /// changed prompt. That is the honest limit of what the mower can currently do,
    /// not a stylistic choice.
    /// </summary>
    public sealed class MowerEngine : MonoBehaviour
    {
        [SerializeField] private float shakeAmplitude = 0.0016f;
        [SerializeField] private float shakeHertz = 27f;

        /// <summary>Petrol in the tank. One fill; there is nothing to run it dry.</summary>
        public bool HasFuel { get; private set; }

        public bool IsRunning { get; private set; }

        /// <summary>Fires on fuelling, starting and stopping, for anything watching.</summary>
        public event System.Action Changed;

        private Vector3 _restPosition;
        private float _phase;

        private void Awake()
        {
            _restPosition = transform.localPosition;
        }

        public void Fill()
        {
            if (HasFuel)
            {
                return;
            }

            HasFuel = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Pull the cord. Returns true if it caught.
        ///
        /// A dry engine turns over and dies, which is the whole feedback: the player
        /// hears nothing and sees nothing change, and the prompt still says the tank
        /// is dry when they look at the filler. Nothing tells them to go and find
        /// petrol, in the same way nothing tells them to go and find a screwdriver.
        /// </summary>
        public bool TryStart()
        {
            if (IsRunning || !HasFuel)
            {
                return false;
            }

            IsRunning = true;
            _phase = 0f;
            Changed?.Invoke();
            return true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            transform.localPosition = _restPosition;
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            // A small-engine idle is fast and shallow. Two frequencies an octave
            // apart, so it reads as an engine rather than as a sine wave.
            _phase += Time.deltaTime * shakeHertz * Mathf.PI * 2f;

            float x = Mathf.Sin(_phase) * shakeAmplitude;
            float y = Mathf.Sin(_phase * 2.1f) * shakeAmplitude * 0.6f;

            transform.localPosition = _restPosition + new Vector3(x, y, x * 0.4f);
        }
    }
}
