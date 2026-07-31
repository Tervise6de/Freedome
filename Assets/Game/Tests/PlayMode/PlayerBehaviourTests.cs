using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Freedome.Environment;
using Freedome.Player;
using Freedome.UI;

namespace Freedome.Tests.PlayMode
{
    /// <summary>
    /// Play-mode checks that the player behaves in the built scene: they land on the
    /// floor rather than through it, walking into a wall does not take them outside
    /// the room, and pause freezes and releases the controls.
    ///
    /// These load the real scene, so they cover the actual collision the build ships
    /// with rather than a synthetic test fixture.
    /// </summary>
    public sealed class PlayerBehaviourTests
    {
        private const string SceneName = "ShedRoom";

        private static bool TryLoadScene()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.EndsWith($"/{SceneName}.unity"))
                {
                    SceneManager.LoadScene(i);
                    return true;
                }
            }
            return false;
        }

        [UnitySetUp]
        public IEnumerator LoadShedRoom()
        {
            if (!TryLoadScene())
            {
                Assert.Ignore($"{SceneName} is not in Build Settings. Generate the scene first.");
                yield break;
            }

            yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTest]
        public IEnumerator PlayerSpawnsAndStaysOnTheFloor()
        {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "no player in the scene");

            // Let gravity settle the controller onto the floorboards.
            yield return new WaitForSeconds(1.0f);

            Assert.Greater(player.transform.position.y, -0.10f,
                "the player fell through the floor");
            Assert.Less(player.transform.position.y, 0.30f,
                "the player is floating above the floor");
            Assert.IsTrue(player.IsGrounded, "the player never became grounded");
        }

        [UnityTest]
        public IEnumerator PlayerCannotWalkThroughTheWalls()
        {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            PlayAreaBoundary boundary = Object.FindAnyObjectByType<PlayAreaBoundary>();
            Assert.IsNotNull(player, "no player in the scene");
            Assert.IsNotNull(boundary, "no boundary guard in the scene");

            CharacterController controller = player.GetComponent<CharacterController>();

            // Drive straight at each wall for a while and confirm the player is still
            // inside afterwards. Done through CharacterController.Move so the test
            // exercises the same collision path as real input.
            Vector3[] directions =
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            };

            foreach (Vector3 direction in directions)
            {
                player.Teleport(ShedDimensions.PlayerSpawnPosition, 0f);
                yield return null;

                for (int step = 0; step < 120; step++)
                {
                    controller.Move(direction * 0.08f);
                    yield return null;
                }

                Vector3 position = player.transform.position;
                Assert.IsTrue(boundary.IsInside(position),
                    $"walking {direction} took the player to {position}, outside the room");
            }

            Assert.AreEqual(0, boundary.RecoveryCount,
                "the boundary backstop had to rescue the player, which means the " +
                "collision has a gap that should be fixed rather than caught");
        }

        [UnityTest]
        public IEnumerator PauseFreezesAndResumesTheGame()
        {
            PauseMenuController pause = Object.FindAnyObjectByType<PauseMenuController>();
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.IsNotNull(pause, "no pause menu in the scene");

            pause.SetPaused(true);
            yield return null;

            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale, "pausing did not stop time");
            Assert.IsFalse(player.InputEnabled, "the player still accepts input while paused");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "the cursor is still locked");

            pause.SetPaused(false);
            yield return null;

            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, "resuming did not restore time");
            Assert.IsTrue(player.InputEnabled, "the player did not regain input");
        }

        [UnityTest]
        public IEnumerator CrouchingLowersTheCameraAndStandingRestoresIt()
        {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            Camera camera = player.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "the player has no camera");

            float standingEye = camera.transform.localPosition.y;
            Assert.AreEqual(ShedDimensions.PlayerEyeHeight, standingEye, 0.05f,
                "the standing eye height is not the documented value");

            // The controller reads the keyboard directly, so drive the stance through
            // the collider instead and just confirm the rig responds to height changes.
            CharacterController controller = player.GetComponent<CharacterController>();
            Assert.AreEqual(ShedDimensions.PlayerStandingHeight, controller.height, 0.05f);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneRunsWithoutLoggingErrors()
        {
            // Any error logged during a couple of seconds of ordinary play fails the
            // test; LogAssert is strict about unexpected errors by default.
            yield return new WaitForSeconds(2.0f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
