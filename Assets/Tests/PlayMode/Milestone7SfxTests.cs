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
    /// <summary>Milestone 7 – P2b: tiếng động đi kèm hành động / thiên tai (sinh bằng code, thay được bằng file riêng).</summary>
    public class Milestone7SfxTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;

        private static ResourceNode AnyTree() =>
            Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)
                .First(n => n.ResourceType != null && n.ResourceType.id == "wood" && !n.Regenerates);

        [UnityTest]
        public IEnumerator EverySound_HasAClip()
        {
            yield return null;
            foreach (SfxKind kind in System.Enum.GetValues(typeof(SfxKind)))
                Assert.IsTrue(SfxManager.HasClip(kind), $"{kind} has a sound");
        }

        [UnityTest]
        public IEnumerator Chopping_AndBuilding_MakeTheirSounds()
        {
            yield return null;
            int chop = SfxManager.PlayCount(SfxKind.Chop);
            AnyTree().Harvest();
            Assert.AreEqual(chop + 1, SfxManager.PlayCount(SfxKind.Chop));

            int build = SfxManager.PlayCount(SfxKind.Build);
            BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Storage.asset"),
                new Vector3Int(-4, -5, 0), spendResources: false);
            Assert.AreEqual(build + 1, SfxManager.PlayCount(SfxKind.Build), "Hammering, not a crash, when a building goes up");
        }

        [UnityTest]
        public IEnumerator Flood_SoundsLikeRain_UntilItEnds()
        {
            yield return null;
            Manager.StartNow(Manager.Find("flood"));
            yield return null;
            Assert.IsTrue(SfxManager.IsRaining);
            Manager.EndCurrent();
            yield return null;
            Assert.IsFalse(SfxManager.IsRaining);
        }

        [UnityTest]
        public IEnumerator Fire_Crackles_UntilPutOut()
        {
            yield return null;
            var wildfire = (WildfireDisaster)Manager.Find("wildfire");
            var tree = AnyTree();
            int thunder = SfxManager.PlayCount(SfxKind.Thunder);
            Fire fire = wildfire.Strike(tree);
            Assert.AreEqual(thunder + 1, SfxManager.PlayCount(SfxKind.Thunder));
            var crackle = tree.GetComponent<AudioSource>();
            Assert.IsNotNull(crackle, "Burning tree crackles");
            Assert.IsTrue(crackle.loop);

            fire.PutOut();
            yield return null;
            Assert.IsNull(tree.GetComponent<AudioSource>(), "Silence once the fire is out");
        }

        [UnityTest]
        public IEnumerator WolfRaid_StartsWithAHowl()
        {
            yield return null;
            int howl = SfxManager.PlayCount(SfxKind.Howl);
            ((WolfRaidDisaster)Manager.Find("wolf_raid")).Spawn();
            Assert.AreEqual(howl + 1, SfxManager.PlayCount(SfxKind.Howl));
        }
    }
}
