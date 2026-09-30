using UnityEngine;

namespace PrehistoricTribe
{
    public enum FarmPlotState
    {
        Empty,
        Growing,
        ReadyToHarvest,
        Withered
    }

    public enum CropStage
    {
        Seed,
        Sprouting,
        Mature,
        Withered
    }

    public class FarmPlot : MonoBehaviour
    {
        [Tooltip("Điểm gắn model cây trồng (thường là mặt trên của ô đất)")]
        [SerializeField] private Transform cropAnchor;

        private GameObject cropVisual;
        private CropData crop;
        private CropStage stage;
        private float stageTimer;

        public FarmPlotState State { get; private set; } = FarmPlotState.Empty;
        public CropData Crop => crop;

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            if (State != FarmPlotState.Growing && !(State == FarmPlotState.ReadyToHarvest && crop.witherTime > 0f))
                return;

            stageTimer += Time.deltaTime;
            AdvanceStage();
        }

        public bool Plant(CropData data)
        {
            if (State != FarmPlotState.Empty || data == null) return false;

            crop = data;
            stage = CropStage.Seed;
            stageTimer = 0f;
            State = FarmPlotState.Growing;
            UpdateVisual();
            return true;
        }

        public bool Harvest()
        {
            if (State != FarmPlotState.ReadyToHarvest) return false;

            foreach (var yield in crop.harvestYield)
                ResourceManager.Instance.AddResource(yield.type, yield.amount);

            Reset();
            return true;
        }

        public void ClearWithered()
        {
            if (State != FarmPlotState.Withered) return;
            Reset();
        }

        private void AdvanceStage()
        {
            switch (stage)
            {
                case CropStage.Seed:
                    if (stageTimer >= crop.timeToSprout)
                        SetStage(CropStage.Sprouting);
                    break;

                case CropStage.Sprouting:
                    if (stageTimer >= crop.timeToMature)
                    {
                        SetStage(CropStage.Mature);
                        State = FarmPlotState.ReadyToHarvest;
                    }
                    break;

                case CropStage.Mature:
                    if (crop.witherTime > 0f && stageTimer >= crop.witherTime)
                    {
                        SetStage(CropStage.Withered);
                        State = FarmPlotState.Withered;
                    }
                    break;
            }
        }

        private void SetStage(CropStage newStage)
        {
            stage = newStage;
            stageTimer = 0f;
            UpdateVisual();
        }

        private void Reset()
        {
            crop = null;
            stage = CropStage.Seed;
            stageTimer = 0f;
            State = FarmPlotState.Empty;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (cropVisual != null) Destroy(cropVisual);
            cropVisual = null;

            if (crop == null) return;

            GameObject model = stage switch
            {
                CropStage.Seed => crop.seedModel,
                CropStage.Sprouting => crop.sproutModel,
                CropStage.Mature => crop.matureModel,
                CropStage.Withered => crop.witheredModel,
                _ => null
            };
            if (model == null) return;

            Transform parent = cropAnchor != null ? cropAnchor : transform;
            cropVisual = Instantiate(model, parent.position, parent.rotation, parent);
        }
    }
}
