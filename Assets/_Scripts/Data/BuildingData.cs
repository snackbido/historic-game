using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "PrehistoricTribe/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject prefab;
        public Vector2Int footprint = Vector2Int.one;
        public List<ResourceAmount> costs = new List<ResourceAmount>();

        [Tooltip("Mở khóa sẵn ngay từ đầu game, không cần tech")]
        public bool unlockedByDefault;

        [Tooltip("Số chỗ ở công trình này thêm vào sức chứa dân số (lều = 2)")]
        public int housing;
    }
}
