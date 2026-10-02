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
    /// <summary>Milestone 7 – P1: hiệu ứng hạt gắn vào các hành động (chặt, cuốc, xây, đánh, mưa lũ, sét).</summary>
    public class Milestone7VfxTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;

        private static BuildingInstance Place(string data, Vector3Int cell) =>
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>($"Assets/_Data/BuildingData_{data}.asset"), cell, spendResources: false);

        private static ResourceNode AnyTree() =>
            Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)
                .First(n => n.ResourceType != null && n.ResourceType.id == "wood" && !n.Regenerates);

        [Test]
        public void Scene_HasVfxManager_WithParticleMaterial()
        {
            var manager = Object.FindFirstObjectByType<VfxManager>();
            Assert.IsNotNull(manager);
            var renderer = manager.GetComponentInChildren<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, "Effect systems are created on Awake");
            Assert.IsNotNull(renderer.sharedMaterial, "Particles have a material (no pink squares)");
        }

        [UnityTest]
        public IEnumerator Chopping_SpraysWoodChips()
        {
            yield return null;
            int before = VfxManager.PlayCount(VfxKind.WoodChips);
            AnyTree().Harvest();
            Assert.AreEqual(before + 1, VfxManager.PlayCount(VfxKind.WoodChips));

            yield return null;
            var chips = Object.FindFirstObjectByType<VfxManager>().GetComponentsInChildren<ParticleSystem>()
                .First(p => p.name == "Vfx_WoodChips");
            Assert.Greater(chips.particleCount, 0, "Chips are actually alive in the scene");
        }

        [UnityTest]
        public IEnumerator PlacingBuilding_RaisesDust_AndPlowingKicksUpSoil()
        {
            yield return null;
            int bigDust = VfxManager.PlayCount(VfxKind.BigDust);
            var plot = Place("DryField", new Vector3Int(-4, -5, 0)).GetComponent<FarmPlot>();
            Assert.AreEqual(bigDust + 1, VfxManager.PlayCount(VfxKind.BigDust), "Dust puff when a building goes up");

            while (plot.State == FarmPlotState.Wild) plot.DoClearWork();
            int dust = VfxManager.PlayCount(VfxKind.Dust);
            plot.DoPlowWork();
            Assert.AreEqual(dust + 1, VfxManager.PlayCount(VfxKind.Dust));
        }

        [UnityTest]
        public IEnumerator Hits_Bleed_ButAreThrottled()
        {
            yield return null;
            var player = GameObject.FindWithTag("Player").GetComponent<HealthComponent>();
            int before = VfxManager.PlayCount(VfxKind.Hit);
            player.TakeDamage(1f);
            player.TakeDamage(1f); // cùng khung hình → không phun thêm
            Assert.AreEqual(before + 1, VfxManager.PlayCount(VfxKind.Hit));
        }

        [UnityTest]
        public IEnumerator Flood_BringsRain_UntilItEnds()
        {
            yield return null;
            Assert.IsFalse(VfxManager.IsRaining);
            Manager.StartNow(Manager.Find("flood"));
            yield return null;
            Assert.IsTrue(VfxManager.IsRaining);
            Manager.EndCurrent();
            yield return null;
            Assert.IsFalse(VfxManager.IsRaining);
        }

        [UnityTest]
        public IEnumerator LightningStrike_Flashes()
        {
            yield return null;
            var wildfire = (WildfireDisaster)Manager.Find("wildfire");
            int before = VfxManager.PlayCount(VfxKind.Lightning);
            Assert.IsNotNull(wildfire.Strike(AnyTree()));
            Assert.AreEqual(before + 1, VfxManager.PlayCount(VfxKind.Lightning));
            int steam = VfxManager.PlayCount(VfxKind.Steam);
            Fire.All[0].Douse(1f);
            Assert.AreEqual(steam + 1, VfxManager.PlayCount(VfxKind.Steam), "Dousing a fire hisses steam");
        }
    }
}
