using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone2FarmingTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Berry_FullLifecycle_PlantGrowHarvest_AddsResourceAndResetsPlot()
        {
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            Assert.IsNotNull(berry);
            Assert.IsNotNull(food);

            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            Assert.AreEqual(FarmPlotState.Empty, plot.State);

            FarmManager.Instance.SelectCrop(berry);
            bool planted = FarmManager.Instance.TryInteract(plot);
            Assert.IsTrue(planted, "Plant should succeed on an empty plot with a crop selected");
            Assert.AreEqual(FarmPlotState.Growing, plot.State);

            float deadline = Time.time + berry.timeToSprout + berry.timeToMature + 5f;
            while (plot.State != FarmPlotState.ReadyToHarvest && Time.time < deadline)
                yield return null;
            Assert.AreEqual(FarmPlotState.ReadyToHarvest, plot.State, "Plot should reach ReadyToHarvest before timeout");

            int foodBefore = ResourceManager.Instance.GetAmount(food);
            bool harvested = FarmManager.Instance.TryInteract(plot);
            Assert.IsTrue(harvested);
            int foodAfter = ResourceManager.Instance.GetAmount(food);
            Assert.AreEqual(foodBefore + berry.harvestYield[0].amount, foodAfter, "Harvest should grant the crop's harvestYield");
            Assert.AreEqual(FarmPlotState.Empty, plot.State, "Plot should reset to Empty after harvest");
        }

        [UnityTest]
        public IEnumerator UnharvestedCrop_WithersAfterWitherTime_AndCanBeCleared()
        {
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");
            var plot = GameObject.Find("FarmPlot_2").GetComponent<FarmPlot>();

            FarmManager.Instance.SelectCrop(berry);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot));

            float deadline = Time.time + berry.timeToSprout + berry.timeToMature + berry.witherTime + 5f;
            while (plot.State != FarmPlotState.Withered && Time.time < deadline)
                yield return null;
            Assert.AreEqual(FarmPlotState.Withered, plot.State, "Plot should wither if not harvested in time");

            plot.ClearWithered();
            Assert.AreEqual(FarmPlotState.Empty, plot.State, "Withered plot should reset to Empty after clearing");
        }

        [UnityTest]
        public IEnumerator Plant_WithoutSelectedCrop_Fails()
        {
            var plot = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            FarmManager.Instance.SelectCrop(null);

            bool planted = FarmManager.Instance.TryInteract(plot);
            Assert.IsFalse(planted, "Planting with no crop selected should fail");
            Assert.AreEqual(FarmPlotState.Empty, plot.State);
            yield return null;
        }
    }
}
