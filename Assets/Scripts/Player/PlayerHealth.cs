using System;
using System.Collections;
using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Energia (pontos de vida dentro de uma vida), invencibilidade temporária,
    /// morte e renascimento no último checkpoint.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] int maxEnergy = 3;
        [SerializeField] float invulnerableTime = 1.5f;
        [SerializeField] float respawnDelay = 1.3f;
        [SerializeField] SpriteRenderer spriteRenderer;

        /// <summary>(energia atual, energia máxima) — usado pelo HUD.</summary>
        public static event Action<int, int> EnergyChanged;

        public int Energy { get; private set; }
        public int MaxEnergy => maxEnergy;
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => invulnerableTimer > 0f;

        PlayerController controller;
        Vector3 respawnPoint;
        float invulnerableTimer;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Start()
        {
            respawnPoint = transform.position;
            Energy = maxEnergy;
            EnergyChanged?.Invoke(Energy, maxEnergy);
        }

        void Update()
        {
            if (invulnerableTimer <= 0f) return;
            invulnerableTimer -= Time.deltaTime;
            // Pisca enquanto está invencível (feedback visual de dano).
            spriteRenderer.enabled = invulnerableTimer <= 0f || Mathf.FloorToInt(invulnerableTimer * 14f) % 2 == 0;
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (IsDead || IsInvulnerable || controller.IsFrozen) return;

            Energy = Mathf.Max(0, Energy - amount);
            EnergyChanged?.Invoke(Energy, maxEnergy);
            AudioManager.Play(Sfx.Hurt);
            Effects.Spawn(FxType.Hurt, transform.position);
            CameraFollow.Shake(0.25f, 0.25f);

            if (Energy <= 0)
            {
                Die();
                return;
            }
            invulnerableTimer = invulnerableTime;
            controller.Hurt(sourcePosition);
        }

        /// <summary>Morte instantânea (queda no abismo, lodo tóxico).</summary>
        public void Kill()
        {
            if (IsDead || controller.IsFrozen) return;
            Energy = 0;
            EnergyChanged?.Invoke(Energy, maxEnergy);
            AudioManager.Play(Sfx.Hurt);
            CameraFollow.Shake(0.35f, 0.3f);
            Die();
        }

        void Die()
        {
            IsDead = true;
            invulnerableTimer = 0f;
            spriteRenderer.enabled = true;
            controller.Die();
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            bool hasLivesLeft = GameManager.Instance == null || GameManager.Instance.LoseLife();
            if (!hasLivesLeft) yield break; // game over: o HUD mostra o painel

            IsDead = false;
            Energy = maxEnergy;
            EnergyChanged?.Invoke(Energy, maxEnergy);
            controller.Respawn(respawnPoint);
            invulnerableTimer = invulnerableTime;
            Effects.Spawn(FxType.Sparkle, respawnPoint);
        }

        /// <summary>Recupera energia. Retorna false se já estava cheia.</summary>
        public bool Heal(int amount)
        {
            if (IsDead || Energy >= maxEnergy) return false;
            Energy = Mathf.Min(maxEnergy, Energy + amount);
            EnergyChanged?.Invoke(Energy, maxEnergy);
            return true;
        }

        public void SetCheckpoint(Vector3 position) => respawnPoint = position;
    }
}
