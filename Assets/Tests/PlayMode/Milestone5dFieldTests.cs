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
    /// <summary>Milestone 5d – F1: người chơi xây ruộng cạn/ruộng nước, nông dân khai hoang rồi mới trồng.</summary>
    public class Milestone5dFieldTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData DryField => Asset<BuildingData>("Assets/_Data/BuildingData_DryField.asset");
        private static BuildingData PaddyField => Asset<BuildingData>("Assets/_Data/BuildingData_PaddyField.asset");
        private static CropData Berry => Asset<CropData>("Assets/_Data/CropData_Berry.asset");
        private static CropData Rice => Asset<CropData>("Assets/_Data/CropData_Rice.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");

        // Ô (-4,-5) → tâm (-3.5, -4.5): đất trống phía tây nam trại. Ô (2,5) → tâm (2.5, 5.5): cạnh ao cá (4.5, 6).
        private static readonly Vector3Int DryCell = new Vector3Int(-4, -5, 0);
        private static readonly Vector3Int PondSideCell = new Vector3Int(2, 5, 0);

        private static FarmPlot Place(BuildingData data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(data, cell, spendResources: false).GetComponent<FarmPlot>();

        /// <summary>Khai hoang + cày + (ruộng nước) cho ngập — sẵn sàng gieo/cấy.</summary>
        private static void ReadyToPlant(FarmPlot plot)
        {
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
            plot.SetWater(1f);
        }

        private static NpcController Farmer => NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static void UnlockRice()
        {
            ResourceManager.Instance.AddResource(Res("Knowledge"), 30);
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Farming.asset")));
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Rice.asset")));
        }

        [UnityTest]
        public IEnumerator NewDryField_IsWild_FarmerClearsIt_ThenPlantsBerries()
        {
            yield return null;
            var plot = Place(DryField, DryCell);
            Assert.AreEqual(FarmPlotState.Wild, plot.State, "A new field starts as wild land");
            Assert.AreEqual(FieldType.Dry, plot.FieldType);
            Assert.IsFalse(plot.Plant(Berry), "Nothing can be planted before the land is cleared");

            var farmer = Farmer;
            farmer.AssignJob(NpcJobFactory.Create(farmer, plot));
            StringAssert.Contains("khai hoang", farmer.CurrentJob.Description);

            yield return WaitUntil(() => plot.State != FarmPlotState.Wild, 40f);
            Assert.AreEqual(FarmPlotState.Unplowed, plot.State, "The farmer clears the field (next: plowing)");

            FarmManager.Instance.SelectCrop(Berry);
            yield return WaitUntil(() => plot.State == FarmPlotState.Growing, 15f);
            Assert.AreEqual(FarmPlotState.Growing, plot.State, "…and keeps working it: plants the chosen seed");
        }

        [UnityTest]
        public IEnumerator Rice_OnlyGrowsInAPaddy_OtherCropsOnlyOnDryLand()
        {
            yield return null;
            var garden = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            Assert.AreEqual(FarmPlotState.Empty, garden.State, "The starting garden plots are already cleared");
            Assert.IsFalse(garden.Plant(Rice), "Rice can't grow on dry land");

            var paddy = Place(PaddyField, PondSideCell);
            Assert.AreEqual(FieldType.Paddy, paddy.FieldType);
            ReadyToPlant(paddy);
            Assert.IsFalse(paddy.Plant(Berry), "Berries don't grow in a flooded paddy");
            Assert.IsTrue(paddy.Plant(Rice));
        }

        [UnityTest]
        public IEnumerator PaddyField_MustBeBuiltNextToWater_NotOnIt()
        {
            yield return null;
            var placer = BuildingPlacer.Instance;
            StringAssert.Contains("mở khóa", placer.PlacementBlocker(PaddyField, PondSideCell), "Paddies come with the rice tech");

            UnlockRice();
            ResourceManager.Instance.AddResource(Res("Wood"), 10);
            Assert.IsNull(placer.PlacementBlocker(PaddyField, PondSideCell), "Next to the pond is fine");
            StringAssert.Contains("nguồn nước", placer.PlacementBlocker(PaddyField, new Vector3Int(-3, 6, 0)), "Far from water is not");
            StringAssert.Contains("mặt nước", placer.PlacementBlocker(DryField, new Vector3Int(4, 5, 0)), "Nothing can be built on the pond");
            Assert.IsNull(placer.PlacementBlocker(DryField, DryCell), "Dry fields go anywhere");
        }

        [UnityTest]
        public IEnumerator EachFieldType_RemembersItsOwnSeed()
        {
            yield return null;
            var dry = Place(DryField, DryCell);
            var paddy = Place(PaddyField, PondSideCell);
            var farm = FarmManager.Instance;

            farm.SelectCrop(Rice);
            farm.SelectCrop(Berry);
            Assert.AreSame(Berry, farm.CropFor(dry));
            Assert.AreSame(Rice, farm.CropFor(paddy), "Choosing berries doesn't make paddies forget rice");

            farm.SelectCrop(null);
            Assert.IsNull(farm.CropFor(dry));
            Assert.IsNull(farm.CropFor(paddy));
        }

        [UnityTest]
        public IEnumerator IdleFarmer_ClearsANewFieldByItself()
        {
            yield return null;
            var plot = Place(DryField, DryCell);
            NpcController.AutoWorkEnabled = true;

            yield return WaitUntil(() => plot.State != FarmPlotState.Wild, 60f);
            Assert.AreEqual(FarmPlotState.Unplowed, plot.State, "Farmers clear wild fields without being told");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsFields_AndClearingProgress()
        {
            SaveSystem.FileNameOverride = "test_savegame_fields.json";
            try
            {
                yield return null;
                var dry = Place(DryField, DryCell);
                for (int i = 0; i < 3; i++) dry.DoClearWork();
                var paddy = Place(PaddyField, PondSideCell);
                ReadyToPlant(paddy);
                Assert.IsTrue(paddy.Plant(Rice));
                float progress = dry.ClearProgress;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var fields = BuildingInstance.All.Where(b => b.Data == DryField || b.Data == PaddyField).ToList();
                Assert.AreEqual(2, fields.Count, "Both fields come back, no duplicates");
                var restoredDry = fields.Single(b => b.Data == DryField).GetComponent<FarmPlot>();
                var restoredPaddy = fields.Single(b => b.Data == PaddyField).GetComponent<FarmPlot>();
                Assert.AreEqual(FarmPlotState.Wild, restoredDry.State);
                Assert.AreEqual(progress, restoredDry.ClearProgress, 0.001f, "Half-cleared land stays half cleared");
                Assert.AreEqual(FarmPlotState.Growing, restoredPaddy.State);
                Assert.AreSame(Rice, restoredPaddy.Crop);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
