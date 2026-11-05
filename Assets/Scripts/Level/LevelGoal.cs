using UnityEngine;

namespace EcoBot
{
    /// <summary>Muda Ancestral no fim da fase: ao tocar, o Eco comemora e a fase termina.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        bool reached;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (reached) return;
            var player = other.GetComponent<PlayerController>();
            var health = other.GetComponent<PlayerHealth>();
            if (player == null || (health != null && health.IsDead)) return;

            reached = true;
            player.Freeze(true);
            Effects.Spawn(FxType.Confetti, transform.position + Vector3.up);
            if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        }
    }
}
