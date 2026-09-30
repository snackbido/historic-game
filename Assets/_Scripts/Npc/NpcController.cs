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
                    AssignJob(new AttackJob(predator, profession));
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
            if (!agent.isOnNavMesh) return;

            switch (State)
            {
                case NpcState.Idle:
                    if (holdPosition)
                    {
                        UpdateHold();
                        break;
                    }
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f) TryWander();
                    break;

                case NpcState.Wandering:
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

        /// <summary>Giao việc (lệnh của người chơi). Xong việc thì đứng giữ vị trí tại đó.</summary>
        public void AssignJob(NpcJob newJob)
        {
            if (newJob == null || !newJob.IsValid) return;

            job = newJob;
            StartHolding();
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
            BecomeIdle();
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
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 target = home + new Vector3(offset.x, 0f, offset.y);
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
            if (profession == null) return;

            if (agent != null) agent.speed = profession.moveSpeed;
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
            holdPosition = holdPosition
        };

        /// <summary>Khôi phục thông tin (vị trí do nơi gọi đặt lúc Instantiate).</summary>
        public void LoadFromSaveData(NpcSaveData saved, ProfessionData savedProfession)
        {
            npcName = saved.npcName;
            gender = saved.gender;
            age = saved.age;
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
