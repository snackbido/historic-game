using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Camera 2.5D: nhìn nghiêng cố định từ trên xuống, bám theo nhân vật, zoom bằng con lăn.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 8f;

        [Tooltip("Góc nhìn xuống (độ). 90 = nhìn thẳng từ trên xuống")]
        [SerializeField, Range(20f, 85f)] private float pitch = 52f;

        [SerializeField] private float distance = 13f;
        [SerializeField] private float minDistance = 7f;
        [SerializeField] private float maxDistance = 24f;
        [SerializeField] private float zoomStep = 1.2f;

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
                distance = Mathf.Clamp(distance - scroll * zoomStep, minDistance, maxDistance);

            Vector3 position = Vector3.Lerp(transform.position, DesiredPosition(), smoothSpeed * Time.deltaTime);
            transform.SetPositionAndRotation(position, Rotation);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.SetPositionAndRotation(DesiredPosition(), Rotation);
        }

        private Quaternion Rotation => Quaternion.Euler(pitch, 0f, 0f);

        private Vector3 DesiredPosition() => target.position - Rotation * Vector3.forward * distance;
    }
}
