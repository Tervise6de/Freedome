using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// A drawer. Slides straight out along one local axis and back again.
    ///
    /// Same rule as everything else here: it moves itself and reports to nothing.
    /// What is inside a drawer is inside it because a drawer is where you keep
    /// things, not because anything needs finding.
    /// </summary>
    public sealed class SlidingPart : Interactable
    {
        [SerializeField] private string closedVerb = "Open the drawer";
        [SerializeField] private string openVerb = "Close the drawer";
        [SerializeField] private Vector3 slideAxis = Vector3.left;
        [SerializeField] private float travel = 0.34f;
        [SerializeField] private float metresPerSecond = 0.6f;

        private Vector3 _closedPosition;
        private float _offset;
        private float _target;

        public bool IsOpen
        {
            get { return _target > 0.001f; }
        }

        /// <summary>How far out it currently is, in metres. Exposed for the tests.</summary>
        public float Offset
        {
            get { return _offset; }
        }

        public override string Prompt
        {
            get { return IsOpen ? openVerb : closedVerb; }
        }

        private void Awake()
        {
            _closedPosition = transform.localPosition;
        }

        public override void Interact(PlayerInteractor actor)
        {
            _target = IsOpen ? 0f : travel;
        }

        private void Update()
        {
            if (Mathf.Approximately(_offset, _target))
            {
                return;
            }

            _offset = Mathf.MoveTowards(_offset, _target, metresPerSecond * Time.deltaTime);
            transform.localPosition = _closedPosition + (slideAxis.normalized * _offset);
        }

        public void Configure(string closed, string open, Vector3 axis, float distance, float speed)
        {
            closedVerb = closed;
            openVerb = open;
            slideAxis = axis;
            travel = distance;
            metresPerSecond = speed;
        }
    }
}
