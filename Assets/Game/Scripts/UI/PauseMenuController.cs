using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Freedome.Player;
using Freedome.Interaction;
using Freedome.Settings;

namespace Freedome.UI
{
    /// <summary>
    /// Escape-key pause menu with a settings page, built entirely from code.
    ///
    /// Generating the UI rather than shipping a prefab keeps the whole interface in
    /// one reviewable file and means the scene generator does not have to wire up
    /// dozens of serialized references it could get wrong. It is a small menu -
    /// resume, settings, restart, quit - which is all this milestone needs.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.08f, 0.88f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color TextColor = new Color(0.90f, 0.89f, 0.86f, 1f);
        private static readonly Color ButtonColor = new Color(0.20f, 0.20f, 0.22f, 1f);
        private static readonly Color ButtonHighlight = new Color(0.30f, 0.31f, 0.33f, 1f);

        private FirstPersonController _player;
        private PlayerLook _look;
        private PlayerInteractor _interactor;
        private GraphicsSettingsController _graphics;

        private GameObject _root;
        private GameObject _mainPage;
        private GameObject _settingsPage;
        private Font _font;

        private bool _paused;

        public bool IsPaused => _paused;

        private void Awake()
        {
            _player = FindAnyObjectByType<FirstPersonController>();
            _look = FindAnyObjectByType<PlayerLook>();
            _interactor = FindAnyObjectByType<PlayerInteractor>();
            _graphics = FindAnyObjectByType<GraphicsSettingsController>();

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            EnsureEventSystem();
            BuildUi();
            SetPaused(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_paused && _settingsPage != null && _settingsPage.activeSelf)
                {
                    ShowPage(false);
                }
                else
                {
                    SetPaused(!_paused);
                }
            }
        }

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public void SetPaused(bool paused)
        {
            _paused = paused;

            if (_root != null)
            {
                _root.SetActive(paused);
            }

            if (paused)
            {
                ShowPage(false);
            }

            Time.timeScale = paused ? 0f : 1f;
            PlayerLook.LockCursor(!paused);

            if (_player != null)
            {
                _player.InputEnabled = !paused;
            }
            if (_look != null)
            {
                _look.InputEnabled = !paused;
            }
            if (_interactor != null)
            {
                // Also clears the prompt, so the pause menu is not drawn over the top
                // of "[E] Open the door".
                _interactor.InputEnabled = !paused;
            }
        }

        private void ShowPage(bool settings)
        {
            if (_mainPage != null)
            {
                _mainPage.SetActive(!settings);
            }
            if (_settingsPage != null)
            {
                _settingsPage.SetActive(settings);
            }
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void BuildUi()
        {
            _root = new GameObject("PauseMenuCanvas");
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            GameObject dim = CreatePanel(_root.transform, "Dim", DimColor);
            Stretch(dim.GetComponent<RectTransform>());

            _mainPage = BuildMainPage(_root.transform);
            _settingsPage = BuildSettingsPage(_root.transform);
        }

        private GameObject BuildMainPage(Transform parent)
        {
            GameObject page = CreatePanel(parent, "MainPage", PanelColor);
            RectTransform rt = page.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 520f);
            rt.anchoredPosition = Vector2.zero;

            CreateLabel(page.transform, "Title", "Paused", 46, TextAnchor.MiddleCenter,
                        new Vector2(0f, 190f), new Vector2(520f, 70f));

            CreateLabel(page.transform, "Subtitle", "Shed Room Demo", 20, TextAnchor.MiddleCenter,
                        new Vector2(0f, 140f), new Vector2(520f, 34f));

            CreateButton(page.transform, "Resume", "Resume", new Vector2(0f, 60f), () => SetPaused(false));
            CreateButton(page.transform, "Settings", "Settings", new Vector2(0f, -10f), () => ShowPage(true));
            CreateButton(page.transform, "Restart", "Restart demo", new Vector2(0f, -80f), Restart);
            CreateButton(page.transform, "Quit", "Quit", new Vector2(0f, -150f), Quit);

            CreateLabel(page.transform, "Controls",
                        "WASD move    Mouse look    Ctrl or C crouch    Esc pause",
                        17, TextAnchor.MiddleCenter, new Vector2(0f, -218f), new Vector2(520f, 30f));

            return page;
        }

        private GameObject BuildSettingsPage(Transform parent)
        {
            GameObject page = CreatePanel(parent, "SettingsPage", PanelColor);
            RectTransform rt = page.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720f, 660f);
            rt.anchoredPosition = Vector2.zero;

            CreateLabel(page.transform, "Title", "Settings", 38, TextAnchor.MiddleCenter,
                        new Vector2(0f, 270f), new Vector2(660f, 60f));

            float y = 190f;
            const float Step = 62f;

            CreateSlider(page.transform, "Mouse sensitivity", new Vector2(0f, y),
                         GameSettings.MinMouseSensitivity, GameSettings.MaxMouseSensitivity,
                         GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v, "0.0");
            y -= Step;

            CreateToggle(page.transform, "Invert vertical look", new Vector2(0f, y),
                         GameSettings.InvertLook, v => GameSettings.InvertLook = v);
            y -= Step;

            CreateSlider(page.transform, "Field of view", new Vector2(0f, y),
                         GameSettings.MinFieldOfView, GameSettings.MaxFieldOfView,
                         GameSettings.FieldOfView, v => GameSettings.FieldOfView = v, "0");
            y -= Step;

            CreateSlider(page.transform, "Head bob", new Vector2(0f, y), 0f, 1f,
                         GameSettings.HeadBobIntensity, v => GameSettings.HeadBobIntensity = v, "0.00");
            y -= Step;

            CreateToggle(page.transform, "Motion blur", new Vector2(0f, y),
                         GameSettings.MotionBlur, v => GameSettings.MotionBlur = v);
            y -= Step;

            CreateToggle(page.transform, "VSync", new Vector2(0f, y),
                         GameSettings.VSync, v => GameSettings.VSync = v);
            y -= Step;

            CreateQualityDropdown(page.transform, new Vector2(0f, y));
            y -= Step + 12f;

            CreateButton(page.transform, "Defaults", "Reset to defaults", new Vector2(-150f, y),
                         () =>
                         {
                             GameSettings.ResetToDefaults();
                             RebuildSettingsPage();
                         }, 280f);

            CreateButton(page.transform, "Back", "Back", new Vector2(160f, y), () => ShowPage(false), 200f);

            page.SetActive(false);
            return page;
        }

        /// <summary>
        /// Rebuilds the settings page after a reset so every control shows the value
        /// that is actually in effect.
        /// </summary>
        private void RebuildSettingsPage()
        {
            if (_settingsPage != null)
            {
                Destroy(_settingsPage);
            }

            _settingsPage = BuildSettingsPage(_root.transform);
            _settingsPage.SetActive(true);
            if (_mainPage != null)
            {
                _mainPage.SetActive(false);
            }

            if (_graphics != null)
            {
                _graphics.Apply();
            }
        }

        // ------------------------------------------------------------------
        // Widget helpers
        // ------------------------------------------------------------------

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = color;

            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Text CreateLabel(Transform parent, string name, string content, int size,
                                 TextAnchor anchor, Vector2 position, Vector2 size2)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size2;

            return text;
        }

        private void CreateButton(Transform parent, string name, string label, Vector2 position,
                                  UnityEngine.Events.UnityAction onClick, float width = 360f)
        {
            GameObject go = CreatePanel(parent, name, ButtonColor);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(width, 54f);

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            // ColorBlock multiplies the graphic's own colour, so these are tints.
            colors.highlightedColor = new Color(1.45f, 1.45f, 1.50f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            CreateLabel(go.transform, "Label", label, 22, TextAnchor.MiddleCenter,
                        Vector2.zero, new Vector2(width - 20f, 40f));
        }

        private void CreateSlider(Transform parent, string label, Vector2 position, float min, float max,
                                  float value, UnityEngine.Events.UnityAction<float> onChanged,
                                  string format)
        {
            CreateLabel(parent, label + "_Label", label, 20, TextAnchor.MiddleLeft,
                        position + new Vector2(-330f, 0f), new Vector2(300f, 34f));

            Text readout = CreateLabel(parent, label + "_Value", value.ToString(format), 20,
                                       TextAnchor.MiddleRight, position + new Vector2(300f, 0f),
                                       new Vector2(90f, 34f));

            GameObject go = new GameObject(label + "_Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position + new Vector2(80f, 0f);
            rt.sizeDelta = new Vector2(340f, 26f);

            GameObject background = CreatePanel(go.transform, "Background", new Color(0.15f, 0.15f, 0.16f, 1f));
            Stretch(background.GetComponent<RectTransform>());

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRt);
            fillAreaRt.offsetMin = new Vector2(0f, 6f);
            fillAreaRt.offsetMax = new Vector2(0f, -6f);

            GameObject fill = CreatePanel(fillArea.transform, "Fill", new Color(0.55f, 0.56f, 0.52f, 1f));
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = new Vector2(10f, 0f);

            GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());

            GameObject handle = CreatePanel(handleArea.transform, "Handle", new Color(0.82f, 0.81f, 0.77f, 1f));
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 30f);

            Slider slider = go.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);

            slider.onValueChanged.AddListener(v =>
            {
                readout.text = v.ToString(format);
                onChanged(v);
            });
        }

        private void CreateToggle(Transform parent, string label, Vector2 position, bool value,
                                  UnityEngine.Events.UnityAction<bool> onChanged)
        {
            CreateLabel(parent, label + "_Label", label, 20, TextAnchor.MiddleLeft,
                        position + new Vector2(-330f, 0f), new Vector2(400f, 34f));

            GameObject go = CreatePanel(parent, label + "_Toggle", new Color(0.15f, 0.15f, 0.16f, 1f));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position + new Vector2(230f, 0f);
            rt.sizeDelta = new Vector2(34f, 34f);

            GameObject check = CreatePanel(go.transform, "Check", new Color(0.72f, 0.74f, 0.66f, 1f));
            RectTransform checkRt = check.GetComponent<RectTransform>();
            Stretch(checkRt);
            checkRt.offsetMin = new Vector2(7f, 7f);
            checkRt.offsetMax = new Vector2(-7f, -7f);

            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = go.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(onChanged);
        }

        private void CreateQualityDropdown(Transform parent, Vector2 position)
        {
            CreateLabel(parent, "Quality_Label", "Graphics quality", 20, TextAnchor.MiddleLeft,
                        position + new Vector2(-330f, 0f), new Vector2(300f, 34f));

            GameObject go = CreatePanel(parent, "Quality_Dropdown", new Color(0.16f, 0.16f, 0.17f, 1f));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position + new Vector2(150f, 0f);
            rt.sizeDelta = new Vector2(300f, 40f);

            Text caption = CreateLabel(go.transform, "Caption", "", 19, TextAnchor.MiddleLeft,
                                       new Vector2(6f, 0f), new Vector2(270f, 34f));

            // A simple cycling control rather than a full dropdown list: it needs no
            // template hierarchy, and there are only a handful of quality levels.
            Button button = go.AddComponent<Button>();
            List<string> names = new List<string>(QualitySettings.names);
            caption.text = names.Count > 0
                ? names[Mathf.Clamp(GameSettings.QualityLevel, 0, names.Count - 1)]
                : "Default";

            button.onClick.AddListener(() =>
            {
                if (names.Count == 0)
                {
                    return;
                }

                int next = (GameSettings.QualityLevel + 1) % names.Count;
                GameSettings.QualityLevel = next;
                caption.text = names[next];
            });
        }
    }
}
