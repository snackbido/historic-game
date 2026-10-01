using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 5b – E3: nhiều loại lương thực và nguồn mới.</summary>
    public class Milestone5bFoodTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceTypeData Pool => Res("Food");
        private static ResourceManager Rm => ResourceManager.Instance;

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [UnityTest]
        public IEnumerator FoodPool_IsTheSumOfAllFoods_AndSpendsPerishablesFirst()
        {
            yield return null;
            Rm.AddResource(Res("Meat"), 2);
            Rm.AddResource(Res("Berries"), 5);
            Rm.AddResource(Res("Rice"), 4);
            Assert.AreEqual(11, Rm.GetAmount(Pool), "\"Thức ăn\" = every food type added together");

            Assert.IsTrue(Rm.SpendAll(new List<ResourceAmount> { new ResourceAmount { type = Pool, amount = 3 } }));
            Assert.AreEqual(0, Rm.GetAmount(Res("Meat")), "Perishable meat is eaten first");
            Assert.AreEqual(8, Rm.GetAmount(Pool));
            Assert.AreEqual(4, Rm.GetAmount(Res("Berries")), "Then the food we have most of");

            Rm.AddResource(Pool, 3);
            Assert.AreEqual(7, Rm.GetAmount(Res("Berries")), "Adding generic food goes into berries");
            StringAssert.Contains("Thịt 0", FoodBreakdownUI.Describe(Rm));
            StringAssert.Contains("Lúa gạo 4", FoodBreakdownUI.Describe(Rm));
        }

        [Test]
        public void EverySourceProducesItsOwnFoodType()
        {
            Assert.AreSame(Res("Berries"), Asset<CropData>("Assets/_Data/CropData_Berry.asset").harvestYield[0].type);
            Assert.AreSame(Res("Rice"), Asset<CropData>("Assets/_Data/CropData_Rice.asset").harvestYield[0].type);
            Assert.AreSame(Res("Vegetables"), Asset<CropData>("Assets/_Data/CropData_Vegetable.asset").harvestYield[0].type);

            var boar = Asset<AnimalData>("Assets/_Data/AnimalData_WildBoar.asset");
            Assert.AreSame(Res("Meat"), boar.products[0].type);
            Assert.AreSame(Res("Meat"), boar.huntYield[0].type);
            Assert.AreSame(Pool, boar.feedCost[0].type, "Animals eat any food");

            var goat = Asset<AnimalData>("Assets/_Data/AnimalData_Goat.asset");
            Assert.AreSame(Res("Milk"), goat.products[0].type);
            Assert.AreSame(Res("Meat"), Asset<PredatorData>("Assets/_Data/PredatorData_Wolf.asset").huntYield[0].type);
            Assert.IsTrue(Res("Meat").perishable && Res("Fish").perishable && Res("Milk").perishable);
            Assert.IsFalse(Res("Rice").perishable);
        }

        [UnityTest]
        public IEnumerator Vegetables_UnlockWithFarming_Rice_NeedsItsOwnTech()
        {
            yield return null;
            var farming = Asset<TechNode>("Assets/_Data/TechNode_Farming.asset");
            var riceTech = Asset<TechNode>("Assets/_Data/TechNode_Rice.asset");
            var rice = Asset<CropData>("Assets/_Data/CropData_Rice.asset");
            var veg = Asset<CropData>("Assets/_Data/CropData_Vegetable.asset");

            Rm.AddResource(Res("Knowledge"), 30);
            Assert.IsFalse(TechManager.Instance.CanUnlock(riceTech), "Rice farming needs Farming first");
            Assert.IsTrue(TechManager.Instance.TryUnlock(farming));
            Assert.IsTrue(TechManager.Instance.IsCropUnlocked(veg));
            Assert.IsFalse(TechManager.Instance.IsCropUnlocked(rice));

            Assert.IsTrue(TechManager.Instance.TryUnlock(riceTech));
            Assert.IsTrue(TechManager.Instance.IsCropUnlocked(rice));
        }

        [UnityTest]
        public IEnumerator RicePlot_GrowsAndHarvestsRice()
        {
            yield return null;
            var rice = Asset<CropData>("Assets/_Data/CropData_Rice.asset");
            // M5d: lúa chỉ cấy trên ruộng nước (xây gần ao, đắp bờ xong).
            var paddy = BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_PaddyField.asset"),
                new Vector3Int(2, 5, 0), spendResources: false);
            var plot = paddy.GetComponent<FarmPlot>();
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
            plot.SetWater(1f); // ngập nước mới cấy được
            FarmManager.Instance.SelectCrop(rice);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot));

            yield return WaitUntil(() => plot.State == FarmPlotState.ReadyToHarvest, rice.timeToSprout + rice.timeToMature + 5f);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot));
            Assert.AreEqual(6, Rm.GetAmount(Res("Rice")), "Rice gives 6 rice per harvest");
        }

        [UnityTest]
        public IEnumerator Goat_TamedThenMilked()
        {
            yield return null;
            var goat = GameObject.Find("Goat").GetComponent<AnimalController>();
            Rm.AddResource(Pool, 10);
            for (int i = 0; i < 3; i++) Assert.IsTrue(TamingSystem.Instance.TryInteract(goat));
            Assert.AreEqual(AnimalState.Tamed, goat.State, "Goat needs 3 feedings");

            yield return WaitUntil(() => goat.ProductReady, goat.Data.productionInterval + 5f);
            Assert.IsTrue(TamingSystem.Instance.TryInteract(goat));
            Assert.AreEqual(2, Rm.GetAmount(Res("Milk")), "Tamed goat gives milk");
        }

        [UnityTest]
        public IEnumerator Pond_GivesFish_NeverDisappears_AndRefills()
        {
            yield return null;
            var pond = GameObject.Find("FishingPond").GetComponent<ResourceNode>();
            Assert.AreEqual("Đánh cá", pond.ActionName);
            int start = pond.AmountRemaining;
            for (int i = 0; i < start; i++) pond.Harvest();
            yield return null;

            Assert.AreEqual(start, Rm.GetAmount(Res("Fish")));
            Assert.IsTrue(pond != null && pond.IsDepleted, "An empty pond stays on the map");
            StringAssert.Contains("đã cạn", InteractionPromptUI.Describe(pond));

            yield return WaitUntil(() => pond.AmountRemaining > 0, 25f);
            Assert.Greater(pond.AmountRemaining, 0, "Fish come back over time");
        }

        [UnityTest]
        public IEnumerator VillagerOrderedToPond_GoesFishing()
        {
            yield return null;
            var villager = NpcController.All.First(n => n.Profession.id == "villager");
            var pond = GameObject.Find("FishingPond").GetComponent<ResourceNode>();

            SelectionManager.Instance.SetSelection(new[] { villager });
            SelectionManager.Instance.IssueCommandAt(pond.transform.position);
            Assert.IsInstanceOf<GatherJob>(villager.CurrentJob);
            Assert.AreEqual("đánh cá", villager.CurrentJob.Description);

            yield return WaitUntil(() => Rm.GetAmount(Res("Fish")) > 0, 40f);
            Assert.Greater(Rm.GetAmount(Res("Fish")), 0, "Villager catches fish from the shore");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsEachFoodType_AndMigratesOldGenericFood()
        {
            yield return null;
            Rm.LoadFromSaveData(new List<ResourceSaveEntry>
            {
                new ResourceSaveEntry { resourceId = "food", amount = 7 }, // save cũ trước E3
                new ResourceSaveEntry { resourceId = "meat", amount = 2 },
            });

            Assert.AreEqual(7, Rm.GetAmount(Res("Berries")), "Old generic food becomes berries");
            Assert.AreEqual(2, Rm.GetAmount(Res("Meat")));
            Assert.AreEqual(9, Rm.GetAmount(Pool));

            var saved = Rm.GetSaveData();
            Assert.IsFalse(saved.Any(e => e.resourceId == "food"), "The pool itself is never saved");
            Assert.AreEqual(7, saved.Single(e => e.resourceId == "berries").amount);
        }
    }
}
