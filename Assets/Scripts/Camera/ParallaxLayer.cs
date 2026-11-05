using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Camada de fundo com efeito parallax: anda mais devagar que a câmera,
    /// dando sensação de profundidade. A imagem se repete infinitamente na horizontal.
    /// </summary>
    [DefaultExecutionOrder(100)] // roda depois da CameraFollow, evitando tremidas
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = parada no mundo (perto), 1 = presa à câmera (muito longe).")]
        [SerializeField, Range(0f, 1f)] float follow = 0.8f;
        [SerializeField] float verticalOffset;

        Transform cam;
        float tileWidth;

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            var sr = GetComponent<SpriteRenderer>();
            tileWidth = sr.sprite.bounds.size.x * transform.lossyScale.x;
        }

        void LateUpdate()
        {
            if (cam == null || tileWidth <= 0f) return;
            float drift = cam.position.x * (1f - follow);
            float offset = Mathf.Repeat(drift, tileWidth);
            transform.position = new Vector3(cam.position.x - offset, cam.position.y + verticalOffset, transform.position.z);
        }
    }
}
