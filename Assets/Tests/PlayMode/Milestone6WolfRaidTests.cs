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
    /// <summary>Milestone 6 – D5: bầy sói đột kích ban đêm; đuốc, hàng rào, người biết đánh ra chặn; sáng ra bầy rút.</summary>
    public class Milestone6WolfRaidTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static DisasterManager Manager => DisasterManager.Instance;
        private static WolfRaidDisaster Raid => (WolfRaidDisaster)Manager.Find("wolf_raid");
        private static BuildingData Torch => Asset<BuildingData>("Assets/_Data/BuildingData_Torch.asset");
        private static BuildingData FenceData => Asset<BuildingData>("Assets/_Data/BuildingData_Fence.asset");
        private static Vector3 Camp => Campfire.All.First(c => c.IsSleepSpot).transform.position;

        private static System.Collections.Generic.List<PredatorAI> Raiders => PredatorAI.All.Where(p => p.IsRaider).ToList();

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static void Night() => DayNightCycle.Instance.SetTime(0.8f);

        [Test]
        public void Raid_IsOneOfTheDisasters_TorchAndFenceAreAvailableFromTheStart()
        {
            Assert.IsNotNull(Raid);
            Assert.AreEqual("Sói đột kích", Raid.DisplayName);
            Assert.IsTrue(Torch.unlockedByDefault);
            Assert.IsTrue(FenceData.unlockedByDefault);
        }

        [UnityTest]
        public IEnumerator Pack_WaitsForNightfall_ThenComesForTheVillage()
        {
            yield return null;
            Manager.StartNow(Raid);
            yield return null;
            Assert.AreEqual(0, Raiders.Count, "Wolves don't raid in daylight");

            Night();
            yield return WaitUntil(() => Raiders.Count > 0, 5f);
            Assert.GreaterOrEqual(Raiders.Count, 2, "A pack arrives at night");
            Assert.LessOrEqual(Raiders.Count, Raid.PackSize());
            Assert.IsTrue(Raiders.All(r => InteractableRegistry.GroundDistance(r.transform.position, Camp) > 12f), "They come out of the forest");

            yield return WaitUntil(() => Raiders.Any(r => InteractableRegistry.GroundDistance(r.transform.position, Camp) < 11f), 40f);
            Assert.IsTrue(Raiders.Any(r => InteractableRegistry.GroundDistance(r.transform.position, Camp) < 11f), "…and close in on the village");
        }

        [UnityTest]
        public IEnumerator Torch_LightsUpAtNight_AndKeepsWolvesAway()
        {
            yield return null;
            var torch = BuildingPlacer.Instance.PlaceBuilding(Torch, new Vector3Int(-6, -8, 0), spendResources: false);
            var fire = torch.GetComponent<Campfire>();
            yield return null;
            Assert.IsFalse(fire.IsLit, "Not lit in daylight");
            Assert.IsFalse(fire.IsSleepSpot, "Nobody sleeps under a torch");

            Night();
            yield return null;
            Assert.IsTrue(fire.IsLit);
            Assert.IsTrue(Campfire.IsProtected(torch.transform.position + new Vector3(-2.5f, 0f, 0f)), "Wolves keep out of the torchlight");
            Assert.IsFalse(Campfire.IsProtected(torch.transform.position + new Vector3(-4.5f, 0f, 0f)), "Beyond the light it is dark");

            torch.Damage(torch.Health.Max * 2f, "thử");
            Assert.IsFalse(Campfire.IsProtected(torch.transform.position + new Vector3(-2.5f, 0f, 0f)), "A knocked-down torch gives no light");
        }

        [UnityTest]
        public IEnumerator Fighters_StayUpAndTakeOnTheRaiders()
        {
            yield return null;
            Night();
            NpcController.AutoWorkEnabled = true;
            Raid.Spawn();
            Assert.Greater(Raiders.Count, 0);

            yield return WaitUntil(() => NpcController.All.Any(n => n.CurrentJob is AttackJob), 60f);
            var defender = NpcController.All.FirstOrDefault(n => n.CurrentJob is AttackJob);
            Assert.IsNotNull(defender, "Someone who can fight goes out to meet the pack");
            Assert.IsTrue(NpcJobFactory.CanFight(defender.Profession));
        }

        [UnityTest]
        public IEnumerator Raiders_ClawAtAFenceInTheirWay()
        {
            yield return null;
            Night();
            // Rào kín 8 ô quanh ô (-7,-9); một nông dân đứng trong.
            var center = new Vector3Int(-7, -9, 0);
            var fences = new System.Collections.Generic.List<BuildingInstance>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    fences.Add(BuildingPlacer.Instance.PlaceBuilding(FenceData, center + new Vector3Int(dx, dy, 0), spendResources: false));
            Vector3 inside = new Vector3(center.x + 0.5f, 0f, center.y + 0.5f);
            var farmer = NpcController.All.First(n => n.Profession != null && n.Profession.id == "farmer");
            farmer.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(inside);
            yield return new WaitForSeconds(0.1f); // rào khoét NavMesh

            var wolfData = Asset<PredatorData>("Assets/_Data/PredatorData_Wolf.asset");
            var wolf = Object.Instantiate(wolfData.prefab, inside + new Vector3(0f, 0f, -3f), Quaternion.identity).GetComponent<PredatorAI>();
            wolf.BeginRaid(inside, inside + new Vector3(0f, 0f, -12f));

            yield return WaitUntil(() => fences.Any(f => f.IsDamaged), 40f);
            Assert.IsTrue(fences.Any(f => f.IsDamaged), "The fence keeps the wolf out, so it claws at it");
        }

        [UnityTest]
        public IEnumerator AtDawn_ThePackWithdraws_AndTheRaidEnds()
        {
            yield return null;
            Night();
            Manager.StartNow(Raid);
            yield return null;
            Assert.Greater(Raiders.Count, 0);

            DayNightCycle.Instance.SetTime(0.1f);
            yield return null;
            yield return null;
            Assert.IsTrue(Raiders.All(r => r.IsRetreating), "Morning comes and the wolves turn back");
            yield return WaitUntil(() => Manager.Phase == DisasterPhase.None, 60f);
            Assert.AreEqual(0, Raiders.Count, "They vanish into the forest");
            Assert.AreEqual(DisasterPhase.None, Manager.Phase, "The raid is over");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsThePackInTheVillage()
        {
            SaveSystem.FileNameOverride = "test_savegame_raid.json";
            try
            {
                yield return null;
                Night();
                Manager.StartNow(Raid);
                yield return null;
                int count = Raiders.Count;
                Assert.Greater(count, 0);

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(count, Raiders.Count, "The same pack is back, not a second one");
                Assert.AreEqual(DisasterPhase.Active, Manager.Phase);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
