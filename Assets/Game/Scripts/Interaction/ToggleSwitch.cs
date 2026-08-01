using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The light switch by the door. Rocks a small plate and turns the ceiling
    /// fitting on and off.
    ///
    /// It controls the lamp and the emissive material on the shade, and nothing
    /// else. Note that with a baked lightmap the switched light is realtime-only:
    /// the bounce contribution stays in the bake either way, so switching off makes
    /// the room dimmer rather than dark. That is honest for a shed with a window in
    /// daylight, which is the only condition this scene is lit for.
    /// </summary>
    public sealed class ToggleSwitch : Interactable
    {
        [SerializeField] private Light controlledLight;
        [SerializeField] private Renderer emissiveShade;
        [SerializeField] private float rockDegrees = 12f;
        [SerializeField] private Vector3 rockAxis = Vector3.right;
        [SerializeField] private bool startOn = true;

        private bool _on;
        private Quaternion _restRotation;
        private MaterialPropertyBlock _block;

        public bool IsOn
        {
            get { return _on; }
        }

        public override string Prompt
        {
            get { return "Light switch"; }
        }

        private void Awake()
        {
            _restRotation = transform.localRotation;
            _on = startOn;
            Apply();
        }

        public override void Interact(PlayerInteractor actor)
        {
            _on = !_on;
            Apply();
        }

        private void Apply()
        {
            transform.localRotation =
                _restRotation * Quaternion.AngleAxis(_on ? -rockDegrees : rockDegrees, rockAxis.normalized);

            if (controlledLight != null)
            {
                controlledLight.enabled = _on;
            }

            if (emissiveShade != null)
            {
                // A property block rather than a material instance, so switching the
                // light does not silently double the material count at runtime.
                _block ??= new MaterialPropertyBlock();
                emissiveShade.GetPropertyBlock(_block);
                _block.SetColor(EmissiveColor, _on ? OnEmission : Color.black);
                emissiveShade.SetPropertyBlock(_block);
            }
        }

        private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");

        // Roughly a 40 W bulb's tint, in the linear HDR values HDRP expects.
        private static readonly Color OnEmission = new Color(2.6f, 2.2f, 1.6f);

        public void Configure(Light lamp, Renderer shade, bool on)
        {
            controlledLight = lamp;
            emissiveShade = shade;
            startOn = on;
        }
    }
}
