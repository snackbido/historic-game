using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public class BuildingInstance : MonoBehaviour
    {
        private static readonly List<BuildingInstance> all = new List<BuildingInstance>();
        /// <summary>Mọi công trình đang có trên bản đồ (vd tính sức chứa dân số theo số lều).</summary>
        public static IReadOnlyList<BuildingInstance> All => all;

        public BuildingData Data { get; private set; }
        public Vector3Int GridPosition { get; private set; }

        public void Initialize(BuildingData data, Vector3Int gridPosition)
        {
            Data = data;
            GridPosition = gridPosition;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();
    }
}
