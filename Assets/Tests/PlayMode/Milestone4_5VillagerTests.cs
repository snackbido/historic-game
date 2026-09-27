using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone4_5VillagerTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Villager_HungerDecaysAndAutoEatsWhenFoodAvailable()
        {
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var villager = GameObject.Find("Villager_1").GetComponent<VillagerController>();

            Assert.AreEqual(100f, villager.Hunger);

            ResourceManager.Instance.AddResource(food, 50);
            int foodBefore = ResourceManager.Instance.GetAmount(food);

            Time.timeScale = 100f;
            float deadline = Time.time + villager.Data.hungerDecayInterval * (100f - villager.Data.lowNeedWarningThreshold + 5f);

            // Stop the very frame the meal happens (food amount changes) so we assert
            // Hunger right at the reset, before any further decay ticks can run.
            while (ResourceManager.Instance.GetAmount(food) == foodBefore && Time.time < deadline)
                yield return null;

            int foodAfter = ResourceManager.Instance.GetAmount(food);
            Assert.Less(foodAfter, foodBefore, "Villager should have auto-eaten, spending Food");
            Assert.AreEqual(100f, villager.Hunger, "Hunger should be restored to 100 right after eating");

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Villager_AssignGathering_WalksToNodeAndHarvests()
        {
            var wood = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Wood.asset");
            var villager = GameObject.Find("Villager_2").GetComponent<VillagerController>();
            var tree = GameObject.Find("Tree_1").GetComponent<ResourceNode>();

            int woodBefore = ResourceManager.Instance.GetAmount(wood);
            villager.AssignGathering(tree);
            Assert.AreEqual(VillagerJob.Gathering, villager.CurrentJob);

            Time.timeScale = 20f;
            float deadline = Time.time + 15f;
            int woodAfter = woodBefore;
            while (Time.time < deadline)
            {
                woodAfter = ResourceManager.Instance.GetAmount(wood);
                if (woodAfter > woodBefore) break;
                yield return null;
            }

            Assert.Greater(woodAfter, woodBefore, "Villager assigned to Gathering should walk to the node and harvest it");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Villager_SetSelected_TogglesSelectionState()
        {
            var villager = GameObject.Find("Villager_1").GetComponent<VillagerController>();
            Assert.IsFalse(villager.IsSelected);

            villager.SetSelected(true);
            Assert.IsTrue(villager.IsSelected);

            villager.SetSelected(false);
            Assert.IsFalse(villager.IsSelected);

            yield return null;
        }
    }
}
