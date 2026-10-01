using System.Collections;
using System.Linq;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 5c – N2: ban đêm dân làng đi ngủ (trong lều hoặc cạnh đống lửa), sáng thì dậy.</summary>
    public class Milestone5cSleepTests : PlayModeTestBase
    {
        private static BuildingData HutData => AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/_Data/BuildingData_Hut.asset");
        private static Campfire Fire => GameObject.Find("Campfire").GetComponent<Campfire>();

        private static BuildingInstance PlaceHut() =>
            BuildingPlacer.Instance.PlaceBuilding(HutData, new Vector3Int(1, -6, 0), spendResources: false);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static IEnumerator FallNight()
        {
            DayNightCycle.Instance.SetTime(0.8f);
            yield return WaitUntil(() => NpcController.All.All(n => n.IsSleeping), 60f);
        }

        [UnityTest]
        public IEnumerator AtNight_HousedSleepInside_HomelessSleepByTheCampfire()
        {
            yield return null;
            var hut = PlaceHut();
            yield return FallNight();

            foreach (var npc in NpcController.All)
            {
                Assert.IsTrue(npc.IsSleeping, $"{npc.NpcName} goes to sleep at night");
                Assert.IsInstanceOf<SleepJob>(npc.CurrentJob);
                if (npc.Home == hut)
                {
                    Assert.IsTrue(npc.IsInsideHut, $"{npc.NpcName} sleeps inside the family hut");
                    Assert.IsFalse(npc.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "Hidden while inside");
                }
                else
                {
                    Assert.IsFalse(npc.IsInsideHut);
                    Assert.Less(InteractableRegistry.GroundDistance(npc.transform.position, Fire.transform.position), 3f,
                        $"{npc.NpcName} has no hut and sleeps by the campfire");
                }
            }
        }

        [UnityTest]
        public IEnumerator Morning_WakesEveryoneUp()
        {
            yield return null;
            var hut = PlaceHut();
            yield return FallNight();
            Assert.IsTrue(NpcController.All.All(n => n.IsSleeping));

            DayNightCycle.Instance.SetTime(0.1f);
            yield return WaitUntil(() => NpcController.All.All(n => !n.IsSleeping), 10f);

            foreach (var npc in NpcController.All)
            {
                Assert.IsFalse(npc.IsSleeping, $"{npc.NpcName} wakes up in the morning");
                Assert.IsFalse(npc.IsInsideHut);
                Assert.IsFalse(npc.CurrentJob is SleepJob);
                Assert.IsTrue(npc.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "Visible again");
            }
        }

        [UnityTest]
        public IEnumerator Sleeping_HealsWounds()
        {
            yield return null;
            var npc = NpcController.All.First();
            npc.Health.SetCurrent(npc.Health.Max * 0.5f);
            float before = npc.Health.Current;

            yield return FallNight();
            float sleptAt = npc.Health.Current;
            yield return WaitUntil(() => npc.Health.Current >= sleptAt + 3f, 10f);
            Assert.Greater(npc.Health.Current, before + 2f, "Resting at night restores health");
        }

        [UnityTest]
        public IEnumerator PlayerOrder_WakesASleeper()
        {
            yield return null;
            yield return FallNight();
            var npc = NpcController.All.First(n => !n.IsInsideHut);

            Assert.IsTrue(npc.MoveTo(npc.transform.position + new Vector3(3f, 0f, 0f)));
            Assert.IsFalse(npc.IsSleeping, "Ordering a villager wakes them up");
            Assert.IsNull(npc.CurrentJob);
        }

        [UnityTest]
        public IEnumerator SleepingInsideAHut_CannotBeSelected()
        {
            yield return null;
            var hut = PlaceHut();
            yield return FallNight();

            var selection = SelectionManager.Instance;
            selection.SelectInScreenRect(new Rect(0f, 0f, Screen.width, Screen.height), additive: false);
            Assert.IsFalse(selection.Selected.Any(n => n.Home == hut), "Villagers inside a hut can't be box-selected");
            Assert.AreEqual(2, selection.Selected.Count, "Those sleeping outside by the fire still can");
        }

        [UnityTest]
        public IEnumerator Births_OnlyHappenAtNight()
        {
            yield return null;
            var hut = PlaceHut();
            ResourceManager.Instance.AddResource(
                AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset"), 20);

            Assert.IsNull(NpcManager.Instance.TryNightBirth(), "Daytime: no births");
            yield return FallNight();
            Assert.IsNotNull(NpcManager.Instance.TryNightBirth(), "Couple asleep in their hut → baby");
        }
    }
}
