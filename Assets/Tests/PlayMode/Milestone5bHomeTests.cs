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
    /// <summary>Milestone 5b – E2: lều là nhà của cặp đôi, chỉ sinh con tại lều.</summary>
    public class Milestone5bHomeTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData HutData => Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset");
        private static ResourceTypeData Food => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
        private static ResourceTypeData Wood => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Wood.asset");

        // Ô (1,-6) → tâm (1.5, -5.5), sát khu dân làng.
        private static BuildingInstance PlaceHut() =>
            BuildingPlacer.Instance.PlaceBuilding(HutData, new Vector3Int(1, -6, 0), spendResources: false);

        private static NpcController HousedMother(BuildingInstance hut) =>
            NpcController.All.First(n => n.Gender == Gender.Female && n.IsAdult && n.Home == hut);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [UnityTest]
        public IEnumerator BuildingAHut_GivesOneCoupleAHome()
        {
            yield return null;
            Assert.IsTrue(NpcController.All.All(n => n.Home == null), "Nobody has a home before any hut exists");

            var hut = PlaceHut();
            var housed = NpcController.All.Where(n => n.Home == hut).ToList();
            Assert.AreEqual(2, housed.Count, "A new hut is given to one couple right away");
            Assert.AreSame(housed[0].Partner, housed[1], "The two residents are a couple");
            StringAssert.Contains("Gia đình:", NpcManager.DescribeFamily(hut));
        }

        [UnityTest]
        public IEnumerator Couple_SleepsAtHome_AndTheBabyIsBornAtNight()
        {
            yield return null;
            var hut = PlaceHut();
            var mother = HousedMother(hut);
            ResourceManager.Instance.AddResource(Food, 20);

            Assert.IsNull(NpcManager.Instance.TryNightBirth(), "No births during the day");

            DayNightCycle.Instance.SetTime(0.8f);
            yield return WaitUntil(() => mother.IsInsideHut && mother.Partner.IsInsideHut, 60f);
            Assert.IsTrue(mother.IsInsideHut && mother.Partner.IsInsideHut, "Both parents go inside their hut at night");

            var baby = NpcManager.Instance.TryNightBirth();
            Assert.IsNotNull(baby, "Baby is born while the couple sleeps at home");
            Assert.AreSame(hut, baby.Home, "Baby belongs to its parents' hut");
            Assert.Less(InteractableRegistry.GroundDistance(baby.transform.position, hut.transform.position), 2.5f, "Born at the hut door");
            Assert.AreEqual(15, ResourceManager.Instance.GetAmount(Food), "Food is spent at birth");
            StringAssert.Contains("1/2 con", NpcManager.DescribeFamily(hut));

            yield return WaitUntil(() => baby.IsInsideHut, 30f);
            Assert.IsTrue(baby.IsInsideHut, "The newborn sleeps in the hut too");
        }

        [UnityTest]
        public IEnumerator FullHut_BlocksBirths_UntilItIsUpgraded()
        {
            yield return null;
            var hut = PlaceHut();
            var mother = HousedMother(hut);
            ResourceManager.Instance.AddResource(Food, 20);

            NpcManager.Instance.SpawnBaby(mother, mother.Partner, hut);
            NpcManager.Instance.SpawnBaby(mother, mother.Partner, hut);
            mother.LastBirthTime = float.NegativeInfinity; // bỏ qua thời gian nghỉ để thử riêng giới hạn chỗ
            StringAssert.Contains("đủ con", NpcManager.Instance.BirthBlocker(mother), "Level-1 hut holds 2 children");

            ResourceManager.Instance.AddResource(Wood, 15);
            Assert.IsTrue(hut.TryUpgrade());
            Assert.IsNull(NpcManager.Instance.BirthBlocker(mother), "Bigger hut → room for another child");
        }

        [UnityTest]
        public IEnumerator HigherHutLevel_ShortensTheWaitBetweenBirths()
        {
            yield return null;
            var hut = PlaceHut();
            float level1 = NpcManager.Instance.BirthCooldownFor(hut);
            hut.SetLevel(5);
            float level5 = NpcManager.Instance.BirthCooldownFor(hut);

            Assert.AreEqual(180f, level1, 0.01f);
            Assert.AreEqual(108f, level5, 0.01f, "Each hut level shortens the wait by 10%");
        }

        [UnityTest]
        public IEnumerator GrownUpChild_LeavesTheFamilyHut()
        {
            yield return null;
            var manager = NpcManager.Instance;
            var field = typeof(NpcManager).GetField("babyDuration", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(manager, 2f);
            typeof(NpcManager).GetField("childDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, 2f);

            var hut = PlaceHut();
            var mother = HousedMother(hut);
            var child = manager.SpawnBaby(mother, mother.Partner, hut);
            Assert.AreSame(hut, child.Home);

            yield return WaitUntil(() => child.IsAdult, 10f);
            Assert.IsTrue(child.IsAdult);
            Assert.IsNull(child.Home, "Adults move out and need their own hut to start a family");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsFamilyHomes()
        {
            SaveSystem.FileNameOverride = "test_savegame_homes.json";
            try
            {
                yield return null;
                var hut = PlaceHut();
                var mother = HousedMother(hut);
                string motherName = mother.NpcName;
                string babyName = NpcManager.Instance.SpawnBaby(mother, mother.Partner, hut).NpcName;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var restoredHut = BuildingInstance.All.Single(b => b.Data == HutData);
                var restoredMother = NpcController.All.First(n => n.NpcName == motherName);
                Assert.AreSame(restoredHut, restoredMother.Home, "Couple keeps its hut after loading");
                Assert.AreSame(restoredHut, restoredMother.Partner.Home);
                Assert.AreSame(restoredHut, NpcController.All.First(n => n.NpcName == babyName).Home, "Child keeps its home");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
