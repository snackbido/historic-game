using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 5c – N3: sói ban đêm lùng sục rộng hơn, né đống lửa trại, sáng về hang.</summary>
    public class Milestone5cWolfNightTests : PlayModeTestBase
    {
        private static Campfire Fire => GameObject.Find("Campfire").GetComponent<Campfire>();
        private static PredatorAI Wolf() => PredatorAI.All.First();

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        private static Vector3 OnNavMesh(Vector3 position)
        {
            Assert.IsTrue(NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas), $"No NavMesh near {position}");
            return hit.position;
        }

        /// <summary>Đặt một dân làng tại chỗ và bắt đứng yên (giữ lệnh → không đi ngủ, không đi dạo).</summary>
        private static NpcController PlaceVillager(Vector3 position)
        {
            var npc = NpcController.All.First(n => n.IsAdult);
            npc.GetComponent<NavMeshAgent>().Warp(position);
            npc.MoveTo(position);
            return npc;
        }

        private static void FreezeWolves()
        {
            foreach (var wolf in PredatorAI.All) wolf.GetComponent<NavMeshAgent>().isStopped = true;
        }

        [UnityTest]
        public IEnumerator Night_WolvesSenseFartherAndRoamWider()
        {
            yield return null;
            var wolf = Wolf();
            Assert.AreEqual(2f, wolf.PatrolRadius);
            Assert.AreEqual(4.5f, wolf.AggroRange);
            Assert.AreEqual(12f, wolf.LeashRange);

            DayNightCycle.Instance.SetTime(0.8f);
            Assert.AreEqual(14f, wolf.PatrolRadius, "At night wolves roam far from the den");
            Assert.AreEqual(7f, wolf.AggroRange, "…notice people from farther away");
            Assert.AreEqual(26f, wolf.LeashRange, "…and chase them all the way to camp");

            yield return WaitUntil(() => PredatorAI.All.Any(w =>
                InteractableRegistry.GroundDistance(w.transform.position, w.Den) > 4f), 40f);
            Assert.IsTrue(PredatorAI.All.Any(w => InteractableRegistry.GroundDistance(w.transform.position, w.Den) > 4f),
                "A wolf wanders beyond its daytime patrol");
        }

        [UnityTest]
        public IEnumerator Night_WolfNoticesAVillagerItWouldIgnoreByDay()
        {
            yield return null;
            FreezeWolves();
            var wolf = Wolf();
            var villager = PlaceVillager(OnNavMesh(wolf.transform.position + new Vector3(6f, 0f, 0f)));
            float distance = InteractableRegistry.GroundDistance(wolf.transform.position, villager.transform.position);
            Assume.That(distance, Is.InRange(5f, 6.8f));

            yield return WaitGameSeconds(1f);
            Assert.AreNotSame(villager.Health, wolf.Target, "By day the wolf ignores someone 6m away");

            DayNightCycle.Instance.SetTime(0.8f);
            yield return WaitUntil(() => wolf.Target == villager.Health, 2f);
            Assert.AreSame(villager.Health, wolf.Target, "At night it notices them");
        }

        [UnityTest]
        public IEnumerator LitCampfire_KeepsWolvesAway()
        {
            yield return null;
            DayNightCycle.Instance.SetTime(0.8f);
            yield return null;
            Assert.IsTrue(Fire.IsLit);

            FreezeWolves();
            var wolf = Wolf();
            Vector3 fire = Fire.transform.position;
            wolf.GetComponent<NavMeshAgent>().Warp(OnNavMesh(fire + new Vector3(-7.5f, 0f, 0f)));
            var villager = PlaceVillager(OnNavMesh(fire + new Vector3(-3f, 0f, 0f)));
            Assume.That(InteractableRegistry.GroundDistance(wolf.transform.position, villager.transform.position), Is.LessThan(6.5f));

            yield return WaitGameSeconds(1f);
            Assert.IsNull(wolf.Target, "Nobody inside the campfire's light is attacked");

            // Sói lọt vào vùng sáng → tự lùi ra ngoài.
            var agent = wolf.GetComponent<NavMeshAgent>();
            agent.Warp(OnNavMesh(fire + new Vector3(-2.5f, 0f, 1.5f)));
            agent.isStopped = false;
            yield return WaitUntil(() => !Campfire.IsProtected(wolf.transform.position), 10f);
            Assert.IsFalse(Campfire.IsProtected(wolf.transform.position), "A wolf caught in the firelight backs off");
        }

        [UnityTest]
        public IEnumerator Dawn_SendsWolvesBackToTheDen()
        {
            yield return null;
            DayNightCycle.Instance.SetTime(0.8f);
            var wolf = Wolf();
            wolf.GetComponent<NavMeshAgent>().Warp(OnNavMesh(wolf.Den + new Vector3(8f, 0f, -5f)));
            yield return WaitGameSeconds(0.5f);

            DayNightCycle.Instance.SetTime(0.1f);
            yield return WaitUntil(() => InteractableRegistry.GroundDistance(wolf.transform.position, wolf.Den) <= 3f, 20f);
            Assert.LessOrEqual(InteractableRegistry.GroundDistance(wolf.transform.position, wolf.Den), 3f,
                "In the morning wolves return to their den");
        }
    }
}
