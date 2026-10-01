using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Guồng nước (Milestone 5d/F7): dựng sát mép ao, cạnh một đoạn mương đã đào. Guồng quay múc nước đổ vào mương
    /// → nước đi xa tới <see cref="CanalNetwork.PumpedReach"/> ô (tự chảy chỉ <see cref="CanalNetwork.GravityReach"/>)
    /// và ruộng sát mương được tưới nhanh gấp đôi. Chưa nối mương thì guồng đứng yên.
    /// </summary>
    public class WaterWheel : MonoBehaviour
    {
        private static readonly List<WaterWheel> all = new List<WaterWheel>();
        public static IReadOnlyList<WaterWheel> All => all;

        [Tooltip("Bánh guồng — quay quanh trục X cục bộ khi đang bơm nước")]
        [SerializeField] private Transform wheel;
        [SerializeField] private float degreesPerSecond = 45f;
        [Tooltip("Dòng nước đổ từ máng xuống mương, hiện khi guồng quay")]
        [SerializeField] private GameObject pouringWater;
        [Tooltip("Model guồng (máng chĩa theo trục +X) — xoay cho máng đổ về phía mương")]
        [SerializeField] private Transform model;

        public bool IsTurning { get; private set; }

        private BuildingInstance building;
        public Vector3Int Cell => building != null ? building.GridPosition : Vector3Int.zero;

        private void Awake()
        {
            building = GetComponent<BuildingInstance>();
            SetTurning(false);
        }

        private void OnEnable()
        {
            all.Add(this);
            CanalNetwork.MarkDirty();
        }

        private void OnDisable()
        {
            all.Remove(this);
            CanalNetwork.MarkDirty();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private void Update()
        {
            CanalNetwork.RecomputeIfDirty();
            if (IsTurning && wheel != null)
                wheel.Rotate(degreesPerSecond * Time.deltaTime, 0f, 0f, Space.Self);
        }

        internal void SetTurning(bool turning, Vector3Int towardCanal = default)
        {
            IsTurning = turning;
            if (turning && model != null && towardCanal != Vector3Int.zero)
                model.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(towardCanal.y, towardCanal.x) * Mathf.Rad2Deg, 0f);
            if (pouringWater != null) pouringWater.SetActive(turning);
        }
    }
}
