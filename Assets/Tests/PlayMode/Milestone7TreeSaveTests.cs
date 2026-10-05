using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Lưu / tải cây (nợ tồn đọng): cây chặt hết không mọc lại, cây chặt dở giữ đúng số gỗ, cây chặt sau lúc lưu thì tải lại có cây.</summary>
    public class Milestone7TreeSaveTests : PlayModeTestBase
    {
        private const string TestSave = "test_savegame_trees.json";

        private static ResourceNode Tree(string name) =>
            InteractableRegistry.All<ResourceNode>().FirstOrDefault(n => n.name == name);

        private static void ChopAll(ResourceNode node)
        {
            while (node != null && node.AmountRemaining > 0) node.Harvest();
        }

        [UnityTest]
        public IEnumerator PartlyChoppedTree_KeepsItsWoodAfterLoad()
        {
            SaveSystem.FileNameOverride = TestSave;
            try
            {
                yield return null;
                ResourceNode tree = Tree("Tree_1");
                Assert.IsNotNull(tree);
                tree.Harvest();
                tree.Harvest();
                int saved = tree.AmountRemaining;
                GameManager.Instance.SaveGame();

                tree.Harvest();
                GameManager.Instance.LoadGame();
                Assert.AreEqual(saved, Tree("Tree_1").AmountRemaining, "Chopped-down wood stays chopped, no free refill");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }

        [UnityTest]
        public IEnumerator TreeChoppedAfterSaving_ComesBackOnLoad()
        {
            SaveSystem.FileNameOverride = TestSave;
            try
            {
                yield return null;
                ResourceNode tree = Tree("Tree_2");
                Vector3 where = tree.transform.position;
                int full = tree.AmountRemaining;
                GameManager.Instance.SaveGame();

                ChopAll(tree);
                yield return null; // cây đổ (Destroy cuối khung hình)
                Assert.IsNull(Tree("Tree_2"));

                GameManager.Instance.LoadGame();
                ResourceNode back = Tree("Tree_2");
                Assert.IsNotNull(back, "Loading a save from before the chop restores the tree");
                Assert.AreEqual(full, back.AmountRemaining);
                Assert.Less(Vector3.Distance(where, back.transform.position), 0.01f, "Same spot");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }

        [UnityTest]
        public IEnumerator FelledTree_StaysGone_WhenContinuingInAFreshWorld()
        {
            SaveSystem.FileNameOverride = TestSave;
            try
            {
                yield return null;
                ChopAll(Tree("Tree_3"));
                yield return null;
                GameManager.Instance.SaveGame();

                // "Chơi tiếp" từ menu chính: scene mới (đủ cây như lúc đầu) rồi tải bản lưu.
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield return null;
                yield return null;
                Assert.IsNotNull(Tree("Tree_3"), "Fresh scene has every tree");
                GameManager.Instance.LoadGame();
                Assert.IsNull(Tree("Tree_3"), "The felled tree does not grow back after loading");
                Assert.IsNotNull(Tree("Tree_4"), "Other trees are untouched");
            }
            finally
            {
                SaveSystem.Delete();
                SaveSystem.FileNameOverride = null;
            }
        }
    }
}
