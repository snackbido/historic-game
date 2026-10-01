using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Dòng gợi ý dưới màn hình cho đối tượng gần nhất, vd "[E] Cho Heo rung ăn (-3 Thuc an) — thuần hóa 1/2".
    /// Giúp người chơi biết trước phím E sẽ làm gì (tránh nhầm lẫn kiểu "cho ăn hay thu hoạch" ở M3).
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private TMP_Text label;

        private void Update()
        {
            if (label == null) return;

            bool placing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;
            string text = placing ? null : DescribeCombat(combat);
            if (text == null && !placing && interaction != null) text = Describe(interaction.Nearest);
            label.text = text ?? string.Empty;
        }

        /// <summary>Có thú dữ trong tầm ném đá thì nhắc phím chiến đấu (ưu tiên hơn gợi ý tương tác).</summary>
        public static string DescribeCombat(PlayerCombat combat)
        {
            if (combat == null) return null;
            PredatorAI target = combat.FindTarget(combat.StoneRange);
            if (target == null) return null;

            string name = target.Data.displayName;
            bool inSpearRange = combat.FindTarget(combat.SpearRange) != null;
            return inSpearRange ? $"[F] Đâm giáo  ·  [R] Ném đá — {name}" : $"[R] Ném đá vào {name}";
        }

        public static string Describe(MonoBehaviour target)
        {
            switch (target)
            {
                case ResourceNode node:
                    return node.IsDepleted
                        ? $"{node.ActionName}: đã cạn — chờ hồi lại"
                        : $"[E] {node.ActionName} — còn {node.AmountRemaining} {node.ResourceType.displayName}";
                case FarmPlot plot:
                    return DescribePlot(plot);
                case AnimalController animal:
                    return DescribeAnimal(animal);
                default:
                    return null;
            }
        }

        private static string DescribePlot(FarmPlot plot)
        {
            switch (plot.State)
            {
                case FarmPlotState.Empty:
                    CropData selected = FarmManager.Instance != null ? FarmManager.Instance.SelectedCrop : null;
                    return selected != null
                        ? $"[E] Gieo {selected.displayName}"
                        : "Ô đất trống — chọn hạt giống ở bảng bên phải";
                case FarmPlotState.Growing:
                    return $"{plot.Crop.displayName} đang lớn — {Mathf.FloorToInt(plot.GrowthProgress * 100f)}%";
                case FarmPlotState.ReadyToHarvest:
                    return $"[E] Thu hoạch {plot.Crop.displayName} ({FormatAmounts(plot.Crop.harvestYield, "+")})";
                case FarmPlotState.Withered:
                    return "[E] Dọn cây héo";
                default:
                    return null;
            }
        }

        private static string DescribeAnimal(AnimalController animal)
        {
            AnimalData data = animal.Data;
            if (data == null) return null;

            string feedCost = FormatAmounts(data.feedCost, "-");
            if (animal.State == AnimalState.Wild)
                return $"[E] Cho {data.displayName} hoang ăn ({feedCost}) — thuần hóa {animal.TamingProgress}/{data.feedingsToTame}";
            if (animal.ProductReady)
                return $"[E] Thu sản phẩm từ {data.displayName} ({FormatAmounts(data.products, "+")})";
            return $"[E] Cho {data.displayName} ăn ({feedCost}) — no {Mathf.RoundToInt(animal.Hunger)}%";
        }

        private static string FormatAmounts(List<ResourceAmount> amounts, string sign)
        {
            var parts = new List<string>();
            foreach (var a in amounts)
                if (a.type != null) parts.Add($"{sign}{a.amount} {a.type.displayName}");
            return string.Join(", ", parts);
        }
    }
}
