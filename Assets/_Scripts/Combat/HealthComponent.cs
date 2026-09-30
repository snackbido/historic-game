using System;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Máu dùng chung cho Player / NPC / thú / công trình.</summary>
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public event Action<HealthComponent> OnChanged;
        public event Action<HealthComponent> OnDied;

        private void Awake()
        {
            Current = maxHealth;
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

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            OnChanged?.Invoke(this);
            if (IsDead) OnDied?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            OnChanged?.Invoke(this);
        }
    }
}
