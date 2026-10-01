using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Quản lý dân làng: tra nghề theo id, lưu/tải, và dân số (quyết định M5 + M5b/E2, 2026-09-30):
    /// cặp nam + nữ trưởng thành cố định; mỗi cặp được một lều làm nhà; chỉ sinh con tại lều khi lều còn
    /// chỗ cho con (theo cấp lều) và đủ thức ăn; em bé lớn dần thành người lớn rồi ra ở riêng.
    /// </summary>
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance { get; private set; }

        /// <summary>Tắt để test cũ không bị em bé mới sinh làm lệch số người.</summary>
        public static bool BirthsEnabled { get; set; } = true;

        /// <summary>Tắt để test cũ không bị dân làng ăn mất thức ăn làm lệch số; test E5 tự bật lại.</summary>
        public static bool HungerEnabled { get; set; } = true;

        [SerializeField] private NpcController npcPrefab;
        [Tooltip("Mọi nghề trong game — dùng để tra ProfessionData theo id khi tải game")]
        [SerializeField] private List<ProfessionData> knownProfessions = new List<ProfessionData>();

        [Header("Dân số")]
        [SerializeField] private float birthCheckInterval = 40f;
        [Tooltip("Một cặp ở lều cấp 1 phải chờ bao lâu (giây) giữa hai lần sinh; mỗi cấp lều rút ngắn 10%")]
        [SerializeField] private float coupleBirthCooldown = 180f;
        [SerializeField] private List<ResourceAmount> birthCost = new List<ResourceAmount>();
        [Tooltip("Nghề mặc định khi trẻ trưởng thành")]
        [SerializeField] private ProfessionData adultProfession;
        [SerializeField] private float babyDuration = 120f;
        [SerializeField] private float childDuration = 180f;
        [SerializeField] private List<string> maleNames = new List<string>();
        [SerializeField] private List<string> femaleNames = new List<string>();

        [Header("Ăn uống (E5)")]
        [Tooltip("Một bữa tốn bao nhiêu (dùng loại gộp Thức ăn → loại nào cũng được, đồ dễ hỏng trước)")]
        [SerializeField] private List<ResourceAmount> mealCost = new List<ResourceAmount>();
        [Tooltip("Người lớn mất bao nhiêu độ no mỗi giây (0,67 ≈ 1 bữa / phút)")]
        [SerializeField] private float fullnessDecayPerSecond = 40f / 60f;
        [Tooltip("Trẻ con ăn ít hơn: hệ số nhân tốc độ đói")]
        [SerializeField] private float childAppetite = 0.5f;
        [Tooltip("Độ no xuống dưới mức này thì ăn một bữa")]
        [SerializeField] private float eatBelow = 60f;
        [SerializeField] private float fullnessPerMeal = 40f;
        [Tooltip("Mất bao nhiêu máu mỗi giây khi đang đói (độ no = 0)")]
        [SerializeField] private float starvingDamagePerSecond = 1f / 3f;
        [SerializeField] private float starvingNoticeInterval = 20f;

        private float birthTimer;
        private float lastStarvingNotice = float.NegativeInfinity;
        private readonly Dictionary<NpcController, float> starvingDamage = new Dictionary<NpcController, float>();

        public IReadOnlyList<ProfessionData> KnownProfessions => knownProfessions;
        public ProfessionData AdultProfession => adultProfession;
        public float BabyDuration => babyDuration;
        public float ChildDuration => childDuration;
        public int Population => NpcController.All.Count;

        /// <summary>Số cặp đôi (người lớn) và số cặp đã có lều.</summary>
        public (int couples, int housed) CoupleStats()
        {
            int couples = 0, housed = 0;
            foreach (var npc in NpcController.All)
            {
                if (npc.Gender != Gender.Female || !npc.IsAdult || npc.Partner == null) continue;
                couples++;
                if (npc.Home != null) housed++;
            }
            return (couples, housed);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            birthTimer = birthCheckInterval;
        }

        private void OnEnable() => EventBus.OnBuildingPlaced += HandleBuildingPlaced;
        private void OnDisable() => EventBus.OnBuildingPlaced -= HandleBuildingPlaced;

        private void Start()
        {
            PairCouples();
            AssignHomes();
        }

        // Vừa xây lều → gán ngay cho cặp chưa có nhà (không phải chờ lượt kiểm tra).
        private void HandleBuildingPlaced(BuildingInstance _) => AssignHomes();

        private void Update()
        {
            if (HungerEnabled) UpdateHunger(Time.deltaTime);

            birthTimer -= Time.deltaTime;
            if (birthTimer > 0f) return;
            birthTimer = birthCheckInterval;

            PairCouples();
            AssignHomes();
            if (BirthsEnabled) TryStartBirth();
        }

        public ProfessionData FindProfession(string id) =>
            string.IsNullOrEmpty(id) ? null : knownProfessions.Find(p => p != null && p.id == id);

        // ─── Gia đình ────────────────────────────────────────────────────────
        /// <summary>Ghép từng người lớn nam chưa có cặp với một người lớn nữ chưa có cặp.</summary>
        public void PairCouples()
        {
            foreach (var man in NpcController.All)
            {
                if (man.Gender != Gender.Male || !man.IsAdult || man.Partner != null) continue;
                foreach (var woman in NpcController.All)
                {
                    if (woman.Gender != Gender.Female || !woman.IsAdult || woman.Partner != null) continue;
                    man.SetPartner(woman);
                    break;
                }
            }
        }

        // ─── Ăn uống (E5) ────────────────────────────────────────────────────
        /// <summary>Số người đang đói (độ no = 0).</summary>
        public int StarvingCount
        {
            get
            {
                int count = 0;
                foreach (var npc in NpcController.All) if (npc.IsStarving) count++;
                return count;
            }
        }

        /// <summary>
        /// Độ no giảm dần; dưới ngưỡng thì ăn một bữa từ kho chung. Hết thức ăn thì đói:
        /// đi chậm (NpcController), không sinh con (BirthBlocker), mất máu dần.
        /// </summary>
        public void UpdateHunger(float deltaTime)
        {
            int starving = 0;
            foreach (var npc in new List<NpcController>(NpcController.All))
            {
                float appetite = npc.IsAdult ? 1f : childAppetite;
                npc.Fullness = Mathf.Max(0f, npc.Fullness - fullnessDecayPerSecond * appetite * deltaTime);

                if (npc.Fullness < eatBelow && ResourceManager.Instance.SpendAll(mealCost))
                    npc.Fullness = Mathf.Min(NpcController.MaxFullness, npc.Fullness + fullnessPerMeal);

                if (!npc.IsStarving) continue;
                starving++;

                // Gom sát thương lẻ theo frame rồi trừ theo số nguyên cho gọn (đói lâu thì chết).
                starvingDamage.TryGetValue(npc, out float pending);
                pending += starvingDamagePerSecond * deltaTime;
                if (pending >= 1f)
                {
                    npc.Health.TakeDamage(Mathf.Floor(pending));
                    pending -= Mathf.Floor(pending);
                }
                starvingDamage[npc] = pending;
            }

            if (starving > 0 && Time.time - lastStarvingNotice >= starvingNoticeInterval)
            {
                lastStarvingNotice = Time.time;
                EventBus.RaiseNotification($"Làng đang thiếu ăn! {starving} người đói — cần thêm lương thực");
            }
        }

        // ─── Nhà (lều) ───────────────────────────────────────────────────────
        /// <summary>Mỗi cặp chưa có nhà được một lều chưa có chủ (lều = công trình có chỗ cho con).</summary>
        public void AssignHomes()
        {
            foreach (var mother in NpcController.All)
            {
                NpcController father = mother.Partner;
                if (mother.Gender != Gender.Female || !mother.IsAdult || father == null) continue;

                // Một người đã có nhà (vd tái hôn) → người kia về ở cùng.
                BuildingInstance home = mother.Home != null ? mother.Home : father.Home;
                if (home == null) home = FindFreeHut();
                if (home == null) continue;
                mother.Home = home;
                father.Home = home;
            }
        }

        private static BuildingInstance FindFreeHut()
        {
            foreach (var building in BuildingInstance.All)
            {
                if (building.Housing <= 0) continue;
                bool owned = false;
                foreach (var npc in NpcController.All)
                    if (npc.IsAdult && npc.Home == building) { owned = true; break; }
                if (!owned) return building;
            }
            return null;
        }

        /// <summary>Số con nhỏ (chưa trưởng thành) đang ở lều này.</summary>
        public static int ChildrenIn(BuildingInstance hut)
        {
            int count = 0;
            foreach (var npc in NpcController.All)
                if (!npc.IsAdult && npc.Home == hut) count++;
            return count;
        }

        /// <summary>Thời gian nghỉ giữa hai lần sinh: lều cấp càng cao càng ngắn (mỗi cấp -10%).</summary>
        public float BirthCooldownFor(BuildingInstance hut) =>
            coupleBirthCooldown * (1f - 0.1f * ((hut != null ? hut.Level : 1) - 1));

        /// <summary>Lý do cặp đôi này chưa sinh con được (null = sinh được).</summary>
        public string BirthBlocker(NpcController mother)
        {
            NpcController father = mother.Partner;
            if (mother.Gender != Gender.Female || !mother.IsAdult || father == null || !father.IsAdult) return "Chưa có cặp";
            if (mother.Home == null) return "Chưa có lều";
            if (mother.IsStarving || father.IsStarving) return "Đang đói";
            if (ChildrenIn(mother.Home) >= mother.Home.Housing) return "Lều đã đủ con — nâng cấp lều để có thêm chỗ";
            if (Time.time - mother.LastBirthTime < BirthCooldownFor(mother.Home)) return "Vừa mới sinh";
            if (!ResourceManager.Instance.CanAfford(birthCost)) return "Thiếu thức ăn";
            return null;
        }

        private static bool IsFree(NpcController npc) =>
            !npc.IsSelected && !npc.IsHoldingPosition &&
            !(npc.CurrentJob is AttackJob) && !(npc.CurrentJob is HomeVisitJob);

        /// <summary>
        /// Chọn một cặp đủ điều kiện và đang rảnh → cả hai về lều (em bé ra đời khi cả hai ở nhà đủ lâu).
        /// Trả về true nếu đã có cặp lên đường.
        /// </summary>
        public bool TryStartBirth()
        {
            foreach (var mother in NpcController.All)
            {
                if (BirthBlocker(mother) != null) continue;
                NpcController father = mother.Partner;
                if (!IsFree(mother) || !IsFree(father)) continue;

                mother.AssignJob(new HomeVisitJob(mother.Home), fromPlayer: false);
                father.AssignJob(new HomeVisitJob(mother.Home), fromPlayer: false);
                return true;
            }
            return false;
        }

        /// <summary>Gọi khi cặp đôi đã ở lều đủ lâu: trừ thức ăn và em bé ra đời trước cửa lều.</summary>
        public NpcController CompleteBirth(NpcController mother, NpcController father, BuildingInstance hut)
        {
            if (BirthBlocker(mother) != null || !ResourceManager.Instance.SpendAll(birthCost)) return null;
            return SpawnBaby(mother, father, hut);
        }

        public NpcController SpawnBaby(NpcController mother, NpcController father, BuildingInstance home = null)
        {
            Gender gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
            string babyName = UniqueName(gender);

            // Ra đời trước cửa lều (cửa quay về phía camera = -Z); không có lều thì cạnh người mẹ.
            Vector3 position = home != null
                ? home.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, -0.9f)
                : mother.transform.position + new Vector3(Random.Range(-0.7f, 0.7f), 0f, -0.7f);
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas)) position = hit.position;

            NpcController baby = Instantiate(npcPrefab, position, Quaternion.identity);
            baby.InitializeAsBaby(babyName, gender);
            baby.Home = home;
            mother.LastBirthTime = Time.time;

            string kind = gender == Gender.Male ? "bé trai" : "bé gái";
            EventBus.RaiseNotification($"{father.NpcName} và {mother.NpcName} có con: {kind} {babyName}!");
            return baby;
        }

        /// <summary>Dòng mô tả gia đình ở lều cho bảng thông tin công trình.</summary>
        public static string DescribeFamily(BuildingInstance hut)
        {
            if (hut == null || hut.Housing <= 0) return null;
            foreach (var npc in NpcController.All)
            {
                if (npc.Gender != Gender.Female || !npc.IsAdult || npc.Home != hut) continue;
                string father = npc.Partner != null ? npc.Partner.NpcName : "?";
                return $"Gia đình: {father} & {npc.NpcName} — {ChildrenIn(hut)}/{hut.Housing} con";
            }
            return "Lều trống — chờ một cặp đôi dọn vào";
        }

        private string UniqueName(Gender gender)
        {
            var used = new HashSet<string>();
            foreach (var npc in NpcController.All) used.Add(npc.NpcName);

            List<string> pool = gender == Gender.Male ? maleNames : femaleNames;
            foreach (string candidate in pool)
                if (!used.Contains(candidate)) return candidate;

            // Hết tên trong danh sách → thêm số thứ tự.
            string baseName = pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : (gender == Gender.Male ? "Bé trai" : "Bé gái");
            for (int i = 2; ; i++)
                if (!used.Contains($"{baseName} {i}")) return $"{baseName} {i}";
        }

        // ─── Lưu / tải ───────────────────────────────────────────────────────
        public List<NpcSaveData> GetSaveData()
        {
            var data = new List<NpcSaveData>();
            foreach (var npc in NpcController.All) data.Add(npc.GetSaveData());
            return data;
        }

        public void LoadFromSaveData(List<NpcSaveData> saved)
        {
            // Dân số thay đổi trong lúc chơi (sinh con, chết) → xóa hết rồi tạo lại theo danh sách đã lưu.
            foreach (var npc in new List<NpcController>(NpcController.All))
            {
                npc.gameObject.SetActive(false); // gỡ khỏi danh sách ngay, Destroy chỉ chạy cuối frame
                Destroy(npc.gameObject);
            }

            if (saved == null || npcPrefab == null) return;
            var loaded = new List<NpcController>();
            foreach (var entry in saved)
            {
                var position = new Vector3(entry.x, 0f, entry.z);
                var rotation = Quaternion.Euler(0f, entry.rotationY, 0f);
                NpcController npc = Instantiate(npcPrefab, position, rotation);
                npc.LoadFromSaveData(entry, FindProfession(entry.professionId));
                loaded.Add(npc);
            }

            // Nối lại các cặp đôi theo tên (phải đợi tạo đủ mọi người).
            foreach (var npc in loaded)
            {
                if (string.IsNullOrEmpty(npc.PendingPartnerName) || npc.Partner != null) continue;
                NpcController partner = loaded.Find(n => n.NpcName == npc.PendingPartnerName);
                if (partner != null) npc.SetPartner(partner);
            }

            // Nối lại nhà theo ô grid (công trình được tải trước NPC trong GameManager).
            foreach (var npc in loaded)
            {
                if (npc.PendingHomeCell == null) continue;
                Vector3Int cell = npc.PendingHomeCell.Value;
                foreach (var building in BuildingInstance.All)
                {
                    if (building.GridPosition.x != cell.x || building.GridPosition.y != cell.y) continue;
                    npc.Home = building;
                    break;
                }
            }
        }
    }
}
