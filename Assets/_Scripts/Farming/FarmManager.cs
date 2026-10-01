using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public class FarmManager : MonoBehaviour
    {
        public static FarmManager Instance { get; private set; }

        [Tooltip("Mọi loại cây trong game — dùng để tra CropData theo id khi tải game")]
        [SerializeField] private List<CropData> knownCrops = new List<CropData>();

        public CropData SelectedCrop { get; private set; }

        // Hạt giống chọn gần nhất cho từng loại ruộng: chọn Lúa rồi chọn Quả mọng → ruộng nước cấy lúa, ruộng cạn trồng quả mọng.
        private readonly Dictionary<FieldType, CropData> seedByField = new Dictionary<FieldType, CropData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SelectCrop(CropData crop)
        {
            SelectedCrop = crop;
            if (crop == null) seedByField.Clear();
            else seedByField[crop.fieldType] = crop;
        }

        /// <summary>Hạt giống sẽ gieo trên ruộng này (null = chưa chọn hạt nào hợp loại ruộng).</summary>
        public CropData CropFor(FarmPlot plot)
        {
            if (plot == null) return null;
            if (seedByField.TryGetValue(plot.FieldType, out CropData chosen)) return chosen;
            if (!plot.IsWetField) return null;

            // Ruộng nước / ruộng mạ chỉ trồng một thứ (lúa / mạ) → không cần chọn, có là trồng.
            CropData only = null;
            foreach (var crop in knownCrops)
            {
                if (crop == null || crop.fieldType != plot.FieldType) continue;
                if (TechManager.Instance != null && !TechManager.Instance.IsCropUnlocked(crop)) continue;
                if (only != null) return null;
                only = crop;
            }
            return only;
        }

        public CropData FindCrop(string id) =>
            string.IsNullOrEmpty(id) ? null : knownCrops.Find(c => c != null && c.id == id);

        public bool TryInteract(FarmPlot plot)
        {
            if (plot == null) return false;

            switch (plot.State)
            {
                case FarmPlotState.Wild:
                    plot.DoClearWork();
                    return true;

                case FarmPlotState.Unplowed:
                    plot.DoPlowWork();
                    return true;

                case FarmPlotState.Empty:
                    return plot.Plant(CropFor(plot));

                case FarmPlotState.ReadyToHarvest:
                    return plot.Harvest();

                case FarmPlotState.Withered:
                    plot.ClearWithered();
                    return true;

                default:
                    return false;
            }
        }
    }
}
