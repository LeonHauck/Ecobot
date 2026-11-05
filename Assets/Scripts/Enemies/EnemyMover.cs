using UnityEngine;

namespace EcoBot
{
    /// <summary>Base dos componentes de movimento de inimigos (corpo cinemático).</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyMover : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [Tooltip("Marque se o desenho original do sprite olha para a direita.")]
        [SerializeField] protected bool spriteFacesRight = true;

        protected Rigidbody2D rb;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        protected void Face(float direction)
        {
            if (spriteRenderer == null || Mathf.Abs(direction) < 0.001f) return;
            spriteRenderer.flipX = spriteFacesRight ? direction < 0f : direction > 0f;
        }
    }
}
