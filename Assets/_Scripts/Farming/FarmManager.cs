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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SelectCrop(CropData crop) => SelectedCrop = crop;

        public CropData FindCrop(string id) =>
            string.IsNullOrEmpty(id) ? null : knownCrops.Find(c => c != null && c.id == id);

        public bool TryInteract(FarmPlot plot)
        {
            if (plot == null) return false;

            switch (plot.State)
            {
                case FarmPlotState.Empty:
                    return SelectedCrop != null && plot.Plant(SelectedCrop);

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
