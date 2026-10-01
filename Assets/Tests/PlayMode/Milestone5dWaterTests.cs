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
    /// <summary>Milestone 5d – F2: cày/xới trước mỗi vụ, mức nước của ruộng, gánh nước từ ao/giếng.</summary>
    public class Milestone5dWaterTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData DryField => Asset<BuildingData>("Assets/_Data/BuildingData_DryField.asset");
        private static BuildingData PaddyField => Asset<BuildingData>("Assets/_Data/BuildingData_PaddyField.asset");
        private static BuildingData Well => Asset<BuildingData>("Assets/_Data/BuildingData_Well.asset");
        private static CropData Berry => Asset<CropData>("Assets/_Data/CropData_Berry.asset");
        private static CropData Rice => Asset<CropData>("Assets/_Data/CropData_Rice.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");

        private static readonly Vector3Int DryCell = new Vector3Int(-4, -5, 0);     // tâm (-3.5, -4.5)
        private static readonly Vector3Int PondSideCell = new Vector3Int(2, 5, 0);  // tâm (2.5, 5.5), cạnh ao (4.5, 6)
        private static readonly Vector3Int WellCell = new Vector3Int(-4, -4, 0);    // tâm (-3.5, -3.5), sát ruộng cạn

        private static FarmPlot Place(BuildingData data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(data, cell, spendResources: false).GetComponent<FarmPlot>();

        private static FarmPlot Garden => GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
        private static NpcController Farmer => NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");

        private static void Clear(FarmPlot plot)
        {
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
        }

        private static void Plow(FarmPlot plot)
        {
            Clear(plot);
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator ClearedLand_MustBePlowed_BeforeSowing()
        {
            yield return null;
            var plot = Place(DryField, DryCell);
            Clear(plot);
            Assert.AreEqual(FarmPlotState.Unplowed, plot.State, "Cleared land still has to be plowed");
            StringAssert.Contains("cày", plot.PlantBlocker(Berry));
            Assert.IsFalse(plot.Plant(Berry));

            plot.DoPlowWork();
            Assert.AreEqual(1f / 3f, plot.PlowProgress, 0.001f);
            plot.DoPlowWork();
            Assert.IsTrue(plot.DoPlowWork(), "Three rounds of hoeing prepare a dry field");
            Assert.AreEqual(FarmPlotState.Empty, plot.State);
            Assert.IsTrue(plot.Plant(Berry), "Dry crops can be sown into dry soil — they need watering to grow");
        }

        [UnityTest]
        public IEnumerator Paddy_MustBeFlooded_BeforeRiceIsTransplanted()
        {
            yield return null;
            var paddy = Place(PaddyField, PondSideCell);
            Plow(paddy);
            Assert.AreEqual(0f, paddy.Water, 0.001f);
            Assert.IsTrue(paddy.IsThirsty, "An empty paddy wants water");
            StringAssert.Contains("ngập", paddy.PlantBlocker(Rice));

            for (int i = 0; i < 3; i++) paddy.AddWater(paddy.WaterPerTrip);
            Assert.GreaterOrEqual(paddy.Water, FarmPlot.FloodedLevel, "Three loads of water flood it");
            Assert.IsFalse(paddy.IsThirsty);
            Assert.IsTrue(paddy.Plant(Rice));
        }

        [UnityTest]
        public IEnumerator WaterEvaporates_ADryCropStopsGrowing_ThenDies()
        {
            yield return null;
            var plot = Garden;
            Assert.AreEqual(1f, plot.Water, 0.05f, "Starting garden plots are watered");
            Assert.IsTrue(plot.Plant(Berry));
            typeof(FarmPlot).GetField("droughtWitherTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(plot, 3f);

            plot.SetWater(0.5f);
            yield return WaitGameSeconds(2f);
            Assert.Less(plot.Water, 0.5f, "Water dries up over time");

            plot.SetWater(0f);
            yield return null;
            float progress = plot.GrowthProgress;
            yield return WaitGameSeconds(1.5f);
            Assert.AreEqual(progress, plot.GrowthProgress, 0.0001f, "No water → no growth");
            Assert.IsTrue(plot.IsThirsty);

            yield return WaitUntil(() => plot.State == FarmPlotState.Withered, 5f);
            Assert.AreEqual(FarmPlotState.Withered, plot.State, "Left dry too long, the crop dies");
        }

        [UnityTest]
        public IEnumerator Farmer_CarriesWaterFromThePond_FloodsThePaddy_ThenTransplantsRice()
        {
            yield return null;
            var paddy = Place(PaddyField, PondSideCell);
            Plow(paddy);
            FarmManager.Instance.SelectCrop(Rice);

            var farmer = Farmer;
            farmer.AssignJob(NpcJobFactory.Create(farmer, paddy));
            bool sawBucket = false;
            float deadline = Time.time + 60f;
            while (paddy.State != FarmPlotState.Growing && Time.time < deadline)
            {
                if (farmer.CurrentJob is FarmJob job && job.IsCarryingWater)
                {
                    sawBucket = true;
                    Assert.AreEqual("gánh nước", job.Description);
                }
                yield return null;
            }

            Assert.IsTrue(sawBucket, "The farmer carries water in a bucket");
            Assert.AreEqual(FarmPlotState.Growing, paddy.State, "Once flooded, rice is transplanted");
            Assert.GreaterOrEqual(paddy.Water, 0.5f);
        }

        [UnityTest]
        public IEnumerator Well_GivesWaterForDryFields_ButNotEnoughForPaddies()
        {
            yield return null;
            Assert.IsFalse(TechManager.Instance.IsBuildingUnlocked(Well), "Wells come with Farming");
            ResourceManager.Instance.AddResource(Res("Knowledge"), 30);
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Farming.asset")));
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Rice.asset")));
            Assert.IsTrue(TechManager.Instance.IsBuildingUnlocked(Well));

            var well = BuildingPlacer.Instance.PlaceBuilding(Well, WellCell, spendResources: false).GetComponent<WaterSource>();
            ResourceManager.Instance.AddResource(Res("Wood"), 10);
            StringAssert.Contains("nguồn nước lớn", BuildingPlacer.Instance.PlacementBlocker(PaddyField, DryCell),
                "A well can't flood a paddy");

            var plot = Place(DryField, DryCell);
            Plow(plot);
            Assert.AreSame(well, WaterSource.Nearest(plot.transform.position));
            Assert.IsTrue(plot.Plant(Berry));
            Assert.IsTrue(plot.IsThirsty, "Freshly sown seeds need water");

            var farmer = Farmer;
            farmer.AssignJob(NpcJobFactory.Create(farmer, plot));
            yield return WaitUntil(() => plot.Water > 0.3f, 40f);
            Assert.Greater(plot.Water, 0.3f, "The farmer waters the field from the well next to it");
        }

        [UnityTest]
        public IEnumerator IdleFarmer_WatersThirstyCropsByItself()
        {
            yield return null;
            var plot = Garden;
            Assert.IsTrue(plot.Plant(Berry));
            plot.SetWater(0f);
            NpcController.AutoWorkEnabled = true;

            yield return WaitUntil(() => plot.Water > 0.3f, 60f);
            Assert.Greater(plot.Water, 0.3f, "Farmers water dry crops without being told");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsWaterAndPlowing()
        {
            SaveSystem.FileNameOverride = "test_savegame_water.json";
            try
            {
                yield return null;
                var plot = Place(DryField, DryCell);
                Clear(plot);
                plot.DoPlowWork();
                plot.SetWater(0.5f);

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var restored = BuildingInstance.All.Single(b => b.Data == DryField).GetComponent<FarmPlot>();
                Assert.AreEqual(FarmPlotState.Unplowed, restored.State);
                Assert.AreEqual(1f / 3f, restored.PlowProgress, 0.001f, "Half-plowed soil stays half plowed");
                Assert.AreEqual(0.5f, restored.Water, 0.02f, "Water level is saved");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
