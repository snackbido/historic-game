using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 5b – E4: sức chứa lương thực theo kho, đồ dễ hỏng để ngoài kho hỏng dần.</summary>
    public class Milestone5bStorageTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceManager Rm => ResourceManager.Instance;

        private static BuildingInstance PlaceStorage() =>
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Storage.asset"),
                new Vector3Int(-5, 1, 0), spendResources: false);

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator WithoutStorage_EachFoodHoldsOnly20_ExtraIsWasted()
        {
            yield return null;
            string last = null;
            void Handler(string m) => last = m;
            EventBus.OnNotification += Handler;

            Assert.AreEqual(20, Rm.FoodCapacity);
            Rm.AddResource(Res("Berries"), 30);
            Rm.AddResource(Res("Rice"), 5);

            EventBus.OnNotification -= Handler;
            Assert.AreEqual(20, Rm.GetAmount(Res("Berries")), "Berries are capped at 20 without a storehouse");
            Assert.AreEqual(5, Rm.GetAmount(Res("Rice")), "The cap is per food type");
            StringAssert.Contains("Kho đầy", last);
            Rm.AddResource(Res("Wood"), 500);
            Assert.AreEqual(500, Rm.GetAmount(Res("Wood")), "Only food is limited by storage");
        }

        [UnityTest]
        public IEnumerator Storehouse_AddsCapacity_ByLevel()
        {
            yield return null;
            var storage = PlaceStorage();
            Assert.AreEqual(20 + 30, Rm.FoodCapacity, "Level-1 storehouse adds 30 per food type");

            Rm.AddResource(Res("Wood"), 25);
            Assert.IsTrue(storage.TryUpgrade());
            Assert.AreEqual(20 + 60, Rm.FoodCapacity, "Level-2 storehouse holds 60 per type");
            StringAssert.Contains("Sức chứa: 80/loại", FoodBreakdownUI.Describe(Rm));
        }

        [UnityTest]
        public IEnumerator MeatLeftOutside_SpoilsOverTime()
        {
            yield return null;
            ResourceManager.SpoilageEnabled = true;
            Rm.AddResource(Res("Meat"), 12);
            Assert.AreEqual(12, Rm.SpoilingAmount(Res("Meat")), "No storehouse → all meat is outside");
            StringAssert.Contains("Thịt 12 (hỏng dần)", FoodBreakdownUI.Describe(Rm));

            yield return WaitGameSeconds(17f);
            Assert.Less(Rm.GetAmount(Res("Meat")), 12, "Meat outside a storehouse rots");
        }

        [UnityTest]
        public IEnumerator MeatInsideStorehouse_KeepsFresh_AndGrainNeverSpoils()
        {
            yield return null;
            PlaceStorage();
            ResourceManager.SpoilageEnabled = true;
            Rm.AddResource(Res("Meat"), 12);
            Rm.AddResource(Res("Rice"), 15);
            Assert.AreEqual(0, Rm.SpoilingAmount(Res("Meat")), "12 meat fits inside the storehouse (30)");

            yield return WaitGameSeconds(35f);
            Assert.AreEqual(12, Rm.GetAmount(Res("Meat")), "Stored meat does not rot");
            Assert.AreEqual(15, Rm.GetAmount(Res("Rice")), "Rice never rots");
        }

        [UnityTest]
        public IEnumerator OnlyThePartAboveStorehouseCapacity_Spoils()
        {
            yield return null;
            PlaceStorage(); // 30 được bảo quản, sức chứa tổng 50
            ResourceManager.SpoilageEnabled = true;
            Rm.AddResource(Res("Fish"), 40);
            Assert.AreEqual(10, Rm.SpoilingAmount(Res("Fish")));

            yield return WaitGameSeconds(130f); // 10 → 8 → 6 → 4 → 3 → 2 → 1 → 0, mỗi 15s
            Assert.AreEqual(30, Rm.GetAmount(Res("Fish")), "Fish rots down to what the storehouse protects, no further");
        }
    }
}
