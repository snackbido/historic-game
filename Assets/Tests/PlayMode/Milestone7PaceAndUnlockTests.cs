using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PrehistoricTribe.Tests
{
    /// <summary>
    /// Milestone 7 – P4 (sau khi user chơi thử): nhịp độ chỉnh được (giãn mọi thời gian chờ của làng) và
    /// mục còn khóa báo rõ cần làm gì để mở.
    /// </summary>
    public class Milestone7PaceAndUnlockTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ResourceTypeData Knowledge => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");

        private readonly List<string> notifications = new List<string>();
        private void Capture(string message) => notifications.Add(message);

        [SetUp]
        public void Listen() => EventBus.OnNotification += Capture;

        [TearDown]
        public void StopListening()
        {
            EventBus.OnNotification -= Capture;
            notifications.Clear();
        }

        private static Button ButtonStartingWith(string panel, string text) =>
            GameObject.Find(panel).GetComponentsInChildren<Button>()
                .First(b => b.GetComponentInChildren<TMP_Text>().text.StartsWith(text));

        // ─── Mở khóa ────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator LockedBuilding_IsListed_AndClickingSaysWhatToResearch()
        {
            yield return null;
            var levee = Asset<BuildingData>("Assets/_Data/BuildingData_Levee.asset");
            Button button = ButtonStartingWith("BuildPanel", levee.displayName);
            Assert.IsTrue(LockedEntry.IsLocked(button), "Levee starts locked but is shown");

            button.onClick.Invoke();
            string hint = notifications.Last();
            StringAssert.Contains("Thủy lợi", hint, "Names the tech that unlocks it");
            StringAssert.Contains("cần nghiên cứu trước", hint, "Irrigation itself needs earlier techs");
        }

        [UnityTest]
        public IEnumerator Research_WithoutKnowledge_SaysHowMuchIsMissing_AndSuccessListsUnlocks()
        {
            yield return null;
            var farming = Asset<TechNode>("Assets/_Data/TechNode_Farming.asset");
            Assert.IsFalse(TechManager.Instance.TryUnlock(farming));
            StringAssert.Contains(Knowledge.displayName, notifications.Last());
            StringAssert.Contains("đang có 0", notifications.Last());

            ResourceManager.Instance.AddResource(Knowledge, 100);
            Assert.IsTrue(TechManager.Instance.TryUnlock(farming));
            StringAssert.Contains("mở khóa:", notifications.Last());
            StringAssert.Contains(Asset<BuildingData>("Assets/_Data/BuildingData_Storage.asset").displayName, notifications.Last());
        }

        [UnityTest]
        public IEnumerator TechPanel_ShowsCosts_AndLocksTechsWithMissingPrerequisites()
        {
            yield return null;
            var farming = Asset<TechNode>("Assets/_Data/TechNode_Farming.asset");
            var irrigation = Asset<TechNode>("Assets/_Data/TechNode_Irrigation.asset");
            string farmingLabel = ButtonStartingWith("TechPanel", farming.displayName).GetComponentInChildren<TMP_Text>().text;
            StringAssert.Contains(TechManager.CostText(farming.cost), farmingLabel, "Cost is on the button");
            Assert.IsTrue(LockedEntry.IsLocked(ButtonStartingWith("TechPanel", irrigation.displayName)));
        }

        // ─── Nhịp độ ────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator SlowerPace_StretchesVillageTimers()
        {
            yield return null;
            float baby = NpcManager.Instance.BabyDuration;
            GamePace.Factor = 2f;
            Assert.AreEqual(baby * 2f, NpcManager.Instance.BabyDuration, 0.01f, "Children grow up slower");

            // Tri thức (mỗi 6s ở nhịp gốc) gần như dừng hẳn khi nhịp độ cực chậm.
            GamePace.Factor = 1000f;
            int before = ResourceManager.Instance.GetAmount(Knowledge);
            float deadline = Time.time + 20f;
            while (Time.time < deadline) yield return null;
            Assert.AreEqual(before, ResourceManager.Instance.GetAmount(Knowledge));

            GamePace.Factor = 1f;
            deadline = Time.time + 20f;
            while (Time.time < deadline) yield return null;
            Assert.Greater(ResourceManager.Instance.GetAmount(Knowledge), before, "Normal pace produces knowledge");
        }

        [UnityTest]
        public IEnumerator Settings_CyclesThroughPacePresets()
        {
            yield return null;
            int original = GamePace.PresetIndex;
            try
            {
                GameMenuUI.Instance.Open(GameMenuUI.MenuScreen.Settings);
                GameMenuUI.Instance.CyclePace();
                Assert.AreEqual((original + 1) % GamePace.Presets.Length, GamePace.PresetIndex);
                var labels = GameMenuUI.Instance.GetComponentsInChildren<TMP_Text>().Select(t => t.text);
                Assert.IsTrue(labels.Any(t => t == $"Nhịp độ: {GamePace.PresetName}"), "Button shows the chosen pace");
            }
            finally
            {
                GamePace.PresetIndex = original;
                GameMenuUI.Instance.Resume();
            }
        }
    }
}
