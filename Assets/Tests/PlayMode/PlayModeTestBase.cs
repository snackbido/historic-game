using System.Collections;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public abstract class PlayModeTestBase
    {
        protected const string ScenePath = "Assets/_Scenes/Gameplay.unity";
        private float originalTimeScale;

        [UnitySetUp]
        public IEnumerator BaseSetUp()
        {
            originalTimeScale = Time.timeScale;
            // Mặc định tắt hành vi tự phát (tự làm việc, sinh con) để test cũ dễ đoán; test M5.6 tự bật lại.
            NpcController.AutoWorkEnabled = false;
            NpcManager.BirthsEnabled = false;
            ResourceManager.SpoilageEnabled = false;
            NpcManager.HungerEnabled = false;
            FarmPlot.WeedsEnabled = false; // cỏ dại làm lệch sản lượng các test cũ — test F4 tự bật
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            // Test cũ chạy giữa ban ngày, thời gian đứng yên (cây không ngừng lớn vì đêm…); test ngày/đêm tự đặt giờ.
            DayNightCycle.Running = false;
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.SetTime(0.3f);
            Time.timeScale = 25f;
        }

        [UnityTearDown]
        public IEnumerator BaseTearDown()
        {
            Time.timeScale = originalTimeScale;
            NpcController.AutoWorkEnabled = true;
            NpcManager.BirthsEnabled = true;
            ResourceManager.SpoilageEnabled = true;
            NpcManager.HungerEnabled = true;
            FarmPlot.WeedsEnabled = true;
            DayNightCycle.Running = true;
            yield return null;
        }
    }
}
