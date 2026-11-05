using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoBot
{
    /// <summary>Escurece a tela, troca de cena e clareia de novo (transição suave).</summary>
    public class SceneFader : MonoBehaviour
    {
        static SceneFader instance;

        [SerializeField] CanvasGroup group;
        [SerializeField] float fadeTime = 0.35f;

        bool busy;

        void Awake()
        {
            if (instance == null) instance = this;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        public static void FadeToScene(string sceneName)
        {
            if (instance == null || instance.group == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            if (!instance.busy) instance.StartCoroutine(instance.Fade(sceneName));
        }

        IEnumerator Fade(string sceneName)
        {
            busy = true;
            group.blocksRaycasts = true;
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / fadeTime;
                yield return null;
            }
            group.alpha = 1f;

            yield return SceneManager.LoadSceneAsync(sceneName);

            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fadeTime;
                yield return null;
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
            busy = false;
        }
    }
}
