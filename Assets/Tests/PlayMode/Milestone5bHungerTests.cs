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
    /// <summary>
    /// Milestone 5b – E5: dân làng ăn hằng ngày. Gọi thẳng NpcManager.UpdateHunger(giây) để kết quả
    /// không phụ thuộc thời gian thực (HungerEnabled vẫn tắt nên Update không chạy song song).
    /// </summary>
    public class Milestone5bHungerTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ResourceTypeData Res(string name) => Asset<ResourceTypeData>($"Assets/_Data/ResourceType_{name}.asset");
        private static ResourceManager Rm => ResourceManager.Instance;
        private static NpcManager Manager => NpcManager.Instance;

        [UnityTest]
        public IEnumerator EachAdult_EatsAboutOneMealPerMinute_FromTheSharedStock()
        {
            yield return null;
            Rm.AddResource(Res("Food"), 10);
            Assert.AreEqual(4, NpcController.All.Count);

            Manager.UpdateHunger(30f);
            Assert.AreEqual(10, Rm.GetAmount(Res("Food")), "Still full after 30s — nobody eats yet");

            Manager.UpdateHunger(31f);
            Assert.AreEqual(6, Rm.GetAmount(Res("Food")), "After ~1 minute each of the 4 villagers ate one meal");
            Assert.IsTrue(NpcController.All.All(n => n.Fullness > 90f));
        }

        [UnityTest]
        public IEnumerator Meals_UsePerishableFoodFirst()
        {
            yield return null;
            Rm.AddResource(Res("Meat"), 3);
            Rm.AddResource(Res("Rice"), 10);
            foreach (var npc in NpcController.All) npc.Fullness = 50f;

            Manager.UpdateHunger(0.01f);
            Assert.AreEqual(0, Rm.GetAmount(Res("Meat")), "Meat would rot, so it is eaten first");
            Assert.AreEqual(9, Rm.GetAmount(Res("Rice")));
        }

        [UnityTest]
        public IEnumerator NoFood_VillagersStarve_SlowDown_CannotHaveBabies_AndLoseHealth()
        {
            yield return null;
            var hut = BuildingPlacer.Instance.PlaceBuilding(Asset<BuildingData>("Assets/_Data/BuildingData_Hut.asset"),
                new Vector3Int(1, -6, 0), spendResources: false);
            var mother = NpcController.All.First(n => n.Gender == Gender.Female && n.Home == hut);
            Rm.AddResource(Res("Food"), 5); // đủ cho sinh con nếu không đói… nhưng sẽ bị ăn hết

            Manager.UpdateHunger(200f);
            Manager.UpdateHunger(1f);
            Assert.AreEqual(0, Rm.GetAmount(Res("Food")), "The 5 food were eaten");
            Manager.UpdateHunger(200f);
            Assert.IsTrue(mother.IsStarving, "No food left → hunger drops to 0");
            yield return null;

            var agent = mother.GetComponent<NavMeshAgent>();
            Assert.AreEqual(mother.Profession.moveSpeed * 0.6f, agent.speed, 0.01f, "Starving villagers walk slower");
            StringAssert.Contains("Đang đói", Manager.BirthBlocker(mother) ?? "");
            StringAssert.Contains("ĐANG ĐÓI: 4", PopulationUI.Describe(Manager));

            float health = mother.Health.Current;
            Manager.UpdateHunger(30f);
            Assert.AreEqual(health - 10f, mother.Health.Current, 0.01f, "Starving costs 1 health every 3 seconds");
        }

        [UnityTest]
        public IEnumerator FoodArriving_EndsStarvation()
        {
            yield return null;
            var npc = NpcController.All[0];
            npc.Fullness = 0f;
            Assert.IsTrue(npc.IsStarving);

            Rm.AddResource(Res("Berries"), 3);
            Manager.UpdateHunger(0.01f);
            Assert.IsFalse(npc.IsStarving, "Eats as soon as there is food");
            Assert.AreEqual(40f, npc.Fullness, 0.1f);
        }

        [UnityTest]
        public IEnumerator Children_EatHalfAsMuch()
        {
            yield return null;
            var mother = NpcController.All.First(n => n.Gender == Gender.Female);
            var baby = Manager.SpawnBaby(mother, mother.Partner);

            Manager.UpdateHunger(30f); // không có thức ăn → chỉ thấy độ no giảm
            Assert.AreEqual(80f, mother.Fullness, 0.1f);
            Assert.AreEqual(90f, baby.Fullness, 0.1f, "Children get hungry half as fast");
        }

        [UnityTest]
        public IEnumerator StarvingTooLong_Kills()
        {
            yield return null;
            var npc = NpcController.All[0];
            npc.Fullness = 0f;

            // Không có thức ăn → cả làng cùng đói; 400s = 133 máu, hơn máu tối đa của mọi nghề (thợ săn 110).
            Manager.UpdateHunger(400f);
            yield return null;
            Assert.IsTrue(npc == null, "A villager who starves long enough dies");
            Assert.AreEqual(0, NpcController.All.Count, "With no food at all, the whole tribe starves");
        }

        [UnityTest]
        public IEnumerator SaveLoad_KeepsFullness()
        {
            SaveSystem.FileNameOverride = "test_savegame_hunger.json";
            try
            {
                yield return null;
                var npc = NpcController.All[0];
                string npcName = npc.NpcName;
                npc.Fullness = 37f;

                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadGame();
                yield return null;

                Assert.AreEqual(37f, NpcController.All.First(n => n.NpcName == npcName).Fullness, 0.01f);
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
