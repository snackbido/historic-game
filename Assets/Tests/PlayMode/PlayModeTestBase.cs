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
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            Time.timeScale = 25f;
        }

        [UnityTearDown]
        public IEnumerator BaseTearDown()
        {
            Time.timeScale = originalTimeScale;
            yield return null;
        }
    }
}
