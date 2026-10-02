using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Cối giã (Milestone 5d/F5): lúa gặt về là bó lúa còn ướt → treo lên giàn phơi nắng → bó khô thì tuốt
    /// lấy thóc → giã thóc trong cối thành gạo ăn được. Luôn chừa lại một ít thóc làm giống cho vụ sau.
    /// Mỗi lượt công (<see cref="DoWork"/>) làm một việc: tuốt bó đã khô → treo bó mới → giã thóc.
    /// </summary>
    public class RiceMortar : MonoBehaviour
    {
        [SerializeField] private ResourceTypeData sheaf;
        [SerializeField] private ResourceTypeData grain;
        [SerializeField] private ResourceTypeData rice;
        [Tooltip("Số giây nắng để phơi khô một bó lúa (ban đêm không khô)")]
        [SerializeField] private float dryTime = 30f;
        [Tooltip("Thóc thu được khi tuốt một bó lúa khô")]
        [SerializeField] private int grainPerSheaf = 2;
        [Tooltip("Thóc giã một mẻ (cũng là số gạo thu được)")]
        [SerializeField] private int grainPerBatch = 2;
        [Tooltip("Số lượt giã cho một mẻ")]
        [SerializeField] private int poundWorkNeeded = 2;
        [Tooltip("Thóc luôn chừa lại làm giống — chỉ giã phần dư")]
        [SerializeField] private int seedReserve = 4;
        [Tooltip("Hình bó lúa trên giàn, mỗi chỗ treo một bó — số phần tử = sức chứa giàn")]
        [SerializeField] private List<Renderer> rackSheaves = new List<Renderer>();
        [SerializeField] private Material wetSheafMaterial;
        [SerializeField] private Material drySheafMaterial;
        [Tooltip("Đống thóc trong lòng cối, hiện khi đang giã dở")]
        [SerializeField] private GameObject grainInMortar;

        /// <summary>Thời gian đã phơi của từng chỗ trên giàn; âm = chỗ trống.</summary>
        private float[] drying;

        public static bool DryingEnabled { get; set; } = true;

        public int RackCapacity => rackSheaves.Count;
        public float DryTime => dryTime;
        public int SeedReserve => seedReserve;
        public int PoundProgress { get; private set; }
        public float PoundFraction => poundWorkNeeded > 0 ? (float)PoundProgress / poundWorkNeeded : 0f;
        public ResourceTypeData Sheaf => sheaf;
        public ResourceTypeData Grain => grain;

        private float[] Drying
        {
            get
            {
                if (drying == null || drying.Length != RackCapacity)
                {
                    drying = new float[RackCapacity];
                    for (int i = 0; i < drying.Length; i++) drying[i] = -1f;
                }
                return drying;
            }
        }

        public int SheavesOnRack => Count(t => t >= 0f);
        public int DrySheaves => Count(t => t >= dryTime);
        public int FreeRackSlots => RackCapacity - SheavesOnRack;

        private static ResourceManager Rm => ResourceManager.Instance;
        private int Stock(ResourceTypeData type) => Rm != null && type != null ? Rm.GetAmount(type) : 0;

        public bool CanThresh => DrySheaves > 0;
        public bool CanHang => FreeRackSlots > 0 && Stock(sheaf) > 0;
        public bool CanPound => PoundProgress > 0 || Stock(grain) >= seedReserve + grainPerBatch;
        /// <summary>Có việc cho người làm ngay bây giờ (phơi thì chỉ chờ nắng, không cần người).</summary>
        public bool HasWork => CanThresh || CanHang || CanPound;

        /// <summary>Việc mà lượt công kế tiếp sẽ làm (cho dòng gợi ý), null = chưa có việc.</summary>
        public string NextTask => CanThresh ? "Tuốt lúa" : CanHang ? "Phơi lúa" : CanPound ? "Giã gạo" : null;

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Start() => UpdateVisual();

        private void Update()
        {
            bool sunny = DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight;
            if (!DryingEnabled || !sunny) return;

            bool changed = false;
            float[] slots = Drying;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] < 0f || slots[i] >= dryTime) continue;
                slots[i] = Mathf.Min(dryTime, slots[i] + Time.deltaTime);
                if (slots[i] >= dryTime) changed = true;
            }
            if (changed) UpdateVisual();
        }

        /// <summary>Làm một lượt công. Trả về false nếu không có gì để làm.</summary>
        public bool DoWork()
        {
            bool worked = CanThresh ? Thresh() : CanHang ? Hang() : CanPound && Pound();
            if (worked) VfxManager.Play(VfxKind.Dust, transform.position + Vector3.up * 0.4f);
            return worked;
        }

        /// <summary>Lý do chưa làm được gì (cho người chơi bấm E), null = có việc.</summary>
        public string IdleReason()
        {
            if (HasWork) return null;
            if (SheavesOnRack > 0) return $"Lúa đang phơi trên giàn ({SheavesOnRack} bó) — chờ khô";
            if (Stock(sheaf) == 0 && Stock(grain) < seedReserve + grainPerBatch)
                return $"Chưa có {Name(sheaf)} để phơi, thóc thì phải chừa {seedReserve} làm giống";
            return "Chưa có việc ở cối giã";
        }

        private bool Thresh()
        {
            float[] slots = Drying;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] < dryTime) continue;
                slots[i] = -1f;
                Rm.AddResource(grain, grainPerSheaf);
                UpdateVisual();
                return true;
            }
            return false;
        }

        private bool Hang()
        {
            float[] slots = Drying;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] >= 0f) continue;
                if (!Rm.TrySpend(sheaf, 1)) return false;
                slots[i] = 0f;
                UpdateVisual();
                return true;
            }
            return false;
        }

        private bool Pound()
        {
            // Mẻ mới: đổ thóc vào cối (trừ ngay để không bị đem gieo mất giữa chừng).
            if (PoundProgress == 0 && !Rm.TrySpend(grain, grainPerBatch)) return false;
            PoundProgress++;
            if (PoundProgress >= poundWorkNeeded)
            {
                PoundProgress = 0;
                Rm.AddResource(rice, grainPerBatch);
            }
            UpdateVisual();
            return true;
        }

        private int Count(System.Func<float, bool> predicate)
        {
            int n = 0;
            foreach (float t in Drying)
                if (predicate(t)) n++;
            return n;
        }

        private static string Name(ResourceTypeData type) => type != null ? type.displayName.ToLowerInvariant() : "?";

        private void UpdateVisual()
        {
            float[] slots = Drying;
            for (int i = 0; i < rackSheaves.Count; i++)
            {
                Renderer r = rackSheaves[i];
                if (r == null) continue;
                r.gameObject.SetActive(slots[i] >= 0f);
                Material m = slots[i] >= dryTime ? drySheafMaterial : wetSheafMaterial;
                if (m != null) r.sharedMaterial = m;
            }
            if (grainInMortar != null) grainInMortar.SetActive(PoundProgress > 0);
        }

        // ─── Lưu / tải (khớp theo tên công trình) ────────────────────────────
        public RiceMortarSaveData GetSaveData() =>
            new RiceMortarSaveData { objectName = name, drying = new List<float>(Drying), poundProgress = PoundProgress };

        public void LoadFromSaveData(RiceMortarSaveData data)
        {
            float[] slots = Drying;
            for (int i = 0; i < slots.Length; i++)
                slots[i] = data != null && data.drying != null && i < data.drying.Count ? data.drying[i] : -1f;
            PoundProgress = data != null ? Mathf.Clamp(data.poundProgress, 0, Mathf.Max(0, poundWorkNeeded - 1)) : 0;
            UpdateVisual();
        }
    }

    [System.Serializable]
    public class RiceMortarSaveData
    {
        public string objectName;
        public List<float> drying = new List<float>();
        public int poundProgress;
    }
}
