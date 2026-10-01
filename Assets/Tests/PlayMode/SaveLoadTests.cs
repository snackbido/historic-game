using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class SaveLoadTests : PlayModeTestBase
    {
        [SetUp]
        public void UseTestSaveFile() => SaveSystem.FileNameOverride = "test_savegame.json";

        [TearDown]
        public void RemoveTestSaveFile()
        {
            SaveSystem.Delete();
            SaveSystem.FileNameOverride = null;
        }

        [UnityTest]
        public IEnumerator SaveLoad_RestoresTechFarmPlotsAndTamedAnimals()
        {
            var berry = AssetDatabase.LoadAssetAtPath<CropData>("Assets/_Data/CropData_Berry.asset");
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var knowledge = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Knowledge.asset");
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>("Assets/_Data/TechNode_Farming.asset");

            // Trạng thái cần lưu: tech đã mở, FarmPlot_1 đang lớn, heo đã thuần.
            ResourceManager.Instance.AddResource(knowledge, 10);
            Assert.IsTrue(TechManager.Instance.TryUnlock(techFarming));
            var plot1 = GameObject.Find("FarmPlot_1").GetComponent<FarmPlot>();
            var plot2 = GameObject.Find("FarmPlot_2").GetComponent<FarmPlot>();
            FarmManager.Instance.SelectCrop(berry);
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot1));

            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();
            ResourceManager.Instance.AddResource(food, 10);
            TamingSystem.Instance.TryInteract(boar);
            TamingSystem.Instance.TryInteract(boar);
            Assert.AreEqual(AnimalState.Tamed, boar.State);
            yield return null;

            int animalsAtSave = InteractableRegistry.All<AnimalController>().Count; // heo rừng + dê núi (E3)
            GameManager.Instance.SaveGame();

            // Làm lệch trạng thái sau khi lưu: gieo thêm FarmPlot_2, xóa con heo.
            Assert.IsTrue(FarmManager.Instance.TryInteract(plot2));
            Object.Destroy(boar.gameObject);
            yield return null;

            GameManager.Instance.LoadGame();
            yield return null;

            Assert.IsTrue(TechManager.Instance.IsUnlocked(techFarming), "Unlocked tech should be restored");
            Assert.AreEqual(FarmPlotState.Growing, plot1.State, "FarmPlot_1 should still be growing after load");
            Assert.AreSame(berry, plot1.Crop);
            Assert.AreEqual(FarmPlotState.Empty, plot2.State, "FarmPlot_2 was planted after saving, so load should clear it");

            var restored = GameObject.Find("WildBoar");
            Assert.IsNotNull(restored, "The saved boar should be recreated on load");
            Assert.AreEqual(AnimalState.Tamed, restored.GetComponent<AnimalController>().State, "Restored boar should still be tamed");
            Assert.AreEqual(animalsAtSave, InteractableRegistry.All<AnimalController>().Count, "Load should restore exactly the saved animals, no duplicates");
        }

        [UnityTest]
        public IEnumerator InteractionPrompt_DescribesWhatEWillDo()
        {
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();
            var tree = GameObject.Find("Tree").GetComponent<ResourceNode>();
            yield return null;

            StringAssert.Contains("thuần hóa 0/", InteractionPromptUI.Describe(boar), "Wild animal prompt should show taming progress");
            StringAssert.Contains("Chặt cây", InteractionPromptUI.Describe(tree));
            Assert.IsNull(InteractionPromptUI.Describe(null));
        }
    }
}
