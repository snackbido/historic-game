using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Một cấp của công trình (cấp 1 = lúc vừa xây).</summary>
    [System.Serializable]
    public class BuildingLevel
    {
        public string displayName;
        [Tooltip("Chi phí nâng LÊN cấp này (cấp 1 dùng BuildingData.costs lúc xây)")]
        public List<ResourceAmount> upgradeCost = new List<ResourceAmount>();
        [Tooltip("Lều: số con nhỏ tối đa gia đình (cặp đôi) sống ở đây nuôi được. >0 = công trình là nhà ở")]
        public int housing;
        [Tooltip("Sức chứa lương thực mỗi loại mà cấp này thêm vào (dùng ở bước kho E4)")]
        public int storageCapacity;
        [Tooltip("Công nghệ cần có để nâng lên cấp này (để trống = không cần)")]
        public TechNode requiredTech;
    }

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

        [Tooltip("Mô tả chức năng hiện trên bảng thông tin công trình")]
        public string functionDescription;

        [Tooltip("Các cấp của công trình, phần tử 0 = cấp 1")]
        public List<BuildingLevel> levels = new List<BuildingLevel>();

        public int MaxLevel => Mathf.Max(1, levels.Count);

        /// <summary>Thông tin cấp (1-based); null nếu công trình không có danh sách cấp.</summary>
        public BuildingLevel GetLevel(int level) =>
            levels.Count == 0 ? null : levels[Mathf.Clamp(level, 1, levels.Count) - 1];
    }
}
