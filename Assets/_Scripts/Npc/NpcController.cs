using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PrehistoricTribe
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum AgeStage
    {
        Baby,
        Child,
        Adult
    }

    public enum NpcState
    {
        Idle,
        Wandering,
        Moving,
        Working
    }

    [System.Serializable]
    public class ProfessionTool
    {
        public ProfessionData profession;
        public GameObject tool;
    }

    [System.Serializable]
    public class NpcSaveData
    {
        public string npcName;
        public string professionId;
        public Gender gender;
        public AgeStage age;
        public float x;
        public float z;
        public float rotationY;
        public float health;
        public bool holdPosition;
        public float ageSeconds;
        public string partnerName;
        // Nhà (lều) — lưu theo ô grid của công trình.
        public bool hasHome;
        public int homeCellX;
        public int homeCellY;
        // Độ no (E5). Save cũ chưa có field này → giữ giá trị mặc định -1 → khi tải coi như no đủ.
        public float fullness = -1f;
    }

    /// <summary>
    /// Dân làng: có tên, giới tính, độ tuổi, nghề nghiệp; đi lại bằng NavMesh.
    /// Khi không có lệnh thì đi dạo quanh "nhà" (điểm đến của lệnh gần nhất).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(HealthComponent))]
    public class NpcController : MonoBehaviour
    {
        private static readonly List<NpcController> all = new List<NpcController>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Tắt để test cũ không bị NPC tự làm việc chen ngang (vd tự gieo ô đất test đang dùng).</summary>
        public static bool AutoWorkEnabled { get; set; } = true;
        public static IReadOnlyList<NpcController> All => all;

        [SerializeField] private string npcName;
        [SerializeField] private Gender gender;
        [SerializeField] private AgeStage age = AgeStage.Adult;
        [SerializeField] private ProfessionData profession;

        [Header("Hiển thị")]
        [SerializeField] private Renderer tunicRenderer;
        [SerializeField] private GameObject maleHair;
        [SerializeField] private GameObject femaleHair;
        [Tooltip("Dụng cụ cầm tay theo nghề — chỉ hiện dụng cụ của nghề hiện tại")]
        [SerializeField] private List<ProfessionTool> tools = new List<ProfessionTool>();
        [Tooltip("Vòng sáng dưới chân khi đang được người chơi chọn")]
        [SerializeField] private GameObject selectionRing;

        [Header("Đi dạo khi rảnh")]
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float minIdlePause = 2f;
        [SerializeField] private float maxIdlePause = 6f;

        [Header("Tự làm việc khi rảnh")]
        [Tooltip("Tìm việc theo nghề trong bán kính này (m) quanh chỗ đang đứng")]
        [SerializeField] private float autoWorkRadius = 6f;
        [SerializeField] private float autoWorkInterval = 2f;

        [Header("Giữ vị trí sau lệnh")]
        [Tooltip("Sau khi xong lệnh và KHÔNG còn được chọn, đứng giữ vị trí trong khoảng thời gian ngẫu nhiên này (giây) rồi quay lại đi dạo")]
        [SerializeField] private float minHoldTime = 20f;
        [SerializeField] private float maxHoldTime = 30f;

        private NavMeshAgent agent;
        private HealthComponent health;
        private Vector3 home;
        private float idleTimer;

        // Đã nhận lệnh từ người chơi → đứng giữ vị trí đó thay vì đi dạo (kiểu RTS),
        // hết hạn sau holdDuration giây đứng rảnh mà không được chọn.
        private bool holdPosition;
        private float holdDuration;
        private float holdElapsed;

        private NpcJob job;
        private bool jobFromPlayer;
        private float autoWorkTimer;
        private float ageSeconds;
        private string pendingPartnerName; // nối lại cặp đôi sau khi tải game
        private float workTimer;
        private float repathTimer;
        private const float RepathInterval = 0.5f;
        private const float FleeDistance = 6f;

        public string NpcName => npcName;
        public Gender Gender => gender;
        public AgeStage Age => age;
        public ProfessionData Profession => profession;
        public HealthComponent Health => health;
        public NpcState State { get; private set; } = NpcState.Idle;
        public bool IsAdult => age == AgeStage.Adult;
        public bool IsSelected { get; private set; }
        public bool IsHoldingPosition => holdPosition;
        public Vector3 Destination => agent.destination;
        public NpcJob CurrentJob => job;
        public NpcController Partner { get; private set; }
        public float AgeSeconds => ageSeconds;
        /// <summary>Lần sinh con gần nhất (Time.time) — NpcManager dùng để giãn cách giữa các lần sinh.</summary>
        public float LastBirthTime { get; set; } = float.NegativeInfinity;
        public string PendingPartnerName => pendingPartnerName;
        /// <summary>Lều của gia đình (cặp đôi + con nhỏ). Null = chưa có nhà.</summary>
        public BuildingInstance Home { get; set; }
        /// <summary>Ô grid của lều đã lưu — NpcManager dùng để nối lại sau khi tải game.</summary>
        public Vector3Int? PendingHomeCell { get; private set; }

        // ─── Ăn uống (E5) ────────────────────────────────────────────────────
        public const float MaxFullness = 100f;
        private const float StarvingSpeedFactor = 0.6f;
        private float baseSpeed = 3f;

        /// <summary>Độ no 0–100. NpcManager giảm dần theo thời gian và cho ăn từ kho chung.</summary>
        public float Fullness { get; set; } = MaxFullness;
        /// <summary>Đói (độ no về 0): đi chậm, không sinh con, mất máu dần.</summary>
        public bool IsStarving => Fullness <= 0f;
        public bool IsHungry => Fullness < 30f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthComponent>();
            home = transform.position;
            idleTimer = Random.Range(minIdlePause, maxIdlePause);
            RefreshVisuals();
            SetSelected(false);

            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDied;
        }

        /// <summary>Bị thú dữ tấn công: người biết đánh thì đánh trả, người không biết thì bỏ chạy.</summary>
        private void HandleDamaged(HealthComponent _, GameObject source)
        {
            if (health.IsDead || source == null) return;
            var predator = source.GetComponent<PredatorAI>();
            if (predator == null) return;

            if (NpcJobFactory.CanFight(profession))
            {
                if (!(job is AttackJob attack && attack.Predator == predator))
                    AssignJob(new AttackJob(predator, profession), fromPlayer: false); // tự vệ xong thì sinh hoạt tiếp
            }
            else
            {
                FleeFrom(source.transform.position);
            }
        }

        private void FleeFrom(Vector3 danger)
        {
            Vector3 away = transform.position - danger;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f;
            MoveTo(transform.position + away.normalized * FleeDistance);
        }

        private void HandleDied(HealthComponent _)
        {
            EventBus.RaiseNotification($"{npcName} đã chết!");
            if (Partner != null) Partner.Partner = null; // người còn lại có thể ghép cặp mới
            gameObject.SetActive(false); // rời khỏi danh sách/đang chọn ngay
            Destroy(gameObject);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        // Dọn danh sách khi tắt Domain Reload (Enter Play Mode Options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private void Update()
        {
            UpdateAge();
            agent.speed = baseSpeed * (IsStarving ? StarvingSpeedFactor : 1f);
            if (!agent.isOnNavMesh) return;

            switch (State)
            {
                case NpcState.Idle:
                    if (holdPosition)
                    {
                        UpdateHold();
                        break;
                    }
                    if (TryAutoWork()) break;
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f) TryWander();
                    break;

                case NpcState.Wandering:
                    if (TryAutoWork()) break; // đang đi dạo mà có việc thì làm luôn
                    if (HasArrived()) BecomeIdle();
                    break;

                case NpcState.Moving:
                    if (HasArrived()) BecomeIdle();
                    break;

                case NpcState.Working:
                    UpdateWork();
                    break;
            }
        }

        /// <summary>Chỉ đếm giờ khi đứng rảnh và không được chọn; hết giờ thì quay lại đi dạo quanh chỗ đang đứng.</summary>
        private void UpdateHold()
        {
            if (IsSelected)
            {
                holdElapsed = 0f;
                return;
            }

            holdElapsed += Time.deltaTime;
            if (holdElapsed < holdDuration) return;

            holdPosition = false;
            home = transform.position;
            idleTimer = Random.Range(minIdlePause, maxIdlePause);
        }

        private void StartHolding()
        {
            holdPosition = true;
            holdElapsed = 0f;
            holdDuration = Random.Range(minHoldTime, maxHoldTime);
        }

        /// <summary>Rảnh (không lệnh, không bị chọn, là người lớn) thì vài giây tìm việc theo nghề một lần.</summary>
        private bool TryAutoWork()
        {
            if (!AutoWorkEnabled || !IsAdult || IsSelected || holdPosition) return false;

            autoWorkTimer -= Time.deltaTime;
            if (autoWorkTimer > 0f) return false;
            autoWorkTimer = autoWorkInterval;

            NpcJob found = NpcJobFactory.FindAutoJob(this, autoWorkRadius);
            if (found == null) return false;
            AssignJob(found, fromPlayer: false);
            return true;
        }

        /// <summary>
        /// Giao việc. Lệnh của người chơi: xong việc thì đứng giữ vị trí. Tự làm khi rảnh: xong thì đi dạo tiếp.
        /// </summary>
        public void AssignJob(NpcJob newJob, bool fromPlayer = true)
        {
            if (newJob == null || !newJob.IsValid) return;

            job = newJob;
            jobFromPlayer = fromPlayer;
            if (fromPlayer) StartHolding();
            else holdPosition = false;
            workTimer = 0f;
            repathTimer = 0f;
            State = NpcState.Working;
        }

        private void UpdateWork()
        {
            if (job == null || !job.IsValid)
            {
                EndJob();
                return;
            }

            Vector3 targetPosition = job.Target.transform.position;
            if (InteractableRegistry.GroundDistance(transform.position, targetPosition) > job.WorkRange)
            {
                // Mục tiêu có thể di chuyển (thú) → cập nhật đường đi định kỳ.
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f)
                {
                    agent.SetDestination(targetPosition);
                    repathTimer = RepathInterval;
                }
                return;
            }

            if (agent.hasPath) agent.ResetPath();
            FaceTowards(targetPosition);

            workTimer -= Time.deltaTime;
            if (workTimer > 0f) return;

            workTimer = job.Interval;
            if (!job.DoWork(this)) EndJob();
        }

        private void EndJob()
        {
            job = null;
            home = transform.position;
            holdElapsed = 0f; // đếm lại từ lúc xong việc
            if (!jobFromPlayer) autoWorkTimer = 0f; // tự làm: tìm ngay việc kế tiếp
            BecomeIdle();
        }

        // ─── Tuổi & gia đình ─────────────────────────────────────────────────
        /// <summary>Khởi tạo em bé mới sinh (gọi ngay sau Instantiate).</summary>
        public void InitializeAsBaby(string babyName, Gender babyGender)
        {
            npcName = babyName;
            name = $"Npc_{babyName}";
            gender = babyGender;
            age = AgeStage.Baby;
            ageSeconds = 0f;
            profession = null;
            holdPosition = false;
            job = null;
            home = transform.position;
            RefreshVisuals();
            health.SetCurrent(health.Max);
            BecomeIdle();
        }

        public void SetPartner(NpcController other)
        {
            Partner = other;
            if (other != null) other.Partner = this;
            pendingPartnerName = null;
        }

        /// <summary>Em bé → trẻ em → người lớn theo thời gian cấu hình ở NpcManager; trưởng thành thì thành Dân làng.</summary>
        private void UpdateAge()
        {
            NpcManager manager = NpcManager.Instance;
            if (age == AgeStage.Adult || manager == null) return;

            ageSeconds += Time.deltaTime;
            float duration = age == AgeStage.Baby ? manager.BabyDuration : manager.ChildDuration;
            if (ageSeconds < duration) return;

            ageSeconds = 0f;
            age = age == AgeStage.Baby ? AgeStage.Child : AgeStage.Adult;
            if (age == AgeStage.Adult)
            {
                SetProfession(manager.AdultProfession);
                Home = null; // trưởng thành thì ra ở riêng, lập gia đình mới cần lều mới
                EventBus.RaiseNotification($"{npcName} đã trưởng thành — có thể giao nghề!");
            }
            RefreshVisuals();
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        /// <summary>Lệnh di chuyển của người chơi: đi tới đó rồi đứng giữ vị trí.</summary>
        public bool MoveTo(Vector3 destination)
        {
            if (!agent.isOnNavMesh || !NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            job = null; // lệnh mới thay thế việc đang làm
            agent.SetDestination(hit.position);
            home = hit.position;
            StartHolding();
            State = NpcState.Moving;
            return true;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionRing != null) selectionRing.SetActive(selected);
        }

        public void SetProfession(ProfessionData newProfession)
        {
            profession = newProfession;
            ApplyProfession();
        }

        private void TryWander()
        {
            // Trẻ con chơi quanh quẩn gần lều nhà mình.
            float radius = IsAdult ? wanderRadius : wanderRadius * 0.5f;
            Vector3 center = !IsAdult && Home != null ? Home.transform.position : home;
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 target = center + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                State = NpcState.Wandering;
            }
            else
            {
                idleTimer = minIdlePause;
            }
        }

        private void BecomeIdle()
        {
            State = NpcState.Idle;
            idleTimer = Random.Range(minIdlePause, maxIdlePause);
        }

        private bool HasArrived() =>
            !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f;

        /// <summary>Cập nhật dụng cụ/tóc/kích thước theo nghề, giới tính, tuổi — gọi được cả trong Editor.</summary>
        public void RefreshVisuals()
        {
            ApplyProfession();
            ApplyAppearance();
        }

        private void ApplyProfession()
        {
            if (profession == null)
            {
                // Trẻ con: chưa có nghề, không cầm dụng cụ, đi chậm.
                foreach (var entry in tools)
                    if (entry.tool != null) entry.tool.SetActive(false);
                baseSpeed = age == AgeStage.Baby ? 1.2f : 1.8f;
                if (agent != null) agent.speed = baseSpeed;
                return;
            }

            baseSpeed = profession.moveSpeed;
            if (agent != null) agent.speed = baseSpeed;
            if (health != null) health.SetMax(profession.maxHealth, refill: false);

            if (tunicRenderer != null)
            {
                // PropertyBlock: đổi màu từng NPC mà không nhân bản material.
                var block = new MaterialPropertyBlock();
                tunicRenderer.GetPropertyBlock(block);
                block.SetColor(ColorId, profession.tunicColor);
                tunicRenderer.SetPropertyBlock(block);
            }

            foreach (var entry in tools)
                if (entry.tool != null) entry.tool.SetActive(entry.profession == profession);
        }

        private void ApplyAppearance()
        {
            if (maleHair != null) maleHair.SetActive(gender == Gender.Male);
            if (femaleHair != null) femaleHair.SetActive(gender == Gender.Female);

            float scale = age switch
            {
                AgeStage.Baby => 0.45f,
                AgeStage.Child => 0.7f,
                _ => 1f
            };
            transform.localScale = Vector3.one * scale;
        }

        public NpcSaveData GetSaveData() => new NpcSaveData
        {
            npcName = npcName,
            professionId = profession != null ? profession.id : null,
            gender = gender,
            age = age,
            x = transform.position.x,
            z = transform.position.z,
            rotationY = transform.eulerAngles.y,
            health = health.Current,
            holdPosition = holdPosition,
            ageSeconds = ageSeconds,
            partnerName = Partner != null ? Partner.NpcName : null,
            hasHome = Home != null,
            homeCellX = Home != null ? Home.GridPosition.x : 0,
            homeCellY = Home != null ? Home.GridPosition.y : 0,
            fullness = Fullness
        };

        /// <summary>Khôi phục thông tin (vị trí do nơi gọi đặt lúc Instantiate).</summary>
        public void LoadFromSaveData(NpcSaveData saved, ProfessionData savedProfession)
        {
            npcName = saved.npcName;
            gender = saved.gender;
            age = saved.age;
            ageSeconds = saved.ageSeconds;
            pendingPartnerName = saved.partnerName; // NpcManager nối lại sau khi tạo đủ mọi người
            PendingHomeCell = saved.hasHome ? new Vector3Int(saved.homeCellX, saved.homeCellY, 0) : (Vector3Int?)null;
            Fullness = saved.fullness >= 0f ? saved.fullness : MaxFullness; // save cũ (chưa có độ no, = -1) → no đủ
            name = $"Npc_{npcName}";
            profession = savedProfession;
            ApplyProfession();
            ApplyAppearance();
            health.SetCurrent(saved.health);
            home = transform.position;
            if (saved.holdPosition) StartHolding();
            else holdPosition = false;
            BecomeIdle();
        }
    }
}
