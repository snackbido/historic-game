using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone5SelectionTests : PlayModeTestBase
    {
        private static Vector2 ScreenPointOf(NpcController npc) =>
            Camera.main.WorldToScreenPoint(npc.transform.position + Vector3.up * 0.5f);

        [UnityTest]
        public IEnumerator BoxOverWholeScreen_SelectsEveryAdultVillager_AndShowsRings()
        {
            yield return null;
            var selection = SelectionManager.Instance;

            selection.SelectInScreenRect(new Rect(0f, 0f, Screen.width, Screen.height), additive: false);

            Assert.AreEqual(NpcController.All.Count(n => n.IsAdult), selection.Selected.Count);
            Assert.IsTrue(selection.Selected.All(n => n.IsSelected), "Selected villagers should show their selection ring");

            selection.ClearSelection();
            Assert.AreEqual(0, selection.Selected.Count);
            Assert.IsFalse(NpcController.All.Any(n => n.IsSelected));
        }

        [UnityTest]
        public IEnumerator ClickSelectsOne_ShiftClickAddsAndToggles()
        {
            yield return null;
            var selection = SelectionManager.Instance;
            var a = NpcController.All[0];
            var b = NpcController.All[1];

            selection.SelectAt(ScreenPointOf(a), additive: false);
            CollectionAssert.AreEqual(new[] { a }, selection.Selected.ToArray());

            selection.SelectAt(ScreenPointOf(b), additive: true);
            CollectionAssert.AreEquivalent(new[] { a, b }, selection.Selected.ToArray());

            selection.SelectAt(ScreenPointOf(a), additive: true);
            CollectionAssert.AreEqual(new[] { b }, selection.Selected.ToArray(), "Shift-click on a selected villager deselects it");

            selection.SelectAt(new Vector2(5f, 5f), additive: false);
            Assert.AreEqual(0, selection.Selected.Count, "Clicking empty ground clears the selection");
        }

        [UnityTest]
        public IEnumerator MoveCommand_SendsGroupToSpreadOutSpots_ThenTheyHoldPosition()
        {
            yield return null;
            var selection = SelectionManager.Instance;
            selection.SelectInScreenRect(new Rect(0f, 0f, Screen.width, Screen.height), additive: false);
            var group = selection.Selected.ToList();
            var target = new Vector3(-4f, 0f, 3.5f);

            selection.IssueMoveCommand(target);
            yield return null;

            Assert.IsTrue(group.All(n => n.State == NpcState.Moving), "Every selected villager should start moving");
            var destinations = group.Select(n => n.Destination).ToList();
            for (int i = 0; i < destinations.Count; i++)
            for (int j = i + 1; j < destinations.Count; j++)
                Assert.Greater(Vector3.Distance(destinations[i], destinations[j]), 0.5f, "Villagers should not stack on one spot");
            Assert.IsTrue(destinations.All(d => InteractableRegistry.GroundDistance(d, target) < 2.5f), "Formation stays around the clicked point");

            float deadline = Time.time + 25f;
            while (group.Any(n => n.State == NpcState.Moving) && Time.time < deadline)
                yield return null;
            Assert.IsTrue(group.All(n => n.State == NpcState.Idle), "Everyone should arrive");

            // Đứng giữ vị trí: một lúc sau vẫn không tự đi dạo.
            float wait = Time.time + 10f;
            while (Time.time < wait) yield return null;
            Assert.IsTrue(group.All(n => n.State == NpcState.Idle && n.IsHoldingPosition), "Commanded villagers hold position instead of wandering");
        }

        [UnityTest]
        public IEnumerator AfterOrder_DeselectedVillagerReturnsToWandering_SelectedOneKeepsHolding()
        {
            yield return null;
            var selection = SelectionManager.Instance;
            var released = NpcController.All[0];
            var kept = NpcController.All[1];

            selection.SetSelection(new[] { released, kept });
            selection.IssueMoveCommand(new Vector3(-4f, 0f, 3.5f));
            selection.SetSelection(new[] { kept }); // bỏ chọn người thứ nhất, vẫn chọn người thứ hai

            float deadline = Time.time + 25f;
            while ((released.State == NpcState.Moving || kept.State == NpcState.Moving) && Time.time < deadline)
                yield return null;
            Assert.IsTrue(released.IsHoldingPosition && kept.IsHoldingPosition, "Both hold position right after arriving");

            // Hết 20–30s đứng rảnh không được chọn → quay lại hành vi ban đầu (đi dạo).
            float wait = Time.time + 32f;
            while (Time.time < wait) yield return null;

            Assert.IsFalse(released.IsHoldingPosition, "Deselected villager stops holding after 20-30s");
            Assert.IsTrue(kept.IsHoldingPosition, "A still-selected villager keeps holding position");

            deadline = Time.time + 15f;
            while (released.State == NpcState.Idle && Time.time < deadline)
                yield return null;
            Assert.AreEqual(NpcState.Wandering, released.State, "Released villager wanders again like at the start");
        }

        [UnityTest]
        public IEnumerator ControlGroups_SaveAndRecall()
        {
            yield return null;
            var selection = SelectionManager.Instance;
            var a = NpcController.All[0];
            var b = NpcController.All[1];

            selection.SelectAt(ScreenPointOf(a), additive: false);
            selection.SelectAt(ScreenPointOf(b), additive: true);
            selection.SaveGroup(2);
            selection.ClearSelection();

            selection.RecallGroup(2);
            CollectionAssert.AreEquivalent(new[] { a, b }, selection.Selected.ToArray());

            // NPC bị xóa khỏi thế giới thì cũng rời khỏi nhóm/đang chọn.
            a.gameObject.SetActive(false);
            Object.Destroy(a.gameObject);
            yield return null;
            CollectionAssert.AreEqual(new[] { b }, selection.Selected.ToArray());
            selection.RecallGroup(2);
            CollectionAssert.AreEqual(new[] { b }, selection.Selected.ToArray());
        }

        [Test]
        public void FormationOffsets_AreUniqueAndCentered()
        {
            var offsets = SelectionManager.FormationOffsets(4, 1f);
            Assert.AreEqual(4, offsets.Distinct().Count());
            Vector3 center = offsets.Aggregate(Vector3.zero, (sum, o) => sum + o) / offsets.Count;
            Assert.Less(center.magnitude, 0.001f, "A full square formation should be centered on the target");
        }
    }
}
