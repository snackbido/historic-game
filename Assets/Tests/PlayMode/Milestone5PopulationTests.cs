using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone5PopulationTests : PlayModeTestBase
    {
        private static NpcController Npc(string professionId) =>
            NpcController.All.First(n => n.Profession != null && n.Profession.id == professionId);

        private static NpcController ByName(string npcName) => NpcController.All.FirstOrDefault(n => n.NpcName == npcName);

        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float deadline = Time.time + gameSeconds;
            while (!condition() && Time.time < deadline)
                yield return null;
        }

        private static void PlaceHut()
        {
            var hut = Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset");
            var go = Object.Instantiate(hut.prefab, new Vector3(4.5f, 0f, -6.5f), Quaternion.identity);
            go.GetComponent<BuildingInstance>().Initialize(hut, new Vector3Int(4, -7, 0));
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        // ─── Tự làm việc khi rảnh ────────────────────────────────────────────
        [UnityTest]
        public IEnumerator IdleVillager_ChopsNearbyTreeByItself_WithoutHoldingPosition()
        {
            yield return null;
            NpcController.AutoWorkEnabled = true;
            var ka = Npc("villager");

            yield return WaitUntil(() => ka.CurrentJob is GatherJob, 20f);
            Assert.IsInstanceOf<GatherJob>(ka.CurrentJob, "Idle villager should go chop a nearby tree");
            Assert.IsFalse(ka.IsHoldingPosition, "Self-assigned work does not hold position afterwards");
        }

        [UnityTest]
        public IEnumerator IdleFarmer_PlantsAndHarvestsByItself()
        {
            yield return null;
            var food = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            FarmManager.Instance.SelectCrop(Asset<CropData>("Assets/_Data/CropData_Berry.asset"));
            int foodBefore = ResourceManager.Instance.GetAmount(food);
            NpcController.AutoWorkEnabled = true;

            var plots = InteractableRegistry.All<FarmPlot>();
            yield return WaitUntil(() => plots.Any(p => p.State == FarmPlotState.Growing), 30f);
            Assert.IsTrue(plots.Any(p => p.State == FarmPlotState.Growing), "Farmer plants empty plots with the selected seed");

            yield return WaitUntil(() => ResourceManager.Instance.GetAmount(food) > foodBefore, 90f);
            Assert.Greater(ResourceManager.Instance.GetAmount(food), foodBefore, "Farmer harvests ripe plots by itself");
        }

        [UnityTest]
        public IEnumerator SelectedVillager_DoesNotWanderOffToWork()
        {
            yield return null;
            NpcController.AutoWorkEnabled = true;
            var ka = Npc("villager");
            SelectionManager.Instance.SetSelection(new[] { ka });

            float end = Time.time + 10f;
            while (Time.time < end) yield return null;
            Assert.IsNull(ka.CurrentJob, "A villager the player is holding selected is left alone");
        }

        [UnityTest]
        public IEnumerator IdleHunter_GuardsVillagerUnderAttack()
        {
            yield return null;
            var other = GameObject.Find("Wolf_1");
            other.SetActive(false);
            Object.Destroy(other);
            var wolf = GameObject.Find("Wolf").GetComponent<PredatorAI>();
            var farmer = Npc("farmer");
            var hunter = Npc("hunter");
            hunter.GetComponent<NavMeshAgent>().Warp(wolf.Den + new Vector3(6f, 0f, -2f));
            farmer.GetComponent<NavMeshAgent>().Warp(wolf.Den + new Vector3(2.5f, 0f, 0f));
            NpcController.AutoWorkEnabled = true;

            yield return WaitUntil(() => hunter.CurrentJob is AttackJob, 20f);
            Assert.IsInstanceOf<AttackJob>(hunter.CurrentJob, "Hunter on guard rushes a wolf attacking someone nearby");
        }

        // ─── Gia đình & dân số ───────────────────────────────────────────────
        [UnityTest]
        public IEnumerator StartingAdults_ArePairedIntoMaleFemaleCouples()
        {
            yield return null;
            foreach (var npc in NpcController.All)
            {
                Assert.IsNotNull(npc.Partner, $"{npc.NpcName} should have a partner");
                Assert.AreNotEqual(npc.Gender, npc.Partner.Gender, "Couples are one man and one woman");
                Assert.AreSame(npc, npc.Partner.Partner, "Partnership is mutual");
            }
            Assert.AreEqual("Dân số: 4/4", PopulationUI.Describe(NpcManager.Instance));
        }

        [UnityTest]
        public IEnumerator Birth_NeedsFreeHousingAndFood()
        {
            yield return null;
            var food = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var manager = NpcManager.Instance;

            Assert.IsNull(manager.TryBirth(), "No birth without food");
            ResourceManager.Instance.AddResource(food, 20);
            Assert.IsNull(manager.TryBirth(), "No birth while every home is full (4/4)");

            PlaceHut();
            yield return null;
            Assert.AreEqual(6, manager.Capacity, "Each hut adds two homes");

            var baby = manager.TryBirth();
            Assert.IsNotNull(baby, "Free home + food → a couple has a baby");
            Assert.AreEqual(AgeStage.Baby, baby.Age);
            Assert.IsFalse(baby.IsAdult);
            Assert.AreEqual(15, ResourceManager.Instance.GetAmount(food), "A birth costs 5 food");

            Assert.IsNotNull(manager.TryBirth(), "The other couple can also have a baby");
            Assert.AreEqual(6, manager.Population);
            Assert.IsNull(manager.TryBirth(), "Homes are full again (6/6)");
        }

        [UnityTest]
        public IEnumerator Baby_GrowsUpIntoASelectableVillager()
        {
            yield return null;
            var manager = NpcManager.Instance;
            SetField(manager, "babyDuration", 5f);
            SetField(manager, "childDuration", 5f);
            var mother = NpcController.All.First(n => n.Gender == Gender.Female);
            var baby = manager.SpawnBaby(mother, mother.Partner);
            yield return null;

            Assert.AreEqual(AgeStage.Baby, baby.Age);
            Assert.Less(baby.transform.localScale.x, 0.5f, "Babies are small");
            SelectionManager.Instance.SelectInScreenRect(new Rect(0f, 0f, Screen.width, Screen.height), additive: false);
            Assert.IsFalse(SelectionManager.Instance.Selected.Contains(baby), "Babies cannot be selected or ordered");

            yield return WaitUntil(() => baby.Age == AgeStage.Child, 10f);
            Assert.AreEqual(AgeStage.Child, baby.Age);

            yield return WaitUntil(() => baby.IsAdult, 10f);
            Assert.IsTrue(baby.IsAdult, "Child becomes an adult over time");
            Assert.AreEqual("villager", baby.Profession.id, "New adults start as villagers");
            Assert.AreEqual(1f, baby.transform.localScale.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsBabiesAndCouples()
        {
            SaveSystem.FileNameOverride = "test_savegame_population.json";
            try
            {
                yield return null;
                var mother = NpcController.All.First(n => n.Gender == Gender.Female);
                string motherName = mother.NpcName;
                string fatherName = mother.Partner.NpcName;
                var baby = NpcManager.Instance.SpawnBaby(mother, mother.Partner);
                string babyName = baby.NpcName;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(5, NpcController.All.Count);
                var restoredBaby = ByName(babyName);
                Assert.IsNotNull(restoredBaby, "Baby is saved");
                Assert.AreEqual(AgeStage.Baby, restoredBaby.Age);
                Assert.AreEqual(fatherName, ByName(motherName).Partner.NpcName, "Couples are restored after loading");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
