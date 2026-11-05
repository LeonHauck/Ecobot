using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Antes da primeira cena carregar, cria o objeto "Systems" (GameManager, AudioManager,
    /// Effects e SceneFader) a partir de Resources/Systems.prefab.
    /// Assim qualquer cena pode ser testada direto no Editor apertando Play.
    /// </summary>
    static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateSystems()
        {
            if (GameManager.Instance != null) return;
            var prefab = Resources.Load<GameObject>("Systems");
            if (prefab == null)
            {
                Debug.LogWarning("EcoBot: Resources/Systems.prefab não encontrado. Rode o menu 'EcoBot > 1. Gerar projeto completo'.");
                return;
            }
            var go = Object.Instantiate(prefab);
            go.name = "Systems";
        }
    }
}
