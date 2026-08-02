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
            ForceDrawer,
            RemoveLock,
            UnscrewPanel,
        }

        [SerializeField] private string requiredItem = "screwdriver";
        [SerializeField] private string idlePrompt = "Four screws hold it down";
        [SerializeField] private string readyPrompt = "Unscrew the panel";
        [SerializeField] private string donePrompt = "The screws are out";
        [SerializeField] private Effect effect = Effect.UnscrewPanel;
        [SerializeField] private GameObject[] removeWhenDone = new GameObject[0];

        private EscapeState _state;
        private Collider _reach;

        private void Awake()
        {
            _state = EscapeState.Find();
            _reach = GetComponent<Collider>();
            StandDown();
        }

        /// <summary>
        /// Once the job is done, get out of the way.
        ///
        /// These fixtures sit on a collider in front of the thing they are attached
        /// to, so that looking at the screws finds the screws rather than the panel
        /// behind them. That is right until the screws are out, at which point the
        /// fixture is still the first thing the interaction ray meets and the thing
        /// behind it can never be reached. The drawer was the fatal case: its
        /// fixture covers the whole drawer front, so a drawer that had just been
        /// levered free could not then be opened, and the way out of the shed
        /// stopped at step two.
        /// </summary>
        private void StandDown()
        {
            if (_reach != null)
            {
                _reach.enabled = !IsDone;
            }
        }

        private bool IsDone
        {
            get
            {
                if (_state == null)
                {
                    return false;
                }

                switch (effect)
                {
                    case Effect.ForceDrawer: return _state.DrawerForced;
                    case Effect.RemoveLock: return _state.LockRemoved;
                    default: return _state.PanelUnscrewed;
                }
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

            switch (effect)
            {
                case Effect.ForceDrawer: _state.SetDrawerForced(); break;
                case Effect.RemoveLock: _state.SetLockRemoved(); break;
                default: _state.SetPanelUnscrewed(); break;
            }

            StandDown();

            // What came off, comes off. Anything parented under the fixture is the
            // part it was holding on - the rim lock case and its screws - and a lock
            // you have just unscrewed should not still be screwed to the door.
            for (int i = 0; i < removeWhenDone.Length; i++)
            {
                if (removeWhenDone[i] != null)
                {
                    removeWhenDone[i].SetActive(false);
                }
            }
        }

        /// <summary>
        /// Objects that stop existing once this fixture has done its job. Set by the
        /// generator; empty for fixtures that only unlock something.
        /// </summary>
        public void Removes(params GameObject[] parts)
        {
            removeWhenDone = parts ?? new GameObject[0];
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
