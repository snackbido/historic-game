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
    /// <summary>Milestone 5d – F7: guồng nước bên ao bơm vào mương → nước đi xa tới 20 ô, tưới nhanh gấp đôi.</summary>
    public class Milestone5dWaterWheelTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData CanalData => Asset<BuildingData>("Assets/_Data/BuildingData_Canal.asset");
        private static BuildingData WheelData => Asset<BuildingData>("Assets/_Data/BuildingData_WaterWheel.asset");
        private static TechNode WheelTech => Asset<TechNode>("Assets/_Data/TechNode_WaterWheel.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceManager Rm => ResourceManager.Instance;

        // Ao cá ở (4.5, 6): mương bắt đầu ô (4,4) chạy xuống nam; guồng ở ô (5,4) sát mép ao, cạnh đầu mương.
        private static Vector3Int Cell(int y) => new Vector3Int(4, y, 0);
        private static readonly Vector3Int WheelCell = new Vector3Int(5, 4, 0);

        private static Canal PlaceCanal(Vector3Int cell)
        {
            var canal = BuildingPlacer.Instance.PlaceBuilding(CanalData, cell, spendResources: false).GetComponent<Canal>();
            canal.SetDigProgress(99);
            return canal;
        }

        private static WaterWheel PlaceWheel(Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(WheelData, cell, spendResources: false).GetComponent<WaterWheel>();

        [Test]
        public void WaterWheelTech_ComesAfterIrrigation()
        {
            Assert.AreEqual("Guồng nước", WheelTech.displayName);
            CollectionAssert.Contains(WheelTech.prerequisites, Asset<TechNode>("Assets/_Data/TechNode_Irrigation.asset"));
            CollectionAssert.Contains(WheelTech.unlockedBuildingIds, "water_wheel");
            Assert.IsTrue(WheelData.requiresOpenWater);
        }

        [UnityTest]
        public IEnumerator Wheel_PushesWaterFurtherThanItFlowsByItself()
        {
            yield return null;
            var canals = Enumerable.Range(0, 16).Select(i => PlaceCanal(Cell(4 - i))).ToList();
            yield return null;
            Assert.IsFalse(canals[8].IsFlowing, "By itself water only runs 8 tiles");

            var wheel = PlaceWheel(WheelCell);
            yield return null;
            Assert.IsTrue(wheel.IsTurning);
            Assert.IsTrue(canals[8].IsFlowing, "The wheel lifts water further");
            Assert.IsTrue(canals[15].IsFlowing, "16 tiles is within the wheel's reach");
            Assert.IsTrue(canals[15].IsPumped);
            Assert.AreEqual(16, canals[15].Distance);
            Assert.IsTrue(canals[0].transform.Find("ArmE").gameObject.activeSelf, "The canal joins up with the wheel");
        }

        [UnityTest]
        public IEnumerator PumpedCanal_WatersFieldsTwiceAsFast()
        {
            yield return null;
            var head = PlaceCanal(Cell(4));
            yield return null;
            float gravityRate = head.IrrigationRate;
            Assert.Greater(gravityRate, 0f);

            PlaceWheel(WheelCell);
            yield return null;
            Assert.AreEqual(gravityRate * 2f, head.IrrigationRate, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Wheel_StandsStill_UntilACanalIsDugBesideIt()
        {
            yield return null;
            var wheel = PlaceWheel(WheelCell);
            yield return null;
            Assert.IsFalse(wheel.IsTurning, "Nothing to pour into");
            Assert.IsFalse(wheel.transform.Find("Level1/Pouring").gameObject.activeSelf);

            PlaceCanal(Cell(4));
            yield return null;
            Assert.IsTrue(wheel.IsTurning);
            Assert.IsTrue(wheel.transform.Find("Level1/Pouring").gameObject.activeSelf, "Water pours into the canal");
            var spokes = wheel.transform.Find("Level1/Wheel");
            Quaternion before = spokes.localRotation;
            yield return new WaitForSeconds(0.5f);
            Assert.AreNotEqual(before, spokes.localRotation, "The wheel turns");
        }

        [UnityTest]
        public IEnumerator Wheel_MustStandRightBesideThePond()
        {
            yield return null;
            Rm.AddResource(Res("Knowledge"), 65);
            Rm.AddResource(Res("Wood"), 10);
            foreach (var tech in new[] { "Farming", "Rice", "Irrigation", "WaterWheel" })
                Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>($"Assets/_Data/TechNode_{tech}.asset")), tech);

            for (int y = 4; y >= 0; y--) PlaceCanal(Cell(y));
            yield return null;
            StringAssert.Contains("sát mép ao", BuildingPlacer.Instance.PlacementBlocker(WheelData, new Vector3Int(5, 0, 0)),
                "A wheel can't lift water out of a canal");
            Assert.IsNull(BuildingPlacer.Instance.PlacementBlocker(WheelData, WheelCell));
        }

        [UnityTest]
        public IEnumerator SaveLoad_TheWheelKeepsPumping()
        {
            SaveSystem.FileNameOverride = "test_savegame_wheel.json";
            try
            {
                yield return null;
                for (int i = 0; i < 10; i++) PlaceCanal(Cell(4 - i));
                PlaceWheel(WheelCell);
                yield return null;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;
                yield return null;

                var wheel = WaterWheel.All.Single(w => w != null && w.isActiveAndEnabled);
                Assert.IsTrue(wheel.IsTurning);
                var far = Canal.All.Single(c => c != null && c.isActiveAndEnabled && c.Cell == Cell(-5));
                Assert.IsTrue(far.IsFlowing, "Tile 10 still gets pumped water after loading");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
