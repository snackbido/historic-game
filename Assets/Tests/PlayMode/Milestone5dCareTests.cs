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
    /// <summary>Milestone 5d – F4: cỏ dại làm giảm sản lượng → làm cỏ; bón phân chuồng từ vật nuôi → tăng sản lượng.</summary>
    public class Milestone5dCareTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static CropData Berry => Asset<CropData>("Assets/_Data/CropData_Berry.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceTypeData Manure => Res("Manure");
        private static ResourceManager Rm => ResourceManager.Instance;

        private static FarmPlot Garden => GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
        private static NpcController Farmer => NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");

        private static void SetWeedGrowth(FarmPlot plot, float perSecond) =>
            typeof(FarmPlot).GetField("weedGrowthPerSecond", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(plot, perSecond);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static IEnumerator HarvestWhenRipe(FarmPlot plot)
        {
            yield return WaitUntil(() => plot.State == FarmPlotState.ReadyToHarvest, Berry.timeToSprout + Berry.timeToMature + 10f);
            Assert.AreEqual(FarmPlotState.ReadyToHarvest, plot.State);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot));
        }

        [Test]
        public void TamedAnimals_AlsoGiveManure()
        {
            foreach (var path in new[] { "Assets/_Data/AnimalData_Goat.asset", "Assets/_Data/AnimalData_WildBoar.asset" })
                Assert.AreEqual(1, Asset<AnimalData>(path).products.Single(p => p.type == Manure).amount, $"{path} gives manure");
        }

        [UnityTest]
        public IEnumerator Weeds_GrowWithTheCrop_AndShrinkTheHarvest()
        {
            yield return null;
            FarmPlot.WeedsEnabled = true;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            SetWeedGrowth(plot, 1f);

            yield return WaitUntil(() => plot.Weeds >= 1f, 5f);
            Assert.IsTrue(plot.NeedsWeeding, "Weeds take over an untended field");
            Assert.IsTrue(plot.transform.Find("Weeds").gameObject.activeSelf, "Weeds are visible");
            Assert.AreEqual(0.6f, plot.YieldMultiplier, 0.001f, "A field full of weeds loses 40% of its harvest");

            int before = Rm.GetAmount(Res("Berries"));
            yield return HarvestWhenRipe(plot);
            Assert.AreEqual(before + 2, Rm.GetAmount(Res("Berries")), "3 berries × 0.6 → 2");
            Assert.AreEqual(0f, plot.Weeds, "Plowing again buries the weeds");
        }

        [UnityTest]
        public IEnumerator Weeding_TwoRoundsClearAWeedyField()
        {
            yield return null;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            plot.SetWeeds(1f);
            Assert.IsFalse(plot.DoWeedWork());
            Assert.IsTrue(plot.DoWeedWork(), "Two rounds of weeding clear it");
            Assert.AreEqual(0f, plot.Weeds);
            Assert.IsFalse(plot.NeedsWeeding);
        }

        [UnityTest]
        public IEnumerator Manure_FertilizesOncePerCrop_AndBoostsTheHarvest()
        {
            yield return null;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            Assert.IsFalse(FarmManager.Instance.TryFertilize(plot), "No manure in stock");

            Rm.AddResource(Manure, 2);
            Assert.IsTrue(FarmManager.Instance.TryFertilize(plot));
            Assert.AreEqual(1, Rm.GetAmount(Manure), "Fertilizing uses one manure");
            Assert.IsTrue(plot.Fertilized);
            Assert.IsTrue(plot.transform.Find("Fertilizer").gameObject.activeSelf);
            Assert.IsFalse(FarmManager.Instance.TryFertilize(plot), "Once per crop is enough");

            int before = Rm.GetAmount(Res("Berries"));
            yield return HarvestWhenRipe(plot);
            Assert.AreEqual(before + 5, Rm.GetAmount(Res("Berries")), "3 berries × 1.5 → 5");
            Assert.IsFalse(plot.Fertilized, "The next crop needs fresh manure");
        }

        [UnityTest]
        public IEnumerator Farmer_WeedsTheField_ThenSpreadsManure()
        {
            yield return null;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            plot.SetWeeds(0.8f);
            Rm.AddResource(Manure, 1);

            var farmer = Farmer;
            farmer.AssignJob(NpcJobFactory.Create(farmer, plot));
            yield return WaitUntil(() => plot.Weeds <= 0f && plot.Fertilized, 30f);
            Assert.AreEqual(0f, plot.Weeds, "The farmer pulls the weeds");
            Assert.IsTrue(plot.Fertilized, "…and spreads the manure");
            Assert.AreEqual(0, Rm.GetAmount(Manure));
        }

        [UnityTest]
        public IEnumerator IdleFarmer_WeedsByItself()
        {
            yield return null;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            plot.SetWeeds(0.8f);
            NpcController.AutoWorkEnabled = true;

            yield return WaitUntil(() => plot.Weeds <= 0f, 40f);
            Assert.AreEqual(0f, plot.Weeds, "Farmers weed without being told");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsWeedsAndManure()
        {
            SaveSystem.FileNameOverride = "test_savegame_care.json";
            try
            {
                yield return null;
                var plot = Garden;
                Assert.IsTrue(plot.Plant(Berry));
                plot.SetWeeds(0.5f);
                Rm.AddResource(Manure, 1);
                Assert.IsTrue(FarmManager.Instance.TryFertilize(plot));

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var restored = Garden;
                Assert.AreEqual(0.5f, restored.Weeds, 0.001f);
                Assert.IsTrue(restored.Fertilized);
                Assert.IsTrue(restored.transform.Find("Weeds").gameObject.activeSelf);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
