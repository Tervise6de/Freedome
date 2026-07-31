using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Freedome.Environment
{
    /// <summary>
    /// F3 toggles a small statistics overlay: frame rate, CPU and GPU frame time,
    /// triangle and draw-call counts and managed memory.
    ///
    /// This exists so the performance review can be done with real numbers taken
    /// from the shipped build rather than from the editor, where the scene view and
    /// inspector distort everything. The counters come from ProfilerRecorder, which
    /// is unavailable in a fully stripped release build - in that case the overlay
    /// falls back to timing only, and says so rather than showing zeros.
    /// </summary>
    public sealed class PerformanceOverlay : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private float sampleWindow = 0.5f;

        private ProfilerRecorder _mainThreadTime;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _gpuTime;

        private bool _visible;
        private float _accumulated;
        private int _frames;
        private float _displayedFps;
        private float _displayedMs;
        private float _worstMs;
        private GUIStyle _style;

        private void OnEnable()
        {
            _visible = visibleOnStart;

            _mainThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _gpuTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");
        }

        private void OnDisable()
        {
            _mainThreadTime.Dispose();
            _drawCalls.Dispose();
            _triangles.Dispose();
            _batches.Dispose();
            _gpuTime.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _visible = !_visible;
            }

            float ms = Time.unscaledDeltaTime * 1000f;
            _accumulated += Time.unscaledDeltaTime;
            _frames++;
            _worstMs = Mathf.Max(_worstMs, ms);

            if (_accumulated >= sampleWindow)
            {
                _displayedFps = _frames / _accumulated;
                _displayedMs = (_accumulated / _frames) * 1000f;
                _accumulated = 0f;
                _frames = 0;
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.95f, 0.95f, 0.92f) },
                padding = new RectOffset(10, 10, 8, 8),
            };

            StringBuilder sb = new StringBuilder(320);
            sb.AppendLine($"{_displayedFps:0.0} fps    {_displayedMs:0.00} ms avg    " +
                          $"{_worstMs:0.00} ms worst");

            if (_mainThreadTime.Valid)
            {
                sb.AppendLine($"CPU main thread  {_mainThreadTime.LastValue / 1e6f:0.00} ms");
            }
            if (_gpuTime.Valid)
            {
                sb.AppendLine($"GPU frame        {_gpuTime.LastValue / 1e6f:0.00} ms");
            }
            if (_triangles.Valid)
            {
                sb.AppendLine($"Triangles        {_triangles.LastValue:N0}");
            }
            if (_drawCalls.Valid)
            {
                sb.AppendLine($"Draw calls       {_drawCalls.LastValue:N0}");
            }
            if (_batches.Valid)
            {
                sb.AppendLine($"Batches          {_batches.LastValue:N0}");
            }

            sb.AppendLine($"Mono heap        {System.GC.GetTotalMemory(false) / (1024f * 1024f):0.0} MB");

            if (!_drawCalls.Valid)
            {
                sb.AppendLine("(render counters unavailable in this build)");
            }

            string text = sb.ToString();
            Vector2 size = _style.CalcSize(new GUIContent(text));

            Rect box = new Rect(12f, 12f, size.x + 20f, (_style.lineHeight * 8f) + 16f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(box, text, _style);
        }

        /// <summary>Resets the worst-frame watermark. Useful between measured runs.</summary>
        public void ResetPeak() => _worstMs = 0f;
    }
}
