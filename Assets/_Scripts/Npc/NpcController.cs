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
        Moving
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

        [Header("Đi dạo khi rảnh")]
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float minIdlePause = 2f;
        [SerializeField] private float maxIdlePause = 6f;

        private NavMeshAgent agent;
        private HealthComponent health;
        private Vector3 home;
        private float idleTimer;

        public string NpcName => npcName;
        public Gender Gender => gender;
        public AgeStage Age => age;
        public ProfessionData Profession => profession;
        public HealthComponent Health => health;
        public NpcState State { get; private set; } = NpcState.Idle;
        public bool IsAdult => age == AgeStage.Adult;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthComponent>();
            home = transform.position;
            idleTimer = Random.Range(minIdlePause, maxIdlePause);
            RefreshVisuals();
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
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f) TryWander();
                    break;

                case NpcState.Wandering:
                case NpcState.Moving:
                    if (HasArrived()) BecomeIdle();
                    break;
            }
        }

        /// <summary>Lệnh di chuyển. Điểm đến trở thành "nhà" mới để đi dạo quanh đó.</summary>
        public bool MoveTo(Vector3 destination)
        {
            if (!agent.isOnNavMesh || !NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            agent.SetDestination(hit.position);
            home = hit.position;
            State = NpcState.Moving;
            return true;
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
            health = health.Current
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
            BecomeIdle();
        }
    }
}
