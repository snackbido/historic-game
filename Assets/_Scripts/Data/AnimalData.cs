using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewAnimalData", menuName = "PrehistoricTribe/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject prefab;

        [Tooltip("Thời gian (giây) để giảm 1 đơn vị no, từ 100 xuống 0")]
        public float hungerDecayInterval = 10f;

        [Tooltip("Mức no tối thiểu (0-100) để vật nuôi sinh sản/sản xuất")]
        public float hungerThresholdForNeeds = 50f;

        [Tooltip("Tài nguyên cần cho 1 lần cho ăn (dùng cả khi thuần hóa lẫn cho ăn định kỳ)")]
        public List<ResourceAmount> feedCost = new List<ResourceAmount>();

        [Tooltip("Số lần cho ăn cần thiết để thuần hóa thành công")]
        public int feedingsToTame = 3;

        [Tooltip("Thời gian (giây) giữa các lần sinh sản khi no đủ")]
        public float reproductionInterval = 60f;

        [Tooltip("Sản phẩm thu được mỗi chu kỳ sản xuất (thịt/da/sữa)")]
        public List<ResourceAmount> products = new List<ResourceAmount>();

        [Tooltip("Thời gian (giây) giữa các lần sản phẩm sẵn sàng để thu")]
        public float productionInterval = 30f;
    }
}
