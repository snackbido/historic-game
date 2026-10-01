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
            Fire fire = target != null ? target.GetComponent<Fire>() : null;
            if (fire != null) return $"[E] Dập lửa — đang cháy {Percent(fire.Intensity)} (chuột phải ra lệnh dân làng gánh nước dập)";

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
                case RiceMortar mortar:
                    return DescribeMortar(mortar);
                case Canal canal:
                    return DescribeCanal(canal);
                case BuildingInstance building:
                    return DescribeDamage(building);
                default:
                    return null;
            }
        }

        private static string Percent(float value) => $"{Mathf.FloorToInt(value * 100f)}%";

        private static string DescribePlot(FarmPlot plot)
        {
            switch (plot.State)
            {
                case FarmPlotState.Wild:
                    return $"[E] Khai hoang {plot.FieldName} — {Mathf.FloorToInt(plot.ClearProgress * 100f)}%";
                case FarmPlotState.Unplowed:
                    return $"[E] Cày/xới đất — {Mathf.FloorToInt(plot.PlowProgress * 100f)}%";
                case FarmPlotState.Empty:
                    CropData selected = FarmManager.Instance != null ? FarmManager.Instance.CropFor(plot) : null;
                    if (selected != null)
                    {
                        string blocker = plot.PlantBlocker(selected);
                        return blocker == null ? $"[E] Gieo {selected.displayName}" : $"{blocker} (nước {Percent(plot.Water)})";
                    }
                    return plot.FieldType == FieldType.Paddy
                        ? "Ruộng nước trống — chọn hạt giống lúa ở bảng bên phải"
                        : "Ô đất trống — chọn hạt giống ở bảng bên phải";
                case FarmPlotState.Growing:
                    string water = plot.HasEnoughWater ? $"nước {Percent(plot.Water)}" : "THIẾU NƯỚC — cây ngừng lớn!";
                    if (plot.IsIrrigated) water += plot.IrrigatedBy.IsPumped ? " (guồng nước)" : " (mương)";
                    string growing = $"{plot.Crop.displayName} đang lớn — {Percent(plot.GrowthProgress)} · {water}";
                    if (plot.Weeds > 0.15f) return $"[E] Làm cỏ (cỏ {Percent(plot.Weeds)}) · {growing}";
                    if (plot.CanFertilize && FarmManager.Instance != null && FarmManager.Instance.HasFertilizer)
                        return $"[E] Bón phân (+{Percent(FarmPlot.FertilizerBonus)} sản lượng) · {growing}";
                    return plot.Fertilized ? $"{growing} · đã bón phân" : growing;
                case FarmPlotState.ReadyToHarvest:
                    return $"[E] {plot.Crop.harvestVerb} {plot.Crop.displayName.ToLowerInvariant()} ({FormatAmounts(plot.Crop.harvestYield, "+")})";
                case FarmPlotState.Withered:
                    return "[E] Dọn cây héo";
                default:
                    return null;
            }
        }

        public static string DescribeDamage(BuildingInstance building)
        {
            if (!building.IsDamaged) return null;
            string state = building.IsCollapsed ? "ĐÃ SẬP" : $"hư hại, còn {Percent(building.HealthFraction)}";
            ResourceAmount cost = building.RepairCost;
            string price = cost.type != null ? $" (-{cost.amount} {cost.type.displayName}/lượt)" : "";
            string blocker = building.RepairBlocker();
            return blocker == null ? $"[E] Sửa {building.LevelName}{price} — {state}" : $"{building.LevelName} {state} — {blocker}";
        }

        private static string DescribeCanal(Canal canal)
        {
            if (!canal.IsDug) return $"[E] Đào mương — {Percent(canal.DigProgress)}";
            if (canal.IsPumped) return $"Mương có nước guồng bơm (cách guồng {canal.Distance} ô) — tưới nhanh ruộng sát bên";
            return canal.IsFlowing
                ? $"Mương có nước (cách ao {canal.Distance} ô) — tưới ruộng sát bên"
                : $"Mương khô — phải nối liền tới ao (nước tự chảy tối đa {CanalNetwork.CurrentGravityReach} ô)";
        }

        private static string DescribeMortar(RiceMortar mortar)
        {
            string rack = $"giàn phơi {mortar.SheavesOnRack}/{mortar.RackCapacity} (khô {mortar.DrySheaves})";
            string task = mortar.NextTask;
            if (task == "Giã gạo" && mortar.PoundProgress > 0) task += $" {Percent(mortar.PoundFraction)}";
            return task != null ? $"[E] {task} · {rack}" : $"{mortar.IdleReason()} · {rack}";
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
