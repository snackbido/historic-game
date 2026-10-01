using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 6 – D3: lũ lụt — mưa tưới mọi ruộng, ao tràn làm úng ruộng cạn + hư nhà gần ao; đê chắn nước.</summary>
    public class Milestone6FloodTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;
        private static FloodDisaster Flood => (FloodDisaster)Manager.Find("flood");
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static CropData Berry => Asset<CropData>("Assets/_Data/CropData_Berry.asset");

        private static BuildingInstance Place(string data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>($"Assets/_Data/BuildingData_{data}.asset"), cell, spendResources: false);

        private static FarmPlot ReadyField(string data, Vector3Int cell)
        {
            var plot = Place(data, cell).GetComponent<FarmPlot>();
            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            while (plot.State == FarmPlotState.Unplowed) plot.DoPlowWork();
            plot.SetWater(1f);
            return plot;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        // Ao cá ở (4.5, 6), mép ao bán kính 0,95.
        private static readonly Vector3Int PondSide = new Vector3Int(2, 5, 0);

        [Test]
        public void Flood_IsOneOfTheDisasters_AndIrrigationUnlocksLevees()
        {
            Assert.IsNotNull(Flood);
            Assert.AreEqual("Lũ lụt", Flood.DisplayName);
            CollectionAssert.Contains(Asset<TechNode>("Assets/_Data/TechNode_Irrigation.asset").unlockedBuildingIds, "levee");
        }

        [UnityTest]
        public IEnumerator HeavyRain_WatersEveryField()
        {
            yield return null;
            var garden = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            garden.SetWater(0f);
            Manager.StartNow(Flood);
            StringAssert.Contains("LŨ LỤT", DisasterBannerUI.Describe(Manager));
            yield return WaitUntil(() => garden.Water > 0.3f, 15f);
            Assert.Greater(garden.Water, 0.3f, "Rain falls on far-away fields too");
        }

        [UnityTest]
        public IEnumerator DryField_ByThePond_Drowns()
        {
            yield return null;
            var field = ReadyField("DryField", PondSide);
            Assert.IsTrue(field.Plant(Berry));
            Manager.StartNow(Flood, durationSeconds: 200f);

            yield return WaitUntil(() => Flood.IsFlooded(field.transform.position), 40f);
            Assert.IsTrue(Flood.IsFlooded(field.transform.position), "The pond overflows onto the field");
            yield return WaitUntil(() => field.State == FarmPlotState.Withered, 60f);
            Assert.AreEqual(FarmPlotState.Withered, field.State, "Berries drown under the flood");
        }

        [UnityTest]
        public IEnumerator Paddy_ByThePond_SurvivesTheFlood()
        {
            yield return null;
            var paddy = ReadyField("PaddyField", PondSide);
            ResourceManager.Instance.AddResource(Res("Seedling"), 1);
            Assert.IsTrue(paddy.Plant(Asset<CropData>("Assets/_Data/CropData_Rice.asset")));
            Manager.StartNow(Flood, durationSeconds: 200f);

            yield return WaitUntil(() => false, 70f);
            Assert.IsTrue(Flood.IsFlooded(paddy.transform.position));
            Assert.AreNotEqual(FarmPlotState.Withered, paddy.State, "Rice stands in water anyway");
        }

        [UnityTest]
        public IEnumerator Buildings_ByThePond_AreDamaged_FarOnesAreNot()
        {
            yield return null;
            var near = Place("Storage", new Vector3Int(2, 4, 0));
            var far = Place("Storage", new Vector3Int(-4, -5, 0));
            Manager.StartNow(Flood, durationSeconds: 200f);

            yield return WaitUntil(() => false, 100f);
            Assert.IsTrue(near.IsDamaged, "The flood eats into the storehouse by the pond");
            Assert.IsFalse(far.IsDamaged, "The one up on dry ground is fine");
        }

        [UnityTest]
        public IEnumerator Levee_BetweenPondAndField_HoldsTheWaterBack()
        {
            yield return null;
            var field = ReadyField("DryField", new Vector3Int(1, 5, 0));
            Assert.IsTrue(field.Plant(Berry));
            Place("Levee", PondSide); // giữa ao và ruộng
            Manager.StartNow(Flood, durationSeconds: 200f);

            yield return WaitUntil(() => Flood.FloodRange >= 3f, 80f);
            Assert.IsTrue(Flood.IsFlooded(new Vector3(7f, 0f, 6f)), "Unprotected ground on the other side floods");
            Assert.IsFalse(Flood.IsFlooded(field.transform.position), "The levee keeps the field dry");
            Assert.AreNotEqual(FarmPlotState.Withered, field.State);
        }

        [UnityTest]
        public IEnumerator Flood_Recedes_WhenItEnds()
        {
            yield return null;
            Manager.StartNow(Flood, durationSeconds: 200f);
            yield return WaitUntil(() => Flood.FloodRange > 1f, 60f);
            Assert.IsNotNull(GameObject.Find("FloodWater"), "Flood water spreads around the pond");

            Manager.EndCurrent();
            yield return null;
            Assert.AreEqual(0f, Flood.FloodRange);
            Assert.IsNull(GameObject.Find("FloodWater"));
        }
    }
}
