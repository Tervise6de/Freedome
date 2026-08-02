using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The two things on the mower you can put your hands on: the filler cap and the
    /// recoil starter.
    ///
    /// One component with a role rather than two classes, because they are the same
    /// shape of thing - look at a named part of a machine, maybe be holding
    /// something, press once - and the same reason <see cref="ToolGatedFixture"/>
    /// carries an effect instead of being three classes.
    ///
    /// The prompt describes the part, never the tool. "The tank is dry" is the
    /// machine telling you what is wrong with it; "go and find petrol" would be the
    /// game telling you what to do next.
    /// </summary>
    public sealed class MowerControl : Interactable
    {
        public enum Role
        {
            FillerCap,
            Starter,
        }

        [SerializeField] private Role role = Role.Starter;
        [SerializeField] private MowerEngine engine;

        /// <summary>Substring of the held item's name that counts as petrol.</summary>
        [SerializeField] private string requiredItem = "petrol";

        public override string Prompt
        {
            get
            {
                if (engine == null)
                {
                    return null;
                }

                if (role == Role.FillerCap)
                {
                    return engine.HasFuel ? "The tank is full" : "The tank is dry";
                }

                return engine.IsRunning ? "Stop the engine" : "Pull the starter";
            }
        }

        /// <summary>
        /// The prompt given what the player is carrying. Only the filler cap changes
        /// with what is in your hand; the starter is a handle you can always pull,
        /// and pulling it on a dry engine is how you find out it is dry.
        /// </summary>
        public string PromptFor(Carryable held)
        {
            if (role == Role.FillerCap && engine != null && !engine.HasFuel && Holding(held))
            {
                return "Fill the tank";
            }

            return Prompt;
        }

        private bool Holding(Carryable held)
        {
            return held != null &&
                   held.DisplayName.IndexOf(requiredItem, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public override void Interact(PlayerInteractor actor)
        {
            if (engine == null)
            {
                return;
            }

            if (role == Role.FillerCap)
            {
                if (Holding(actor != null ? actor.Carried : null))
                {
                    engine.Fill();
                }

                return;
            }

            // The starter is the whole control. A real mower is stopped by letting go
            // of the bail on the handlebar, but putting a second collider 400 mm away
            // for that would be two things to find for one machine that does not
            // matter. Pull to start, pull again to stop, and the prompt says which.
            if (engine.IsRunning)
            {
                engine.Stop();
            }
            else
            {
                engine.TryStart();
            }
        }

        public void Configure(Role what, MowerEngine target, string item)
        {
            role = what;
            engine = target;
            requiredItem = item;
        }
    }
}
