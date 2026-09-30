using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone5NpcTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator StartingVillagers_HaveBothGendersAndOneProfessionEach()
        {
            yield return null;
            var npcs = NpcController.All;

            Assert.That(npcs.Count, Is.InRange(3, 4), "Game should start with 3-4 villagers");
            Assert.IsTrue(npcs.Any(n => n.Gender == Gender.Male), "At least one male villager");
            Assert.IsTrue(npcs.Any(n => n.Gender == Gender.Female), "At least one female villager");
            Assert.IsTrue(npcs.All(n => n.Profession != null && n.IsAdult), "Starting villagers are adults with a profession");
            Assert.AreEqual(npcs.Count, npcs.Select(n => n.Profession).Distinct().Count(), "Each starting villager has a different profession");

            foreach (var npc in npcs)
            {
                var agent = npc.GetComponent<NavMeshAgent>();
                Assert.IsTrue(agent.isOnNavMesh, $"{npc.NpcName} should stand on the baked NavMesh");
                Assert.AreEqual(npc.Profession.moveSpeed, agent.speed, 0.001f, "Agent speed comes from the profession");
                Assert.AreEqual(npc.Profession.maxHealth, npc.Health.Max, 0.001f, "Max health comes from the profession");
            }
        }

        [UnityTest]
        public IEnumerator MoveTo_WalksAroundTheCampOnTheNavMesh()
        {
            yield return null;
            var npc = NpcController.All[0];
            var destination = new Vector3(-4f, 0f, 3.5f);

            Assert.IsTrue(npc.MoveTo(destination), "Destination on open ground should be reachable");
            Assert.AreEqual(NpcState.Moving, npc.State);

            float deadline = Time.time + 20f;
            while (npc.State == NpcState.Moving && Time.time < deadline)
                yield return null;

            Assert.AreNotEqual(NpcState.Moving, npc.State, "NPC should arrive before the timeout");
            Assert.Less(InteractableRegistry.GroundDistance(npc.transform.position, destination), 0.6f);
        }

        [Test]
        public void HealthComponent_TakeDamageToZero_RaisesDiedOnce()
        {
            var go = new GameObject("HealthTest");
            var health = go.AddComponent<HealthComponent>();
            health.SetMax(50f);
            int died = 0;
            health.OnDied += _ => died++;

            health.TakeDamage(20f);
            Assert.AreEqual(30f, health.Current, 0.001f);
            health.TakeDamage(100f);
            health.TakeDamage(10f);

            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0f, health.Current);
            Assert.AreEqual(1, died, "OnDied should fire exactly once");
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SaveLoad_RestoresVillagersWithProfessionAndGender()
        {
            SaveSystem.FileNameOverride = "test_savegame_npc.json";
            try
            {
                yield return null;
                var scout = NpcManager.Instance.FindProfession("scout");
                var first = NpcController.All[0];
                string firstName = first.NpcName;
                Gender firstGender = first.Gender;
                first.SetProfession(scout);
                int count = NpcController.All.Count;

                GameManager.Instance.SaveGame();

                first.gameObject.SetActive(false);
                Object.Destroy(first.gameObject);
                yield return null;
                Assert.AreEqual(count - 1, NpcController.All.Count);

                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(count, NpcController.All.Count, "Load should restore every villager without duplicates");
                var restored = NpcController.All.First(n => n.NpcName == firstName);
                Assert.AreSame(scout, restored.Profession, "Changed profession should be saved");
                Assert.AreEqual(firstGender, restored.Gender);
                Assert.IsTrue(restored.GetComponent<NavMeshAgent>().isOnNavMesh, "Recreated villager should be placed on the NavMesh");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
