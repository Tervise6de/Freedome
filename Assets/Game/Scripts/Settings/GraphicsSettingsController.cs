using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Freedome.Settings
{
    /// <summary>
    /// Applies the values in <see cref="GameSettings"/> to the things that actually
    /// render: the camera's field of view, the motion blur override in the scene's
    /// global volume, the quality level and vsync.
    ///
    /// It subscribes to GameSettings.Changed, so the settings menu only has to write
    /// a value and everything downstream follows.
    /// </summary>
    public sealed class GraphicsSettingsController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Volume globalVolume;

        private MotionBlur _motionBlur;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (globalVolume == null)
            {
                globalVolume = FindAnyObjectByType<Volume>();
            }

            CacheOverrides();
        }

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Apply;
        }

        private void CacheOverrides()
        {
            if (globalVolume == null || globalVolume.profile == null)
            {
                return;
            }

            // Instantiating the profile means runtime changes do not dirty the asset
            // on disk when the demo is run from inside the editor.
            VolumeProfile runtimeProfile = globalVolume.profile;
            if (!runtimeProfile.TryGet(out _motionBlur))
            {
                _motionBlur = null;
            }
        }

        public void Apply()
        {
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = GameSettings.FieldOfView;
            }

            if (_motionBlur != null)
            {
                _motionBlur.active = GameSettings.MotionBlur;
            }

            int desiredQuality = GameSettings.QualityLevel;
            if (desiredQuality != QualitySettings.GetQualityLevel())
            {
                QualitySettings.SetQualityLevel(desiredQuality, true);
            }

            QualitySettings.vSyncCount = GameSettings.VSync ? 1 : 0;
            Application.targetFrameRate = GameSettings.VSync ? -1 : 60;
        }
    }
}
