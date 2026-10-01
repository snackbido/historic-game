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
    /// <summary>Milestone 6 – D2: hạn hán — ruộng khô nhanh, ao cạn (cá không sinh sôi, mương tự chảy ngắn lại); guồng nước vẫn bơm.</summary>
    public class Milestone6DroughtTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;
        private static DroughtDisaster Drought => (DroughtDisaster)Manager.Find("drought");
        private static FarmPlot Garden => GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
        private static GameObject Pond => GameObject.Find("FishingPond");

        private static Canal PlaceCanal(int y)
        {
            var canal = BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Canal.asset"),
                new Vector3Int(4, y, 0), spendResources: false).GetComponent<Canal>();
            canal.SetDigProgress(99);
            return canal;
        }

        private static IEnumerator Wait(float gameSeconds)
        {
            float end = Time.time + gameSeconds;
            while (Time.time < end) yield return null;
        }

        [Test]
        public void Drought_IsOneOfTheDisasters()
        {
            Assert.IsNotNull(Drought);
            Assert.AreEqual("Hạn hán", Drought.DisplayName);
            Assert.IsFalse(string.IsNullOrEmpty(Drought.WarningMessage));
        }

        [UnityTest]
        public IEnumerator Fields_DryOutThreeTimesFaster()
        {
            yield return null;
            var plot = Garden;
            plot.SetWater(1f);
            yield return Wait(10f);
            float normalLoss = 1f - plot.Water;

            Manager.StartNow(Drought);
            StringAssert.Contains("HẠN HÁN", DisasterBannerUI.Describe(Manager));
            plot.SetWater(1f);
            yield return Wait(10f);
            float droughtLoss = 1f - plot.Water;
            Assert.Greater(droughtLoss, normalLoss * 2.5f, "Fields lose water about three times as fast");
        }

        [UnityTest]
        public IEnumerator Pond_ShrinksAndStopsBreedingFish()
        {
            yield return null;
            var fish = Pond.GetComponent<ResourceNode>();
            fish.Harvest();
            fish.Harvest();
            int left = fish.AmountRemaining;
            Vector3 fullSize = Pond.transform.Find("Water").localScale;

            Manager.StartNow(Drought, durationSeconds: 100f);
            yield return Wait(30f);
            Assert.AreEqual(left, fish.AmountRemaining, "No new fish while the pond is low");
            Assert.Less(Pond.transform.Find("Water").localScale.x, fullSize.x * 0.7f, "The pond shrinks");

            Manager.EndCurrent();
            Assert.AreEqual(fullSize, Pond.transform.Find("Water").localScale, "The pond fills up again");
            Assert.IsFalse(fish.RegenPaused);
            Assert.AreEqual(1f, FarmPlot.EvaporationMultiplier);
        }

        [UnityTest]
        public IEnumerator Canals_RunShort_ButTheWaterWheelStillPumps()
        {
            yield return null;
            var canals = Enumerable.Range(0, 6).Select(i => PlaceCanal(4 - i)).ToList();
            yield return null;
            Assert.IsTrue(canals[5].IsFlowing);

            Manager.StartNow(Drought);
            yield return null;
            Assert.IsTrue(canals[2].IsFlowing, "The low pond still feeds a short canal");
            Assert.IsFalse(canals[3].IsFlowing, "…but not further than 3 tiles");
            Assert.Less(canals[0].IrrigationRate, CanalNetwork.IrrigationPerSecond, "and it waters slowly");

            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_WaterWheel.asset"),
                new Vector3Int(5, 4, 0), spendResources: false);
            yield return null;
            Assert.IsTrue(canals[5].IsFlowing, "A water wheel beats the drought");

            Manager.EndCurrent();
            yield return null;
            Assert.IsFalse(CanalNetwork.LowWater);
        }

        [UnityTest]
        public IEnumerator SaveLoad_DuringADrought_KeepsItGoing()
        {
            SaveSystem.FileNameOverride = "test_savegame_drought.json";
            try
            {
                yield return null;
                Vector3 fullSize = Pond.transform.Find("Water").localScale;
                Manager.StartNow(Drought, durationSeconds: 100f);
                yield return Wait(30f);

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreSame(Drought, Manager.Current);
                Assert.AreEqual(DisasterPhase.Active, Manager.Phase);
                Assert.AreEqual(3f, FarmPlot.EvaporationMultiplier);
                Manager.EndCurrent();
                Assert.AreEqual(fullSize, Pond.transform.Find("Water").localScale, "Reloading doesn't shrink the pond for good");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
