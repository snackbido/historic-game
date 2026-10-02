using System;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Máu dùng chung cho Player / NPC / thú / công trình.</summary>
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        [Header("Hồi máu (0 = không tự hồi)")]
        [SerializeField] private float regenPerSecond;
        [Tooltip("Không bị đánh trong bao nhiêu giây thì bắt đầu hồi máu")]
        [SerializeField] private float regenDelay = 6f;

        private float lastDamageTime = float.NegativeInfinity;
        private float lastVfxTime = float.NegativeInfinity;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public event Action<HealthComponent> OnChanged;
        public event Action<HealthComponent> OnDied;
        /// <summary>Bị đánh: (máu, kẻ gây sát thương — có thể null).</summary>
        public event Action<HealthComponent, GameObject> OnDamaged;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void Update()
        {
            if (regenPerSecond <= 0f || IsDead || Current >= maxHealth) return;
            if (Time.time - lastDamageTime < regenDelay) return;
            Heal(regenPerSecond * Time.deltaTime);
        }

        public void SetMax(float max, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, max);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            OnChanged?.Invoke(this);
        }

        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            OnChanged?.Invoke(this);
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            lastDamageTime = Time.time;
            OnChanged?.Invoke(this);
            OnDamaged?.Invoke(this, source);
            if (IsDead) OnDied?.Invoke(this);
            PlayDamageVfx();
        }

        /// <summary>Hạt máu khi người/thú bị đánh, bụi khi công trình hư (thiên tai gây hư liên tục nên giãn nhịp).</summary>
        private void PlayDamageVfx()
        {
            bool building = GetComponent<BuildingInstance>() != null;
            if (building) { if (IsDead) return; } // công trình sập: BuildingInstance tự phun bụi lớn
            else if (IsDead) { VfxManager.Play(VfxKind.BigDust, transform.position, SfxKind.Hit); return; }

            float interval = building ? 1.5f : 0.15f;
            if (Time.time - lastVfxTime < interval) return;
            lastVfxTime = Time.time;
            VfxManager.Play(building ? VfxKind.Dust : VfxKind.Hit, transform.position + Vector3.up * (building ? 0.5f : 0.7f));
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            OnChanged?.Invoke(this);
        }
    }
}
