using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Freedome.Interaction;

namespace Freedome.Tests.PlayMode
{
    /// <summary>
    /// The interaction behaviour that only a running game can answer.
    ///
    /// Everything here is deliberately about *state*, not about appearance: whether
    /// carrying and dropping leaves the inventory and the object agreeing with each
    /// other, whether a hinge actually reaches its target angle, whether stowing an
    /// object and taking it back out returns the same object. None of it can run in
    /// the compile check, because all of it needs Update to be called.
    ///
    /// These have never executed. They are written to be what the GitHub Actions
    /// run executes first.
    /// </summary>
    public sealed class InteractionPlayTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("InteractionPlayTestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private Carryable MakeCarryable(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<BoxCollider>();
            go.AddComponent<Rigidbody>();

            Carryable carry = go.AddComponent<Carryable>();
            carry.Configure(name, new Vector3(0.25f, -0.2f, 0.45f), Vector3.zero);
            return carry;
        }

        private PlayerInteractor MakeInteractor()
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(_root.transform, false);

            GameObject cam = new GameObject("Camera");
            cam.transform.SetParent(player.transform, false);
            cam.AddComponent<Camera>();

            player.AddComponent<PlayerInventory>();
            return player.AddComponent<PlayerInteractor>();
        }

        [UnityTest]
        public IEnumerator PickingUpPutsTheObjectInTheInventoryAndInTheHand()
        {
            PlayerInteractor interactor = MakeInteractor();
            Carryable item = MakeCarryable("tin");
            yield return null;

            interactor.TryCarry(item);
            yield return null;

            Assert.AreEqual(1, interactor.Inventory.Count, "the item did not enter the inventory");
            Assert.AreSame(item, interactor.Carried, "the item is not in the player's hand");
            Assert.IsTrue(item.IsHeld);
        }

        [UnityTest]
        public IEnumerator DroppingRemovesItFromTheInventoryAndLeavesItInTheWorld()
        {
            PlayerInteractor interactor = MakeInteractor();
            Carryable item = MakeCarryable("tin");
            yield return null;

            interactor.TryCarry(item);
            yield return null;
            interactor.Drop();
            yield return null;

            Assert.AreEqual(0, interactor.Inventory.Count, "the inventory still holds a dropped item");
            Assert.IsNull(interactor.Carried);
            Assert.IsFalse(item.IsHeld);
            Assert.IsTrue(item.gameObject.activeSelf, "the dropped object is still deactivated");
            Assert.IsFalse(item.transform.IsChildOf(interactor.transform),
                "the dropped object is still parented under the player");
        }

        [UnityTest]
        public IEnumerator SelectingAnotherSlotStowsTheFirstItemRatherThanDroppingIt()
        {
            PlayerInteractor interactor = MakeInteractor();
            Carryable first = MakeCarryable("tin");
            Carryable second = MakeCarryable("rule");
            yield return null;

            interactor.TryCarry(first);
            interactor.TryCarry(second);
            yield return null;

            Assert.AreEqual(2, interactor.Inventory.Count);
            Assert.AreSame(second, interactor.Carried, "the second item should be in hand");
            Assert.IsFalse(first.gameObject.activeSelf, "the first item should be stowed out of sight");

            interactor.Inventory.Select(0);
            yield return null;

            Assert.AreSame(first, interactor.Carried, "selecting slot 0 did not bring the first item back");
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.IsFalse(second.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator TheInventoryRefusesMoreThanItsCapacity()
        {
            PlayerInteractor interactor = MakeInteractor();
            yield return null;

            for (int i = 0; i < PlayerInventory.Capacity + 3; i++)
            {
                interactor.TryCarry(MakeCarryable($"item{i}"));
            }
            yield return null;

            Assert.AreEqual(PlayerInventory.Capacity, interactor.Inventory.Count,
                "the inventory took more than it should hold");
        }

        [UnityTest]
        public IEnumerator AHingedPartReachesItsOpenAngleAndComesBack()
        {
            GameObject hinge = new GameObject("hinge");
            hinge.transform.SetParent(_root.transform, false);

            HingedPart part = hinge.AddComponent<HingedPart>();
            part.Configure("Open", "Close", Vector3.up, 92f, 900f, null);
            yield return null;

            Assert.IsFalse(part.IsOpen);

            part.Interact(null);
            for (int i = 0; i < 30 && Mathf.Abs(part.Angle - 92f) > 0.5f; i++)
            {
                yield return null;
            }

            Assert.AreEqual(92f, part.Angle, 0.5f, "the part never reached its open angle");
            Assert.IsTrue(part.IsOpen);

            part.Interact(null);
            for (int i = 0; i < 30 && Mathf.Abs(part.Angle) > 0.5f; i++)
            {
                yield return null;
            }

            Assert.AreEqual(0f, part.Angle, 0.5f, "the part never closed again");
            Assert.IsFalse(part.IsOpen);
        }

        [UnityTest]
        public IEnumerator ASlidingPartReturnsExactlyToWhereItStarted()
        {
            GameObject drawer = new GameObject("drawer");
            drawer.transform.SetParent(_root.transform, false);
            drawer.transform.localPosition = new Vector3(1.4f, 0.5f, 0.6f);

            SlidingPart slide = drawer.AddComponent<SlidingPart>();
            slide.Configure("Open", "Close", Vector3.left, 0.30f, 4f);
            yield return null;

            Vector3 closed = drawer.transform.localPosition;

            slide.Interact(null);
            for (int i = 0; i < 40 && !Mathf.Approximately(slide.Offset, 0.30f); i++)
            {
                yield return null;
            }
            Assert.AreEqual(0.30f, slide.Offset, 0.005f, "the drawer never opened fully");

            slide.Interact(null);
            for (int i = 0; i < 40 && slide.Offset > 0.0001f; i++)
            {
                yield return null;
            }

            // Drift here would mean the drawer creeps out of its carcass over a
            // session, which is the failure mode of animating by delta rather than
            // from a remembered origin.
            Assert.AreEqual(closed.x, drawer.transform.localPosition.x, 0.001f,
                "the drawer did not return to its closed position");
        }

        [UnityTest]
        public IEnumerator SwitchingTheLightTogglesTheLamp()
        {
            GameObject lampGo = new GameObject("lamp");
            lampGo.transform.SetParent(_root.transform, false);
            Light lamp = lampGo.AddComponent<Light>();

            GameObject pivot = new GameObject("switch");
            pivot.transform.SetParent(_root.transform, false);

            ToggleSwitch toggle = pivot.AddComponent<ToggleSwitch>();
            toggle.Configure(lamp, null, true);
            yield return null;

            Assert.IsTrue(toggle.IsOn);
            Assert.IsTrue(lamp.enabled, "the lamp did not start on");

            toggle.Interact(null);
            yield return null;

            Assert.IsFalse(toggle.IsOn);
            Assert.IsFalse(lamp.enabled, "the switch did not turn the lamp off");
        }

        /// <summary>
        /// The one that closes the route.
        ///
        /// A tool-gated fixture sits on a collider in front of whatever it is holding
        /// shut, so that the interaction ray finds the screws rather than the thing
        /// behind them. It has to give that up once its job is done. On the drawer,
        /// where the fixture covers the whole drawer front, keeping it meant the
        /// drawer could be levered free and then never opened.
        /// </summary>
        [UnityTest]
        public IEnumerator AFinishedFixtureStopsBlockingWhatIsBehindIt()
        {
            GameObject stateGo = new GameObject("EscapeState");
            stateGo.transform.SetParent(_root.transform, false);
            EscapeState state = stateGo.AddComponent<EscapeState>();

            GameObject fixtureGo = new GameObject("Fixture");
            fixtureGo.transform.SetParent(_root.transform, false);
            Collider reach = fixtureGo.AddComponent<BoxCollider>();

            ToolGatedFixture fixture = fixtureGo.AddComponent<ToolGatedFixture>();
            fixture.Configure("offcut", "Swollen shut", "Lever it open", "It moves freely now",
                              ToolGatedFixture.Effect.ForceDrawer);
            yield return null;

            Assert.IsTrue(reach.enabled, "the fixture was not in the way to begin with");

            PlayerInteractor interactor = MakeInteractor();
            Carryable offcut = MakeCarryable("timber offcut");
            yield return null;

            interactor.TryCarry(offcut);
            yield return null;

            fixture.Interact(interactor);
            yield return null;

            Assert.IsTrue(state.DrawerForced, "using the offcut on the fixture did nothing");
            Assert.IsFalse(reach.enabled,
                "the finished fixture is still the first thing the interaction ray meets");
        }
    }
}
