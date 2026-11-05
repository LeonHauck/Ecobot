using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Comportamento comum dos inimigos: causar dano ao encostar no jogador
    /// e ser derrotado quando o jogador pisa em cima (se canBeStomped).
    /// O movimento fica em outro componente (PatrolMover ou FlyingMover).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] bool canBeStomped = true;
        [SerializeField] int damage = 1;
        [SerializeField] int scoreValue = 200;
        [SerializeField] Animator animator;
        [SerializeField] float destroyDelay = 0.6f;

        static readonly int DieHash = Animator.StringToHash("Die");

        Collider2D col;
        bool dead;

        public bool IsDead => dead;

        void Awake() => col = GetComponent<Collider2D>();

        void OnCollisionEnter2D(Collision2D c) => HandleContact(c.collider);
        void OnCollisionStay2D(Collision2D c) => HandleContact(c.collider);
        void OnTriggerEnter2D(Collider2D other) => HandleContact(other);

        void HandleContact(Collider2D other)
        {
            if (dead) return;
            var health = other.GetComponent<PlayerHealth>();
            if (health == null || health.IsDead) return;
            var player = other.GetComponent<PlayerController>();

            // Pisão: os pés do jogador estão acima do centro do inimigo.
            bool stomp = other.bounds.min.y >= col.bounds.center.y && player.VerticalVelocity <= 2f;
            if (canBeStomped && stomp)
            {
                player.Bounce();
                Defeat();
            }
            else
            {
                health.TakeDamage(damage, transform.position);
            }
        }

        public void Defeat()
        {
            if (dead) return;
            dead = true;
            col.enabled = false;
            foreach (var mover in GetComponents<EnemyMover>()) mover.enabled = false;
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;

            if (animator != null) animator.SetTrigger(DieHash);
            AudioManager.Play(Sfx.Stomp);
            Effects.Spawn(FxType.Poof, transform.position);
            Effects.Text("+" + scoreValue, transform.position + Vector3.up * 0.8f, new Color(1f, 0.9f, 0.5f));
            if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
            Destroy(gameObject, destroyDelay);
        }
    }
}
