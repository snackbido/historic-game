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
    public class Milestone5JobTests : PlayModeTestBase
    {
        private static NpcController Npc(string professionId) =>
            NpcController.All.First(n => n.Profession != null && n.Profession.id == professionId);

        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [UnityTest]
        public IEnumerator MixedGroupOnTree_GathererChopsItDown_ScoutJustFollows()
        {
            yield return null;
            var villager = Npc("villager");
            var scout = Npc("scout");
            var tree = GameObject.Find("Tree").GetComponent<ResourceNode>();
            var wood = tree.ResourceType;
            int woodBefore = ResourceManager.Instance.GetAmount(wood);

            SelectionManager.Instance.SetSelection(new[] { villager, scout });
            SelectionManager.Instance.IssueCommandAt(tree.transform.position);

            Assert.IsInstanceOf<GatherJob>(villager.CurrentJob, "Villager can gather");
            Assert.AreEqual(NpcState.Working, villager.State);
            Assert.IsNull(scout.CurrentJob, "Scout cannot gather");
            Assert.AreEqual(NpcState.Moving, scout.State, "Scout follows to the tree instead");

            yield return WaitUntil(() => tree == null, 60f);
            Assert.IsTrue(tree == null, "Tree should be chopped down");
            Assert.AreEqual(woodBefore + 10, ResourceManager.Instance.GetAmount(wood), "All 10 wood goes to the tribe");

            yield return null;
            Assert.AreEqual(NpcState.Idle, villager.State, "Villager stops working once the tree is gone");
            Assert.IsNull(villager.CurrentJob);
        }

        [UnityTest]
        public IEnumerator FarmerOnPlot_PlantsHarvestsAndReplants()
        {
            yield return null;
            var knowledge = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");
            var food = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var berry = Asset<CropData>("Assets/_Data/CropData_Berry.asset");
            ResourceManager.Instance.AddResource(knowledge, 10);
            TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Farming.asset"));
            FarmManager.Instance.SelectCrop(berry);

            var farmer = Npc("farmer");
            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            int foodBefore = ResourceManager.Instance.GetAmount(food);

            SelectionManager.Instance.SetSelection(new[] { farmer });
            SelectionManager.Instance.IssueCommandAt(plot.transform.position);
            Assert.IsInstanceOf<FarmJob>(farmer.CurrentJob);

            yield return WaitUntil(() => plot.State == FarmPlotState.Growing, 20f);
            Assert.AreEqual(FarmPlotState.Growing, plot.State, "Farmer should plant the selected seed");

            yield return WaitUntil(() => ResourceManager.Instance.GetAmount(food) > foodBefore, 60f);
            Assert.AreEqual(foodBefore + berry.harvestYield[0].amount, ResourceManager.Instance.GetAmount(food), "Farmer harvests the ripe crop");

            yield return WaitUntil(() => plot.State == FarmPlotState.Growing, 12f); // cày lại đất (M5d/F2) rồi gieo
            Assert.AreEqual(FarmPlotState.Growing, plot.State, "Farmer replants right after harvesting");
            Assert.IsInstanceOf<FarmJob>(farmer.CurrentJob, "Farming keeps going until a new order");
        }

        [UnityTest]
        public IEnumerator FarmerOnPlot_WithoutSelectedSeed_StopsWorking()
        {
            yield return null;
            FarmManager.Instance.SelectCrop(null);
            var farmer = Npc("farmer");
            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();

            SelectionManager.Instance.SetSelection(new[] { farmer });
            SelectionManager.Instance.IssueCommandAt(plot.transform.position);

            yield return WaitUntil(() => farmer.CurrentJob == null, 30f);
            Assert.IsNull(farmer.CurrentJob, "No seed selected → the farming job ends");
            Assert.AreEqual(FarmPlotState.Empty, plot.State);
        }

        [UnityTest]
        public IEnumerator HunterOnWildBoar_KillsItAndBringsMeat()
        {
            yield return null;
            var food = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var hunter = Npc("hunter");
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();
            int huntYield = boar.Data.huntYield[0].amount;
            int foodBefore = ResourceManager.Instance.GetAmount(food);

            SelectionManager.Instance.SetSelection(new[] { hunter });
            SelectionManager.Instance.IssueCommandAt(boar.transform.position);
            Assert.IsInstanceOf<HuntJob>(hunter.CurrentJob);

            yield return WaitUntil(() => boar == null, 40f);
            Assert.IsTrue(boar == null, "Hunted boar should die and disappear");
            Assert.AreEqual(foodBefore + huntYield, ResourceManager.Instance.GetAmount(food), "Hunting gives meat");
            yield return null;
            Assert.IsNull(hunter.CurrentJob);
        }

        [UnityTest]
        public IEnumerator FarmerOnWildBoar_FeedsItUntilTamed()
        {
            yield return null;
            var food = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            ResourceManager.Instance.AddResource(food, 10);
            var farmer = Npc("farmer");
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();

            SelectionManager.Instance.SetSelection(new[] { farmer });
            SelectionManager.Instance.IssueCommandAt(boar.transform.position);
            Assert.IsInstanceOf<TendAnimalJob>(farmer.CurrentJob, "Farmer tames wild animals instead of hunting them");

            yield return WaitUntil(() => boar.State == AnimalState.Tamed, 40f);
            Assert.AreEqual(AnimalState.Tamed, boar.State);
            yield return WaitUntil(() => farmer.CurrentJob == null, 5f);
            Assert.IsNull(farmer.CurrentJob, "Taming job ends once the animal is tamed");
        }

        [UnityTest]
        public IEnumerator ScoutOnFarmPlot_Follows_AndMoveOrderCancelsJobs()
        {
            yield return null;
            var scout = Npc("scout");
            var villager = Npc("villager");
            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            var tree = GameObject.Find("Tree").GetComponent<ResourceNode>();

            SelectionManager.Instance.SetSelection(new[] { scout });
            SelectionManager.Instance.IssueCommandAt(plot.transform.position);
            Assert.IsNull(scout.CurrentJob, "Scouts cannot farm");
            Assert.AreEqual(NpcState.Moving, scout.State);

            SelectionManager.Instance.SetSelection(new[] { villager });
            SelectionManager.Instance.IssueCommandAt(tree.transform.position);
            Assert.IsNotNull(villager.CurrentJob);

            SelectionManager.Instance.IssueCommandAt(new Vector3(-4f, 0f, 3.5f));
            Assert.IsNull(villager.CurrentJob, "A move order replaces the current job");
            Assert.AreEqual(NpcState.Moving, villager.State);
        }
    }
}
