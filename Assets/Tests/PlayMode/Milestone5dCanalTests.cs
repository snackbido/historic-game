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
    /// <summary>Milestone 5d – F6: đào mương nối từ ao → nước tự chảy (tối đa 8 ô) → ruộng sát mương tự được tưới.</summary>
    public class Milestone5dCanalTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData CanalData => Asset<BuildingData>("Assets/_Data/BuildingData_Canal.asset");
        private static BuildingData DryField => Asset<BuildingData>("Assets/_Data/BuildingData_DryField.asset");
        private static BuildingData PaddyField => Asset<BuildingData>("Assets/_Data/BuildingData_PaddyField.asset");
        private static TechNode Irrigation => Asset<TechNode>("Assets/_Data/TechNode_Irrigation.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");

        // Ao cá ở (4.5, 6): ô (4,4) → (4.5, 4.5) nằm sát mép ao; mương chạy thẳng xuống phía nam.
        private static Vector3Int Cell(int y) => new Vector3Int(4, y, 0);

        private static Canal PlaceCanal(Vector3Int cell, bool dug = true)
        {
            var canal = BuildingPlacer.Instance.PlaceBuilding(CanalData, cell, spendResources: false).GetComponent<Canal>();
            if (dug) canal.SetDigProgress(99);
            return canal;
        }

        private static FarmPlot PlaceField(BuildingData data, Vector3Int cell)
        {
            var plot = BuildingPlacer.Instance.PlaceBuilding(data, cell, spendResources: false).GetComponent<FarmPlot>();
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
            return plot;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [Test]
        public void IrrigationTech_ComesAfterRice_AndUnlocksCanals()
        {
            Assert.AreEqual("Thủy lợi", Irrigation.displayName);
            CollectionAssert.Contains(Irrigation.prerequisites, Asset<TechNode>("Assets/_Data/TechNode_Rice.asset"));
            CollectionAssert.Contains(Irrigation.unlockedBuildingIds, "canal");
            Assert.IsFalse(CanalData.unlockedByDefault);
        }

        [UnityTest]
        public IEnumerator Canal_MustBeDug_BeforeWaterFlows()
        {
            yield return null;
            var canal = PlaceCanal(Cell(4), dug: false);
            yield return null;
            Assert.IsFalse(canal.IsFlowing, "A staked-out canal is still just ground");
            Assert.IsTrue(canal.transform.Find("Marked").gameObject.activeSelf);

            for (int i = 0; i < 3; i++) Assert.IsFalse(canal.DoDigWork());
            Assert.IsTrue(canal.DoDigWork(), "Four rounds of digging finish a stretch");
            yield return null;
            Assert.IsTrue(canal.IsFlowing, "Next to the pond, water runs in");
            Assert.AreEqual(1, canal.Distance);
            Assert.IsTrue(canal.transform.Find("CenterWater").gameObject.activeSelf, "Water is visible in the canal");
            Assert.IsFalse(canal.transform.Find("Marked").gameObject.activeSelf);
            Assert.IsTrue(canal.GetComponent<WaterSource>().enabled, "A flowing canal is a water source");
        }

        [UnityTest]
        public IEnumerator Water_RunsAlongConnectedCanals_UpToEightTiles()
        {
            yield return null;
            var canals = Enumerable.Range(0, 9).Select(i => PlaceCanal(Cell(4 - i))).ToList();
            yield return null;

            Assert.IsTrue(canals[7].IsFlowing, "8 tiles from the pond still get water");
            Assert.AreEqual(8, canals[7].Distance);
            Assert.IsFalse(canals[8].IsFlowing, "Water doesn't run uphill forever (water wheel comes later)");
            Assert.IsTrue(canals[3].transform.Find("ArmN").gameObject.activeSelf, "Neighbouring canals are joined up");
            Assert.IsFalse(canals[3].transform.Find("ArmE").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Gap_InTheCanal_StopsTheWater()
        {
            yield return null;
            var head = PlaceCanal(Cell(4));
            var cut = PlaceCanal(Cell(3), dug: false);
            var tail = PlaceCanal(Cell(2));
            yield return null;
            Assert.IsTrue(head.IsFlowing);
            Assert.IsFalse(tail.IsFlowing, "An undug stretch blocks the water");

            while (!cut.IsDug) cut.DoDigWork();
            yield return null;
            Assert.IsTrue(tail.IsFlowing, "Digging the gap lets the water through");
        }

        [UnityTest]
        public IEnumerator FieldBesideAFlowingCanal_WatersItself()
        {
            yield return null;
            PlaceCanal(Cell(4));
            PlaceCanal(Cell(3));
            var field = PlaceField(DryField, new Vector3Int(3, 3, 0));
            field.SetWater(0f);
            yield return null;

            Assert.IsTrue(field.IsIrrigated);
            Assert.IsFalse(field.IsThirsty, "Nobody needs to carry water to an irrigated field");
            yield return WaitUntil(() => field.Water >= 0.95f, 25f);
            Assert.GreaterOrEqual(field.Water, 0.95f, "The canal fills the field");
        }

        [UnityTest]
        public IEnumerator Paddy_CanBeBuiltFarFromThePond_BesideACanal()
        {
            yield return null;
            Rm.AddResource(Res("Knowledge"), 45);
            Rm.AddResource(Res("Wood"), 10);
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Farming.asset")));
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Rice.asset")));
            Assert.IsTrue(TechManager.Instance.TryUnlock(Irrigation));
            Assert.IsTrue(TechManager.Instance.IsBuildingUnlocked(CanalData));

            var paddyCell = new Vector3Int(3, 0, 0);
            StringAssert.Contains("nguồn nước lớn", BuildingPlacer.Instance.PlacementBlocker(PaddyField, paddyCell), "Too far from the pond");
            for (int y = 4; y >= 0; y--) PlaceCanal(Cell(y));
            yield return null;
            Assert.IsNull(BuildingPlacer.Instance.PlacementBlocker(PaddyField, paddyCell), "A flowing canal is enough water for a paddy");

            var paddy = PlaceField(PaddyField, paddyCell);
            paddy.SetWater(0f);
            yield return null;
            yield return WaitUntil(() => paddy.Water >= FarmPlot.FloodedLevel, 25f);
            Assert.GreaterOrEqual(paddy.Water, FarmPlot.FloodedLevel, "The canal floods the paddy without carrying water");
            Assert.IsFalse(paddy.IsThirsty);
        }

        [UnityTest]
        public IEnumerator Farmer_DigsTheCanal_WhenOrdered()
        {
            yield return null;
            var canal = PlaceCanal(Cell(4), dug: false);
            var farmer = NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");
            var job = NpcJobFactory.Create(farmer, canal);
            Assert.IsInstanceOf<DigCanalJob>(job);
            farmer.AssignJob(job);

            yield return WaitUntil(() => canal.IsDug, 60f);
            Assert.IsTrue(canal.IsDug, "The farmer digs the canal");
            yield return null;
            Assert.IsTrue(canal.IsFlowing);
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsDugCanals_AndTheirWater()
        {
            SaveSystem.FileNameOverride = "test_savegame_canal.json";
            try
            {
                yield return null;
                PlaceCanal(Cell(4));
                var half = PlaceCanal(Cell(3), dug: false);
                half.DoDigWork();
                yield return null;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;
                yield return null;

                var restored = Canal.All.Where(c => c != null && c.isActiveAndEnabled).ToList();
                Assert.AreEqual(2, restored.Count);
                var head = restored.Single(c => c.Cell == Cell(4));
                var rest = restored.Single(c => c.Cell == Cell(3));
                Assert.IsTrue(head.IsDug);
                Assert.IsTrue(head.IsFlowing, "Water flows again after loading");
                Assert.AreEqual(1, rest.DigProgressCount, "Half-dug stretch keeps its progress");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }

        private static ResourceManager Rm => ResourceManager.Instance;
    }
}
