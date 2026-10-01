using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Đê (M6/D3): ụ đất đắp giữa ao và ruộng/nhà. Nước lũ tràn từ ao tới một điểm mà đường đi cắt ngang một đoạn đê
    /// (cách tâm đê không quá <see cref="BlockRadius"/>) thì bị chặn lại.
    /// </summary>
    public class Levee : MonoBehaviour
    {
        private static readonly List<Levee> all = new List<Levee>();
        public static IReadOnlyList<Levee> All => all;

        /// <summary>Một đoạn đê chắn được dòng nước đi qua trong bán kính này (m) — xếp đê liền nhau thành bờ kín.</summary>
        public const float BlockRadius = 0.75f;

        [Tooltip("Model đê (thân dài theo trục X) — xoay để thân đê chắn ngang hướng ra ao")]
        [SerializeField] private Transform model;

        private void Start()
        {
            WaterSource pond = null;
            foreach (var source in WaterSource.All)
                if (source.IsNatural && source.FeedsPaddies &&
                    (pond == null || source.DistanceToEdge(transform.position) < pond.DistanceToEdge(transform.position)))
                    pond = source;
            if (pond == null || model == null) return;
            Vector3 toPond = pond.transform.position - transform.position;
            // Ao nằm phía đông/tây → thân đê chạy bắc–nam.
            if (Mathf.Abs(toPond.x) > Mathf.Abs(toPond.z)) model.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        /// <summary>Có đoạn đê nào chắn đường nước chảy từ <paramref name="from"/> tới <paramref name="to"/> không.</summary>
        public static bool Blocks(Vector3 from, Vector3 to)
        {
            foreach (var levee in all)
                if (DistanceToSegment(levee.transform.position, from, to) <= BlockRadius) return true;
            return false;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var p = new Vector2(point.x, point.z);
            var a2 = new Vector2(a.x, a.z);
            var ab = new Vector2(b.x, b.z) - a2;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a2, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a2 + ab * t);
        }
    }
}
