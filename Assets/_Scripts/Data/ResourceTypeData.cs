using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewResourceType", menuName = "PrehistoricTribe/Resource Type")]
    public class ResourceTypeData : ScriptableObject
    {
        public string id;
        public string displayName;
    }
}
