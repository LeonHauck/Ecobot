using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Câmera que segue o jogador com suavização, olha um pouco à frente na direção
    /// em que ele está virado, respeita os limites da fase e pode tremer (dano).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] float lookAhead = 2f;
        [SerializeField] bool useBounds = true;
        [SerializeField] Rect bounds = new Rect(0, 0, 100, 20);

        static CameraFollow instance;

        Camera cam;
        PlayerController player;
        Vector3 basePosition;
        Vector3 velocity;
        float currentLook;
        float shakeTimer, shakeDuration, shakeStrength;

        void Awake()
        {
            instance = this;
            cam = GetComponent<Camera>();
            if (target != null) player = target.GetComponent<PlayerController>();
        }

        void Start()
        {
            var level = FindAnyObjectByType<LevelInfo>();
            if (level != null) bounds = level.CameraBounds;
            Snap();
        }

        void LateUpdate()
        {
            if (target == null) return;

            float desiredLook = player != null ? (player.FacingRight ? lookAhead : -lookAhead) : 0f;
            currentLook = Mathf.Lerp(currentLook, desiredLook, 2.5f * Time.deltaTime);

            Vector3 goal = Goal();
            basePosition = Vector3.SmoothDamp(basePosition, goal, ref velocity, smoothTime);
            basePosition = Clamp(basePosition);

            Vector3 shake = Vector3.zero;
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float s = shakeStrength * (shakeTimer / shakeDuration);
                shake = new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0f);
            }
            transform.position = basePosition + shake;
        }

        Vector3 Goal()
        {
            var p = target.position + (Vector3)offset + Vector3.right * currentLook;
            p.z = transform.position.z;
            return p;
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!useBounds) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = bounds.width < halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            p.y = bounds.height < halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return p;
        }

        void Snap()
        {
            if (target == null) return;
            currentLook = 0f;
            basePosition = Clamp(Goal());
            velocity = Vector3.zero;
            transform.position = basePosition;
        }

        public static void SnapToTarget()
        {
            if (instance != null) instance.Snap();
        }

        public static void Shake(float strength, float duration)
        {
            if (instance == null) return;
            instance.shakeStrength = strength;
            instance.shakeDuration = duration;
            instance.shakeTimer = duration;
        }
    }
}
