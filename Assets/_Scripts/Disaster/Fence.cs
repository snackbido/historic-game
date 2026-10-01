using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Hàng rào cọc nhọn (M6/D5): chắn kín cả ô (NavMeshObstacle) — người và thú đều phải đi vòng, nên nhớ chừa cổng.
    /// Sói đột kích bị rào chặn thì cào phá; rào sập (hết máu) thì đi qua được. Hàng cọc tự xoay theo rào bên cạnh.
    /// </summary>
    public class Fence : MonoBehaviour
    {
        private static readonly List<Fence> all = new List<Fence>();
        public static IReadOnlyList<Fence> All => all;

        [Tooltip("Model hàng cọc (chạy theo trục X) — xoay 90° khi rào nối theo hướng bắc–nam")]
        [SerializeField] private Transform model;

        private BuildingInstance building;
        public BuildingInstance Building => building != null ? building : building = GetComponent<BuildingInstance>();
        public bool IsStanding => Building != null && !Building.IsCollapsed;

        private void OnEnable()
        {
            all.Add(this);
            EventBus.OnBuildingPlaced += HandlePlaced;
        }

        private void OnDisable()
        {
            all.Remove(this);
            EventBus.OnBuildingPlaced -= HandlePlaced;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private void Start() => Orient();

        private void HandlePlaced(BuildingInstance _) => Orient();

        private void Orient()
        {
            if (model == null || Building == null) return;
            Vector3Int cell = Building.GridPosition;
            bool eastWest = HasFenceAt(cell + Vector3Int.right) || HasFenceAt(cell + Vector3Int.left);
            bool northSouth = HasFenceAt(cell + new Vector3Int(0, 1, 0)) || HasFenceAt(cell + new Vector3Int(0, -1, 0));
            model.localRotation = northSouth && !eastWest ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        }

        private static bool HasFenceAt(Vector3Int cell)
        {
            foreach (var fence in all)
                if (fence.Building != null && fence.Building.GridPosition == cell) return true;
            return false;
        }

        /// <summary>Đoạn rào còn đứng gần điểm này nhất (null nếu không có trong bán kính).</summary>
        public static Fence NearestStanding(Vector3 position, float radius)
        {
            Fence best = null;
            float bestDistance = radius;
            foreach (var fence in all)
            {
                if (!fence.IsStanding) continue;
                float d = InteractableRegistry.GroundDistance(position, fence.transform.position);
                if (d > bestDistance) continue;
                best = fence;
                bestDistance = d;
            }
            return best;
        }
    }
}
