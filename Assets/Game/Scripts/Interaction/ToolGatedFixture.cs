using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// Something you can only do with the right thing in your hand.
    ///
    /// The two steps of the way out share a shape: look at a fixed part of the
    /// building, be holding a specific object, use it, and the building changes.
    /// Unscrewing the floor panel needs the screwdriver; levering the skirt board
    /// needs the timber offcut.
    ///
    /// The prompt never names the tool you are missing. It says what you are
    /// looking at and, once you are holding the right thing, what you can do with
    /// it. Telling the player "you need a screwdriver" would turn the shed into a
    /// checklist, and the whole point of putting the screws on the panel in the
    /// first place was that somebody who looks at it can see what it needs.
    /// </summary>
    public sealed class ToolGatedFixture : Interactable
    {
        public enum Effect
        {
            UnscrewPanel,
            LeverSkirt,
        }

        [SerializeField] private string requiredItem = "screwdriver";
        [SerializeField] private string idlePrompt = "Four screws hold it down";
        [SerializeField] private string readyPrompt = "Unscrew the panel";
        [SerializeField] private string donePrompt = "The screws are out";
        [SerializeField] private Effect effect = Effect.UnscrewPanel;

        private EscapeState _state;

        private void Awake()
        {
            _state = EscapeState.Find();
        }

        private bool IsDone
        {
            get
            {
                if (_state == null)
                {
                    return false;
                }

                return effect == Effect.UnscrewPanel ? _state.PanelUnscrewed : _state.SkirtRemoved;
            }
        }

        public override string Prompt
        {
            get { return IsDone ? donePrompt : idlePrompt; }
        }

        /// <summary>
        /// Always true. The prompt changes with what is in your hand rather than
        /// vanishing - a fixture you cannot currently act on still deserves to tell
        /// you what it is, which is how the player works out what to go and find.
        /// </summary>
        public override bool CanInteract
        {
            get { return true; }
        }

        /// <summary>The prompt to show given what the player is carrying.</summary>
        public string PromptFor(Carryable held)
        {
            if (IsDone)
            {
                return donePrompt;
            }

            return Holding(held) ? readyPrompt : idlePrompt;
        }

        private bool Holding(Carryable held)
        {
            return held != null &&
                   held.DisplayName.IndexOf(requiredItem, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public override void Interact(PlayerInteractor actor)
        {
            if (_state == null || IsDone || !Holding(actor.Carried))
            {
                return;
            }

            if (effect == Effect.UnscrewPanel)
            {
                _state.SetPanelUnscrewed();
            }
            else
            {
                _state.SetSkirtRemoved();
            }
        }

        public void Configure(string item, string idle, string ready, string done, Effect what)
        {
            requiredItem = item;
            idlePrompt = idle;
            readyPrompt = ready;
            donePrompt = done;
            effect = what;
        }
    }
}
