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
    /// <summary>Milestone 5b – E1: công trình nâng cấp 5 cấp.</summary>
    public class Milestone5bBuildingTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData Hut => Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset");
        private static BuildingData Storage => Asset<BuildingData>("Assets/_Data/BuildingData_Storage.asset");
        private static ResourceTypeData Wood => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Wood.asset");
        private static ResourceTypeData Knowledge => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");

        private static BuildingInstance Place(BuildingData data, int cellX, int cellY) =>
            BuildingPlacer.Instance.PlaceBuilding(data, new Vector3Int(cellX, cellY, 0), spendResources: false);

        private static int ActiveModelLevel(BuildingInstance building)
        {
            for (int level = 1; level <= 5; level++)
            {
                var model = building.transform.Find($"Level{level}");
                if (model != null && model.gameObject.activeSelf) return level;
            }
            return 0;
        }

        [UnityTest]
        public IEnumerator Hut_UpgradesThroughFiveLevels_AddingHousing()
        {
            yield return null;
            var hut = Place(Hut, -5, 1);
            Assert.AreEqual(1, hut.Level);
            Assert.AreEqual("Lều da", hut.LevelName);
            Assert.AreEqual(2, hut.Housing, "Level-1 hut has room for 2 children");
            Assert.AreEqual(1, ActiveModelLevel(hut));

            ResourceManager.Instance.AddResource(Wood, 200);
            ResourceManager.Instance.AddResource(Knowledge, 50);
            for (int i = 0; i < 4; i++) Assert.IsTrue(hut.TryUpgrade(), $"Upgrade to level {i + 2}");

            Assert.AreEqual(5, hut.Level);
            Assert.AreEqual("Nhà dài", hut.LevelName);
            Assert.AreEqual(6, hut.Housing, "Level-5 longhouse has room for 6 children");
            Assert.AreEqual(5, ActiveModelLevel(hut), "Model changes with the level");
            Assert.AreEqual(200 - (15 + 25 + 40 + 60), ResourceManager.Instance.GetAmount(Wood), "Each upgrade costs wood");
            Assert.IsTrue(hut.IsMaxLevel);
            Assert.IsFalse(hut.TryUpgrade(), "Cannot go past level 5");
        }

        [UnityTest]
        public IEnumerator Upgrade_FailsWithoutResources_AndExplainsWhy()
        {
            yield return null;
            var storage = Place(Storage, -5, 1);
            Assert.AreEqual(30, storage.StorageCapacity);

            Assert.IsFalse(storage.TryUpgrade());
            Assert.AreEqual("Chưa đủ tài nguyên", storage.UpgradeBlocker());
            Assert.AreEqual(1, storage.Level);
            StringAssert.Contains("Lên Kho lớn", BuildingInfoPanelUI.DescribeNext(storage, storage.UpgradeBlocker()));

            ResourceManager.Instance.AddResource(Wood, 25);
            Assert.IsTrue(storage.TryUpgrade());
            Assert.AreEqual(60, storage.StorageCapacity);
        }

        [UnityTest]
        public IEnumerator ClickingBuilding_SelectsIt_ShowsPanel_AndSelectingVillagerDeselects()
        {
            yield return null;
            var hut = Place(Hut, -4, 1);
            Physics.SyncTransforms();
            yield return null;

            Vector2 screen = Camera.main.WorldToScreenPoint(hut.transform.position + Vector3.up * 0.5f);
            SelectionManager.Instance.SelectAt(screen, additive: false);
            yield return null;

            Assert.AreSame(hut, SelectionManager.Instance.SelectedBuilding, "Clicking a hut selects it");
            Assert.IsTrue(hut.transform.Find("SelectionRing").gameObject.activeSelf);
            var panel = GameObject.Find("BuildingInfoPanel").transform.Find("Content").gameObject;
            Assert.IsTrue(panel.activeSelf, "Building info panel opens");

            SelectionManager.Instance.SetSelection(new[] { NpcController.All[0] });
            yield return null;
            Assert.IsNull(SelectionManager.Instance.SelectedBuilding, "Selecting a villager deselects the building");
            Assert.IsFalse(panel.activeSelf);
            Assert.IsFalse(hut.transform.Find("SelectionRing").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsBuildingLevels()
        {
            SaveSystem.FileNameOverride = "test_savegame_buildings.json";
            try
            {
                yield return null;
                var hut = Place(Hut, -5, 1);
                ResourceManager.Instance.AddResource(Wood, 100);
                ResourceManager.Instance.AddResource(Knowledge, 20);
                Assert.IsTrue(hut.TryUpgrade());
                Assert.IsTrue(hut.TryUpgrade());

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                var restored = BuildingInstance.All.Single(b => b.Data == Hut);
                Assert.AreEqual(3, restored.Level, "Hut level survives save/load");
                Assert.AreEqual(3, ActiveModelLevel(restored));
                Assert.AreEqual(4, restored.Housing);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
