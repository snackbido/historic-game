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
    /// <summary>Milestone 5d – F3: gieo thóc giống ở ruộng mạ → nhổ mạ → cấy sang ruộng nước; gặt lúa để lại thóc giống.</summary>
    public class Milestone5dSeedlingTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static BuildingData Seedbed => Asset<BuildingData>("Assets/_Data/BuildingData_Seedbed.asset");
        private static BuildingData PaddyField => Asset<BuildingData>("Assets/_Data/BuildingData_PaddyField.asset");
        private static CropData SeedlingCrop => Asset<CropData>("Assets/_Data/CropData_RiceSeedling.asset");
        private static CropData Rice => Asset<CropData>("Assets/_Data/CropData_Rice.asset");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceTypeData SeedGrain => Res("RiceSeed");
        private static ResourceTypeData Seedlings => Res("Seedling");
        private static ResourceManager Rm => ResourceManager.Instance;

        // Cạnh ao cá (4.5, 6): ruộng mạ ô (2,5) → (2.5, 5.5); ruộng nước ô (3,4) → (3.5, 4.5).
        private static readonly Vector3Int SeedbedCell = new Vector3Int(2, 5, 0);
        private static readonly Vector3Int PaddyCell = new Vector3Int(3, 4, 0);

        private static FarmPlot Place(BuildingData data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(data, cell, spendResources: false).GetComponent<FarmPlot>();

        private static void ReadyAndFlooded(FarmPlot plot)
        {
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
            plot.SetWater(1f);
        }

        private static void UnlockRice()
        {
            Rm.AddResource(Res("Knowledge"), 30);
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Farming.asset")));
            Assert.IsTrue(TechManager.Instance.TryUnlock(Asset<TechNode>("Assets/_Data/TechNode_Rice.asset")));
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [Test]
        public void RiceHarvest_LeavesSeedGrain_SeedbedYieldsSeedlingBundles()
        {
            Assert.AreEqual(FieldType.Seedbed, SeedlingCrop.fieldType);
            Assert.AreSame(SeedGrain, SeedlingCrop.plantCost.Single().type, "Seedlings are sown from seed grain");
            Assert.AreEqual(3, SeedlingCrop.harvestYield.Single(y => y.type == Seedlings).amount, "One seedbed gives 3 bundles");
            Assert.AreSame(Seedlings, Rice.plantCost.Single().type, "Rice is transplanted from seedlings");
            Assert.AreEqual(2, Rice.harvestYield.Single(y => y.type == SeedGrain).amount, "Each rice harvest keeps 2 seed grain");
            Assert.AreEqual(6, Rice.harvestYield.Single(y => y.type == Res("Rice")).amount);
        }

        [UnityTest]
        public IEnumerator RiceTech_UnlocksSeedbeds_AndGivesSomeSeedGrain()
        {
            yield return null;
            Assert.IsFalse(TechManager.Instance.IsBuildingUnlocked(Seedbed));
            UnlockRice();
            Assert.IsTrue(TechManager.Instance.IsBuildingUnlocked(Seedbed));
            Assert.IsTrue(TechManager.Instance.IsCropUnlocked(SeedlingCrop));
            Assert.AreEqual(4, Rm.GetAmount(SeedGrain), "Learning rice farming comes with seed grain for a first crop");
        }

        [UnityTest]
        public IEnumerator Seedbed_SownWithSeedGrain_GrowsSeedlings_ThatArePulledIntoBundles()
        {
            yield return null;
            var bed = Place(Seedbed, SeedbedCell);
            Assert.AreEqual(FieldType.Seedbed, bed.FieldType);
            ReadyAndFlooded(bed);
            StringAssert.Contains("Thóc giống", bed.PlantBlocker(SeedlingCrop), "No seed grain → nothing to sow");

            Rm.AddResource(SeedGrain, 2);
            Assert.IsTrue(bed.Plant(SeedlingCrop));
            Assert.AreEqual(1, Rm.GetAmount(SeedGrain), "Sowing uses one seed grain");

            yield return WaitUntil(() => bed.State == FarmPlotState.ReadyToHarvest,
                SeedlingCrop.timeToSprout + SeedlingCrop.timeToMature + 5f);
            Assert.AreEqual(FarmPlotState.ReadyToHarvest, bed.State);
            Assert.IsTrue(FarmManager.Instance.TryInteract(bed));
            Assert.AreEqual(3, Rm.GetAmount(Seedlings), "Pulling the seedlings gives 3 bundles");
        }

        [UnityTest]
        public IEnumerator Paddy_NeedsASeedlingBundle_ToTransplantRice()
        {
            yield return null;
            var paddy = Place(PaddyField, PaddyCell);
            ReadyAndFlooded(paddy);
            StringAssert.Contains("Mạ", paddy.PlantBlocker(Rice), "Rice can't be transplanted without seedlings");
            Assert.IsFalse(paddy.Plant(Rice));

            Rm.AddResource(Seedlings, 1);
            Assert.IsNull(paddy.PlantBlocker(Rice));
            Assert.IsTrue(paddy.Plant(Rice));
            Assert.AreEqual(0, Rm.GetAmount(Seedlings), "Transplanting uses the bundle");
        }

        [UnityTest]
        public IEnumerator WetFields_PickTheirOnlyCrop_WithoutChoosingASeed()
        {
            yield return null;
            var bed = Place(Seedbed, SeedbedCell);
            var paddy = Place(PaddyField, PaddyCell);
            Assert.IsNull(FarmManager.Instance.CropFor(paddy), "Rice isn't known yet");

            UnlockRice();
            Assert.AreSame(SeedlingCrop, FarmManager.Instance.CropFor(bed));
            Assert.AreSame(Rice, FarmManager.Instance.CropFor(paddy));
        }

        [UnityTest]
        public IEnumerator Farmer_RaisesSeedlings_ThenTransplantsThemIntoThePaddy()
        {
            yield return null;
            UnlockRice();
            var bed = Place(Seedbed, SeedbedCell);
            var paddy = Place(PaddyField, PaddyCell);
            var farmer = NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");

            // Ruộng mạ: khai hoang → cày → gánh nước cho ngập → gieo thóc → chờ mạ lên → nhổ mạ.
            farmer.AssignJob(NpcJobFactory.Create(farmer, bed));
            yield return WaitUntil(() => Rm.GetAmount(Seedlings) >= 3, 120f);
            Assert.GreaterOrEqual(Rm.GetAmount(Seedlings), 3, "The farmer raises and pulls a seedbed of seedlings");
            Assert.AreEqual(3, Rm.GetAmount(SeedGrain), "One seed grain went into the seedbed");

            // Ruộng nước: đắp bờ → cày → cho ngập → cấy mạ.
            farmer.AssignJob(NpcJobFactory.Create(farmer, paddy));
            yield return WaitUntil(() => paddy.State == FarmPlotState.Growing, 90f);
            Assert.AreEqual(FarmPlotState.Growing, paddy.State, "…then transplants them into the flooded paddy");
            Assert.AreSame(Rice, paddy.Crop);
        }
    }
}
