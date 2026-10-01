using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Hạn hán (M6/D2): nắng gắt nhiều ngày — ruộng bốc hơi nhanh gấp <see cref="evaporationMultiplier"/>, ao cạn dần
    /// (mặt nước co lại, cá không sinh sôi, mương tự chảy chỉ tới vài ô và tưới chậm). Giếng (nước ngầm) và guồng nước
    /// không bị ảnh hưởng — đó là cách chống hạn.
    /// </summary>
    public class DroughtDisaster : DisasterEvent
    {
        [SerializeField] private float evaporationMultiplier = 3f;
        [Tooltip("Mặt ao co lại còn chừng này phần lúc hạn nặng nhất")]
        [SerializeField] private float pondShrinkTo = 0.55f;
        [Tooltip("Phần đầu/cuối thời lượng để ao cạn dần / đầy lại")]
        [SerializeField] private float rampFraction = 0.2f;

        private readonly Dictionary<Transform, Vector3> pondSurfaces = new Dictionary<Transform, Vector3>();
        private readonly List<ResourceNode> pausedFish = new List<ResourceNode>();
        private bool applied;

        public bool IsApplied => applied;

        public override void OnBegin()
        {
            Restore(); // phòng gọi lại khi đang hạn (vd tải game)
            applied = true;
            FarmPlot.EvaporationMultiplier = evaporationMultiplier;
            CanalNetwork.LowWater = true;

            pondSurfaces.Clear();
            pausedFish.Clear();
            foreach (var source in WaterSource.All)
            {
                if (!source.IsNatural || !source.FeedsPaddies) continue;
                Transform surface = source.transform.Find("Water");
                if (surface != null) pondSurfaces[surface] = surface.localScale;
                var fish = source.GetComponent<ResourceNode>();
                if (fish != null)
                {
                    fish.RegenPaused = true;
                    pausedFish.Add(fish);
                }
            }
        }

        public override void OnTick(float deltaTime, float progress)
        {
            // Ao cạn dần, giữ cạn, rồi đầy lại cuối đợt hạn.
            float ramp = Mathf.Max(0.01f, rampFraction);
            float severity = Mathf.Clamp01(Mathf.Min(progress / ramp, (1f - progress) / ramp));
            float scale = Mathf.Lerp(1f, pondShrinkTo, severity);
            foreach (var pair in pondSurfaces)
                if (pair.Key != null) pair.Key.localScale = new Vector3(pair.Value.x * scale, pair.Value.y, pair.Value.z * scale);
        }

        public override void OnEnd() => Restore();

        // Scene đóng giữa đợt hạn (thoát game, chạy test): trả lại các giá trị toàn cục.
        private void OnDisable() => Restore();

        private void Restore()
        {
            if (!applied) return;
            applied = false;
            FarmPlot.EvaporationMultiplier = 1f;
            CanalNetwork.LowWater = false;
            foreach (var pair in pondSurfaces)
                if (pair.Key != null) pair.Key.localScale = pair.Value;
            foreach (var fish in pausedFish)
                if (fish != null) fish.RegenPaused = false;
            pondSurfaces.Clear();
            pausedFish.Clear();
        }
    }
}
