using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Dados de uma fase (nome, música, limites da câmera, tempo-alvo).
    /// Existe um em cada cena de fase; ao iniciar, avisa o GameManager e toca a música.
    /// </summary>
    public class LevelInfo : MonoBehaviour
    {
        [SerializeField] int levelIndex;
        [SerializeField] string displayName = "Fase";
        [SerializeField, TextArea(2, 4)] string introText = "";
        [SerializeField] AudioClip music;
        [Tooltip("Tempo (s) para ganhar bônus: cada segundo abaixo dele vale pontos.")]
        [SerializeField] float parTime = 120f;
        [SerializeField] Rect cameraBounds = new Rect(0, 0, 100, 20);

        public int LevelIndex => levelIndex;
        public string DisplayName => displayName;
        public string IntroText => introText;
        public float ParTime => parTime;
        public Rect CameraBounds => cameraBounds;

        void Awake()
        {
            // Garantia: se os colisores do Tilemap foram salvos sem forma, gera agora.
            foreach (var tc in FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>())
                tc.ProcessTilemapChanges();
            foreach (var cc in FindObjectsByType<CompositeCollider2D>())
                if (cc.pathCount == 0) cc.GenerateGeometry();
        }

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.BeginLevel(this);
            AudioManager.PlayMusic(music);
        }

        public int CountSeeds()
        {
            int count = 0;
            foreach (var c in FindObjectsByType<Collectible>())
                if (c.Type == CollectibleType.Seed) count++;
            return count;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
        }
    }
}
