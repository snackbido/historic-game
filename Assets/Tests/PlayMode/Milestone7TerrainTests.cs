using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Địa hình đồi núi (.claude/LANDSCAPE_DESIGN.md): độ cao từ lưới, sông/biển chặn đường, chỗ cạn lội được, không xây trên dốc.</summary>
    public class Milestone7TerrainTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        [UnityTest]
        public IEnumerator World_HasRealHills_RiverAndSea()
        {
            yield return null;
            Assert.IsTrue(WorldTerrain.Exists);
            Assert.Greater(WorldTerrain.HeightAt(13f, -12f), 1.2f, "A knoll inside the play area");
            Assert.Greater(WorldTerrain.HeightAt(-30f, 40f), 5f, "Mountains to the north");
            Assert.IsTrue(WorldTerrain.IsUnderwater(-24f, 11f), "River west of the village");
            Assert.IsTrue(WorldTerrain.IsUnderwater(0f, -44f), "Sea to the south");
            Assert.Less(Mathf.Abs(WorldTerrain.HeightAt(0f, -2f)), 0.3f, "Village ground stays nearly level");
        }

        [UnityTest]
        public IEnumerator DeepWaterBlocks_FordCanBeWaded()
        {
            yield return null;
            Assert.IsFalse(WorldTerrain.IsWalkable(-24f, 11f), "Deep river");
            Assert.IsFalse(WorldTerrain.IsWalkable(0f, -44f), "Sea");
            Assert.IsTrue(WorldTerrain.IsWalkable(-22.5f, -2f), "Shallow ford");
            Assert.IsTrue(WorldTerrain.IsWalkable(13f, -12f), "Gentle knoll can be climbed");
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(13f, WorldTerrain.HeightAt(13f, -12f), -12f), out _, 1f, NavMesh.AllAreas),
                "Villagers can walk up the knoll too");
        }

        [UnityTest]
        public IEnumerator Player_StandsOnTheGround()
        {
            yield return null;
            var player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            player.SetPosition(new Vector3(13f, 0f, -12f));
            Assert.AreEqual(WorldTerrain.HeightAt(13f, -12f), player.GetPosition().y, 0.01f, "Placed on top of the knoll");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(WorldTerrain.HeightAt(13f, -12f), player.GetPosition().y, 0.05f, "Stays on the ground (no gravity drift, no floating)");
        }

        [UnityTest]
        public IEnumerator Building_OnSlopeOrUnderwater_IsRefused()
        {
            yield return null;
            var hut = Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset");
            var placer = BuildingPlacer.Instance;
            StringAssert.Contains("nước", placer.PlacementBlocker(hut, placer.WorldToCell(new Vector3(-24f, 0f, 11f))));
            // Sườn núi phía bắc: tìm một ô thật sự dốc rồi thử xây.
            Vector3? steep = null;
            for (float z = 26f; z <= 44f && steep == null; z += 1f)
            for (float x = -40f; x <= 40f && steep == null; x += 1f)
            {
                var p = placer.WorldToCell(new Vector3(x, 0f, z));
                Vector3 center = new Vector3(p.x + 0.5f, 0f, p.y + 0.5f);
                if (!WorldTerrain.IsUnderwater(center.x, center.z, 0.2f) && WorldTerrain.HeightRange(center, 1f) > 0.9f) steep = center;
            }
            Assert.IsNotNull(steep, "The mountains have steep slopes");
            StringAssert.Contains("dốc", placer.PlacementBlocker(hut, placer.WorldToCell(steep.Value)));

            var placed = placer.PlaceBuilding(hut, placer.WorldToCell(new Vector3(13f, 0f, -12f)), spendResources: false);
            Assert.AreEqual(WorldTerrain.HeightAt(placed.transform.position), placed.transform.position.y, 0.01f, "Sits on the knoll, not floating or buried");
        }

        [UnityTest]
        public IEnumerator MouseRay_HitsTheHillSurface()
        {
            yield return null;
            Vector3 target = WorldTerrain.Ground(new Vector3(13f, 0f, -12f));
            var ray = new Ray(target + new Vector3(0f, 20f, -15f), (new Vector3(0f, -20f, 15f)).normalized);
            Assert.IsTrue(WorldTerrain.Raycast(ray, out Vector3 hit));
            Assert.Less(Vector3.Distance(hit, target), 0.1f);
        }
    }
}
