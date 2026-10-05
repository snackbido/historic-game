using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Cuộn UV dọc của mặt nước thác (LandscapeBuilder) để trông như nước đang chảy, không cần shader riêng.
    /// Dùng material riêng của chính thác (không dùng chung với biển/ao) nên không ảnh hưởng chỗ khác.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class WaterfallScroll : MonoBehaviour
    {
        [SerializeField] private float speed = 1.4f;
        private Renderer rend;
        private Material instanceMat;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            instanceMat = rend.material; // tạo bản sao instance — an toàn khi offset riêng
        }

        private void Update()
        {
            var offset = instanceMat.mainTextureOffset;
            offset.y -= speed * Time.deltaTime;
            instanceMat.mainTextureOffset = offset;
        }
    }
}
