using System;

namespace PrehistoricTribe
{
    public static class EventBus
    {
        public static event Action<ResourceTypeData, int> OnResourceChanged;
        public static event Action<BuildingInstance> OnBuildingPlaced;
        public static event Action<TechNode> OnTechUnlocked;

        public static void RaiseResourceChanged(ResourceTypeData type, int newAmount) => OnResourceChanged?.Invoke(type, newAmount);
        public static void RaiseBuildingPlaced(BuildingInstance instance) => OnBuildingPlaced?.Invoke(instance);
        public static void RaiseTechUnlocked(TechNode tech) => OnTechUnlocked?.Invoke(tech);
    }
}
