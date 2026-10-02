using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone4TechTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Knowledge_AccruesOverTime_AndUnlocksBuildingAndCrop()
        {
            var knowledge = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>("Assets/_Data/TechNode_Farming.asset");
            var storage = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/_Data/BuildingData_Storage.asset");
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");

            Assert.IsFalse(TechManager.Instance.IsUnlocked(techFarming));
            Assert.IsFalse(TechManager.Instance.IsBuildingUnlocked(storage), "Storage should be locked before tech_farming");
            Assert.IsFalse(TechManager.Instance.IsCropUnlocked(berry), "Berry should be locked before tech_farming");

            float deadline = Time.time + 45f; // 5 tri thức × 6s/điểm (nhịp đã giãn 2026-09-30)
            while (ResourceManager.Instance.GetAmount(knowledge) < techFarming.cost[0].amount && Time.time < deadline)
                yield return null;
            Assert.IsTrue(TechManager.Instance.CanUnlock(techFarming), "Should afford tech_farming once enough Knowledge accrued");

            bool unlocked = TechManager.Instance.TryUnlock(techFarming);
            Assert.IsTrue(unlocked);
            Assert.IsTrue(TechManager.Instance.IsUnlocked(techFarming));
            Assert.IsTrue(TechManager.Instance.IsBuildingUnlocked(storage), "Storage should unlock after tech_farming");
            Assert.IsTrue(TechManager.Instance.IsCropUnlocked(berry), "Berry should unlock after tech_farming");
        }

        [UnityTest]
        public IEnumerator Toolbar_ShowsStorageAndBerryLocked_ThenUnlockedAfterResearch()
        {
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>("Assets/_Data/TechNode_Farming.asset");
            var knowledge = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");
            var storage = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/_Data/BuildingData_Storage.asset");
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");

            yield return null;
            var toolbar = ToolbarUI.Instance;
            Assert.AreEqual(UnlockState.Locked, toolbar.StateOf(storage), "Locked tools are shown greyed so players know they exist");
            Assert.IsTrue(toolbar.IsShown(storage));
            Assert.AreEqual(UnlockState.Locked, toolbar.StateOf(berry));

            ResourceManager.Instance.AddResource(knowledge, 100);
            Assert.IsTrue(TechManager.Instance.TryUnlock(techFarming));
            yield return null;

            Assert.AreEqual(UnlockState.Unlocked, toolbar.StateOf(storage), "Storage becomes buildable after tech unlock");
            Assert.AreEqual(UnlockState.Unlocked, toolbar.StateOf(berry), "Berry becomes plantable after tech unlock");
        }

        [UnityTest]
        public IEnumerator TryUnlock_WithoutEnoughKnowledge_Fails()
        {
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>("Assets/_Data/TechNode_Farming.asset");

            bool unlocked = TechManager.Instance.TryUnlock(techFarming);
            Assert.IsFalse(unlocked, "Unlock should fail with 0 starting Knowledge");
            Assert.IsFalse(TechManager.Instance.IsUnlocked(techFarming));
            yield return null;
        }
    }
}
