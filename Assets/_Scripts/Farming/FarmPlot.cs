using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public enum FarmPlotState
    {
        /// <summary>Đất đã cày/xới, sẵn sàng gieo.</summary>
        Empty,
        Growing,
        ReadyToHarvest,
        Withered,
        /// <summary>Ruộng mới xây: còn là đất hoang, phải khai hoang/đắp bờ trước khi trồng (Milestone 5d).</summary>
        Wild,
        /// <summary>Đất đã khai hoang nhưng chưa cày/xới — phải cày trước mỗi vụ (Milestone 5d/F2).</summary>
        Unplowed
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
        public int plowWorkDone;
        // Save cũ chưa có → giữ -1 → coi như đầy nước (trước F2 ruộng không cần nước).
        public float water = -1f;
        public float droughtTimer;
        public float weeds;
        public bool fertilized;
    }

    /// <summary>
    /// Một ô ruộng. Quy trình (Milestone 5d): đất hoang → khai hoang → cày/xới → (ruộng nước: dẫn nước cho ngập)
    /// → gieo/cấy → lớn (cần đủ nước, ban đêm không lớn) → thu hoạch → lại phải cày cho vụ sau.
    /// </summary>
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
        [Tooltip("Số lượt công để cày/xới trước mỗi vụ")]
        [SerializeField] private int plowWorkNeeded = 3;

        [Header("Nước")]
        [Tooltip("Mức nước lúc bắt đầu (0..1) — ô vườn có sẵn được tưới đầy")]
        [SerializeField, Range(0f, 1f)] private float startWater;
        [Tooltip("Nước mất đi mỗi giây ban ngày (ban đêm còn 30%)")]
        [SerializeField] private float evaporationPerSecond = 1f / 150f;
        [Tooltip("Thiếu nước liên tục bao lâu (giây) thì cây chết khô")]
        [SerializeField] private float droughtWitherTime = 60f;

        [Header("Chăm sóc (F4)")]
        [Tooltip("Cỏ dại mọc thêm mỗi giây (ban ngày, khi có cây) — 1/60: một phút là cỏ phủ kín")]
        [SerializeField] private float weedGrowthPerSecond = 1f / 60f;
        [Tooltip("Cỏ dại (hiện + to dần theo mức cỏ)")]
        [SerializeField] private Transform weedsVisual;
        [Tooltip("Phân đã bón (hiện khi đã bón)")]
        [SerializeField] private GameObject fertilizerVisual;

        [Header("Hình ảnh")]
        [Tooltip("Hiện khi còn là đất hoang")]
        [SerializeField] private GameObject wildVisual;
        [Tooltip("Đất đã khai hoang nhưng chưa cày (mặt đất phẳng, cứng)")]
        [SerializeField] private GameObject unplowedVisual;
        [Tooltip("Đất đã cày/xới (luống, bùn)")]
        [SerializeField] private GameObject preparedVisual;
        [Tooltip("Mặt nước (ruộng nước) / đất ướt (ruộng cạn) — hiện theo mức nước")]
        [SerializeField] private Transform waterVisual;

        /// <summary>Tắt cỏ dại (test cũ đếm sản lượng chính xác).</summary>
        public static bool WeedsEnabled { get; set; } = true;

        /// <summary>Cỏ từ mức này trở lên thì nên làm cỏ.</summary>
        public const float WeedingNeededAt = 0.4f;
        private const float WeedRemovedPerWork = 0.5f;
        /// <summary>Cỏ phủ kín làm mất tối đa chừng này sản lượng.</summary>
        public const float MaxWeedPenalty = 0.4f;
        /// <summary>Bón phân tăng sản lượng thêm chừng này.</summary>
        public const float FertilizerBonus = 0.5f;

        /// <summary>Ruộng nước phải ngập ít nhất mức này mới cấy được lúa.</summary>
        public const float FloodedLevel = 0.6f;
        private const float NightEvaporationFactor = 0.3f;
        /// <summary>Hệ số bốc hơi toàn cục (hạn hán M6/D2 đặt 3).</summary>
        public static float EvaporationMultiplier { get; set; } = 1f;

        private GameObject cropVisual;
        private CropData crop;
        private CropStage stage;
        private float stageTimer;
        private int clearWorkDone;
        private int plowWorkDone;
        private float droughtTimer;

        public FarmPlotState State { get; private set; } = FarmPlotState.Empty;
        public CropData Crop => crop;
        public FieldType FieldType => fieldType;
        public string FieldName => fieldType switch { FieldType.Paddy => "ruộng nước", FieldType.Seedbed => "ruộng mạ", _ => "ruộng cạn" };

        /// <summary>Ruộng nước / ruộng mạ: phải giữ ngập nước.</summary>
        public bool IsWetField => fieldType != FieldType.Dry;

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

        /// <summary>Mức nước 0..1.</summary>
        public float Water { get; private set; }

        /// <summary>Mức cỏ dại 0..1 (chỉ mọc khi đang có cây).</summary>
        public float Weeds { get; private set; }

        /// <summary>Vụ này đã bón phân chưa.</summary>
        public bool Fertilized { get; private set; }

        public bool NeedsWeeding => State == FarmPlotState.Growing && Weeds >= WeedingNeededAt;
        public bool CanFertilize => State == FarmPlotState.Growing && !Fertilized;

        /// <summary>Hệ số sản lượng: bón phân +50%, cỏ phủ kín −40%.</summary>
        public float YieldMultiplier => (Fertilized ? 1f + FertilizerBonus : 1f) * (1f - MaxWeedPenalty * Weeds);

        /// <summary>0..1 tiến độ khai hoang.</summary>
        public float ClearProgress => State == FarmPlotState.Wild ? (float)clearWorkDone / Mathf.Max(1, clearWorkNeeded) : 1f;

        /// <summary>0..1 tiến độ cày/xới.</summary>
        public float PlowProgress => State == FarmPlotState.Unplowed ? (float)plowWorkDone / Mathf.Max(1, plowWorkNeeded) : 1f;

        /// <summary>Cây đang có đủ nước để lớn không.</summary>
        public bool HasEnoughWater => crop == null || Water >= crop.minWater;

        /// <summary>Mương có nước chảy sát ruộng này (F6) — tự được tưới, không cần gánh.</summary>
        public Canal IrrigatedBy { get; internal set; }
        public bool IsIrrigated => IrrigatedBy != null && IrrigatedBy.IsFlowing;

        /// <summary>Đang thiếu nước (có cây mà khô, hoặc ruộng nước chưa ngập để cấy).</summary>
        public bool IsThirsty => !IsIrrigated && State switch
        {
            FarmPlotState.Growing => Water < RefillBelow,
            FarmPlotState.Empty => IsWetField && Water < FloodedLevel,
            _ => false
        };

        /// <summary>Đang có cây mà nước xuống dưới mức này thì nên đi gánh nước thêm.</summary>
        private float RefillBelow => IsWetField ? FloodedLevel : 0.35f;

        /// <summary>Một chuyến gánh nước đổ vào được bao nhiêu (ruộng nước cần nhiều chuyến mới ngập).</summary>
        public float WaterPerTrip => fieldType switch { FieldType.Paddy => 0.25f, FieldType.Seedbed => 0.35f, _ => 0.4f };

        /// <summary>Cây này trồng được trên ruộng này không (lúa cần ruộng nước).</summary>
        public bool Accepts(CropData data) => data != null && data.fieldType == fieldType;

        /// <summary>Lý do không gieo/cấy được lúc này (null = được).</summary>
        public string PlantBlocker(CropData data)
        {
            if (data == null) return "Chưa chọn hạt giống";
            if (!Accepts(data)) return $"{data.displayName} không trồng được trên {FieldName}";
            if (State == FarmPlotState.Wild) return "Phải khai hoang trước";
            if (State == FarmPlotState.Unplowed) return "Phải cày/xới đất trước";
            if (State != FarmPlotState.Empty) return "Ruộng đang có cây";
            if (IsWetField && Water < FloodedLevel) return "Ruộng chưa ngập nước — phải dẫn nước vào trước khi cấy";
            if (!HasSeedFor(data)) return MissingSeedMessage(data);
            return null;
        }

        /// <summary>Kho có đủ giống (thóc giống / mạ) để gieo cây này không.</summary>
        public static bool HasSeedFor(CropData data) =>
            data != null && (data.plantCost.Count == 0 || ResourceManager.Instance.CanAfford(data.plantCost));

        private static string MissingSeedMessage(CropData data)
        {
            var names = new List<string>();
            foreach (var cost in data.plantCost)
                if (cost.type != null) names.Add(cost.type.displayName);
            string missing = string.Join(", ", names);
            return data.fieldType == FieldType.Paddy
                ? $"Thiếu {missing} để cấy — gieo mạ ở ruộng mạ rồi nhổ mạ"
                : $"Thiếu {missing} để gieo";
        }

        private void Awake()
        {
            if (startsWild) State = FarmPlotState.Wild;
            Water = startsWild ? 0f : startWater;
            UpdateSoilVisual();
            UpdateCareVisual();
        }

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
            Evaporate(Time.deltaTime * (night ? NightEvaporationFactor : 1f));
            if (IsIrrigated && State != FarmPlotState.Wild && Water < 1f)
                AddWater(IrrigatedBy.IrrigationRate * Time.deltaTime);

            // Cây chỉ lớn khi có nắng (Milestone 5c) — ban đêm đứng yên, kể cả không héo thêm.
            if (night) return;

            if (State == FarmPlotState.Growing && !HasEnoughWater)
            {
                // Khô hạn: không lớn; khô lâu quá thì chết.
                droughtTimer += Time.deltaTime;
                if (droughtTimer >= droughtWitherTime)
                {
                    droughtTimer = 0f;
                    SetStage(CropStage.Withered);
                    State = FarmPlotState.Withered;
                    EventBus.RaiseNotification($"{crop.displayName} chết khô vì thiếu nước!");
                }
                return;
            }
            droughtTimer = 0f;

            if (State == FarmPlotState.Growing && WeedsEnabled && Weeds < 1f)
                SetWeeds(Weeds + weedGrowthPerSecond * Time.deltaTime);

            if (State != FarmPlotState.Growing && !(State == FarmPlotState.ReadyToHarvest && crop.witherTime > 0f))
                return;

            stageTimer += Time.deltaTime;
            AdvanceStage();
        }

        private void Evaporate(float seconds)
        {
            if (Water <= 0f || State == FarmPlotState.Wild) return;
            SetWater(Water - evaporationPerSecond * EvaporationMultiplier * seconds);
        }

        public void AddWater(float amount) => SetWater(Water + amount);

        public void SetWater(float level)
        {
            Water = Mathf.Clamp01(level);
            UpdateWaterVisual();
        }

        public bool Plant(CropData data)
        {
            if (PlantBlocker(data) != null) return false;
            if (data.plantCost.Count > 0 && !ResourceManager.Instance.SpendAll(data.plantCost)) return false;

            crop = data;
            stage = CropStage.Seed;
            stageTimer = 0f;
            droughtTimer = 0f;
            State = FarmPlotState.Growing;
            UpdateVisual();
            return true;
        }

        public bool Harvest()
        {
            if (State != FarmPlotState.ReadyToHarvest) return false;

            float multiplier = YieldMultiplier;
            foreach (var yield in crop.harvestYield)
                ResourceManager.Instance.AddResource(yield.type, HarvestAmount(yield.amount, multiplier));

            ResetToUnplowed();
            return true;
        }

        /// <summary>Sản lượng sau khi tính phân bón / cỏ dại (làm tròn lên từ .5, ít nhất 1).</summary>
        public static int HarvestAmount(int baseAmount, float multiplier) =>
            Mathf.Max(1, Mathf.FloorToInt(baseAmount * multiplier + 0.5f));

        /// <summary>Một lượt làm cỏ. Trả về true khi ruộng đã sạch cỏ.</summary>
        public bool DoWeedWork()
        {
            SetWeeds(Weeds - WeedRemovedPerWork);
            return Weeds <= 0f;
        }

        public void SetWeeds(float level)
        {
            Weeds = Mathf.Clamp01(level);
            UpdateCareVisual();
        }

        /// <summary>Bón phân cho vụ đang trồng (chi phí trả ở <see cref="FarmManager.TryFertilize"/>).</summary>
        public bool ApplyFertilizer()
        {
            if (!CanFertilize) return false;
            Fertilized = true;
            UpdateCareVisual();
            return true;
        }

        /// <summary>Một lượt công khai hoang. Trả về true khi lượt này làm xong.</summary>
        public bool DoClearWork()
        {
            if (State != FarmPlotState.Wild) return false;
            clearWorkDone++;
            if (clearWorkDone < clearWorkNeeded) return false;

            clearWorkDone = 0;
            ResetToUnplowed();
            EventBus.RaiseNotification($"Đã khai hoang xong {FieldName} — giờ cần cày/xới đất");
            return true;
        }

        /// <summary>Một lượt công cày/xới. Trả về true khi lượt này làm xong (đất sẵn sàng gieo).</summary>
        public bool DoPlowWork()
        {
            if (State != FarmPlotState.Unplowed) return false;
            plowWorkDone++;
            if (plowWorkDone < plowWorkNeeded) return false;

            plowWorkDone = 0;
            State = FarmPlotState.Empty;
            UpdateSoilVisual();
            return true;
        }

        public void ClearWithered()
        {
            if (State != FarmPlotState.Withered) return;
            ResetToUnplowed();
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

        /// <summary>Thu hoạch / dọn cây xong: đất chai lại, vụ sau phải cày/xới lại.</summary>
        private void ResetToUnplowed()
        {
            ClearCrop();
            plowWorkDone = 0;
            State = FarmPlotState.Unplowed;
            UpdateSoilVisual();
        }

        private void ClearCrop()
        {
            crop = null;
            stage = CropStage.Seed;
            stageTimer = 0f;
            droughtTimer = 0f;
            Weeds = 0f; // cày lại đất là lấp luôn cỏ
            Fertilized = false;
            UpdateVisual();
            UpdateCareVisual();
        }

        private void UpdateCareVisual()
        {
            if (weedsVisual != null)
            {
                bool show = Weeds > 0.15f;
                weedsVisual.gameObject.SetActive(show);
                if (show) weedsVisual.localScale = new Vector3(1f, Mathf.Lerp(0.4f, 1f, Weeds), 1f);
            }
            if (fertilizerVisual != null) fertilizerVisual.SetActive(Fertilized);
        }

        public FarmPlotSaveData GetSaveData() => new FarmPlotSaveData
        {
            plotName = name,
            cropId = crop != null ? crop.id : null,
            stage = stage,
            stageTimer = stageTimer,
            state = State,
            clearWorkDone = clearWorkDone,
            plowWorkDone = plowWorkDone,
            water = Water,
            droughtTimer = droughtTimer,
            weeds = Weeds,
            fertilized = Fertilized
        };

        public void LoadFromSaveData(FarmPlotSaveData data, CropData cropData)
        {
            ClearCrop();
            clearWorkDone = plowWorkDone = 0;
            if (data == null)
            {
                State = FarmPlotState.Empty;
                UpdateSoilVisual();
                return;
            }

            SetWater(data.water < 0f ? 1f : data.water);
            bool hasCrop = data.state == FarmPlotState.Growing || data.state == FarmPlotState.ReadyToHarvest ||
                           data.state == FarmPlotState.Withered;
            if (hasCrop && cropData == null)
            {
                State = FarmPlotState.Unplowed; // cây không còn trong game → đất trống chưa cày
            }
            else if (hasCrop)
            {
                crop = cropData;
                stage = data.stage;
                stageTimer = data.stageTimer;
                droughtTimer = data.droughtTimer;
                State = data.state;
                Weeds = data.weeds;
                Fertilized = data.fertilized;
                UpdateVisual();
                UpdateCareVisual();
            }
            else
            {
                State = data.state;
                clearWorkDone = data.clearWorkDone;
                plowWorkDone = data.plowWorkDone;
            }
            UpdateSoilVisual();
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

        private void UpdateSoilVisual()
        {
            bool wild = State == FarmPlotState.Wild;
            bool unplowed = State == FarmPlotState.Unplowed;
            if (wildVisual != null) wildVisual.SetActive(wild);
            if (unplowedVisual != null) unplowedVisual.SetActive(unplowed);
            // Ô vườn cũ không có hình "chưa cày" → vẫn hiện luống đất.
            if (preparedVisual != null) preparedVisual.SetActive(!wild && !(unplowed && unplowedVisual != null));
            UpdateWaterVisual();
        }

        private void UpdateWaterVisual()
        {
            if (waterVisual == null) return;
            bool show = State != FarmPlotState.Wild && Water > 0.05f;
            waterVisual.gameObject.SetActive(show);
            if (!show) return;

            // Nước dâng dần trong bờ ruộng; ruộng cạn: vệt đất ướt rộng dần.
            Vector3 p = waterVisual.localPosition;
            if (IsWetField)
                waterVisual.localPosition = new Vector3(p.x, Mathf.Lerp(0.035f, 0.09f, Water), p.z);
            else
                waterVisual.localScale = new Vector3(Mathf.Lerp(0.5f, 0.94f, Water), waterVisual.localScale.y, Mathf.Lerp(0.5f, 0.94f, Water));
        }
    }
}
