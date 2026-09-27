using UnityEngine;

namespace PrehistoricTribe
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        private Rigidbody2D body;
        private Vector2 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }

        private void FixedUpdate()
        {
            Vector2 isoDirection = IsometricUtility.InputToIsometric(moveInput);
            if (isoDirection.sqrMagnitude > 0.0001f) isoDirection.Normalize();
            body.MovePosition(body.position + isoDirection * moveSpeed * Time.fixedDeltaTime);
        }

        public Vector2 GetPosition() => body.position;

        public void SetPosition(Vector2 position)
        {
            body.position = position;
            transform.position = position;
        }
    }
}
