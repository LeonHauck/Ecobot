using UnityEngine;

namespace EcoBot
{
    public enum CollectibleType
    {
        Seed,       // semente: pontos (objetivo principal de coleta)
        Battery,    // bateria: recupera 1 de energia
        GoldenLeaf  // folha dourada: +1 vida
    }

    /// <summary>Item coletável: aplica o efeito, toca som, solta partículas e some.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] CollectibleType type = CollectibleType.Seed;
        [SerializeField] int points = 100;

        bool collected;

        public CollectibleType Type => type;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            var player = other.GetComponent<PlayerHealth>();
            if (player == null || player.IsDead) return;
            collected = true;

            var gm = GameManager.Instance;
            Vector3 textPos = transform.position + Vector3.up * 0.6f;
            switch (type)
            {
                case CollectibleType.Seed:
                    if (gm != null) gm.CollectSeed(points);
                    AudioManager.Play(Sfx.Collect);
                    Effects.Text("+" + points, textPos, new Color(1f, 0.88f, 0.35f));
                    break;

                case CollectibleType.Battery:
                    if (player.Heal(1))
                    {
                        AudioManager.Play(Sfx.Energy);
                        Effects.Text("+1 ENERGIA", textPos, new Color(0.5f, 1f, 0.6f));
                    }
                    else
                    {
                        // Energia cheia: vira pontos.
                        if (gm != null) gm.AddScore(points * 2);
                        AudioManager.Play(Sfx.Collect);
                        Effects.Text("+" + points * 2, textPos, new Color(0.5f, 1f, 0.6f));
                    }
                    break;

                case CollectibleType.GoldenLeaf:
                    if (gm != null) gm.AddLife();
                    AudioManager.Play(Sfx.ExtraLife);
                    Effects.Text("+1 VIDA", textPos, new Color(1f, 0.95f, 0.4f));
                    break;
            }

            Effects.Spawn(FxType.Sparkle, transform.position);
            Destroy(gameObject);
        }
    }
}
