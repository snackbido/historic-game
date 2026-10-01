using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Một ô mương (Milestone 5d/F6, công nghệ Thủy lợi). Mới đặt chỉ là đường cắm cọc — nông dân đào xong mới
    /// dẫn được nước. Mương nối liền (4 hướng) với ao thì nước chảy theo, tối đa <see cref="CanalNetwork.GravityReach"/>
    /// ô tính từ ao. Ruộng nằm sát mương có nước thì tự được tưới, khỏi gánh; mương có nước cũng là nguồn nước
    /// để xây ruộng nước và để gánh.
    /// </summary>
    public class Canal : MonoBehaviour
    {
        private static readonly List<Canal> all = new List<Canal>();
        public static IReadOnlyList<Canal> All => all;

        [SerializeField] private int digWorkNeeded = 4;
        [Tooltip("Cọc + dây đánh dấu khi chưa đào")]
        [SerializeField] private GameObject markedVisual;
        [Tooltip("Lòng mương: giữa + 4 nhánh (Bắc, Đông, Nam, Tây) — nhánh chỉ hiện khi nối sang ô bên cạnh")]
        [SerializeField] private GameObject centerTrench;
        [SerializeField] private GameObject[] armTrenches = new GameObject[4];
        [Tooltip("Mặt nước trong lòng mương (giữa + 4 nhánh), hiện khi có nước chảy")]
        [SerializeField] private GameObject[] waterSurfaces = new GameObject[5];
        [SerializeField] private WaterSource waterSource;

        public int DigProgressCount { get; private set; }
        public float DigProgress => digWorkNeeded > 0 ? (float)DigProgressCount / digWorkNeeded : 1f;
        public bool IsDug => DigProgressCount >= digWorkNeeded;
        public bool IsFlowing { get; private set; }
        /// <summary>Nước do guồng nước bơm tới (đi xa hơn, tưới nhanh hơn).</summary>
        public bool IsPumped { get; private set; }
        /// <summary>Ruộng sát mương này được tưới bao nhiêu mỗi giây.</summary>
        public float IrrigationRate => !IsFlowing ? 0f : CanalNetwork.IrrigationPerSecond * (IsPumped ? CanalNetwork.PumpedIrrigationFactor : 1f);
        /// <summary>Số ô tính từ ao (1 = sát ao), 0 = không có nước.</summary>
        public int Distance { get; private set; }

        private BuildingInstance building;
        public Vector3Int Cell => building != null ? building.GridPosition : Vector3Int.zero;

        private void Awake()
        {
            building = GetComponent<BuildingInstance>();
            UpdateVisual(new bool[4]);
        }

        private void OnEnable()
        {
            all.Add(this);
            UpdateRegistration();
            EventBus.OnBuildingPlaced += HandleBuildingPlaced;
            CanalNetwork.MarkDirty();
        }

        private void OnDisable()
        {
            all.Remove(this);
            InteractableRegistry.Unregister(this);
            EventBus.OnBuildingPlaced -= HandleBuildingPlaced;
            CanalNetwork.MarkDirty();
            if (all.Count == 0) CanalNetwork.ClearIrrigation(); // không còn mương nào chạy Update để tính lại
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private static void HandleBuildingPlaced(BuildingInstance _) => CanalNetwork.MarkDirty();

        private void Update() => CanalNetwork.RecomputeIfDirty();

        /// <summary>Đào một lượt. Trả về true khi vừa đào xong.</summary>
        public bool DoDigWork()
        {
            if (IsDug) return true;
            DigProgressCount++;
            if (!IsDug)
            {
                UpdateVisual(new bool[4]);
                return false;
            }
            UpdateRegistration();
            EventBus.RaiseNotification("Đã đào xong một đoạn mương");
            CanalNetwork.Recompute();
            return true;
        }

        public void SetDigProgress(int count)
        {
            DigProgressCount = Mathf.Clamp(count, 0, digWorkNeeded);
            UpdateRegistration();
            CanalNetwork.MarkDirty();
            UpdateVisual(new bool[4]);
        }

        /// <summary>Chỉ mương chưa đào mới cần bấm E (đào xong thì nhường phím E cho ruộng bên cạnh).</summary>
        private void UpdateRegistration()
        {
            if (IsDug) InteractableRegistry.Unregister(this);
            else InteractableRegistry.Register(this);
        }

        internal void SetFlow(int distance, bool pumped, bool[] openArms)
        {
            Distance = distance;
            IsPumped = pumped;
            IsFlowing = distance > 0;
            if (waterSource != null) waterSource.enabled = IsFlowing;
            UpdateVisual(openArms);
        }

        private void UpdateVisual(bool[] openArms)
        {
            if (markedVisual != null) markedVisual.SetActive(!IsDug);
            if (centerTrench != null) centerTrench.SetActive(IsDug);
            for (int i = 0; i < armTrenches.Length; i++)
                if (armTrenches[i] != null) armTrenches[i].SetActive(IsDug && openArms[i]);
            for (int i = 0; i < waterSurfaces.Length; i++)
                if (waterSurfaces[i] != null) waterSurfaces[i].SetActive(IsDug && IsFlowing && (i == 0 || openArms[i - 1]));
        }

        public CanalSaveData GetSaveData() => new CanalSaveData { objectName = name, digWork = DigProgressCount };
    }

    [System.Serializable]
    public class CanalSaveData
    {
        public string objectName;
        public int digWork;
    }

    /// <summary>
    /// Tính dòng chảy cho cả hệ thống mương: mương đã đào sát ao là đầu nguồn, nước chảy sang mương liền kề
    /// (loang theo chiều rộng) tới tối đa <see cref="GravityReach"/> ô. Đánh dấu ruộng nằm sát mương có nước.
    /// </summary>
    public static class CanalNetwork
    {
        /// <summary>Nước tự chảy được bao nhiêu ô tính từ ao.</summary>
        public static int GravityReach = 8;
        /// <summary>Guồng nước bơm vào mương thì nước đi được bao nhiêu ô tính từ guồng.</summary>
        public static int PumpedReach = 20;
        /// <summary>Ruộng sát mương có nước được tưới bao nhiêu mỗi giây (đầy ruộng sau ~12s).</summary>
        public static float IrrigationPerSecond = 1f / 12f;
        /// <summary>Mương có guồng bơm tưới nhanh gấp mấy lần.</summary>
        public const float PumpedIrrigationFactor = 2f;
        /// <summary>Mương cách mép ao chừng này (m) thì nối được vào ao.</summary>
        public const float FeedRange = 1.2f;

        // Bắc (+y ô = +z thế giới), Đông, Nam, Tây.
        private static readonly Vector3Int[] Directions =
            { new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0) };

        private static bool dirty = true;

        public static void MarkDirty() => dirty = true;

        public static void RecomputeIfDirty()
        {
            if (dirty) Recompute();
        }

        /// <summary>Không còn mương nào: mọi ruộng thôi được tưới.</summary>
        public static void ClearIrrigation()
        {
            foreach (var plot in InteractableRegistry.All<FarmPlot>())
                if (plot != null) plot.IrrigatedBy = null;
        }

        public static void Recompute()
        {
            dirty = false;
            var byCell = new Dictionary<Vector3Int, Canal>();
            foreach (var canal in Canal.All)
                if (canal != null) byCell[canal.Cell] = canal;

            var plotsByCell = new Dictionary<Vector3Int, FarmPlot>();
            foreach (var plot in InteractableRegistry.All<FarmPlot>())
            {
                plot.IrrigatedBy = null;
                if (BuildingPlacer.Instance != null) plotsByCell[BuildingPlacer.Instance.WorldToCell(plot.transform.position)] = plot;
            }

            var wheelsByCell = new Dictionary<Vector3Int, WaterWheel>();
            foreach (var wheel in WaterWheel.All)
                if (wheel != null) wheelsByCell[wheel.Cell] = wheel;

            // Nước tự chảy từ các mương sát ao; guồng nước bơm vào mương sát guồng, đẩy đi xa hơn.
            var gravity = Spread(byCell, byCell.Where(p => p.Value.IsDug && TouchesOpenWater(p.Value)).Select(p => p.Key), GravityReach);
            var pumped = Spread(byCell, byCell.Where(p => p.Value.IsDug && Neighbours(p.Key).Any(wheelsByCell.ContainsKey)).Select(p => p.Key), PumpedReach);
            foreach (var pair in wheelsByCell)
            {
                Vector3Int[] fed = Neighbours(pair.Key).Where(c => byCell.TryGetValue(c, out var canal) && canal.IsDug).ToArray();
                pair.Value.SetTurning(fed.Length > 0, fed.Length > 0 ? fed[0] - pair.Key : default);
            }

            foreach (var pair in byCell)
            {
                Canal canal = pair.Value;
                gravity.TryGetValue(pair.Key, out int byGravity);
                bool isPumped = pumped.TryGetValue(pair.Key, out int byPump);
                int d = byGravity > 0 && (!isPumped || byGravity < byPump) ? byGravity : byPump;
                var arms = new bool[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector3Int next = pair.Key + Directions[i];
                    arms[i] = byCell.TryGetValue(next, out var other) && other.IsDug || plotsByCell.ContainsKey(next) || wheelsByCell.ContainsKey(next);
                    if (d > 0 && plotsByCell.TryGetValue(next, out var plot)) plot.IrrigatedBy = canal;
                }
                if (byGravity == 1) arms[ArmTowardOpenWater(canal)] = true; // nhánh nối ra ao
                canal.SetFlow(d, isPumped, arms);
            }
        }

        private static IEnumerable<Vector3Int> Neighbours(Vector3Int cell) => Directions.Select(d => cell + d);

        /// <summary>Loang theo chiều rộng qua các mương đã đào: ô nguồn = 1, tới tối đa <paramref name="reach"/> ô.</summary>
        private static Dictionary<Vector3Int, int> Spread(Dictionary<Vector3Int, Canal> byCell, IEnumerable<Vector3Int> sources, int reach)
        {
            var distance = new Dictionary<Vector3Int, int>();
            var queue = new Queue<Vector3Int>();
            foreach (var cell in sources)
            {
                distance[cell] = 1;
                queue.Enqueue(cell);
            }
            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                int d = distance[cell];
                if (d >= reach) continue;
                foreach (var next in Neighbours(cell))
                {
                    if (distance.ContainsKey(next) || !byCell.TryGetValue(next, out var canal) || !canal.IsDug) continue;
                    distance[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return distance;
        }

        /// <summary>Nguồn nước tự nhiên (ao) — không tính mương khác hay giếng.</summary>
        private static bool IsOpenWater(WaterSource source) => source.FeedsPaddies && source.IsNatural;

        private static bool TouchesOpenWater(Canal canal)
        {
            foreach (var source in WaterSource.All)
                if (IsOpenWater(source) && source.DistanceToEdge(canal.transform.position) <= FeedRange) return true;
            return false;
        }

        private static int ArmTowardOpenWater(Canal canal)
        {
            Vector3 position = canal.transform.position;
            WaterSource nearest = null;
            foreach (var source in WaterSource.All)
                if (IsOpenWater(source) && (nearest == null || source.DistanceToEdge(position) < nearest.DistanceToEdge(position)))
                    nearest = source;
            if (nearest == null) return 0;

            Vector3 toWater = nearest.transform.position - position;
            int best = 0;
            float bestDot = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                float dot = Directions[i].x * toWater.x + Directions[i].y * toWater.z;
                if (dot <= bestDot) continue;
                bestDot = dot;
                best = i;
            }
            return best;
        }
    }
}
