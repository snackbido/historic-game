using UnityEngine;

namespace PrehistoricTribe
{
    public class BuildingInstance : MonoBehaviour
    {
        public BuildingData Data { get; private set; }
        public Vector3Int GridPosition { get; private set; }

        public void Initialize(BuildingData data, Vector3Int gridPosition)
        {
            Data = data;
            GridPosition = gridPosition;
        }
    }
}
