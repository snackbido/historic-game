using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Danh sách các đối tượng tương tác được (ResourceNode, FarmPlot, AnimalController).
    /// Tìm đối tượng gần nhất bằng khoảng cách trên mặt đất thay vì collider, để logic
    /// tương tác không phụ thuộc vào vật lý 2D/3D (lý do: ARCHITECTURE.md § 5).
    /// </summary>
    public static class InteractableRegistry
    {
        private static readonly List<MonoBehaviour> items = new List<MonoBehaviour>();

        public static void Register(MonoBehaviour item)
        {
            if (!items.Contains(item)) items.Add(item);
        }

        public static void Unregister(MonoBehaviour item) => items.Remove(item);

        public static MonoBehaviour FindNearest(Vector3 position, float radius)
        {
            MonoBehaviour nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var item in items)
            {
                if (item == null || !item.isActiveAndEnabled) continue;

                float distance = GroundDistance(position, item.transform.position);
                if (distance <= radius && distance < nearestDistance)
                {
                    nearest = item;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        /// <summary>Tất cả đối tượng loại T đang hoạt động (vd mọi FarmPlot/AnimalController khi lưu game).</summary>
        public static List<T> All<T>() where T : MonoBehaviour
        {
            var result = new List<T>();
            foreach (var item in items)
                if (item is T typed && typed != null) result.Add(typed);
            return result;
        }

        public static float GroundDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // Dọn danh sách khi tắt Domain Reload (Enter Play Mode Options) để không giữ tham chiếu cũ.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => items.Clear();
    }
}
