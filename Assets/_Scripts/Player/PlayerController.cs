using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Di chuyển trên mặt đất (mặt phẳng XZ) bằng WASD/mũi tên.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 12f;

        [Tooltip("Model hiển thị, xoay theo hướng đi (Rigidbody bị khóa xoay nên không xoay object gốc)")]
        [SerializeField] private Transform visual;

        private Rigidbody body;
        private Vector3 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (GameMenuUI.IsOpen)
            {
                moveInput = Vector3.zero;
                return;
            }
            moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            if (visual != null && moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion facing = Quaternion.LookRotation(moveInput);
                visual.rotation = Quaternion.Slerp(visual.rotation, facing, turnSpeed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        public Vector3 GetPosition() => body.position;

        public void SetPosition(Vector3 position)
        {
            body.position = position;
            transform.position = position;
        }
    }
}
