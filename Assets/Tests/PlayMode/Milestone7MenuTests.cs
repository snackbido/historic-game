using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 7 – P3: menu chính, tạm dừng (Esc), cài đặt (âm lượng, pixel).</summary>
    public class Milestone7MenuTests : PlayModeTestBase
    {
        private static GameMenuUI Menu => GameMenuUI.Instance;

        [UnityTest]
        public IEnumerator Tests_SkipTheMainMenu()
        {
            yield return null;
            Assert.IsNotNull(Menu);
            Assert.IsFalse(GameMenuUI.IsOpen);
            Assert.AreEqual(25f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pausing_FreezesTime_AndResumeRestoresIt()
        {
            yield return null;
            Menu.Open(GameMenuUI.MenuScreen.Pause);
            Assert.IsTrue(GameMenuUI.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, "Game stops while paused");
            Menu.OpenSettings();
            Assert.AreEqual(GameMenuUI.MenuScreen.Settings, Menu.Current);
            Assert.AreEqual(0f, Time.timeScale, "Still paused in settings");
            yield return null;
            var music = Menu.GetComponentsInChildren<UnityEngine.UI.Slider>()[0];
            Assert.AreEqual(MusicManager.Volume, music.value, 0.001f, "Slider shows the saved volume");
            Assert.AreEqual(music.value, music.fillRect.anchorMax.x, 0.01f, "Fill bar drawn at that volume");
            Menu.Resume();
            Assert.IsFalse(GameMenuUI.IsOpen);
            Assert.AreEqual(25f, Time.timeScale, "Time runs again at the previous speed");
        }

        [UnityTest]
        public IEnumerator Settings_PixelToggle_SwitchesTheCamera_AndIsRemembered()
        {
            yield return null;
            var pixel = Camera.main.GetComponent<PixelArtCamera>();
            bool original = pixel.PixelModeOn;
            bool savedOriginal = PixelArtCamera.SavedPreference;
            try
            {
                Menu.Open(GameMenuUI.MenuScreen.Settings);
                Menu.TogglePixelMode();
                Assert.AreEqual(!original, pixel.PixelModeOn);
                Assert.AreEqual(!original, PixelArtCamera.SavedPreference);
            }
            finally
            {
                pixel.PixelModeOn = original;
                PixelArtCamera.SavedPreference = savedOriginal;
                Menu.Resume();
            }
        }

        [UnityTest]
        public IEnumerator BackToMainMenu_ReloadsTheScene_ShowingTheMainMenu()
        {
            yield return null;
            var oldMenu = Menu;
            Menu.Open(GameMenuUI.MenuScreen.Pause);
            GameMenuUI.ShowMainMenuOnStart = true;
            Menu.BackToMainMenu();
            yield return null;
            yield return null;
            Assert.IsTrue(Menu != null && Menu != oldMenu, "Fresh scene");
            Assert.AreEqual(GameMenuUI.MenuScreen.Main, Menu.Current);
            Assert.AreEqual(0f, Time.timeScale, "The world waits behind the main menu");
            Menu.NewGame();
            Assert.IsFalse(GameMenuUI.IsOpen);
            Assert.AreEqual(25f, Time.timeScale);
        }
    }
}
