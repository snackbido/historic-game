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
            Vector3 step = moveInput * moveSpeed * Time.fixedDeltaTime;
            Vector3 next = body.position + step;
            // Địa hình đồi núi: vách quá dốc / nước sâu / mép bản đồ thì không bước tới — thử trượt dọc theo một trục
            // để không bị "dính" khi đi chéo vào sườn núi hay bờ sông.
            if (!WorldTerrain.IsWalkable(next.x, next.z))
            {
                Vector3 alongX = body.position + new Vector3(step.x, 0f, 0f);
                Vector3 alongZ = body.position + new Vector3(0f, 0f, step.z);
                next = WorldTerrain.IsWalkable(alongX.x, alongX.z) ? alongX
                    : WorldTerrain.IsWalkable(alongZ.x, alongZ.z) ? alongZ
                    : body.position;
            }
            body.linearVelocity = Vector3.zero; // va vào nhà/rào không bị đẩy trôi hay nảy lên
            body.MovePosition(WorldTerrain.Ground(next)); // bám mặt đất (leo dốc / xuống dốc)
        }

        public Vector3 GetPosition() => body.position;

        public void SetPosition(Vector3 position)
        {
            position = WorldTerrain.Ground(position);
            body.position = position;
            transform.position = position;
        }
    }
}
