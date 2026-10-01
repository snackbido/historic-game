using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Bảng thông tin khi click vào công trình: tên cấp hiện tại, chức năng, cấp kế tiếp (lợi ích + chi phí)
    /// và nút Nâng cấp. Ẩn khi không chọn công trình nào.
    /// </summary>
    public class BuildingInfoPanelUI : MonoBehaviour
    {
        [Tooltip("Object CON chứa nội dung bảng — không phải object gắn script này")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text functionLabel;
        [SerializeField] private TMP_Text nextLevelLabel;
        [SerializeField] private Button upgradeButton;

        private BuildingInstance building;

        private void OnEnable()
        {
            EventBus.OnBuildingSelected += Show;
            EventBus.OnBuildingUpgraded += HandleUpgraded;
            EventBus.OnResourceChanged += HandleResourceChanged;
        }

        private void OnDisable()
        {
            EventBus.OnBuildingSelected -= Show;
            EventBus.OnBuildingUpgraded -= HandleUpgraded;
            EventBus.OnResourceChanged -= HandleResourceChanged;
        }

        private void Start()
        {
            if (upgradeButton != null) upgradeButton.onClick.AddListener(UpgradeSelected);
            Show(null);
        }

        private void HandleUpgraded(BuildingInstance upgraded)
        {
            if (upgraded == building) Refresh();
        }

        // Tài nguyên thay đổi → nút Nâng cấp có thể bật/tắt.
        private void HandleResourceChanged(ResourceTypeData _, int __)
        {
            if (building != null) Refresh();
        }

        private void Show(BuildingInstance selected)
        {
            building = selected;
            if (panelRoot != null) panelRoot.SetActive(building != null);
            if (building != null) Refresh();
        }

        private void UpgradeSelected()
        {
            if (building != null) building.TryUpgrade();
        }

        private void Refresh()
        {
            BuildingData data = building.Data;
            if (titleLabel != null)
                titleLabel.text = $"{building.LevelName}  (cấp {building.Level}/{data.MaxLevel})";
            if (functionLabel != null) functionLabel.text = DescribeFunction(building, building.CurrentLevel);

            string blocker = building.UpgradeBlocker();
            if (nextLevelLabel != null) nextLevelLabel.text = DescribeNext(building, blocker);

            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(!building.IsMaxLevel);
                upgradeButton.interactable = blocker == null;
                var label = upgradeButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = "Nâng cấp";
            }
        }

        public static string DescribeFunction(BuildingInstance building, BuildingLevel level)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(building.Data.functionDescription)) sb.Append(building.Data.functionDescription);
            if (level != null && level.housing > 0) sb.Append(sb.Length > 0 ? " · " : "").Append($"Chỗ cho con: {level.housing}");
            if (level != null && level.storageCapacity > 0)
                sb.Append(sb.Length > 0 ? " · " : "").Append($"Chứa: {level.storageCapacity} mỗi loại lương thực");

            string family = NpcManager.DescribeFamily(building);
            if (family != null) sb.Append('\n').Append(family);
            return sb.ToString();
        }

        public static string DescribeNext(BuildingInstance building, string blocker)
        {
            if (building.IsMaxLevel) return "Đã đạt cấp tối đa";

            BuildingLevel next = building.NextLevel;
            BuildingLevel current = building.CurrentLevel;
            var gains = new List<string>();
            if (next.housing > (current?.housing ?? 0)) gains.Add($"{next.housing} chỗ cho con, sinh nhanh hơn");
            if (next.storageCapacity > (current?.storageCapacity ?? 0)) gains.Add($"chứa {next.storageCapacity}/loại");

            string text = $"Lên {next.displayName}: {string.Join(", ", gains)} — Chi phí: {FormatCost(next.upgradeCost)}";
            return blocker != null ? $"{text}\n({blocker})" : text;
        }

        private static string FormatCost(List<ResourceAmount> costs)
        {
            var parts = new List<string>();
            foreach (var c in costs)
                if (c.type != null) parts.Add($"{c.amount} {c.type.displayName}");
            return parts.Count > 0 ? string.Join(", ", parts) : "miễn phí";
        }
    }
}
