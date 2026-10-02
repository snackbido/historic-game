using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Thiết kế lại UI (.claude/RE-DESIGNUI.md): thanh tab cạnh dưới, 4 trạng thái mở khóa, tooltip, chấm báo + toast, dải tài nguyên.</summary>
    public class Milestone7ToolbarTests : PlayModeTestBase
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static ToolbarUI Toolbar => ToolbarUI.Instance;
        private static ResourceTypeData Knowledge => Asset<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");
        private static TechNode Farming => Asset<TechNode>("Assets/_Data/TechNode_Farming.asset");
        private static TechNode Rice => Asset<TechNode>("Assets/_Data/TechNode_Rice.asset");
        private static BuildingData Building(string name) => Asset<BuildingData>($"Assets/_Data/BuildingData_{name}.asset");

        [UnityTest]
        public IEnumerator Tabs_OpenOneAtATime_AndClickingAgainCloses()
        {
            yield return null;
            Assert.IsFalse(Toolbar.PanelVisible, "Starts closed — the screen stays clear");
            Toolbar.Toggle(ToolCategory.Build);
            yield return null;
            Assert.IsTrue(Toolbar.PanelVisible);
            Assert.AreEqual(ToolCategory.Build, Toolbar.Current);
            Toolbar.Toggle(ToolCategory.Research);
            Assert.AreEqual(ToolCategory.Research, Toolbar.Current, "Switching tab swaps content, no second panel");
            Toolbar.Toggle(ToolCategory.Research);
            yield return null;
            Assert.IsFalse(Toolbar.PanelVisible);
        }

        [UnityTest]
        public IEnumerator UnlockStates_HiddenLockedReadyUnlocked()
        {
            yield return null;
            Assert.AreEqual(UnlockState.Unlocked, Toolbar.StateOf(Building("Hut")));
            Assert.AreEqual(UnlockState.Locked, Toolbar.StateOf(Building("Storage")), "Its tech has no prerequisites → visible");
            Assert.AreEqual(UnlockState.Hidden, Toolbar.StateOf(Rice), "Needs Farming first → not shown yet");
            Assert.IsFalse(Toolbar.IsShown(Rice));
            Assert.AreEqual(UnlockState.Hidden, Toolbar.StateOf(Building("PaddyField")));

            ResourceManager.Instance.AddResource(Knowledge, 100);
            Assert.AreEqual(UnlockState.Ready, Toolbar.StateOf(Farming));
            Assert.AreEqual(UnlockState.Ready, Toolbar.StateOf(Building("Storage")), "Follows the tech that unlocks it");

            Assert.IsTrue(TechManager.Instance.TryUnlock(Farming));
            Assert.AreEqual(UnlockState.Unlocked, Toolbar.StateOf(Farming));
            Assert.AreNotEqual(UnlockState.Hidden, Toolbar.StateOf(Rice), "Farming done → Rice appears");
            Assert.IsTrue(Toolbar.IsShown(Rice));
        }

        [UnityTest]
        public IEnumerator Ready_ShowsBadgeAndToastOnce_ClickingToastOpensResearch()
        {
            yield return null;
            Assert.IsFalse(Toolbar.HasBadge(ToolCategory.Research));
            ResourceManager.Instance.AddResource(Knowledge, 100);
            Assert.IsTrue(Toolbar.HasBadge(ToolCategory.Research), "Red dot on the tab");
            StringAssert.Contains("Có thể nghiên cứu", ToastUI.Current);
            StringAssert.Contains(Farming.displayName, ToastUI.Current);
            Assert.IsTrue(ToastUI.HasAction);

            ToastUI.ClickCurrent();
            Assert.AreEqual(ToolCategory.Research, Toolbar.Current, "Toast opens the right tab");

            // Tri thức tụt dưới ngưỡng rồi lên lại → không báo lần hai.
            Assert.IsTrue(ResourceManager.Instance.TrySpend(Knowledge, 100));
            Assert.IsFalse(Toolbar.HasBadge(ToolCategory.Research));
            ResourceManager.Instance.AddResource(Knowledge, 100);
            Assert.IsNull(ToastUI.Current, "Announced only once per tech");
            Assert.IsTrue(Toolbar.HasBadge(ToolCategory.Research));
        }

        [UnityTest]
        public IEnumerator Tooltip_ListsConditions_WithWhatYouHave()
        {
            yield return null;
            string tip = Toolbar.TooltipFor(Building("Storage"));
            StringAssert.Contains($"Nghiên cứu: {Farming.displayName}", tip);
            StringAssert.Contains(UnlockRules.Missing, tip);
            StringAssert.Contains("đang có 0", tip);

            ResourceManager.Instance.AddResource(Knowledge, 100);
            StringAssert.Contains(UnlockRules.Ok, Toolbar.TooltipFor(Farming), "Cost now met");
        }

        [UnityTest]
        public IEnumerator Panel_HidesWhilePlacing_AndComesBack()
        {
            yield return null;
            Toolbar.Open(ToolCategory.Build);
            Toolbar.Click(Building("Hut"));
            yield return null;
            Assert.IsTrue(BuildingPlacer.Instance.IsPlacing);
            Assert.IsFalse(Toolbar.PanelVisible, "Panel must not cover the placement spot");
            BuildingPlacer.Instance.CancelSelection();
            yield return null;
            Assert.IsTrue(Toolbar.PanelVisible);
        }

        [UnityTest]
        public IEnumerator ClickingUnlockedCrop_SelectsItsSeed()
        {
            yield return null;
            ResourceManager.Instance.AddResource(Knowledge, 100);
            Assert.IsTrue(TechManager.Instance.TryUnlock(Farming));
            var berry = Asset<CropData>("Assets/_Data/CropData_Berry.asset");
            Toolbar.Click(berry);
            Assert.IsTrue(FarmManager.Instance.IsSelected(berry));
        }

        [UnityTest]
        public IEnumerator Hud_AlwaysShowsCoreResources_OthersOnlyWhenOwned()
        {
            yield return null;
            var wood = Asset<ResourceTypeData>("Assets/_Data/ResourceType_Wood.asset");
            var sheaf = Asset<ResourceTypeData>("Assets/_Data/ResourceType_RiceSheaf.asset");
            Assert.IsTrue(HudUI.Instance.IsShown(wood));
            Assert.IsTrue(HudUI.Instance.IsShown(Knowledge));
            Assert.IsFalse(HudUI.Instance.IsShown(sheaf), "Nothing harvested yet → hidden");
            ResourceManager.Instance.AddResource(sheaf, 3);
            Assert.IsTrue(HudUI.Instance.IsShown(sheaf));
            Assert.AreEqual("3", HudUI.Instance.ValueText(sheaf));
            StringAssert.Contains("Sức chứa", HudUI.Instance.TooltipFor(Asset<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset")),
                "Capacity moved into the tooltip");
            Assert.IsFalse(string.IsNullOrEmpty(HudUI.Instance.PeopleText));
        }

        [Test]
        public void EveryTool_HasAnIcon()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:BuildingData", new[] { "Assets/_Data" }))
                Assert.IsNotNull(Asset<BuildingData>(AssetDatabase.GUIDToAssetPath(guid)).icon, AssetDatabase.GUIDToAssetPath(guid));
            foreach (var guid in AssetDatabase.FindAssets("t:CropData", new[] { "Assets/_Data" }))
                Assert.IsNotNull(Asset<CropData>(AssetDatabase.GUIDToAssetPath(guid)).icon, AssetDatabase.GUIDToAssetPath(guid));
            foreach (var guid in AssetDatabase.FindAssets("t:TechNode", new[] { "Assets/_Data" }))
                Assert.IsNotNull(Asset<TechNode>(AssetDatabase.GUIDToAssetPath(guid)).icon, AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsNotNull(Asset<ResourceTypeData>("Assets/_Data/ResourceType_Wood.asset").icon);
        }
    }
}
