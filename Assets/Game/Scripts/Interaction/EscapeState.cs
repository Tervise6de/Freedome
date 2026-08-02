using UnityEngine;

namespace Freedome.Interaction
{
    /// <summary>
    /// The one place that knows how far along the way out the player is.
    ///
    /// This is the thing the environment milestone spent its whole life refusing
    /// to add - a flag that one object sets and another object reads. It exists
    /// now because the project is an escape game, and an escape needs a state that
    /// survives the object you are touching.
    ///
    /// Kept deliberately small: four booleans and an event. No score, no timer, no
    /// fail state, no branching. If this class starts growing a progression system,
    /// that is a new decision, not an extension of this one.
    /// </summary>
    public sealed class EscapeState : MonoBehaviour
    {
        /// <summary>The swollen bench drawer has been levered open.</summary>
        public bool DrawerForced { get; private set; }

        /// <summary>The rim lock case is off the inside face of the door.</summary>
        public bool LockRemoved { get; private set; }

        /// <summary>
        /// The four screws holding the service panel down are out. Not part of the
        /// way out - lifting the panel shows you 230 mm of joists and dirt, which is
        /// what is actually under a shed on bearers. It is a dead end that rewards
        /// looking, and it stays in because a building where only the useful things
        /// open is a building that tells you which things are useful.
        /// </summary>
        public bool PanelUnscrewed { get; private set; }

        /// <summary>The player has got out of the shed.</summary>
        public bool Escaped { get; private set; }

        /// <summary>Fires whenever any of the above changes, for the HUD.</summary>
        public event System.Action Changed;

        public static EscapeState Find()
        {
            return FindAnyObjectByType<EscapeState>();
        }

        public void SetDrawerForced()
        {
            if (DrawerForced)
            {
                return;
            }

            DrawerForced = true;
            Changed?.Invoke();
        }

        public void SetLockRemoved()
        {
            if (LockRemoved)
            {
                return;
            }

            LockRemoved = true;
            Changed?.Invoke();
        }

        public void SetPanelUnscrewed()
        {
            if (PanelUnscrewed)
            {
                return;
            }

            PanelUnscrewed = true;
            Changed?.Invoke();
        }

        public void SetEscaped()
        {
            if (Escaped || !LockRemoved)
            {
                return;
            }

            Escaped = true;
            Changed?.Invoke();
        }

        // No Reset(). The pause menu restarts by reloading the scene, which builds
        // a fresh EscapeState, so a reset method would be dead code that looks like
        // a supported way to do it.
    }
}
