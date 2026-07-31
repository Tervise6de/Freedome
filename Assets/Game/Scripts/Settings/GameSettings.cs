using UnityEngine;

namespace Freedome.Settings
{
    /// <summary>
    /// Player-facing settings, backed by PlayerPrefs so they survive a restart of
    /// the demo. Everything is exposed through properties that clamp on write, so a
    /// hand-edited preference file cannot put the game into an unusable state.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMouseSensitivity = "freedome.mouseSensitivity";
        private const string KeyInvertY = "freedome.invertY";
        private const string KeyFieldOfView = "freedome.fieldOfView";
        private const string KeyHeadBob = "freedome.headBob";
        private const string KeyMotionBlur = "freedome.motionBlur";
        private const string KeyQualityLevel = "freedome.qualityLevel";
        private const string KeyVSync = "freedome.vsync";

        public const float MinMouseSensitivity = 0.2f;
        public const float MaxMouseSensitivity = 6.0f;
        public const float DefaultMouseSensitivity = 2.0f;

        public const float MinFieldOfView = 60f;
        public const float MaxFieldOfView = 100f;
        public const float DefaultFieldOfView = 75f;

        /// <summary>Raised whenever any setting changes so listeners can reapply.</summary>
        public static event System.Action Changed;

        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity),
                               MinMouseSensitivity, MaxMouseSensitivity);
            set
            {
                PlayerPrefs.SetFloat(KeyMouseSensitivity,
                    Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity));
                Notify();
            }
        }

        public static bool InvertLook
        {
            get => PlayerPrefs.GetInt(KeyInvertY, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(KeyInvertY, value ? 1 : 0);
                Notify();
            }
        }

        public static float FieldOfView
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(KeyFieldOfView, DefaultFieldOfView),
                               MinFieldOfView, MaxFieldOfView);
            set
            {
                PlayerPrefs.SetFloat(KeyFieldOfView, Mathf.Clamp(value, MinFieldOfView, MaxFieldOfView));
                Notify();
            }
        }

        /// <summary>0 disables head bob entirely; 1 is the authored amount.</summary>
        public static float HeadBobIntensity
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyHeadBob, 0.6f));
            set
            {
                PlayerPrefs.SetFloat(KeyHeadBob, Mathf.Clamp01(value));
                Notify();
            }
        }

        public static bool MotionBlur
        {
            get => PlayerPrefs.GetInt(KeyMotionBlur, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(KeyMotionBlur, value ? 1 : 0);
                Notify();
            }
        }

        public static int QualityLevel
        {
            get
            {
                int stored = PlayerPrefs.GetInt(KeyQualityLevel, QualitySettings.GetQualityLevel());
                return Mathf.Clamp(stored, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            }
            set
            {
                int clamped = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                PlayerPrefs.SetInt(KeyQualityLevel, clamped);
                Notify();
            }
        }

        public static bool VSync
        {
            get => PlayerPrefs.GetInt(KeyVSync, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(KeyVSync, value ? 1 : 0);
                Notify();
            }
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyMouseSensitivity);
            PlayerPrefs.DeleteKey(KeyInvertY);
            PlayerPrefs.DeleteKey(KeyFieldOfView);
            PlayerPrefs.DeleteKey(KeyHeadBob);
            PlayerPrefs.DeleteKey(KeyMotionBlur);
            PlayerPrefs.DeleteKey(KeyQualityLevel);
            PlayerPrefs.DeleteKey(KeyVSync);
            Notify();
        }

        private static void Notify()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
