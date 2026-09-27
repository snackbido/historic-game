using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewVillagerData", menuName = "PrehistoricTribe/Villager Data")]
    public class VillagerData : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject prefab;

        public float moveSpeed = 2.5f;

        [Tooltip("Thời gian (giây) để giảm 1 đơn vị đói/ngủ/ấm, từ 100 xuống 0")]
        public float hungerDecayInterval = 15f;
        public float sleepDecayInterval = 25f;
        public float warmthDecayInterval = 30f;

        [Tooltip("Dưới ngưỡng này (0-100) sẽ cảnh báo người chơi / tự ăn")]
        public float lowNeedWarningThreshold = 30f;

        [Tooltip("Tài nguyên tự tiêu khi đói thấp (ăn để hồi no về 100)")]
        public ResourceTypeData foodResource;
        public int foodPerMeal = 1;
    }
}
