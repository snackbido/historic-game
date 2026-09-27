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
        [SerializeField] private SpriteRenderer cropRenderer;

        private CropData crop;
        private CropStage stage;
        private float stageTimer;

        public FarmPlotState State { get; private set; } = FarmPlotState.Empty;
        public CropData Crop => crop;
        public CropStage Stage => stage;
        public float StageTimer => stageTimer;

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

            string summary = string.Join(", ", crop.harvestYield.ConvertAll(y => $"+{y.amount} {y.type.displayName}"));
            EventBus.RaiseNotification($"Thu hoạch {crop.displayName}: {summary}");

            Reset();
            return true;
        }

        public void ClearWithered()
        {
            if (State != FarmPlotState.Withered) return;
            Reset();
        }

        public void LoadState(CropData loadedCrop, CropStage loadedStage, float loadedStageTimer, FarmPlotState loadedState)
        {
            crop = loadedCrop;
            stage = loadedStage;
            stageTimer = loadedStageTimer;
            State = loadedState;
            UpdateVisual();
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
            if (cropRenderer == null) return;

            if (crop == null)
            {
                cropRenderer.sprite = null;
                return;
            }

            cropRenderer.sprite = stage switch
            {
                CropStage.Seed => crop.seedSprite,
                CropStage.Sprouting => crop.sproutSprite,
                CropStage.Mature => crop.matureSprite,
                CropStage.Withered => crop.witheredSprite,
                _ => null
            };
        }
    }
}
