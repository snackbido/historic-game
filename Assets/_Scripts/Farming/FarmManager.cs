using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct FarmPlotSaveEntry
    {
        public string plotName;
        public string cropId;
        public int stage;
        public float stageTimer;
        public int state;
    }

    public class FarmManager : MonoBehaviour
    {
        public static FarmManager Instance { get; private set; }

        [SerializeField] private List<CropData> knownCrops = new List<CropData>();

        private readonly Dictionary<string, CropData> cropsById = new Dictionary<string, CropData>();

        public CropData SelectedCrop { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var crop in knownCrops)
                if (crop != null) cropsById[crop.id] = crop;
        }

        public void SelectCrop(CropData crop) => SelectedCrop = crop;

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

        public List<FarmPlotSaveEntry> GetSaveData()
        {
            var data = new List<FarmPlotSaveEntry>();
            foreach (var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude))
            {
                data.Add(new FarmPlotSaveEntry
                {
                    plotName = plot.name,
                    cropId = plot.Crop != null ? plot.Crop.id : string.Empty,
                    stage = (int)plot.Stage,
                    stageTimer = plot.StageTimer,
                    state = (int)plot.State
                });
            }
            return data;
        }

        public void LoadFromSaveData(List<FarmPlotSaveEntry> data)
        {
            if (data == null) return;

            var plotsByName = new Dictionary<string, FarmPlot>();
            foreach (var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude))
                plotsByName[plot.name] = plot;

            foreach (var entry in data)
            {
                if (!plotsByName.TryGetValue(entry.plotName, out var plot)) continue;

                CropData crop = null;
                if (!string.IsNullOrEmpty(entry.cropId))
                    cropsById.TryGetValue(entry.cropId, out crop);

                plot.LoadState(crop, (CropStage)entry.stage, entry.stageTimer, (FarmPlotState)entry.state);
            }
        }
    }
}
