using UnityEngine;

namespace EcoBot
{
    public enum FxType { Sparkle, Dust, Poof, Hurt, Confetti }

    /// <summary>
    /// Cria efeitos visuais (partículas e textos flutuantes) em qualquer ponto do mundo.
    /// Uso: Effects.Spawn(FxType.Sparkle, posicao);  Effects.Text("+100", posicao, cor);
    /// </summary>
    public class Effects : MonoBehaviour
    {
        static Effects instance;

        [SerializeField] GameObject sparkle;
        [SerializeField] GameObject dust;
        [SerializeField] GameObject poof;
        [SerializeField] GameObject hurt;
        [SerializeField] GameObject confetti;
        [SerializeField] FloatingText floatingText;

        void Awake()
        {
            if (instance == null) instance = this;
        }

        public static void Spawn(FxType type, Vector3 position)
        {
            if (instance == null) return;
            GameObject prefab = type switch
            {
                FxType.Sparkle => instance.sparkle,
                FxType.Dust => instance.dust,
                FxType.Poof => instance.poof,
                FxType.Hurt => instance.hurt,
                _ => instance.confetti
            };
            if (prefab != null) Instantiate(prefab, position, Quaternion.identity);
        }

        public static void Text(string text, Vector3 position, Color color)
        {
            if (instance == null || instance.floatingText == null) return;
            var ft = Instantiate(instance.floatingText, position, Quaternion.identity);
            ft.Setup(text, color);
        }
    }
}
