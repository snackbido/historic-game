using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Thú dữ (kẻ địch): đi tuần quanh hang, đuổi và tấn công người lại gần.</summary>
    [CreateAssetMenu(fileName = "NewPredatorData", menuName = "PrehistoricTribe/Predator Data")]
    public class PredatorData : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject prefab;

        public float maxHealth = 50f;
        public float patrolSpeed = 2f;
        public float chaseSpeed = 3.8f;

        [Tooltip("Người/NPC lại gần trong bán kính này (m) thì bị đuổi")]
        public float aggroRange = 4.5f;
        [Tooltip("Không đuổi xa hơn khoảng cách này tính từ hang (m)")]
        public float leashRange = 12f;
        [Tooltip("Bán kính đi tuần quanh hang (m)")]
        public float patrolRadius = 2f;

        public float attackDamage = 8f;
        public float attackRange = 1.2f;
        public float attackInterval = 1.2f;

        [Tooltip("Tài nguyên rơi ra khi bị hạ")]
        public List<ResourceAmount> huntYield = new List<ResourceAmount>();
    }
}
