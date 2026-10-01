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
    /// <summary>Milestone 5d – F5: gặt về là bó lúa → phơi trên giàn → tuốt lấy thóc → giã thành gạo (chừa thóc giống).</summary>
    public class Milestone5dMortarTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData MortarData => Asset<BuildingData>("Assets/_Data/BuildingData_RiceMortar.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceTypeData Sheaf => Res("RiceSheaf");
        private static ResourceTypeData Grain => Res("RiceSeed");
        private static ResourceTypeData Rice => Res("Rice");
        private static ResourceManager Rm => ResourceManager.Instance;

        private static readonly Vector3Int MortarCell = new Vector3Int(-4, -5, 0);

        private static RiceMortar Place() =>
            BuildingPlacer.Instance.PlaceBuilding(MortarData, MortarCell, spendResources: false).GetComponent<RiceMortar>();

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [Test]
        public void RiceTech_UnlocksTheMortar_AndRiceIsReapedAsSheaves()
        {
            var riceTech = Asset<TechNode>("Assets/_Data/TechNode_Rice.asset");
            CollectionAssert.Contains(riceTech.unlockedBuildingIds, "rice_mortar");
            Assert.IsFalse(MortarData.unlockedByDefault);
            var rice = Asset<CropData>("Assets/_Data/CropData_Rice.asset");
            Assert.AreSame(Sheaf, rice.harvestYield.Single().type, "Reaped rice is a sheaf, not food yet");
            Assert.AreEqual(ResourceCategory.Material, Sheaf.category);
            Assert.AreEqual(ResourceCategory.Food, Rice.category);
        }

        [UnityTest]
        public IEnumerator Sheaves_HangOnTheRack_DryInTheSun_ThenThreshIntoGrain()
        {
            yield return null;
            var mortar = Place();
            Rm.AddResource(Sheaf, 2);

            Assert.IsTrue(mortar.DoWork());
            Assert.IsTrue(mortar.DoWork());
            Assert.AreEqual(2, mortar.SheavesOnRack);
            Assert.AreEqual(0, Rm.GetAmount(Sheaf), "Both sheaves went onto the rack");
            Assert.IsTrue(mortar.transform.Find("Level1/Sheaf0").gameObject.activeSelf, "Hanging sheaves are visible");
            Assert.IsFalse(mortar.HasWork, "Wet sheaves can't be threshed yet");
            Assert.IsFalse(mortar.DoWork());

            yield return WaitUntil(() => mortar.DrySheaves == 2, mortar.DryTime + 10f);
            Assert.AreEqual(2, mortar.DrySheaves, "The sun dries them");

            Assert.IsTrue(mortar.DoWork());
            Assert.IsTrue(mortar.DoWork());
            Assert.AreEqual(4, Rm.GetAmount(Grain), "Each dry sheaf threshes into 2 grain");
            Assert.AreEqual(0, mortar.SheavesOnRack);
        }

        [UnityTest]
        public IEnumerator Sheaves_DontDryAtNight()
        {
            yield return null;
            var mortar = Place();
            Rm.AddResource(Sheaf, 1);
            Assert.IsTrue(mortar.DoWork());

            DayNightCycle.Instance.SetTime(0.85f);
            yield return WaitUntil(() => false, mortar.DryTime + 5f);
            Assert.AreEqual(0, mortar.DrySheaves, "No sun, no drying");

            DayNightCycle.Instance.SetTime(0.3f);
            yield return WaitUntil(() => mortar.DrySheaves == 1, mortar.DryTime + 10f);
            Assert.AreEqual(1, mortar.DrySheaves);
        }

        [UnityTest]
        public IEnumerator Pounding_TurnsGrainIntoRice_ButKeepsSeedGrain()
        {
            yield return null;
            var mortar = Place();
            Rm.AddResource(Grain, mortar.SeedReserve);
            Assert.IsFalse(mortar.HasWork, "The seed grain is kept for sowing");
            StringAssert.Contains("giống", mortar.IdleReason());

            Rm.AddResource(Grain, 2);
            Assert.AreEqual("Giã gạo", mortar.NextTask);
            Assert.IsTrue(mortar.DoWork());
            Assert.IsTrue(mortar.transform.Find("Level1/Grain").gameObject.activeSelf, "Grain sits in the mortar while pounding");
            Assert.AreEqual(0, Rm.GetAmount(Rice), "One round of pounding isn't enough");
            Assert.IsTrue(mortar.DoWork());
            Assert.AreEqual(2, Rm.GetAmount(Rice), "2 grain → 2 rice");
            Assert.AreEqual(mortar.SeedReserve, Rm.GetAmount(Grain));
            Assert.IsFalse(mortar.HasWork);
        }

        [UnityTest]
        public IEnumerator Farmers_DryThreshAndPound_ByThemselves()
        {
            yield return null;
            var mortar = Place();
            Rm.AddResource(Sheaf, 2);
            Rm.AddResource(Grain, 2);
            NpcController.AutoWorkEnabled = true;

            // 2 bó → 4 thóc, cộng 2 sẵn có = 6 → giã 2, chừa 4 làm giống.
            yield return WaitUntil(() => Rm.GetAmount(Rice) >= 2, 150f);
            Assert.AreEqual(2, Rm.GetAmount(Rice), "Farmers turn sheaves into rice");
            Assert.AreEqual(mortar.SeedReserve, Rm.GetAmount(Grain), "…and keep the seed grain");
            Assert.AreEqual(0, mortar.SheavesOnRack);
        }

        [UnityTest]
        public IEnumerator OrderedFarmer_HangsTheSheaves()
        {
            yield return null;
            var mortar = Place();
            Rm.AddResource(Sheaf, 3);
            var farmer = NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");
            var job = NpcJobFactory.Create(farmer, mortar);
            Assert.IsInstanceOf<MortarJob>(job);
            farmer.AssignJob(job);

            yield return WaitUntil(() => mortar.SheavesOnRack == 3, 40f);
            Assert.AreEqual(3, mortar.SheavesOnRack);
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsTheRackAndThePounding()
        {
            SaveSystem.FileNameOverride = "test_savegame_mortar.json";
            try
            {
                yield return null;
                var mortar = Place();
                Rm.AddResource(Sheaf, 1);
                Rm.AddResource(Grain, mortar.SeedReserve + 2);
                Assert.IsTrue(mortar.DoWork()); // treo 1 bó
                RiceMortar.DryingEnabled = false;
                Assert.IsTrue(mortar.DoWork()); // giã dở 1 mẻ
                Assert.AreEqual(1, mortar.PoundProgress);

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var restored = InteractableRegistry.All<RiceMortar>().Single(m => m != null && m.isActiveAndEnabled);
                Assert.AreNotSame(mortar, restored);
                Assert.AreEqual(1, restored.SheavesOnRack);
                Assert.AreEqual(1, restored.PoundProgress);
                Assert.IsTrue(restored.DoWork());
                Assert.AreEqual(2, Rm.GetAmount(Rice), "The half-pounded batch finishes after loading");
            }
            finally
            {
                RiceMortar.DryingEnabled = true;
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
