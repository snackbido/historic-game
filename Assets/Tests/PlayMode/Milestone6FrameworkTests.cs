using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Thiên tai giả cho test khung: đếm số lần được gọi.</summary>
    public class TestDisaster : DisasterEvent
    {
        public int Begins, Ends, Warnings;
        public float LastProgress;

        public override void OnWarning() => Warnings++;
        public override void OnBegin() => Begins++;
        public override void OnTick(float deltaTime, float progress) => LastProgress = progress;
        public override void OnEnd() => Ends++;
    }

    /// <summary>Milestone 6 – D1: lịch thiên tai (báo trước → diễn ra → kết thúc), công trình hư hại/sập và sửa chữa.</summary>
    public class Milestone6FrameworkTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceManager Rm => ResourceManager.Instance;
        private static DisasterManager Manager => DisasterManager.Instance;

        private static TestDisaster AddTestDisaster(float durationDays = 0.02f)
        {
            var disaster = Manager.gameObject.AddComponent<TestDisaster>();
            var type = typeof(DisasterEvent);
            type.GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, "test");
            type.GetField("displayName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, "Thử nghiệm");
            type.GetField("warningMessage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, "mây đen kéo tới");
            type.GetField("activeMessage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, "mọi người cẩn thận");
            type.GetField("endMessage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, "Trời yên trở lại");
            type.GetField("durationDays", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(disaster, durationDays);
            return disaster;
        }

        private static void SetWarningDays(float days) =>
            typeof(DisasterManager).GetField("warningDays", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Manager, days);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static BuildingInstance PlaceStorage() =>
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Storage.asset"), new Vector3Int(-4, -5, 0), spendResources: false);

        [UnityTest]
        public IEnumerator Disaster_IsForetold_ThenStrikes_ThenPasses()
        {
            yield return null;
            var disaster = AddTestDisaster();
            SetWarningDays(0.01f);

            Manager.StartWarning(disaster);
            Assert.AreEqual(DisasterPhase.Warning, Manager.Phase);
            Assert.AreEqual(1, disaster.Warnings);
            StringAssert.Contains("Điềm báo: mây đen kéo tới", DisasterBannerUI.Describe(Manager));
            Assert.AreEqual(0, disaster.Begins, "Nothing happens during the warning");

            yield return WaitUntil(() => Manager.Phase == DisasterPhase.Active, 30f);
            Assert.AreEqual(1, disaster.Begins, "Then it strikes");
            StringAssert.Contains("THỬ NGHIỆM", DisasterBannerUI.Describe(Manager));

            yield return WaitUntil(() => Manager.Phase == DisasterPhase.None, 60f);
            Assert.AreEqual(1, disaster.Ends, "…and passes when its time is up");
            Assert.Greater(disaster.LastProgress, 0.5f);
            Assert.IsNull(DisasterBannerUI.Describe(Manager));
            Assert.GreaterOrEqual(Manager.SecondsUntilNext, 2f * Manager.DayLength, "The next one is 2–3 days away");
            Assert.LessOrEqual(Manager.SecondsUntilNext, 3f * Manager.DayLength);
        }

        [UnityTest]
        public IEnumerator RandomSchedule_PicksADisaster_WhenItsTimeComes()
        {
            yield return null;
            var disaster = AddTestDisaster();
            Assert.GreaterOrEqual(Manager.SecondsUntilNext, Manager.DayLength, "The first day is safe");

            DisasterManager.RandomEnabled = true;
            Manager.ScheduleIn(2f);
            yield return WaitUntil(() => Manager.Phase != DisasterPhase.None, 10f);
            Assert.AreEqual(DisasterPhase.Warning, Manager.Phase, "It always comes with a warning");
            Assert.IsNotNull(Manager.Current, "One of the disasters was picked");
        }

        [UnityTest]
        public IEnumerator DamagedBuilding_CollapsesAtZero_AndStopsWorking()
        {
            yield return null;
            var storage = PlaceStorage();
            int capacity = storage.StorageCapacity;
            Assert.Greater(capacity, 0);

            storage.Damage(storage.Health.Max * 0.5f, "thử");
            Assert.IsTrue(storage.IsDamaged);
            Assert.IsFalse(storage.IsCollapsed);
            Assert.AreEqual(capacity, storage.StorageCapacity, "A damaged storehouse still works");

            storage.Damage(storage.Health.Max, "thử");
            Assert.IsTrue(storage.IsCollapsed);
            Assert.AreEqual(0, storage.StorageCapacity, "A collapsed storehouse holds nothing");
            Assert.AreNotEqual(Quaternion.identity, storage.transform.Find("Level1").localRotation, "It looks collapsed");
        }

        [UnityTest]
        public IEnumerator Repairing_CostsWood_AndBringsItBack()
        {
            yield return null;
            var storage = PlaceStorage();
            storage.Damage(storage.Health.Max * 2f, "thử");
            Assert.IsTrue(storage.IsCollapsed);
            Assert.IsNotNull(storage.RepairBlocker(), "No wood, no repairs");
            Assert.IsFalse(storage.DoRepairWork());

            var wood = Res("Wood");
            Rm.AddResource(wood, 10);
            int before = Rm.GetAmount(wood);
            Assert.IsTrue(storage.DoRepairWork());
            Assert.AreEqual(before - 1, Rm.GetAmount(wood), "Each round of repairs uses one wood");
            Assert.IsFalse(storage.IsCollapsed, "It stands again after the first round");
            Assert.AreEqual(Quaternion.identity, storage.transform.Find("Level1").localRotation);
            for (int i = 0; i < 3; i++) Assert.IsTrue(storage.DoRepairWork());
            Assert.IsFalse(storage.IsDamaged, "Four rounds fix it completely");
            Assert.IsNull(InteractionPromptUI.Describe(storage), "Nothing left to repair");
        }

        [UnityTest]
        public IEnumerator Villagers_RepairDamagedBuildings_ByThemselves()
        {
            yield return null;
            var storage = PlaceStorage();
            Rm.AddResource(Res("Wood"), 10);
            storage.Damage(storage.Health.Max * 0.6f, "thử");
            StringAssert.Contains("[E] Sửa", InteractionPromptUI.Describe(storage));
            NpcController.AutoWorkEnabled = true;

            yield return WaitUntil(() => !storage.IsDamaged, 60f);
            Assert.IsFalse(storage.IsDamaged, "Villagers fix the storehouse without being told");
        }

        [UnityTest]
        public IEnumerator FamilyFromACollapsedHut_SleepsByTheFire()
        {
            yield return null;
            var hut = BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset"), new Vector3Int(1, -6, 0), spendResources: false);
            yield return null;
            var owner = NpcController.All.First(n => n.Home == hut);
            hut.Damage(hut.Health.Max * 2f, "thử");

            DayNightCycle.Instance.SetTime(0.8f);
            yield return WaitUntil(() => owner.IsSleeping, 40f);
            Assert.IsTrue(owner.IsSleeping);
            Assert.IsFalse(owner.IsInsideHut, "A collapsed hut gives no shelter");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsDamage_AndTheDisasterInProgress()
        {
            SaveSystem.FileNameOverride = "test_savegame_disaster.json";
            try
            {
                yield return null;
                var disaster = AddTestDisaster(durationDays: 1f);
                var storage = PlaceStorage();
                storage.Damage(storage.Health.Max * 0.5f, "thử");
                Manager.StartNow(disaster);

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(1, disaster.Ends, "The running disaster is cleaned up before loading…");
                Assert.AreEqual(2, disaster.Begins, "…and resumed from the save");
                Assert.AreEqual(DisasterPhase.Active, Manager.Phase);
                Assert.AreSame(disaster, Manager.Current);
                var restored = BuildingInstance.All.Single(b => b.isActiveAndEnabled && b.Data != null && b.Data.id == "storage");
                Assert.AreEqual(0.5f, restored.HealthFraction, 0.01f, "Damage is saved");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
