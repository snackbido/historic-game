using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public enum DisasterPhase { None, Warning, Active }

    /// <summary>
    /// Lịch thiên tai (Milestone 6): ngày đầu yên ổn, sau đó cứ 2–3 ngày game lại có một thiên tai ngẫu nhiên.
    /// Mỗi thiên tai được báo trước ~nửa ngày (dấu hiệu + thông báo), rồi diễn ra trong thời lượng của nó.
    /// Thời gian đếm bằng giây game nên vẫn chạy cả khi chu kỳ ngày/đêm đứng yên.
    /// </summary>
    public class DisasterManager : MonoBehaviour
    {
        public static DisasterManager Instance { get; private set; }

        /// <summary>Tắt lịch ngẫu nhiên (test tự gọi <see cref="StartWarning"/>/<see cref="StartNow"/>).</summary>
        public static bool RandomEnabled { get; set; } = true;

        [Tooltip("Thiên tai đầu tiên sớm nhất sau chừng này ngày")]
        [SerializeField] private float firstDisasterAfterDays = 1.5f;
        [SerializeField] private float minDaysBetween = 2f;
        [SerializeField] private float maxDaysBetween = 3f;
        [Tooltip("Báo trước bao lâu (ngày game)")]
        [SerializeField] private float warningDays = 0.5f;

        public DisasterPhase Phase { get; private set; }
        public DisasterEvent Current { get; private set; }
        /// <summary>Giây còn lại của giai đoạn hiện tại (báo trước / đang diễn ra).</summary>
        public float PhaseSecondsLeft { get; private set; }
        public float SecondsUntilNext { get; private set; }

        private float phaseLength;

        public float DayLength => DayNightCycle.Instance != null ? DayNightCycle.Instance.DayLengthSeconds : 1200f;
        public IReadOnlyList<DisasterEvent> Events => GetComponents<DisasterEvent>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SecondsUntilNext = firstDisasterAfterDays * DayLength;
        }

        /// <summary>Hẹn thiên tai ngẫu nhiên kế tiếp sau chừng này giây game.</summary>
        public void ScheduleIn(float seconds) => SecondsUntilNext = Mathf.Max(0f, seconds);

        public DisasterEvent Find(string id)
        {
            foreach (var e in Events)
                if (e.Id == id) return e;
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (Phase)
            {
                case DisasterPhase.None:
                    if (!RandomEnabled) return;
                    SecondsUntilNext -= dt;
                    if (SecondsUntilNext > 0f) return;
                    DisasterEvent picked = PickRandom();
                    if (picked != null) StartWarning(picked);
                    else SecondsUntilNext = 0.25f * DayLength; // chưa có gì xảy ra được, thử lại sau
                    return;

                case DisasterPhase.Warning:
                    PhaseSecondsLeft -= dt;
                    if (PhaseSecondsLeft <= 0f) Begin(Current, Current.DurationDays * DayLength);
                    return;

                case DisasterPhase.Active:
                    PhaseSecondsLeft -= dt;
                    float progress = phaseLength > 0f ? 1f - Mathf.Clamp01(PhaseSecondsLeft / phaseLength) : 1f;
                    Current.OnTick(dt, progress);
                    if (PhaseSecondsLeft <= 0f || Current.IsFinishedEarly) EndCurrent();
                    return;
            }
        }

        private DisasterEvent PickRandom()
        {
            var candidates = new List<DisasterEvent>();
            float total = 0f;
            foreach (var e in Events)
            {
                if (e.Weight <= 0f || !e.CanHappen()) continue;
                candidates.Add(e);
                total += e.Weight;
            }
            float roll = Random.value * total;
            foreach (var e in candidates)
            {
                roll -= e.Weight;
                if (roll <= 0f) return e;
            }
            return candidates.Count > 0 ? candidates[candidates.Count - 1] : null;
        }

        /// <summary>Báo trước một thiên tai — sau <see cref="warningDays"/> nó sẽ ập tới.</summary>
        public void StartWarning(DisasterEvent disaster)
        {
            if (disaster == null || Phase != DisasterPhase.None) return;
            Current = disaster;
            Phase = DisasterPhase.Warning;
            PhaseSecondsLeft = warningDays * DayLength;
            disaster.OnWarning();
            EventBus.RaiseNotification($"Điềm báo: {disaster.WarningMessage}");
        }

        /// <summary>Cho thiên tai ập tới ngay (bỏ qua báo trước, hoặc khi đang báo trước thì vào luôn).</summary>
        public void StartNow(DisasterEvent disaster, float? durationSeconds = null)
        {
            if (disaster == null) return;
            if (Phase == DisasterPhase.Active) EndCurrent();
            Begin(disaster, durationSeconds ?? disaster.DurationDays * DayLength);
        }

        private void Begin(DisasterEvent disaster, float seconds)
        {
            Current = disaster;
            Phase = DisasterPhase.Active;
            phaseLength = Mathf.Max(0.01f, seconds);
            PhaseSecondsLeft = phaseLength;
            EventBus.RaiseNotification($"{disaster.DisplayName.ToUpperInvariant()}! {disaster.ActiveMessage}");
            disaster.OnBegin();
        }

        /// <summary>Kết thúc thiên tai đang diễn ra (hoặc hủy báo trước) và hẹn lần tiếp theo.</summary>
        public void EndCurrent()
        {
            DisasterEvent ended = Current;
            DisasterPhase phase = Phase;
            Current = null;
            Phase = DisasterPhase.None;
            PhaseSecondsLeft = 0f;
            SecondsUntilNext = Random.Range(minDaysBetween, maxDaysBetween) * DayLength;
            if (ended == null || phase != DisasterPhase.Active) return;
            ended.OnEnd();
            if (!string.IsNullOrEmpty(ended.EndMessage)) EventBus.RaiseNotification(ended.EndMessage);
        }

        // ─── Lưu / tải ───────────────────────────────────────────────────────
        public DisasterSaveData GetSaveData() => new DisasterSaveData
        {
            eventId = Current != null ? Current.Id : null,
            phase = (int)Phase,
            phaseSecondsLeft = PhaseSecondsLeft,
            phaseLength = phaseLength,
            secondsUntilNext = SecondsUntilNext,
            saved = true
        };

        public void LoadFromSaveData(DisasterSaveData data)
        {
            // Gỡ ảnh hưởng của thiên tai đang chạy trước khi khôi phục.
            if (Phase == DisasterPhase.Active && Current != null) Current.OnEnd();
            Current = null;
            Phase = DisasterPhase.None;
            PhaseSecondsLeft = 0f;
            if (data == null || !data.saved)
            {
                SecondsUntilNext = firstDisasterAfterDays * DayLength;
                return;
            }

            SecondsUntilNext = data.secondsUntilNext;
            DisasterEvent disaster = Find(data.eventId);
            if (disaster == null) return;
            Current = disaster;
            Phase = (DisasterPhase)data.phase;
            PhaseSecondsLeft = data.phaseSecondsLeft;
            phaseLength = data.phaseLength;
            if (Phase == DisasterPhase.Warning) disaster.OnWarning();
            else if (Phase == DisasterPhase.Active) disaster.OnBegin();
        }
    }

    [System.Serializable]
    public class DisasterSaveData
    {
        public bool saved; // save cũ thiếu cả khối này → false
        public string eventId;
        public int phase;
        public float phaseSecondsLeft;
        public float phaseLength;
        public float secondsUntilNext;
    }
}
