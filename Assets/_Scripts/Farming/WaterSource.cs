using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Nguồn nước (Milestone 5d): ao, sau này giếng/mương. Ruộng nước phải đặt gần nguồn nước;
    /// không được xây đè lên mặt nước.
    /// </summary>
    public class WaterSource : MonoBehaviour
    {
        private static readonly List<WaterSource> all = new List<WaterSource>();
        public static IReadOnlyList<WaterSource> All => all;

        [Tooltip("Bán kính mặt nước (m) — tính khoảng cách từ mép nước")]
        [SerializeField] private float radius = 1f;
        [Tooltip("Đủ nước cho ruộng nước (ao, mương) — giếng thì không, chỉ để gánh nước")]
        [SerializeField] private bool feedsPaddies = true;

        public float Radius => radius;
        public bool FeedsPaddies => feedsPaddies;

        /// <summary>Đứng cách tâm chừng này là múc được nước.</summary>
        public float DrawRange => radius + 0.9f;

        /// <summary>Nguồn nước gần điểm này nhất (null nếu không có).</summary>
        public static WaterSource Nearest(Vector3 position)
        {
            WaterSource best = null;
            float bestDistance = float.MaxValue;
            foreach (var source in all)
            {
                float d = source.DistanceToEdge(position);
                if (d >= bestDistance) continue;
                best = source;
                bestDistance = d;
            }
            return best;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        /// <summary>Khoảng cách từ điểm tới mép nước (âm = đang ở trên mặt nước).</summary>
        public float DistanceToEdge(Vector3 position) =>
            InteractableRegistry.GroundDistance(position, transform.position) - radius;

        /// <summary>Có nguồn nước nào mà mép nước cách điểm này không quá <paramref name="range"/> mét.</summary>
        public static bool AnyWithin(Vector3 position, float range, bool forPaddy = false)
        {
            foreach (var source in all)
                if ((!forPaddy || source.feedsPaddies) && source.DistanceToEdge(position) <= range) return true;
            return false;
        }

        /// <summary>Điểm này nằm trên/quá sát mặt nước (không xây được).</summary>
        public static bool IsOnWater(Vector3 position, float clearance)
        {
            foreach (var source in all)
                if (source.DistanceToEdge(position) < clearance) return true;
            return false;
        }
    }
}
