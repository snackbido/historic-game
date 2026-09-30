using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PrehistoricTribe.Tests
{
    public class Milestone5UiTests : PlayModeTestBase
    {
        private static NpcController Npc(string professionId) =>
            NpcController.All.First(n => n.Profession != null && n.Profession.id == professionId);

        private static GameObject PanelContent => GameObject.Find("SelectionPanel").transform.Find("Content").gameObject;

        private static Button[] Chips() =>
            PanelContent.transform.Find("ProfessionChips").GetComponentsInChildren<Button>();

        private static string Label(Component c) => c.GetComponentInChildren<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Panel_ShowsSelectionGroupedByProfession_AndHidesWhenEmpty()
        {
            yield return null;
            Assert.IsFalse(PanelContent.activeSelf, "Panel is hidden while nothing is selected");

            SelectionManager.Instance.SetSelection(NpcController.All);
            yield return null; // chờ Destroy các ô cũ

            Assert.IsTrue(PanelContent.activeSelf);
            StringAssert.Contains($"{NpcController.All.Count} người", Label(PanelContent.transform.Find("Title")));
            string members = Label(PanelContent.transform.Find("Members"));
            foreach (var npc in NpcController.All) StringAssert.Contains(npc.NpcName, members);

            var chipLabels = Chips().Select(Label).ToList();
            Assert.AreEqual(4, chipLabels.Count, "One chip per profession in the selection");
            Assert.IsTrue(chipLabels.All(l => l.EndsWith("×1")), "Each starting villager has a different profession");

            SelectionManager.Instance.ClearSelection();
            yield return null;
            Assert.IsFalse(PanelContent.activeSelf);
        }

        [UnityTest]
        public IEnumerator ClickingProfessionChip_KeepsOnlyThatProfession()
        {
            yield return null;
            SelectionManager.Instance.SetSelection(NpcController.All);
            yield return null;

            Button hunterChip = Chips().First(b => Label(b).StartsWith("Thợ săn"));
            hunterChip.onClick.Invoke();
            yield return null;

            CollectionAssert.AreEqual(new[] { Npc("hunter") }, SelectionManager.Instance.Selected.ToArray());
            Assert.AreEqual(1, Chips().Length);
        }

        [UnityTest]
        public IEnumerator ChangeProfession_AppliesToSelection_AndUpdatesStats()
        {
            yield return null;
            var ka = Npc("villager");
            var hunter = NpcManager.Instance.FindProfession("hunter");

            SelectionManager.Instance.SetSelection(new[] { ka });
            SelectionPanelUI.ChangeProfession(hunter);
            yield return null;

            Assert.AreSame(hunter, ka.Profession);
            Assert.AreEqual(hunter.moveSpeed, ka.GetComponent<NavMeshAgent>().speed, 0.001f, "Speed follows the new profession");
            Assert.AreEqual(hunter.maxHealth, ka.Health.Max, 0.001f);
            Assert.AreEqual("Thợ săn ×1", Label(Chips().Single()), "Panel refreshes to the new profession");
        }

        [UnityTest]
        public IEnumerator HealthBar_ShowsWhenSelectedOrHurt()
        {
            yield return null;
            var ka = Npc("villager");
            var kaBar = ka.transform.Find("HealthBar").gameObject;
            var boar = GameObject.Find("WildBoar");
            var boarBar = boar.transform.Find("HealthBar").gameObject;

            yield return null;
            Assert.IsFalse(kaBar.activeSelf, "Healthy, unselected villager shows no bar");
            Assert.IsFalse(boarBar.activeSelf);

            SelectionManager.Instance.SetSelection(new[] { ka });
            yield return null;
            Assert.IsTrue(kaBar.activeSelf, "Selected villager shows the bar");

            SelectionManager.Instance.ClearSelection();
            ka.Health.TakeDamage(10f);
            boar.GetComponent<HealthComponent>().TakeDamage(10f);
            yield return null;
            Assert.IsTrue(kaBar.activeSelf, "Hurt villager shows the bar");
            Assert.IsTrue(boarBar.activeSelf, "Hurt animal shows the bar (e.g. while hunted)");

            var fill = kaBar.transform.Find("FillPivot");
            Assert.Less(fill.localScale.x, 0.58f, "Fill shrinks with lost health");
        }
    }
}
