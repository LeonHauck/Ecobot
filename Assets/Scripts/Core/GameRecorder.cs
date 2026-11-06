using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace EcoBot
{
    /// <summary>
    /// Modo gravação de gameplay (para trailers):
    ///   EcoBot.exe -gravar &lt;pasta&gt; [-ffmpeg &lt;caminho&gt;] [-video 1920x1080] [-encoder libx264]
    ///
    /// - Trava o tempo do jogo em 60 quadros por segundo (Time.captureFramerate), então o vídeo
    ///   sai perfeito mesmo que o computador fique lento enquanto grava.
    /// - Envia cada quadro para o FFmpeg, que gera "gravacao.mp4".
    /// - Anota em "eventos.csv" os sons e músicas tocados, com o número do quadro,
    ///   para o áudio ser reconstruído em sincronia na edição do trailer.
    /// </summary>
    public class GameRecorder : MonoBehaviour
    {
        public const int Fps = 60;

        static GameRecorder instance;
        public static bool Active => instance != null;

        Process ffmpeg;
        Stream pipe;
        StreamWriter log;
        RenderTexture screenTexture, outputTexture;
        byte[] frameBuffer;
        bool hasFrame;
        int width = 1920, height = 1080;
        int capturedFrames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return; // ferramenta só para Windows
            string folder = Arg("-gravar");
            if (folder == null) return;
            if (folder == "") folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Gravacao");

            var go = new GameObject("GameRecorder");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameRecorder>();
            instance.Begin(folder, Arg("-ffmpeg") ?? "ffmpeg", Arg("-video"), Arg("-encoder") ?? "libx264");
        }

        /// <summary>Valor do argumento de linha de comando (null = ausente, "" = sem valor).</summary>
        static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            if (i < 0) return null;
            return i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : "";
        }

        void Begin(string folder, string ffmpegPath, string size, string encoder)
        {
            if (!string.IsNullOrEmpty(size))
            {
                var parts = size.ToLowerInvariant().Split('x');
                if (parts.Length == 2) { int.TryParse(parts[0], out width); int.TryParse(parts[1], out height); }
            }

            Time.captureFramerate = Fps;
            Application.targetFrameRate = Fps;
            QualitySettings.vSyncCount = 0;

            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "eventos.csv")) { AutoFlush = true };
            log.WriteLine("frame,tipo,valor,volume,pitch");

            outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            frameBuffer = new byte[width * height * 4];

            string quality = encoder == "libx264" ? "-preset ultrafast -crf 14" : "-global_quality 20";
            var info = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -hide_banner -loglevel error -f rawvideo -pix_fmt rgba -s {width}x{height} " +
                            $"-framerate {Fps} -i - -c:v {encoder} {quality} -pix_fmt yuv420p " +
                            $"\"{Path.Combine(folder, "gravacao.mp4")}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            try
            {
                ffmpeg = Process.Start(info);
                pipe = ffmpeg.StandardInput.BaseStream;
            }
            catch (Exception e)
            {
                Debug.LogError("GameRecorder: não foi possível iniciar o FFmpeg: " + e.Message);
                Destroy(gameObject);
                return;
            }

            AudioManager.SfxPlayed += OnSfx;
            AudioManager.MusicChanged += OnMusic;
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(CaptureLoop());
            Debug.Log($"GameRecorder: gravando {width}x{height} a {Fps} fps em {folder}");
        }

        // ------------------------------------------------------------ eventos
        /// <summary>Marca um momento na linha do tempo da gravação (usado pelo TrailerDirector).</summary>
        public static void Mark(string type, string value)
        {
            if (instance != null) instance.Write(type, value);
        }

        void Write(string type, string value, float volume = 0f, float pitch = 1f)
        {
            log?.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:0.###},{4:0.###}",
                capturedFrames, type, value, volume, pitch));
        }

        void OnSfx(AudioClip clip, float volume, float pitch) => Write("sfx", clip.name, volume, pitch);
        void OnMusic(AudioClip clip) => Write("music", clip != null ? clip.name : "", 0.45f);
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Write("scene", scene.name);

        // ------------------------------------------------------------ captura de quadros
        IEnumerator CaptureLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (pipe != null)
            {
                yield return endOfFrame;
                CaptureFrame();
            }
        }

        void CaptureFrame()
        {
            if (screenTexture == null || screenTexture.width != Screen.width || screenTexture.height != Screen.height)
            {
                if (screenTexture != null) screenTexture.Release();
                screenTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            }
            ScreenCapture.CaptureScreenshotIntoRenderTexture(screenTexture);
            Graphics.Blit(screenTexture, outputTexture);
            capturedFrames++;
            AsyncGPUReadback.Request(outputTexture, 0, TextureFormat.RGBA32, OnReadback);
        }

        void OnReadback(AsyncGPUReadbackRequest request)
        {
            if (pipe == null) return;
            // Se a leitura falhar, repete o quadro anterior para o vídeo não perder a sincronia.
            if (!request.hasError)
            {
                request.GetData<byte>().CopyTo(frameBuffer);
                hasFrame = true;
            }
            if (!hasFrame) return;
            try
            {
                pipe.Write(frameBuffer, 0, frameBuffer.Length);
            }
            catch (IOException e)
            {
                Debug.LogError("GameRecorder: o FFmpeg parou de receber quadros: " + e.Message);
                Finish();
            }
        }

        void OnApplicationQuit() => Finish();

        void Finish()
        {
            if (pipe == null) return;
            AsyncGPUReadback.WaitAllRequests();
            Write("end", "");
            AudioManager.SfxPlayed -= OnSfx;
            AudioManager.MusicChanged -= OnMusic;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            try { pipe.Close(); } catch (IOException) { }
            pipe = null;
            ffmpeg?.WaitForExit(30000);
            log?.Close();
            log = null;
        }
    }
}
