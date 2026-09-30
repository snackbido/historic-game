using System.Collections;
using System.Linq;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone5CombatTests : PlayModeTestBase
    {
        private static NpcController Npc(string professionId) =>
            NpcController.All.First(n => n.Profession != null && n.Profession.id == professionId);

        private static PredatorAI Wolf(string name = "Wolf") => GameObject.Find(name).GetComponent<PredatorAI>();

        /// <summary>Chỉ để lại 1 con sói cho test chiến đấu dễ đoán.</summary>
        private static PredatorAI SingleWolf()
        {
            var other = GameObject.Find("Wolf_1");
            other.SetActive(false);
            Object.Destroy(other);
            return Wolf();
        }

        private static void Warp(NpcController npc, Vector3 position) =>
            npc.GetComponent<NavMeshAgent>().Warp(position);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        [UnityTest]
        public IEnumerator Wolves_PatrolAroundDen_WhenNobodyIsClose()
        {
            yield return null;
            var wolves = PredatorAI.All.ToList();
            Assert.AreEqual(2, wolves.Count, "Scene starts with two wolves at the den");

            float end = Time.time + 20f;
            while (Time.time < end) yield return null;

            foreach (var wolf in wolves)
            {
                Assert.AreEqual(PredatorState.Patrol, wolf.State, "Nobody is near the den → keep patrolling");
                Assert.Less(InteractableRegistry.GroundDistance(wolf.transform.position, wolf.Den), wolf.Data.patrolRadius + 1.5f);
            }
            Assert.IsTrue(NpcController.All.All(n => n.Health.Current >= n.Health.Max), "Wolves do not raid the camp on their own");
        }

        [UnityTest]
        public IEnumerator VillagerNearDen_GetsBitten_AndFightsBack()
        {
            yield return null;
            var wolf = SingleWolf();
            var ka = Npc("villager");
            Warp(ka, wolf.Den + new Vector3(3f, 0f, 0f));

            yield return WaitUntil(() => ka == null || ka.Health.Current < ka.Health.Max, 20f);
            Assert.IsTrue(ka != null && ka.Health.Current < ka.Health.Max, "Wolf should chase and bite a villager who comes close");
            Assert.IsInstanceOf<AttackJob>(ka.CurrentJob, "A villager who can fight bites back automatically");
            Assert.Less(wolf.Health.Current, wolf.Health.Max + 0.01f);
        }

        [UnityTest]
        public IEnumerator FarmerNearDen_FleesInsteadOfFighting()
        {
            yield return null;
            var wolf = SingleWolf();
            var farmer = Npc("farmer");
            Warp(farmer, wolf.Den + new Vector3(3f, 0f, 0f));

            yield return WaitUntil(() => farmer == null || farmer.Health.Current < farmer.Health.Max, 20f);
            Assert.IsTrue(farmer != null, "Farmer should survive a single bite");
            Assert.IsNull(farmer.CurrentJob, "Farmers cannot fight");
            Assert.AreEqual(NpcState.Moving, farmer.State, "Bitten farmer runs away");
            Assert.Greater(InteractableRegistry.GroundDistance(farmer.Destination, wolf.transform.position),
                InteractableRegistry.GroundDistance(farmer.transform.position, wolf.transform.position), "Flee destination is away from the wolf");
        }

        [UnityTest]
        public IEnumerator RightClickWolf_FightersAttack_NonFighterStays_HunterKillsIt()
        {
            yield return null;
            var wolf = SingleWolf();
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            int foodBefore = ResourceManager.Instance.GetAmount(food);
            int meat = wolf.Data.huntYield[0].amount;
            var hunter = Npc("hunter");
            var farmer = Npc("farmer");

            SelectionManager.Instance.SetSelection(new[] { hunter, farmer });
            SelectionManager.Instance.IssueCommandAt(wolf.transform.position);

            Assert.IsInstanceOf<AttackJob>(hunter.CurrentJob);
            Assert.IsNull(farmer.CurrentJob, "Non-fighters are not sent into the fight");
            Assert.AreNotEqual(NpcState.Moving, farmer.State, "…and do not follow either");

            yield return WaitUntil(() => wolf == null, 60f);
            Assert.IsTrue(wolf == null, "Hunter should kill the wolf");
            Assert.AreEqual(foodBefore + meat, ResourceManager.Instance.GetAmount(food), "Wolf drops meat");
            Assert.IsTrue(hunter != null && !hunter.Health.IsDead, "Hunter survives one wolf");
        }

        [UnityTest]
        public IEnumerator PlayerSpearAndStone_DamageNearestWolf_WithCooldown()
        {
            yield return null;
            var wolf = SingleWolf();
            var player = GameObject.FindWithTag("Player");
            var combat = player.GetComponent<PlayerCombat>();
            var controller = player.GetComponent<PlayerController>();

            controller.SetPosition(wolf.transform.position + new Vector3(1f, 0f, 0f));
            yield return null;
            float before = wolf.Health.Current;
            Assert.IsTrue(combat.TryMeleeAttack(), "Spear hits a wolf right next to the player");
            Assert.Less(wolf.Health.Current, before);
            Assert.IsFalse(combat.TryMeleeAttack(), "Spear has a cooldown");

            controller.SetPosition(wolf.transform.position + new Vector3(6f, 0f, 0f));
            yield return null;
            Assert.IsFalse(combat.TryMeleeAttack(), "Spear cannot reach 6m");
            before = wolf.Health.Current;
            Assert.IsTrue(combat.TryThrowStone(), "Stone reaches 6m");
            Assert.Less(wolf.Health.Current, before);
            StringAssert.Contains("Ném đá", InteractionPromptUI.DescribeCombat(combat), "Prompt tells the player how to fight");
        }

        [UnityTest]
        public IEnumerator PlayerKnockedOut_RespawnsAtCampWithFullHealth()
        {
            yield return null;
            var player = GameObject.FindWithTag("Player");
            var health = player.GetComponent<HealthComponent>();
            player.GetComponent<PlayerController>().SetPosition(new Vector3(-6f, 0f, 6f));

            health.TakeDamage(1000f);
            yield return null;

            Assert.AreEqual(health.Max, health.Current, 0.001f, "Player is healed after being knocked out");
            Assert.Less(InteractableRegistry.GroundDistance(player.transform.position, Vector3.zero), 0.5f, "Player is brought back to camp");
        }

        [UnityTest]
        public IEnumerator VillagerDeath_RemovesThemAndAnnounces()
        {
            yield return null;
            var ka = Npc("villager");
            int count = NpcController.All.Count;
            string last = null;
            void Handler(string m) => last = m;
            EventBus.OnNotification += Handler;

            SelectionManager.Instance.SetSelection(new[] { ka });
            ka.Health.TakeDamage(1000f);
            yield return null;

            EventBus.OnNotification -= Handler;
            Assert.AreEqual(count - 1, NpcController.All.Count);
            Assert.AreEqual(0, SelectionManager.Instance.Selected.Count, "Dead villagers leave the selection");
            StringAssert.Contains("chết", last);
        }

        [UnityTest]
        public IEnumerator Health_RegeneratesAfterNotBeingHitForAWhile()
        {
            yield return null;
            var ka = Npc("villager");
            ka.Health.TakeDamage(30f);
            float hurt = ka.Health.Current;

            float end = Time.time + 3f;
            while (Time.time < end) yield return null;
            Assert.AreEqual(hurt, ka.Health.Current, 0.001f, "No regen right after being hit");

            end = Time.time + 12f;
            while (Time.time < end) yield return null;
            Assert.Greater(ka.Health.Current, hurt + 5f, "Health comes back after the regen delay");
        }

        [UnityTest]
        public IEnumerator SaveLoad_RestoresWolvesAndTheirHealth()
        {
            SaveSystem.FileNameOverride = "test_savegame_wolves.json";
            try
            {
                yield return null;
                var wolf = Wolf();
                wolf.Health.TakeDamage(20f);
                float saved = wolf.Health.Current;
                GameManager.Instance.SaveGame();

                wolf.gameObject.SetActive(false);
                Object.Destroy(wolf.gameObject);
                yield return null;
                Assert.AreEqual(1, PredatorAI.All.Count);

                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(2, PredatorAI.All.Count, "Both wolves come back, no duplicates");
                Assert.AreEqual(saved, Wolf().Health.Current, 0.001f, "Wounded wolf keeps its health");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
