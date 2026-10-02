using UnityEngine;
using UnityEngine.AI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Bầy sói đột kích (M6/D5): đêm xuống, cả bầy từ bìa rừng kéo thẳng vào làng, cắn người ở ngoài và vật nuôi,
    /// cào phá hàng rào chắn đường. Sợ ánh lửa (đống lửa trại, đuốc). Người biết đánh ra chặn. Sáng ra bầy rút về rừng.
    /// Bắt đầu ban ngày thì chờ tới tối mới kéo tới; hết sói (bị hạ hoặc đã rút) thì kết thúc.
    /// </summary>
    public class WolfRaidDisaster : DisasterEvent
    {
        [SerializeField] private PredatorData wolf;
        [SerializeField] private int basePackSize = 3;
        [Tooltip("Cứ chừng này ngày thì bầy đông thêm một con")]
        [SerializeField] private int daysPerExtraWolf = 3;
        [SerializeField] private int maxPackSize = 6;
        [Tooltip("Bầy xuất hiện cách làng chừng này mét")]
        [SerializeField] private float spawnDistance = 20f;

        private bool spawned;

        public bool HasArrived => spawned;
        public override bool CanHappen() => wolf != null && wolf.prefab != null;

        public override bool IsFinishedEarly => spawned && RaidersLeft() == 0;

        private static bool IsNight => DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;

        public static int RaidersLeft()
        {
            int count = 0;
            foreach (var predator in PredatorAI.All)
                if (predator.IsRaider) count++;
            return count;
        }

        public int PackSize()
        {
            int day = DayNightCycle.Instance != null ? DayNightCycle.Instance.Day : 1;
            return Mathf.Clamp(basePackSize + (day - 1) / Mathf.Max(1, daysPerExtraWolf), 1, maxPackSize);
        }

        public override void OnBegin()
        {
            spawned = RaidersLeft() > 0; // tải game giữa đợt: bầy đã được khôi phục cùng thú dữ
            if (!spawned && IsNight) Spawn();
        }

        public override void OnTick(float deltaTime, float progress)
        {
            if (!spawned && IsNight) Spawn();
        }

        public override void OnEnd()
        {
            spawned = false;
        }

        /// <summary>Bầy sói xuất hiện ở bìa rừng, kéo về phía làng.</summary>
        public void Spawn()
        {
            spawned = true;
            Vector3 village = Campfire.All.Count > 0 ? Campfire.All[0].transform.position : Vector3.zero;
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            Vector3 edge = village + new Vector3(dir.x, 0f, dir.y) * spawnDistance;

            int count = PackSize();
            for (int i = 0; i < count; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * 2f;
                Vector3 point = edge + new Vector3(jitter.x, 0f, jitter.y);
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 6f, NavMesh.AllAreas)) continue;
                GameObject go = Instantiate(wolf.prefab, hit.position, Quaternion.identity);
                go.name = $"RaidWolf_{i}";
                go.GetComponent<PredatorAI>()?.BeginRaid(village, hit.position);
            }
            SfxManager.Play(SfxKind.Howl, null, 0.6f); // tiếng hú dày hơn các tiếng khác
            EventBus.RaiseNotification($"Bầy {count} con sói đang kéo vào làng! Vào lều, đứng gần lửa, người biết đánh ra chặn!");
        }
    }
}
