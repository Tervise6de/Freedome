using UnityEngine;
using Freedome.Settings;

namespace Freedome.Player
{
    /// <summary>
    /// Walking head bob, applied as a small offset on top of whatever height the
    /// controller has set for the camera.
    ///
    /// The amplitude is small on purpose. Bob exists here to sell the walking pace
    /// and give the room a sense of scale, not to be noticed; players who dislike it
    /// can take it to zero in the settings menu, which disables the effect entirely.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HeadBob : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float bobFrequency = 1.75f;   // steps per metre travelled
        [SerializeField] private float verticalAmplitude = 0.022f;
        [SerializeField] private float horizontalAmplitude = 0.014f;
        [SerializeField] private float rollAmplitude = 0.35f;  // degrees
        [SerializeField] private float settleSpeed = 6f;

        private FirstPersonController _controller;
        private float _phase;
        private float _weight;

        private void Awake()
        {
            _controller = GetComponent<FirstPersonController>();

            if (cameraPivot == null)
            {
                Camera child = GetComponentInChildren<Camera>();
                if (child != null)
                {
                    cameraPivot = child.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (cameraPivot == null)
            {
                return;
            }

            float intensity = GameSettings.HeadBobIntensity;

            // Phase advances with distance travelled, so bob stays in step with the
            // player's feet whether they are walking or crouch-walking.
            float speed = _controller.CurrentSpeed;
            bool moving = speed > 0.05f && _controller.IsGrounded;

            if (moving)
            {
                _phase += speed * bobFrequency * Mathf.PI * Time.deltaTime;
            }

            float targetWeight = moving ? 1f : 0f;
            _weight = Mathf.MoveTowards(_weight, targetWeight, settleSpeed * Time.deltaTime);

            float amount = _weight * intensity;
            if (amount <= 0.0001f)
            {
                return;
            }

            float vertical = Mathf.Sin(_phase * 2f) * verticalAmplitude * amount;
            float horizontal = Mathf.Sin(_phase) * horizontalAmplitude * amount;
            float roll = Mathf.Sin(_phase) * rollAmplitude * amount;

            // Both of these compose onto values written earlier the same frame -
            // y by FirstPersonController.UpdateCameraHeight, the rotation by
            // PlayerLook.Update - and both of those write absolutely, every frame.
            // Nothing here may become the sole author of a channel, or the offset
            // accumulates instead of being an offset.
            Vector3 local = cameraPivot.localPosition;
            local.y += vertical;
            local.x = horizontal;
            cameraPivot.localPosition = local;

            cameraPivot.localRotation *= Quaternion.Euler(0f, 0f, roll);
        }
    }
}
