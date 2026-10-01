using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PrehistoricTribe
{
    public enum PredatorState
    {
        Patrol,
        Chase,
        Attack
    }

    [System.Serializable]
    public class PredatorSaveData
    {
        public string predatorId;
        public string objectName;
        public float x;
        public float z;
        public float denX;
        public float denZ;
        public float health;
    }

    /// <summary>
    /// AI thú dữ (EnemyAI trong ARCHITECTURE.md): đi tuần quanh hang → đuổi người/NPC lại gần
    /// → tấn công trong tầm. Bỏ cuộc khi mục tiêu chạy quá xa hang. Bị đánh thì quay sang đánh kẻ tấn công.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(HealthComponent))]
    public class PredatorAI : MonoBehaviour
    {
        private static readonly List<PredatorAI> all = new List<PredatorAI>();
        public static IReadOnlyList<PredatorAI> All => all;

        private const float ThinkInterval = 0.3f;

        [SerializeField] private PredatorData data;

        private NavMeshAgent agent;
        private HealthComponent health;
        private HealthComponent playerHealth;
        private Vector3 den;
        private HealthComponent target;
        private float thinkTimer;
        private float attackTimer;
        private float patrolPause;

        public PredatorData Data => data;
        public HealthComponent Health => health;
        public HealthComponent Target => target;
        public Vector3 Den => den;
        public PredatorState State { get; private set; } = PredatorState.Patrol;

        // Ban đêm sói lùng sục rộng hơn, nhìn xa hơn, đuổi xa hơn; sáng ra thì về lại quanh hang.
        private static bool IsNight => DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        public float PatrolRadius => IsNight ? data.nightPatrolRadius : data.patrolRadius;
        public float AggroRange => IsNight ? data.nightAggroRange : data.aggroRange;
        public float LeashRange => IsNight ? data.nightLeashRange : data.leashRange;
        private bool patrollingAtNight;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthComponent>();
            den = transform.position;
            patrolPause = Random.Range(1f, 3f);

            var player = GameObject.FindWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<HealthComponent>();

            ApplyData();
            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDied;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private void ApplyData()
        {
            if (data == null) return;
            agent.speed = data.patrolSpeed;
            health.SetMax(data.maxHealth);
        }

        private void Update()
        {
            if (data == null || !agent.isOnNavMesh || health.IsDead) return;

            // Chọn/cập nhật mục tiêu vài lần mỗi giây, không cần mỗi frame.
            thinkTimer -= Time.deltaTime;
            bool think = thinkTimer <= 0f;
            if (think)
            {
                thinkTimer = ThinkInterval;
                if (!IsValidTarget(target)) target = FindTarget();
            }

            if (target == null)
            {
                Patrol();
                return;
            }

            float distance = InteractableRegistry.GroundDistance(transform.position, target.transform.position);
            if (distance > data.attackRange)
            {
                // Mục tiêu di chuyển → cập nhật đường đi khi vừa bắt đầu đuổi và mỗi lượt "suy nghĩ".
                if (State != PredatorState.Chase || think) agent.SetDestination(target.transform.position);
                State = PredatorState.Chase;
                agent.speed = data.chaseSpeed;
                return;
            }

            // Trong tầm: đứng lại, quay mặt về mục tiêu và cắn theo nhịp.
            State = PredatorState.Attack;
            if (agent.hasPath) agent.ResetPath();
            FaceTowards(target.transform.position);
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;

            attackTimer = data.attackInterval;
            target.TakeDamage(data.attackDamage, gameObject);
        }

        private void Patrol()
        {
            if (State != PredatorState.Patrol)
            {
                State = PredatorState.Patrol;
                agent.speed = data.patrolSpeed;
                agent.SetDestination(den); // bỏ cuộc → quay về hang
            }

            bool night = IsNight;
            if (night != patrollingAtNight)
            {
                patrollingAtNight = night;
                if (!night) agent.SetDestination(den); // trời sáng → về hang
            }

            // Lửa trại vừa nhóm mà đang đứng trong vùng sáng → lùi ra ngoài.
            if (Campfire.IsProtected(transform.position))
            {
                RetreatFromFire();
                return;
            }

            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f) return;

            patrolPause -= Time.deltaTime;
            if (patrolPause > 0f) return;
            patrolPause = night ? Random.Range(1f, 3f) : Random.Range(2f, 5f);

            // Thử vài điểm, bỏ qua điểm nằm trong vùng sáng của lửa trại.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * PatrolRadius;
                if (!NavMesh.SamplePosition(den + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) continue;
                if (Campfire.IsProtected(hit.position)) continue;
                agent.SetDestination(hit.position);
                return;
            }
        }

        private void RetreatFromFire()
        {
            foreach (var fire in Campfire.All)
            {
                if (!fire.IsLit) continue;
                Vector3 away = transform.position - fire.transform.position;
                away.y = 0f;
                if (away.magnitude > fire.SafeRadius) continue;
                if (away.sqrMagnitude < 0.01f) away = den - fire.transform.position;
                Vector3 outside = fire.transform.position + away.normalized * (fire.SafeRadius + 2f);
                if (NavMesh.SamplePosition(outside, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                return;
            }
        }

        /// <summary>Người đang ngủ trong lều thì thú dữ không với tới.</summary>
        private static bool IsInsideHut(HealthComponent candidate)
        {
            var npc = candidate.GetComponent<NpcController>();
            return npc != null && npc.IsInsideHut;
        }

        private HealthComponent FindTarget()
        {
            HealthComponent best = null;
            float bestDistance = AggroRange;

            void Consider(HealthComponent candidate)
            {
                if (!IsValidTarget(candidate)) return;
                float d = InteractableRegistry.GroundDistance(transform.position, candidate.transform.position);
                if (d > bestDistance) return;
                best = candidate;
                bestDistance = d;
            }

            foreach (var npc in NpcController.All) Consider(npc.Health);
            Consider(playerHealth);
            return best;
        }

        private bool IsValidTarget(HealthComponent candidate) =>
            candidate != null && candidate.isActiveAndEnabled && !candidate.IsDead &&
            !IsInsideHut(candidate) &&
            !Campfire.IsProtected(candidate.transform.position) && // không dám lại gần lửa trại đang cháy
            InteractableRegistry.GroundDistance(den, candidate.transform.position) <= LeashRange;

        private void HandleDamaged(HealthComponent _, GameObject source)
        {
            if (source == null) return;
            var attacker = source.GetComponent<HealthComponent>();
            if (IsValidTarget(attacker)) target = attacker;
        }

        private void HandleDied(HealthComponent _)
        {
            if (data != null)
            {
                foreach (var drop in data.huntYield)
                    ResourceManager.Instance.AddResource(drop.type, drop.amount);
                EventBus.RaiseNotification($"Đã hạ {data.displayName}!");
            }

            gameObject.SetActive(false); // gỡ khỏi danh sách ngay
            Destroy(gameObject);
        }

        private void FaceTowards(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        public PredatorSaveData GetSaveData() => new PredatorSaveData
        {
            predatorId = data != null ? data.id : null,
            objectName = name,
            x = transform.position.x,
            z = transform.position.z,
            denX = den.x,
            denZ = den.z,
            health = health.Current
        };

        public void LoadFromSaveData(PredatorSaveData saved, PredatorData source)
        {
            data = source;
            ApplyData();
            den = new Vector3(saved.denX, 0f, saved.denZ);
            health.SetCurrent(saved.health);
            target = null;
            State = PredatorState.Patrol;
        }
    }
}
