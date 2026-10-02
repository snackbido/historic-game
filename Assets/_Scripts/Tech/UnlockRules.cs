using System.Collections.Generic;
using System.Text;

namespace PrehistoricTribe
{
    public enum ToolCategory { Build, Farm, Research }

    /// <summary>
    /// Ẩn: chưa có công nghệ tiền đề nào mở (chưa cần biết tới). Khóa: thấy được nhưng còn thiếu điều kiện.
    /// Sẵn sàng: đủ điều kiện, chỉ cần bấm nghiên cứu. Đã mở: dùng được.
    /// </summary>
    public enum UnlockState { Hidden, Locked, Ready, Unlocked }

    /// <summary>
    /// Trạng thái mở khóa của công cụ trên thanh công cụ (thiết kế lại UI, .claude/RE-DESIGNUI.md bước 3) và chữ cho tooltip.
    /// Công trình / cây trồng mở bằng công nghệ → trạng thái của chúng đi theo công nghệ mở ra chúng.
    /// </summary>
    public static class UnlockRules
    {
        public const string Ok = "<color=#6BD66B>√</color>";
        public const string Missing = "<color=#FF6B6B>×</color>";

        private static TechManager Tech => TechManager.Instance;
        private static ResourceManager Resources => ResourceManager.Instance;

        public static UnlockState Of(TechNode tech)
        {
            if (tech == null || Tech == null) return UnlockState.Hidden;
            if (Tech.IsUnlocked(tech)) return UnlockState.Unlocked;
            bool anyPrerequisite = tech.prerequisites.Count == 0 || tech.prerequisites.Exists(p => p != null && Tech.IsUnlocked(p));
            if (!anyPrerequisite) return UnlockState.Hidden;
            bool allPrerequisites = tech.prerequisites.TrueForAll(p => p == null || Tech.IsUnlocked(p));
            return allPrerequisites && Resources != null && Resources.CanAfford(tech.cost) ? UnlockState.Ready : UnlockState.Locked;
        }

        public static UnlockState Of(BuildingData building)
        {
            if (building == null || Tech == null) return UnlockState.Hidden;
            if (Tech.IsBuildingUnlocked(building)) return UnlockState.Unlocked;
            return ViaTech(Tech.FindUnlocker(building));
        }

        public static UnlockState Of(CropData crop)
        {
            if (crop == null || Tech == null) return UnlockState.Hidden;
            if (Tech.IsCropUnlocked(crop)) return UnlockState.Unlocked;
            return ViaTech(Tech.FindUnlocker(crop));
        }

        private static UnlockState ViaTech(TechNode tech)
        {
            if (tech == null) return UnlockState.Hidden; // không có đường mở
            UnlockState state = Of(tech);
            return state == UnlockState.Unlocked ? UnlockState.Locked : state;
        }

        // ─── Tooltip ────────────────────────────────────────────────────────
        public static string Tooltip(BuildingData building)
        {
            var sb = new StringBuilder($"<b>{building.displayName}</b>");
            if (!string.IsNullOrEmpty(building.functionDescription)) sb.Append($"\n<size=85%>{building.functionDescription}</size>");
            if (Of(building) == UnlockState.Unlocked)
            {
                AppendCosts(sb, "Xây", building.costs);
                if (building.requiresOpenWater) sb.Append("\n<size=85%>Đặt sát bờ ao/suối tự nhiên</size>");
                else if (building.requiresWaterWithin > 0f) sb.Append($"\n<size=85%>Cần nguồn nước trong {building.requiresWaterWithin:0.#}m</size>");
            }
            else AppendTechRequirement(sb, Tech != null ? Tech.FindUnlocker(building) : null);
            return sb.ToString();
        }

        public static string Tooltip(CropData crop)
        {
            var sb = new StringBuilder($"<b>{crop.displayName}</b>");
            sb.Append($"\n<size=85%>Trồng ở {(crop.fieldType == FieldType.Paddy ? "ruộng nước" : "ruộng cạn")}</size>");
            if (Of(crop) == UnlockState.Unlocked)
            {
                AppendCosts(sb, "Gieo", crop.plantCost);
                sb.Append("\n<size=85%>Chọn giống rồi bấm vào ruộng để gieo</size>");
            }
            else AppendTechRequirement(sb, Tech != null ? Tech.FindUnlocker(crop) : null);
            return sb.ToString();
        }

        public static string Tooltip(TechNode tech)
        {
            var sb = new StringBuilder($"<b>{tech.displayName}</b>");
            string unlocks = Tech != null ? Tech.UnlocksText(tech) : null;
            if (unlocks != null) sb.Append($"\n<size=85%>Mở khóa: {unlocks}</size>");
            if (Of(tech) == UnlockState.Unlocked) sb.Append($"\n{Ok} Đã nghiên cứu");
            else
            {
                AppendTechConditions(sb, tech, "");
                if (Of(tech) == UnlockState.Ready) sb.Append("\n<color=#FFD24A>Bấm để nghiên cứu</color>");
            }
            return sb.ToString();
        }

        private static void AppendTechRequirement(StringBuilder sb, TechNode tech)
        {
            if (tech == null)
            {
                sb.Append($"\n{Missing} Chưa mở được");
                return;
            }
            sb.Append($"\n{Missing} Nghiên cứu: {tech.displayName}");
            AppendTechConditions(sb, tech, "    ");
        }

        /// <summary>Từng điều kiện của công nghệ: công nghệ cần trước + chi phí (kèm số đang có).</summary>
        private static void AppendTechConditions(StringBuilder sb, TechNode tech, string indent)
        {
            foreach (var prerequisite in tech.prerequisites)
                if (prerequisite != null)
                    sb.Append($"\n{indent}{Mark(Tech != null && Tech.IsUnlocked(prerequisite))} Cần trước: {prerequisite.displayName}");
            foreach (var item in tech.cost) AppendCostLine(sb, item, indent);
        }

        private static void AppendCosts(StringBuilder sb, string verb, List<ResourceAmount> costs)
        {
            if (costs.Count == 0) return;
            sb.Append($"\n<size=85%>{verb} tốn:</size>");
            foreach (var item in costs) AppendCostLine(sb, item, "");
        }

        private static void AppendCostLine(StringBuilder sb, ResourceAmount item, string indent)
        {
            if (item.type == null) return;
            int have = Resources != null ? Resources.GetAmount(item.type) : 0;
            sb.Append($"\n{indent}{Mark(have >= item.amount)} {item.type.displayName} {item.amount}  <size=85%>(đang có {have})</size>");
        }

        private static string Mark(bool ok) => ok ? Ok : Missing;
    }
}
