using System;

namespace PrehistoricTribe
{
    public static class EventBus
    {
        public static event Action<ResourceType, int> OnResourceChanged;
        public static event Action<BuildingInstance> OnBuildingPlaced;

        public static void RaiseResourceChanged(ResourceType type, int newAmount) => OnResourceChanged?.Invoke(type, newAmount);
        public static void RaiseBuildingPlaced(BuildingInstance instance) => OnBuildingPlaced?.Invoke(instance);
    }
}
