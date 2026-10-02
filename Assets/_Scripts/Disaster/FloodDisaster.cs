using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Lũ lụt (M6/D3): mưa lớn tưới đẫm mọi ruộng, ao tràn bờ — nước lan dần ra tới <see cref="maxFloodRange"/> mét quanh
    /// ao rồi rút. Trong vùng ngập: ruộng cạn ngập quá <see cref="drownSeconds"/> giây thì cây chết úng (ruộng nước chịu
    /// được), công trình mất máu dần. Đê nằm giữa ao và đối tượng thì chặn được nước.
    /// </summary>
    public class FloodDisaster : DisasterEvent
    {
        [Tooltip("Nước tràn xa nhất bao nhiêu mét tính từ mép ao (lúc lũ đỉnh)")]
        [SerializeField] private float maxFloodRange = 3.5f;
        [Tooltip("Phần đầu/cuối thời lượng để nước dâng dần / rút dần")]
        [SerializeField] private float rampFraction = 0.25f;
        [Tooltip("Ruộng cạn ngập liên tục chừng này giây thì cây chết úng")]
        [SerializeField] private float drownSeconds = 40f;
        [Tooltip("Công trình ngập mất chừng này phần máu tối đa mỗi phút")]
        [SerializeField] private float buildingDamagePerMinute = 0.12f;
        [Tooltip("Mưa tưới thêm cho mọi ruộng mỗi giây")]
        [SerializeField] private float rainPerSecond = 1f / 20f;
        [SerializeField] private Material floodWaterMaterial;

        private readonly Dictionary<FarmPlot, float> submerged = new Dictionary<FarmPlot, float>();
        private readonly List<GameObject> floodVisuals = new List<GameObject>();

        /// <summary>Nước đang tràn bao xa từ mép ao (m).</summary>
        public float FloodRange { get; private set; }

        public override bool CanHappen() => Ponds().Count > 0;

        private static List<WaterSource> Ponds()
        {
            var ponds = new List<WaterSource>();
            foreach (var source in WaterSource.All)
                if (source.IsNatural && source.FeedsPaddies) ponds.Add(source);
            return ponds;
        }

        /// <summary>Điểm này đang bị nước lũ tràn tới (và không có đê chắn).</summary>
        public bool IsFlooded(Vector3 position)
        {
            if (FloodRange <= 0f) return false;
            foreach (var pond in Ponds())
            {
                float edge = pond.DistanceToEdge(position);
                if (edge < 0f || edge > FloodRange) continue;
                Vector3 toPoint = position - pond.transform.position;
                toPoint.y = 0f;
                Vector3 shore = pond.transform.position + toPoint.normalized * pond.Radius;
                if (!Levee.Blocks(shore, position)) return true;
            }
            return false;
        }

        public override void OnBegin()
        {
            Clear();
            VfxManager.SetRain(true);
            foreach (var pond in Ponds())
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(disc.GetComponent<Collider>());
                disc.name = "FloodWater";
                disc.transform.SetParent(pond.transform, false);
                disc.transform.localPosition = new Vector3(0f, 0.015f, 0f);
                disc.transform.localScale = new Vector3(0f, 0.005f, 0f);
                var renderer = disc.GetComponent<Renderer>();
                if (floodWaterMaterial != null) renderer.sharedMaterial = floodWaterMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                floodVisuals.Add(disc);
            }
        }

        public override void OnTick(float deltaTime, float progress)
        {
            float ramp = Mathf.Max(0.01f, rampFraction);
            float severity = Mathf.Clamp01(Mathf.Min(progress / ramp, (1f - progress) / ramp));
            FloodRange = maxFloodRange * severity;

            foreach (var disc in floodVisuals)
            {
                if (disc == null) continue;
                var pond = disc.GetComponentInParent<WaterSource>();
                float diameter = 2f * (pond != null ? pond.Radius + FloodRange : FloodRange);
                disc.transform.localScale = new Vector3(diameter, 0.005f, diameter);
            }

            foreach (var plot in InteractableRegistry.All<FarmPlot>())
            {
                if (plot.State != FarmPlotState.Wild) plot.AddWater(rainPerSecond * deltaTime); // mưa lớn
                if (!IsFlooded(plot.transform.position))
                {
                    submerged.Remove(plot);
                    continue;
                }
                plot.SetWater(1f);
                if (plot.FieldType != FieldType.Dry) continue; // ruộng nước chịu ngập được
                submerged.TryGetValue(plot, out float seconds);
                seconds += deltaTime;
                submerged[plot] = seconds;
                if (seconds >= drownSeconds && plot.KillCrop("chết úng vì lũ")) submerged[plot] = 0f;
            }

            foreach (var building in BuildingInstance.All)
            {
                if (building.Health == null || building.IsCollapsed || !IsFlooded(building.transform.position)) continue;
                building.Damage(building.Health.Max * buildingDamagePerMinute / 60f * deltaTime, "lũ cuốn");
            }
        }

        public override void OnEnd() => Clear();

        private void OnDisable() => Clear();

        private void Clear()
        {
            FloodRange = 0f;
            VfxManager.SetRain(false);
            submerged.Clear();
            foreach (var disc in floodVisuals)
                if (disc != null) Destroy(disc);
            floodVisuals.Clear();
        }
    }
}
