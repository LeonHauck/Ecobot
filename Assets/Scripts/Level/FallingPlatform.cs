using System.Collections;
using UnityEngine;

namespace EcoBot
{
    /// <summary>Plataforma rachada: treme quando o jogador pisa, cai e reaparece depois.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FallingPlatform : MonoBehaviour
    {
        [SerializeField] float shakeTime = 0.5f;
        [SerializeField] float respawnTime = 3f;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite crackedSprite;

        Rigidbody2D rb;
        Collider2D col;
        Sprite normalSprite;
        Vector3 startPosition;
        bool triggered;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            startPosition = transform.position;
            if (spriteRenderer != null) normalSprite = spriteRenderer.sprite;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggered || collision.collider.GetComponent<PlayerController>() == null) return;
            // Só dispara quando o jogador pisa em cima.
            if (collision.collider.bounds.min.y < col.bounds.max.y - 0.1f) return;
            StartCoroutine(FallRoutine());
        }

        IEnumerator FallRoutine()
        {
            triggered = true;
            if (spriteRenderer != null && crackedSprite != null) spriteRenderer.sprite = crackedSprite;
            AudioManager.Play(Sfx.Crumble);

            for (float t = 0f; t < shakeTime; t += Time.deltaTime)
            {
                if (spriteRenderer != null)
                    spriteRenderer.transform.localPosition = new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f);
                yield return null;
            }
            if (spriteRenderer != null) spriteRenderer.transform.localPosition = Vector3.zero;

            Effects.Spawn(FxType.Dust, transform.position);
            col.enabled = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;

            yield return new WaitForSeconds(respawnTime);

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.position = startPosition;
            transform.position = startPosition;
            col.enabled = true;
            if (spriteRenderer != null) spriteRenderer.sprite = normalSprite;
            Effects.Spawn(FxType.Sparkle, startPosition);
            triggered = false;
        }
    }
}
