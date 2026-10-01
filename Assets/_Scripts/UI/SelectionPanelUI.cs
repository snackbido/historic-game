using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Bảng "Đang chọn" (quyết định M5: hiển thị theo nhóm và theo nghề): tên từng người, ô theo nghề
    /// (click để chỉ giữ người nghề đó) và nút đổi nghề cho cả nhóm. Ẩn khi không chọn ai.
    /// Mỗi danh sách nút có container riêng (tránh lỗi xóa nhầm nút đã gặp ở M4).
    /// </summary>
    public class SelectionPanelUI : MonoBehaviour
    {
        [Tooltip("Object CON chứa nội dung bảng — không phải object gắn script này (ẩn nó sẽ tắt luôn việc nghe sự kiện)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text membersLabel;
        [SerializeField] private Transform professionChipContainer;
        [SerializeField] private Transform changeProfessionContainer;
        [SerializeField] private Button buttonPrefab;

        private readonly List<ProfessionData> professions = new List<ProfessionData>();

        private void OnEnable() => EventBus.OnSelectionChanged += Refresh;
        private void OnDisable() => EventBus.OnSelectionChanged -= Refresh;

        private void Start()
        {
            if (NpcManager.Instance != null) professions.AddRange(NpcManager.Instance.KnownProfessions);
            BuildChangeProfessionButtons();
            Refresh(SelectionManager.Instance != null ? SelectionManager.Instance.Selected : null);
        }

        private void BuildChangeProfessionButtons()
        {
            if (buttonPrefab == null || changeProfessionContainer == null) return;
            foreach (var profession in professions)
            {
                Button button = CreateButton(changeProfessionContainer, $"→ {profession.displayName}", profession.tunicColor);
                button.onClick.AddListener(() => ChangeProfession(profession));
            }
        }

        private void Refresh(IReadOnlyList<NpcController> selected)
        {
            bool visible = selected != null && selected.Count > 0;
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (!visible) return;

            if (titleLabel != null) titleLabel.text = $"Đang chọn: {selected.Count} người";
            if (membersLabel != null) membersLabel.text = DescribeMembers(selected);
            RebuildProfessionChips(selected);
        }

        private static string DescribeMembers(IReadOnlyList<NpcController> selected)
        {
            var sb = new StringBuilder();
            foreach (var npc in selected)
            {
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(npc.NpcName).Append(npc.Gender == Gender.Male ? " (nam)" : " (nữ)");
                if (npc.IsStarving) sb.Append(" (đói)");
            }
            return sb.ToString();
        }

        private void RebuildProfessionChips(IReadOnlyList<NpcController> selected)
        {
            if (buttonPrefab == null || professionChipContainer == null) return;

            foreach (Transform child in professionChipContainer)
                Destroy(child.gameObject);

            // Đếm theo thứ tự nghề trong danh sách (ổn định, không nhảy chỗ mỗi lần chọn lại).
            var counts = new Dictionary<ProfessionData, int>();
            foreach (var npc in selected)
            {
                if (npc.Profession == null) continue;
                counts.TryGetValue(npc.Profession, out int count);
                counts[npc.Profession] = count + 1;
            }

            foreach (var profession in professions)
            {
                if (!counts.TryGetValue(profession, out int count)) continue;
                Button chip = CreateButton(professionChipContainer, $"{profession.displayName} ×{count}", profession.tunicColor);
                chip.onClick.AddListener(() => FilterByProfession(profession));
            }
        }

        private static void FilterByProfession(ProfessionData profession)
        {
            var selection = SelectionManager.Instance;
            if (selection == null) return;
            var filtered = new List<NpcController>();
            foreach (var npc in selection.Selected)
                if (npc.Profession == profession) filtered.Add(npc);
            selection.SetSelection(filtered);
        }

        /// <summary>Đổi nghề cho mọi người đang chọn (public để test gọi trực tiếp).</summary>
        public static void ChangeProfession(ProfessionData profession)
        {
            var selection = SelectionManager.Instance;
            if (selection == null || selection.Selected.Count == 0) return;

            var changed = new List<NpcController>(selection.Selected);
            foreach (var npc in changed) npc.SetProfession(profession);

            EventBus.RaiseNotification($"Đã đổi nghề {changed.Count} người thành {profession.displayName}");
            selection.SetSelection(changed); // làm mới bảng (ô nghề thay đổi)
        }

        private Button CreateButton(Transform parent, string text, Color tint)
        {
            Button button = Instantiate(buttonPrefab, parent);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = text;
                label.fontSize = 18f;
                label.color = Color.white;
            }

            // Tô nút theo màu áo của nghề (tối đi một chút cho chữ trắng dễ đọc).
            var image = button.GetComponent<Image>();
            if (image != null) image.color = Color.Lerp(tint, Color.black, 0.25f);
            return button;
        }
    }
}
