using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>
    /// Captura de telas.
    /// - F12 salva um print a qualquer momento (pasta "Prints" ao lado do executável, ou Docs/Prints no Editor).
    /// - Abrindo o jogo com "-tour &lt;pasta&gt;", ele percorre menu, fases e telas sozinho,
    ///   tira os prints usados na documentação e fecha.
    /// </summary>
    public class ScreenshotTool : MonoBehaviour
    {
        string outputDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return; // ferramenta só para Windows
            var go = new GameObject("ScreenshotTool");
            DontDestroyOnLoad(go);
            var tool = go.AddComponent<ScreenshotTool>();

            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-tour");
            if (i >= 0 && i + 1 < args.Length)
            {
                tool.outputDir = args[i + 1];
                tool.StartCoroutine(tool.Tour());
            }
            else
            {
                string root = Application.isEditor
                    ? Path.Combine(Application.dataPath, "..", "Docs")
                    : Path.GetDirectoryName(Application.dataPath);
                tool.outputDir = Path.Combine(root, "Prints");
            }
        }

        void Update()
        {
            if (GameInput.ScreenshotPressed) Capture("print_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        void Capture(string name)
        {
            Directory.CreateDirectory(outputDir);
            string path = Path.Combine(outputDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Print salvo: " + path);
        }

        // ------------------------------------------------------------ tour automático
        IEnumerator Tour()
        {
            AudioListener.volume = 0f;

            yield return Load("MainMenu", 2.5f);
            yield return Shot("01_menu");
            Click("Jogar");
            yield return Wait(1f);
            yield return Shot("02_historia");

            yield return Load("MainMenu", 1.5f);
            Click("ComoJogar");
            yield return Wait(1f);
            yield return Shot("03_como_jogar");

            yield return Load("Level1", 1.3f);
            yield return Shot("04_fase1_inicio");
            yield return Teleport(new Vector3(77.5f, 4.5f), 4.5f);
            yield return Shot("05_fase1_cipo");

            yield return Load("Level2", 0.5f);
            yield return Teleport(new Vector3(80.5f, 7.5f), 4.5f);
            yield return Shot("06_fase2_caverna");

            yield return Load("Level3", 0.5f);
            yield return Teleport(new Vector3(55.5f, 5.5f), 4.5f);
            yield return Shot("07_fase3_fabrica");

            yield return Load("Level4", 0.5f);
            yield return Teleport(new Vector3(8.5f, 53.5f), 4.5f);
            yield return Shot("08_fase4_chamine");

            GameManager.Instance.SetPaused(true);
            yield return Wait(0.6f);
            yield return Shot("09_pausa");
            GameManager.Instance.SetPaused(false);

            yield return Load("Level1", 0.5f);
            yield return Teleport(new Vector3(141.5f, 8.5f), 4f);
            yield return Teleport(new Vector3(144.5f, 8.5f), 3f);
            yield return Shot("10_fase_concluida");

            yield return Load("Victory", 2.5f);
            yield return Shot("11_vitoria");

            Application.Quit();
        }

        static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        static IEnumerator Load(string scene, float wait)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(wait);
        }

        static IEnumerator Teleport(Vector3 position, float wait)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.Respawn(position);
            yield return new WaitForSecondsRealtime(wait);
        }

        static void Click(string buttonName)
        {
            var go = GameObject.Find(buttonName);
            var button = go != null ? go.GetComponent<Button>() : null;
            if (button != null) button.onClick.Invoke();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Capture(name);
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
