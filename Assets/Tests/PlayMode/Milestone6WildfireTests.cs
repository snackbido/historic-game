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
    /// <summary>Milestone 6 – D4: cháy rừng — sét đánh cháy cây, lửa lan, cây/nhà/ruộng bị cháy; dân làng gánh nước dập lửa.</summary>
    public class Milestone6WildfireTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;
        private static WildfireDisaster Wildfire => (WildfireDisaster)Manager.Find("wildfire");
        private static CropData Berry => Asset<CropData>("Assets/_Data/CropData_Berry.asset");

        private static BuildingInstance Place(string data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>($"Assets/_Data/BuildingData_{data}.asset"), cell, spendResources: false);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static void NoSpread() => Wildfire.FireSettings.spreadChance = 0f;

        [Test]
        public void Wildfire_IsOneOfTheDisasters()
        {
            Assert.IsNotNull(Wildfire);
            Assert.AreEqual("Cháy rừng", Wildfire.DisplayName);
        }

        [UnityTest]
        public IEnumerator Lightning_SetsTreesAlight()
        {
            yield return null;
            Manager.StartNow(Wildfire);
            StringAssert.Contains("CHÁY RỪNG", DisasterBannerUI.Describe(Manager));
            Assert.AreEqual(2, Fire.All.Count, "Two lightning strikes");
            Assert.IsTrue(Fire.All.All(f => f.Target is ResourceNode), "Lightning hits trees");
            Assert.IsNotNull(Fire.All[0].transform.Find("Flame"), "Flames are visible");
        }

        [UnityTest]
        public IEnumerator Fire_SpreadsToNeighbours()
        {
            yield return null;
            var first = Place("Storage", new Vector3Int(-4, -5, 0));
            var second = Place("Storage", new Vector3Int(-3, -5, 0));
            Wildfire.FireSettings.spreadChance = 1f;
            Assert.IsNotNull(Wildfire.Strike(first));

            yield return WaitUntil(() => Fire.IsBurning(second), 30f);
            Assert.IsTrue(Fire.IsBurning(second), "The fire jumps to the storehouse next door");
        }

        [UnityTest]
        public IEnumerator BurningBuilding_CollapsesAndTheFireGoesOut()
        {
            yield return null;
            NoSpread();
            var storage = Place("Storage", new Vector3Int(-4, -5, 0));
            Wildfire.Strike(storage);

            yield return WaitUntil(() => storage.IsCollapsed, 120f);
            Assert.IsTrue(storage.IsCollapsed, "Left alone, the storehouse burns down");
            yield return null;
            Assert.IsFalse(Fire.IsBurning(storage), "Nothing left to burn");
        }

        [UnityTest]
        public IEnumerator BurningTree_BurnsDownToAStump()
        {
            yield return null;
            NoSpread();
            var tree = InteractableRegistry.All<ResourceNode>().First(n => Fire.CanBurn(n));
            Vector3 spot = tree.transform.position;
            Wildfire.Strike(tree);

            yield return WaitUntil(() => tree == null, 60f);
            Assert.IsTrue(tree == null, "The tree is gone");
            Assert.IsTrue(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Any(t => t.name == "CharredStump" && Vector3.Distance(t.position, spot) < 0.5f), "A charred stump is left");
        }

        [UnityTest]
        public IEnumerator DryCrops_Burn_WetFieldsDont()
        {
            yield return null;
            var dry = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            var wet = GameObject.Find("FarmPlot_2").GetComponent<FarmPlot>();
            Assert.IsTrue(dry.Plant(Berry));
            Assert.IsTrue(wet.Plant(Berry));
            dry.SetWater(0.2f);
            wet.SetWater(1f);

            Assert.IsFalse(Fire.CanBurn(wet), "A well-watered field won't catch fire");
            NoSpread();
            Assert.IsNotNull(Wildfire.Strike(dry));
            yield return WaitUntil(() => dry.State == FarmPlotState.Withered, 30f);
            Assert.AreEqual(FarmPlotState.Withered, dry.State, "The dry crop burns");
        }

        [UnityTest]
        public IEnumerator Player_CanBeatOutAFire()
        {
            yield return null;
            NoSpread();
            var tree = InteractableRegistry.All<ResourceNode>().First(n => Fire.CanBurn(n));
            var fire = Wildfire.Strike(tree);
            StringAssert.Contains("[E] Dập lửa", InteractionPromptUI.Describe(tree));
            int beats = 0;
            while (!fire.Douse(PlayerInteraction.PlayerBeatFire) && beats < 20) beats++;
            yield return null;
            Assert.IsFalse(Fire.IsBurning(tree), "Beating it a few times puts it out");
            Assert.IsTrue(tree != null);
        }

        [UnityTest]
        public IEnumerator Villagers_WakeUpAndCarryWaterToTheFire()
        {
            yield return null;
            NoSpread();
            Wildfire.FireSettings.buildingDamagePerSecond = 1f / 400f; // cháy chậm để kịp dập
            DayNightCycle.Instance.SetTime(0.8f); // giữa đêm
            NpcController.AutoWorkEnabled = true;
            var storage = Place("Storage", new Vector3Int(-4, -5, 0));
            Wildfire.Strike(storage);

            yield return WaitUntil(() => NpcController.All.Any(n => n.CurrentJob is FirefightJob), 20f);
            Assert.IsTrue(NpcController.All.Any(n => n.CurrentJob is FirefightJob), "Even at night someone runs to the fire");
            yield return WaitUntil(() => !Fire.IsBurning(storage), 150f);
            Assert.IsFalse(Fire.IsBurning(storage), "The villagers put it out");
            Assert.IsFalse(storage.IsCollapsed);
        }

        [UnityTest]
        public IEnumerator Wildfire_EndsEarly_OnceEveryFireIsOut()
        {
            yield return null;
            Manager.StartNow(Wildfire);
            Assert.AreEqual(DisasterPhase.Active, Manager.Phase);
            foreach (var fire in Fire.All.ToList()) fire.Douse(10f);
            yield return null;
            yield return null;
            Assert.AreEqual(DisasterPhase.None, Manager.Phase, "No fires left — the danger is over");
        }
    }
}
