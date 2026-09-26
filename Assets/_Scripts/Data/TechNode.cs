using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewTechNode", menuName = "PrehistoricTribe/Tech Node")]
    public class TechNode : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
        public List<TechNode> prerequisites = new List<TechNode>();

        [Tooltip("id của các BuildingData được mở khóa khi tech này hoàn thành")]
        public List<string> unlockedBuildingIds = new List<string>();

        [Tooltip("id của các CropData được mở khóa khi tech này hoàn thành")]
        public List<string> unlockedCropIds = new List<string>();
    }
}
