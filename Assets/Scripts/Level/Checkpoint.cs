using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Estação de recarga: ao tocar, vira o novo ponto de renascimento
    /// e recarrega toda a energia do Eco.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] Animator animator;

        static readonly int ActiveHash = Animator.StringToHash("Active");
        static Checkpoint current;

        bool active;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (active) return;
            var player = other.GetComponent<PlayerHealth>();
            if (player == null || player.IsDead) return;

            if (current != null && current != this) current.SetActive(false);
            current = this;
            SetActive(true);

            player.SetCheckpoint(transform.position);
            player.Heal(player.MaxEnergy);
            AudioManager.Play(Sfx.Checkpoint);
            Effects.Spawn(FxType.Sparkle, transform.position + Vector3.up);
            Effects.Text("CHECKPOINT!", transform.position + Vector3.up * 2f, new Color(0.6f, 1f, 0.6f));
        }

        void SetActive(bool value)
        {
            active = value;
            if (animator != null) animator.SetBool(ActiveHash, value);
        }
    }
}
