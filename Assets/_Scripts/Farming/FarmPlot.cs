using UnityEngine;

namespace PrehistoricTribe
{
    public enum FarmPlotState
    {
        Empty,
        Growing,
        ReadyToHarvest,
        Withered,
        /// <summary>Ruộng mới xây: còn là đất hoang, phải khai hoang/đắp bờ trước khi trồng (Milestone 5d).</summary>
        Wild
    }

    public enum CropStage
    {
        Seed,
        Sprouting,
        Mature,
        Withered
    }

    [System.Serializable]
    public class FarmPlotSaveData
    {
        public string plotName;
        public string cropId;
        public CropStage stage;
        public float stageTimer;
        public FarmPlotState state;
        public int clearWorkDone;
    }

    public class FarmPlot : MonoBehaviour
    {
        [Tooltip("Điểm gắn model cây trồng (thường là mặt trên của ô đất)")]
        [SerializeField] private Transform cropAnchor;

        [Header("Ruộng (Milestone 5d)")]
        [SerializeField] private FieldType fieldType = FieldType.Dry;
        [Tooltip("Ruộng mới xây bắt đầu là đất hoang (ô vườn có sẵn trong scene thì không)")]
        [SerializeField] private bool startsWild;
        [Tooltip("Số lượt công (mỗi lượt ~1s của một nông dân) để khai hoang/đắp bờ")]
        [SerializeField] private int clearWorkNeeded = 8;
        [Tooltip("Hiện khi còn là đất hoang")]
        [SerializeField] private GameObject wildVisual;
        [Tooltip("Hiện khi đã khai hoang (đất đã làm, bờ ruộng)")]
        [SerializeField] private GameObject preparedVisual;

        private GameObject cropVisual;
        private CropData crop;
        private CropStage stage;
        private float stageTimer;
        private int clearWorkDone;

        public FarmPlotState State { get; private set; } = FarmPlotState.Empty;
        public CropData Crop => crop;
        public FieldType FieldType => fieldType;
        public string FieldName => fieldType == FieldType.Paddy ? "ruộng nước" : "ruộng cạn";

        /// <summary>0..1 tiến độ khai hoang.</summary>
        public float ClearProgress => State == FarmPlotState.Wild ? (float)clearWorkDone / Mathf.Max(1, clearWorkNeeded) : 1f;

        /// <summary>Cây này trồng được trên ruộng này không (lúa cần ruộng nước).</summary>
        public bool Accepts(CropData data) => data != null && data.fieldType == fieldType;

        private void Awake()
        {
            if (startsWild) State = FarmPlotState.Wild;
            UpdateSoilVisual();
        }

        /// <summary>0..1 từ lúc gieo tới lúc chín — dùng cho gợi ý trên UI.</summary>
        public float GrowthProgress
        {
            get
            {
                if (crop == null) return 0f;
                float total = crop.timeToSprout + crop.timeToMature;
                if (total <= 0f) return 1f;
                return stage switch
                {
                    CropStage.Seed => stageTimer / total,
                    CropStage.Sprouting => (crop.timeToSprout + stageTimer) / total,
                    _ => 1f
                };
            }
        }

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            // Cây chỉ lớn khi có nắng (Milestone 5c) — ban đêm đứng yên, kể cả không héo thêm.
            if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight) return;

            if (State != FarmPlotState.Growing && !(State == FarmPlotState.ReadyToHarvest && crop.witherTime > 0f))
                return;

            stageTimer += Time.deltaTime;
            AdvanceStage();
        }

        public bool Plant(CropData data)
        {
            if (State != FarmPlotState.Empty || !Accepts(data)) return false;

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

        /// <summary>Một lượt công khai hoang. Trả về true khi lượt này làm xong (đất sẵn sàng để trồng).</summary>
        public bool DoClearWork()
        {
            if (State != FarmPlotState.Wild) return false;
            clearWorkDone++;
            if (clearWorkDone < clearWorkNeeded) return false;

            clearWorkDone = 0;
            State = FarmPlotState.Empty;
            UpdateSoilVisual();
            EventBus.RaiseNotification($"Đã khai hoang xong {FieldName}");
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
            UpdateSoilVisual();
        }

        private void UpdateSoilVisual()
        {
            bool wild = State == FarmPlotState.Wild;
            if (wildVisual != null) wildVisual.SetActive(wild);
            if (preparedVisual != null) preparedVisual.SetActive(!wild);
        }

        public FarmPlotSaveData GetSaveData() => new FarmPlotSaveData
        {
            plotName = name,
            cropId = crop != null ? crop.id : null,
            stage = stage,
            stageTimer = stageTimer,
            state = State,
            clearWorkDone = clearWorkDone
        };

        public void LoadFromSaveData(FarmPlotSaveData data, CropData cropData)
        {
            if (data != null && data.state == FarmPlotState.Wild)
            {
                Reset();
                State = FarmPlotState.Wild;
                clearWorkDone = data.clearWorkDone;
                UpdateSoilVisual();
                return;
            }

            if (data == null || cropData == null)
            {
                Reset();
                return;
            }

            crop = cropData;
            stage = data.stage;
            stageTimer = data.stageTimer;
            State = data.state;
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
