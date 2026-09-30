using System;
using System.Collections.Generic;

namespace PrehistoricTribe
{
    public static class EventBus
    {
        public static event Action<ResourceTypeData, int> OnResourceChanged;
        public static event Action<BuildingInstance> OnBuildingPlaced;
        public static event Action<TechNode> OnTechUnlocked;
        public static event Action<string> OnNotification;
        /// <summary>Bắn sau khi LoadGame khôi phục xong — UI phụ thuộc trạng thái mở khóa tự làm mới.</summary>
        public static event Action OnGameLoaded;
        /// <summary>Danh sách NPC đang được chọn thay đổi (dùng cho UI nhóm/nghề).</summary>
        public static event Action<IReadOnlyList<NpcController>> OnSelectionChanged;
        /// <summary>Công trình đang được chọn thay đổi (null = bỏ chọn).</summary>
        public static event Action<BuildingInstance> OnBuildingSelected;
        public static event Action<BuildingInstance> OnBuildingUpgraded;

        public static void RaiseResourceChanged(ResourceTypeData type, int newAmount) => OnResourceChanged?.Invoke(type, newAmount);
        public static void RaiseBuildingPlaced(BuildingInstance instance) => OnBuildingPlaced?.Invoke(instance);
        public static void RaiseTechUnlocked(TechNode tech) => OnTechUnlocked?.Invoke(tech);
        public static void RaiseNotification(string message) => OnNotification?.Invoke(message);
        public static void RaiseGameLoaded() => OnGameLoaded?.Invoke();
        public static void RaiseSelectionChanged(IReadOnlyList<NpcController> selected) => OnSelectionChanged?.Invoke(selected);
        public static void RaiseBuildingSelected(BuildingInstance building) => OnBuildingSelected?.Invoke(building);
        public static void RaiseBuildingUpgraded(BuildingInstance building) => OnBuildingUpgraded?.Invoke(building);
    }
}
