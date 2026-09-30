using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Quản lý dân làng: tra nghề theo id, lưu/tải, và dân số (quyết định M5, 2026-09-30):
    /// cặp nam + nữ trưởng thành cố định; còn chỗ ở + đủ thức ăn thì sinh em bé; em bé lớn dần thành người lớn.
    /// </summary>
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance { get; private set; }

        /// <summary>Tắt để test cũ không bị em bé mới sinh làm lệch số người.</summary>
        public static bool BirthsEnabled { get; set; } = true;

        [SerializeField] private NpcController npcPrefab;
        [Tooltip("Mọi nghề trong game — dùng để tra ProfessionData theo id khi tải game")]
        [SerializeField] private List<ProfessionData> knownProfessions = new List<ProfessionData>();

        [Header("Dân số")]
        [Tooltip("Chỗ ở sẵn có khi chưa xây lều (hang/đống lửa ban đầu)")]
        [SerializeField] private int baseHousing = 4;
        [SerializeField] private float birthCheckInterval = 20f;
        [Tooltip("Một cặp phải chờ bao lâu (giây) giữa hai lần sinh")]
        [SerializeField] private float coupleBirthCooldown = 90f;
        [SerializeField] private List<ResourceAmount> birthCost = new List<ResourceAmount>();
        [Tooltip("Nghề mặc định khi trẻ trưởng thành")]
        [SerializeField] private ProfessionData adultProfession;
        [SerializeField] private float babyDuration = 60f;
        [SerializeField] private float childDuration = 90f;
        [SerializeField] private List<string> maleNames = new List<string>();
        [SerializeField] private List<string> femaleNames = new List<string>();

        private float birthTimer;

        public IReadOnlyList<ProfessionData> KnownProfessions => knownProfessions;
        public ProfessionData AdultProfession => adultProfession;
        public float BabyDuration => babyDuration;
        public float ChildDuration => childDuration;
        public int Population => NpcController.All.Count;

        /// <summary>Sức chứa = chỗ ở ban đầu + chỗ ở của mọi công trình (mỗi lều +2).</summary>
        public int Capacity
        {
            get
            {
                int capacity = baseHousing;
                foreach (var building in BuildingInstance.All)
                    if (building.Data != null) capacity += building.Data.housing;
                return capacity;
            }
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

        private void Start() => PairCouples();

        private void Update()
        {
            birthTimer -= Time.deltaTime;
            if (birthTimer > 0f) return;
            birthTimer = birthCheckInterval;

            PairCouples();
            if (BirthsEnabled) TryBirth();
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

        /// <summary>Thử cho một cặp sinh con. Trả về em bé, hoặc null nếu hết chỗ ở / thiếu thức ăn / chưa cặp nào sẵn sàng.</summary>
        public NpcController TryBirth()
        {
            if (Population >= Capacity) return null;
            if (!ResourceManager.Instance.CanAfford(birthCost)) return null;

            foreach (var mother in NpcController.All)
            {
                NpcController father = mother.Partner;
                if (mother.Gender != Gender.Female || !mother.IsAdult || father == null || !father.IsAdult) continue;
                if (Time.time - mother.LastBirthTime < coupleBirthCooldown) continue;

                ResourceManager.Instance.SpendAll(birthCost);
                return SpawnBaby(mother, father);
            }
            return null;
        }

        public NpcController SpawnBaby(NpcController mother, NpcController father)
        {
            Gender gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
            string babyName = UniqueName(gender);

            Vector2 offset = Random.insideUnitCircle.normalized * 0.7f;
            Vector3 position = mother.transform.position + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas)) position = hit.position;

            NpcController baby = Instantiate(npcPrefab, position, Quaternion.identity);
            baby.InitializeAsBaby(babyName, gender);
            mother.LastBirthTime = Time.time;

            string kind = gender == Gender.Male ? "bé trai" : "bé gái";
            EventBus.RaiseNotification($"{father.NpcName} và {mother.NpcName} có con: {kind} {babyName}!");
            return baby;
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
        }
    }
}
