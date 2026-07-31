using UnityEngine;
using Freedome.Settings;

namespace Freedome.Player
{
    /// <summary>
    /// Mouse look. Yaw turns the body so movement always follows the view; pitch is
    /// applied to the camera pivot alone and clamped just short of vertical.
    ///
    /// Sensitivity is frame-rate independent because mouse deltas are already
    /// per-frame movement - multiplying them by Time.deltaTime is the classic bug
    /// that makes aiming feel different at 60 and 144 fps, so it is not done here.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float pitchLimit = 88f;
        [SerializeField] private float smoothing = 0.03f;

        private float _pitch;
        private Vector2 _smoothedDelta;

        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            if (cameraPivot == null)
            {
                Camera child = GetComponentInChildren<Camera>();
                if (child != null)
                {
                    cameraPivot = child.transform;
                }
            }
        }

        private void OnEnable()
        {
            LockCursor(true);
        }

        private void Update()
        {
            if (!InputEnabled)
            {
                _smoothedDelta = Vector2.zero;
                return;
            }

            float sensitivity = GameSettings.MouseSensitivity;
            Vector2 raw = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * sensitivity;

            // A touch of smoothing takes the stair-stepping off low-poll-rate mice
            // without adding perceptible lag.
            float t = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothing);
            _smoothedDelta = Vector2.Lerp(_smoothedDelta, raw, t);

            transform.Rotate(Vector3.up, _smoothedDelta.x, Space.World);

            float pitchDelta = GameSettings.InvertLook ? _smoothedDelta.y : -_smoothedDelta.y;
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -pitchLimit, pitchLimit);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ResetPitch()
        {
            _pitch = 0f;
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }
        }
    }
}
