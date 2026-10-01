using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 5c – N1: chu kỳ ngày/đêm, ánh sáng, đống lửa, cây ngừng lớn ban đêm.</summary>
    public class Milestone5cDayNightTests : PlayModeTestBase
    {
        private static DayNightCycle Cycle => DayNightCycle.Instance;
        private static Light Sun => GameObject.Find("Sun").GetComponent<Light>();
        private static Campfire Fire => GameObject.Find("Campfire").GetComponent<Campfire>();

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Time_Advances_AndRollsOverToTheNextDay()
        {
            yield return null;
            Assert.AreEqual(1200f, Cycle.DayLengthSeconds, "One day lasts 20 minutes");
            Cycle.SetTime(0.99f, 1);
            DayNightCycle.Running = true;

            yield return WaitGameSeconds(30f); // 30s = 2,5% của ngày
            Assert.AreEqual(2, Cycle.Day, "Passing the end of the night starts day 2");
            Assert.Less(Cycle.TimeOfDay, 0.1f);
            Assert.IsFalse(Cycle.IsNight, "It is morning again");
        }

        [UnityTest]
        public IEnumerator Night_IsDark_AndTheCampfireLightsUp_Day_IsBright()
        {
            yield return null;
            Cycle.SetTime(0.85f);
            yield return null;
            Assert.IsTrue(Cycle.IsNight);
            Assert.Less(Sun.intensity, 0.3f, "Only dim moonlight at night");
            Assert.IsTrue(Fire.IsLit, "Campfire burns at night");
            Assert.IsTrue(Fire.GetComponentInChildren<Light>().enabled);

            Cycle.SetTime(0.35f);
            yield return null;
            Assert.IsFalse(Cycle.IsNight);
            Assert.Greater(Sun.intensity, 0.9f, "Full sunlight at midday");
            Assert.IsFalse(Fire.IsLit, "Fire is out during the day");
            Assert.Less(RenderSettings.ambientLight.grayscale, 0.5f);
        }

        [UnityTest]
        public IEnumerator NightStartAndDayStart_AreAnnounced()
        {
            yield return null;
            int nights = 0, days = 0;
            void OnNight() => nights++;
            void OnDay() => days++;
            EventBus.OnNightStarted += OnNight;
            EventBus.OnDayStarted += OnDay;

            Cycle.SetTime(0.8f);
            Cycle.SetTime(0.85f); // vẫn là đêm → không bắn lại
            Cycle.SetTime(0.1f);

            EventBus.OnNightStarted -= OnNight;
            EventBus.OnDayStarted -= OnDay;
            Assert.AreEqual(1, nights);
            Assert.AreEqual(1, days);
        }

        [UnityTest]
        public IEnumerator Crops_StopGrowingAtNight()
        {
            yield return null;
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");
            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            FarmManager.Instance.SelectCrop(berry);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot));

            Cycle.SetTime(0.85f);
            yield return WaitGameSeconds(berry.timeToSprout + berry.timeToMature + 5f);
            Assert.AreEqual(FarmPlotState.Growing, plot.State, "No growth in the dark");
            Assert.Less(plot.GrowthProgress, 0.05f);

            Cycle.SetTime(0.2f);
            yield return WaitGameSeconds(berry.timeToSprout + berry.timeToMature + 5f);
            Assert.AreEqual(FarmPlotState.ReadyToHarvest, plot.State, "Grows normally in daylight");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsTimeOfDayAndDayNumber()
        {
            SaveSystem.FileNameOverride = "test_savegame_daynight.json";
            try
            {
                yield return null;
                Cycle.SetTime(0.82f, 4);
                GameManager.Instance.SaveGame();
                Cycle.SetTime(0.2f, 1);

                GameManager.Instance.LoadGame();
                yield return null;
                Assert.AreEqual(0.82f, Cycle.TimeOfDay, 0.001f);
                Assert.AreEqual(4, Cycle.Day);
                Assert.IsTrue(Cycle.IsNight);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
