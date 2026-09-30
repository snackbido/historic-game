using System;

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

        public static void RaiseResourceChanged(ResourceTypeData type, int newAmount) => OnResourceChanged?.Invoke(type, newAmount);
        public static void RaiseBuildingPlaced(BuildingInstance instance) => OnBuildingPlaced?.Invoke(instance);
        public static void RaiseTechUnlocked(TechNode tech) => OnTechUnlocked?.Invoke(tech);
        public static void RaiseNotification(string message) => OnNotification?.Invoke(message);
        public static void RaiseGameLoaded() => OnGameLoaded?.Invoke();
    }
}
