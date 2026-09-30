using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Vũ khí tiền sử của nhân vật chính (SPEC § 3.5): F = đâm giáo (tầm gần), R = ném đá (tầm xa).
    /// Tự nhắm thú dữ gần nhất trong tầm. Bị hạ thì được đưa về trại hồi phục đầy máu.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Đâm giáo")]
        [SerializeField] private KeyCode spearKey = KeyCode.F;
        [SerializeField] private float spearDamage = 15f;
        [SerializeField] private float spearRange = 1.8f;
        [SerializeField] private float spearCooldown = 0.8f;

        [Header("Ném đá")]
        [SerializeField] private KeyCode stoneKey = KeyCode.R;
        [SerializeField] private float stoneDamage = 8f;
        [SerializeField] private float stoneRange = 8f;
        [SerializeField] private float stoneCooldown = 1.5f;

        [Header("Bị hạ")]
        [SerializeField] private Vector3 respawnPoint = Vector3.zero;

        private HealthComponent health;
        private PlayerController controller;
        private float spearReadyTime;
        private float stoneReadyTime;

        public float StoneRange => stoneRange;
        public float SpearRange => spearRange;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            controller = GetComponent<PlayerController>();
            health.OnDied += HandleDied;
        }

        private void Update()
        {
            if (Input.GetKeyDown(spearKey)) TryMeleeAttack();
            if (Input.GetKeyDown(stoneKey)) TryThrowStone();
        }

        public bool TryMeleeAttack()
        {
            if (Time.time < spearReadyTime) return false;
            PredatorAI target = FindTarget(spearRange);
            if (target == null) return false;

            spearReadyTime = Time.time + spearCooldown;
            target.Health.TakeDamage(spearDamage, gameObject);
            return true;
        }

        public bool TryThrowStone()
        {
            if (Time.time < stoneReadyTime) return false;
            PredatorAI target = FindTarget(stoneRange);
            if (target == null) return false;

            stoneReadyTime = Time.time + stoneCooldown;
            StoneProjectile.Launch(transform.position + Vector3.up * 0.8f, target.transform);
            target.Health.TakeDamage(stoneDamage, gameObject);
            return true;
        }

        /// <summary>Thú dữ gần nhất trong tầm (null nếu không có).</summary>
        public PredatorAI FindTarget(float range)
        {
            PredatorAI best = null;
            float bestDistance = range;
            foreach (var predator in PredatorAI.All)
            {
                if (predator.Health.IsDead) continue;
                float distance = InteractableRegistry.GroundDistance(transform.position, predator.transform.position);
                if (distance > bestDistance) continue;
                best = predator;
                bestDistance = distance;
            }
            return best;
        }

        private void HandleDied(HealthComponent _)
        {
            EventBus.RaiseNotification("Bạn bị thương nặng! Được đưa về trại hồi phục");
            if (controller != null) controller.SetPosition(respawnPoint);
            health.SetCurrent(health.Max);
        }
    }

    /// <summary>Hòn đá bay từ người ném tới mục tiêu — chỉ để nhìn (sát thương tính ngay lúc ném).</summary>
    public class StoneProjectile : MonoBehaviour
    {
        private const float FlightTime = 0.25f;

        private Vector3 start;
        private Transform target;
        private float elapsed;

        public static void Launch(Vector3 from, Transform target)
        {
            var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(stone.GetComponent<Collider>());
            stone.name = "Stone";
            stone.transform.position = from;
            stone.transform.localScale = Vector3.one * 0.12f;
            var projectile = stone.AddComponent<StoneProjectile>();
            projectile.start = from;
            projectile.target = target;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (target == null || elapsed >= FlightTime)
            {
                Destroy(gameObject);
                return;
            }

            float t = elapsed / FlightTime;
            Vector3 end = target.position + Vector3.up * 0.4f;
            // Đường cong nhẹ cho giống ném.
            transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
        }
    }
}
