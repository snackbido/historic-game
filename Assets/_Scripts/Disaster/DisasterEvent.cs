using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Một loại thiên tai (Milestone 6). Gắn lên cùng object với <see cref="DisasterManager"/>; manager chọn ngẫu nhiên
    /// theo <see cref="weight"/>, báo trước (<see cref="warningMessage"/>), rồi gọi Begin → Tick mỗi frame → End.
    /// Thời lượng tính theo ngày game.
    /// </summary>
    public abstract class DisasterEvent : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Dấu hiệu báo trước, vd \"Trời oi bức, ao bắt đầu cạn\"")]
        [SerializeField] private string warningMessage;
        [Tooltip("Mô tả khi đang diễn ra (hiện trên băng cảnh báo)")]
        [SerializeField] private string activeMessage;
        [SerializeField] private string endMessage;
        [Tooltip("Thời gian diễn ra (ngày game)")]
        [SerializeField] private float durationDays = 1f;
        [Tooltip("Tỉ lệ được chọn so với các thiên tai khác")]
        [SerializeField] private float weight = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string WarningMessage => warningMessage;
        public string ActiveMessage => activeMessage;
        public string EndMessage => endMessage;
        public float DurationDays => durationDays;
        public float Weight => weight;

        /// <summary>Có thể xảy ra lúc này không (vd lũ cần có ao).</summary>
        public virtual bool CanHappen() => true;

        /// <summary>Bắt đầu giai đoạn báo trước (dấu hiệu trên bản đồ, nếu có).</summary>
        public virtual void OnWarning() { }

        public abstract void OnBegin();

        /// <summary>Mỗi frame khi đang diễn ra. <paramref name="progress"/> 0..1 theo thời lượng.</summary>
        public abstract void OnTick(float deltaTime, float progress);

        /// <summary>Kết thúc (hết giờ, hoặc bị dừng sớm / tải game) — gỡ mọi ảnh hưởng tạm thời.</summary>
        public abstract void OnEnd();

        /// <summary>Kết thúc sớm trước hạn (vd đám cháy đã dập tắt hết).</summary>
        public virtual bool IsFinishedEarly => false;
    }
}
