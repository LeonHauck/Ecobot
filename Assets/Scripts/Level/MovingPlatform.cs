using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Plataforma que vai e volta entre dois pontos. Expõe a própria velocidade
    /// para o jogador "pegar carona" (ver PlayerController.MoveNormal).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;
        [SerializeField] float speed = 2.5f;
        [SerializeField] float waitTime = 0.6f;
        [Tooltip("Começa no ponto B (útil para alternar plataformas vizinhas).")]
        [SerializeField] bool startAtB;

        Rigidbody2D rb;
        Vector2 target;
        float waitTimer;

        public Vector2 Velocity { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Vector2 start = startAtB ? pointB : pointA;
            transform.position = start;
            rb.position = start;
            target = startAtB ? pointA : pointB;
        }

        void FixedUpdate()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                Velocity = Vector2.zero;
                return;
            }

            Vector2 pos = rb.position;
            Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime);
            Velocity = (next - pos) / Time.fixedDeltaTime;
            rb.MovePosition(next);

            if ((next - target).sqrMagnitude < 0.0001f)
            {
                target = target == pointA ? pointB : pointA;
                waitTimer = waitTime;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA, pointB);
            Gizmos.DrawWireCube(pointA, new Vector3(3f, 0.5f));
            Gizmos.DrawWireCube(pointB, new Vector3(3f, 0.5f));
        }
    }
}
