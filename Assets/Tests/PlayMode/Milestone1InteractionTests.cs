using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone1InteractionTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator NearestInteractable_OnGroundPlane_FindsTreeInRangeOnly()
        {
            var player = GameObject.FindWithTag("Player");
            var controller = player.GetComponent<PlayerController>();
            var interaction = player.GetComponent<PlayerInteraction>();
            var tree = GameObject.Find("Tree").GetComponent<ResourceNode>();

            // Cây ở (2, 0, 1): đứng lệch trên trục Z (mặt đất 2.5D) vẫn phải tìm thấy.
            controller.SetPosition(tree.transform.position + new Vector3(-0.8f, 0f, -0.6f));
            yield return null;
            yield return null;
            Assert.AreSame(tree, interaction.Nearest, "Tree within interact range on the XZ plane should be the nearest interactable");

            controller.SetPosition(new Vector3(-10f, 0f, -10f));
            yield return null;
            yield return null;
            Assert.IsNull(interaction.Nearest, "Nothing should be interactable far away from every object");
        }

        [UnityTest]
        public IEnumerator HarvestingTree_UntilDepleted_UnregistersIt()
        {
            var tree = GameObject.Find("Tree").GetComponent<ResourceNode>();
            var wood = tree.ResourceType;
            int before = ResourceManager.Instance.GetAmount(wood);

            for (int i = 0; i < 10; i++) tree.Harvest();
            yield return null;

            Assert.AreEqual(before + 10, ResourceManager.Instance.GetAmount(wood));
            Assert.IsTrue(tree == null, "Depleted tree should be destroyed");
            Assert.IsNull(InteractableRegistry.FindNearest(new Vector3(2f, 0f, 1f), 0.1f),
                "Destroyed tree must not stay in the interactable registry");
        }
    }
}
