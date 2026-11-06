using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>
    /// Piloto automático para gravar o trailer:
    ///   EcoBot.exe -trailer roteiro.txt [-turbo] [-log posicoes.csv]
    ///
    /// Lê um roteiro de cenas e "aperta os botões" pelo jogador (GameInput.Override).
    /// Formato do roteiro:
    ///   cena &lt;nome&gt; &lt;Cena&gt; [x,y] [recarregar]
    ///     R2      direita por 2 s          L@40    esquerda até x &lt;= 40
    ///     RSA@70  corre (S) para a direita com pulo automático (A) até x &gt;= 70
    ///     RJ0.4   direita segurando pulo   U@12    sobe (cipó/escada) até y &gt;= 12
    ///     N1      parado 1 s               N@chao  espera tocar o chão
    ///     clique:Jogar   aperta o botão "Jogar" da interface
    /// Com -turbo o jogo roda o mais rápido possível (o tempo de jogo continua em 60 quadros/s).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class TrailerDirector : MonoBehaviour, IInputOverride
    {
        class Command
        {
            public string text;
            public float h, v;
            public bool run, jump, auto;
            public float seconds = -1f;
            public bool untilGrounded;
            public bool hasTarget;
            public float target;
            public string click;
        }

        class Shot
        {
            public string name, scene;
            public bool hasPosition, reload;
            public Vector2 position;
            public readonly List<Command> commands = new List<Command>();
        }

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool RunHeld { get; private set; }

        readonly List<Shot> shots = new List<Shot>();
        StreamWriter positions;
        PlayerController player;
        PlayerHealth health;
        Collider2D playerCollider;
        string currentShot = "", currentCommand = "";
        float autoHold, autoCooldown;
        int startFrame;
        ContactFilter2D solidFilter, anyFilter;
        readonly List<RaycastHit2D> rayHits = new List<RaycastHit2D>();
        readonly List<Collider2D> overlaps = new List<Collider2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return; // ferramenta só para Windows
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-trailer");
            if (i < 0 || i + 1 >= args.Length) return;

            var go = new GameObject("TrailerDirector");
            DontDestroyOnLoad(go);
            var director = go.AddComponent<TrailerDirector>();
            int log = Array.IndexOf(args, "-log");
            string logPath = log >= 0 && log + 1 < args.Length
                ? args[log + 1]
                : Path.Combine(Path.GetDirectoryName(args[i + 1]) ?? ".", "posicoes.csv");
            director.Begin(args[i + 1], logPath, Array.IndexOf(args, "-turbo") >= 0);
        }

        void Begin(string scriptPath, string logPath, bool turbo)
        {
            Parse(File.ReadAllLines(scriptPath));
            if (!GameRecorder.Active) Time.captureFramerate = GameRecorder.Fps;
            if (turbo)
            {
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
            }
            AudioListener.volume = 0f;
            GameManager.AutoContinueEnabled = false;

            solidFilter = new ContactFilter2D { useTriggers = false };
            anyFilter = new ContactFilter2D { useTriggers = true };

            positions = new StreamWriter(logPath) { AutoFlush = false };
            positions.WriteLine("frame,cena,comando,x,y,vx,vy,chao,escalando,energia,vidas,morto");
            GameInput.Override = this;
            StartCoroutine(Run());
        }

        // ------------------------------------------------------------ roteiro
        void Parse(string[] lines)
        {
            Shot shot = null;
            foreach (string raw in lines)
            {
                string line = raw.Split('#')[0].Trim();
                if (line.Length == 0) continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] == "cena")
                {
                    shot = new Shot { name = parts[1], scene = parts[2] };
                    for (int k = 3; k < parts.Length; k++)
                    {
                        if (parts[k] == "recarregar") { shot.reload = true; continue; }
                        string[] xy = parts[k].Split(',');
                        shot.position = new Vector2(F(xy[0]), F(xy[1]));
                        shot.hasPosition = true;
                    }
                    shots.Add(shot);
                    continue;
                }
                if (shot == null) continue;
                foreach (string token in parts) shot.commands.Add(ParseCommand(token));
            }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        static Command ParseCommand(string token)
        {
            var c = new Command { text = token };
            if (token.StartsWith("clique:")) { c.click = token.Substring(7); c.seconds = 0.6f; return c; }

            int i = 0;
            for (; i < token.Length && char.IsLetter(token[i]); i++)
            {
                switch (token[i])
                {
                    case 'R': c.h = 1f; break;
                    case 'L': c.h = -1f; break;
                    case 'U': c.v = 1f; break;
                    case 'D': c.v = -1f; break;
                    case 'S': c.run = true; break;
                    case 'J': c.jump = true; break;
                    case 'A': c.auto = true; break;
                }
            }
            string rest = token.Substring(i);
            if (rest == "@chao") c.untilGrounded = true;
            else if (rest.StartsWith("@")) { c.hasTarget = true; c.target = F(rest.Substring(1)); }
            else c.seconds = rest.Length > 0 ? F(rest) : 0f;
            return c;
        }

        // ------------------------------------------------------------ execução
        IEnumerator Run()
        {
            startFrame = Time.frameCount;
            yield return null;

            foreach (var shot in shots)
            {
                currentShot = shot.name;
                if (shot.reload || SceneManager.GetActiveScene().name != shot.scene)
                {
                    ClearInput();
                    SceneManager.LoadScene(shot.scene);
                    yield return null;
                    yield return null;
                }
                player = FindAnyObjectByType<PlayerController>();
                health = player != null ? player.GetComponent<PlayerHealth>() : null;
                playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
                if (shot.hasPosition && player != null) player.Respawn(shot.position);

                GameRecorder.Mark("shot_start", shot.name);
                foreach (var command in shot.commands)
                {
                    currentCommand = command.text;
                    yield return Execute(command);
                }
                GameRecorder.Mark("shot_end", shot.name);
            }

            ClearInput();
            GameRecorder.Mark("done", "");
            positions.Close();
            positions = null;
            yield return null;
            Application.Quit();
        }

        IEnumerator Execute(Command c)
        {
            if (c.click != null)
            {
                var go = GameObject.Find(c.click);
                var button = go != null ? go.GetComponent<Button>() : null;
                if (button != null) button.onClick.Invoke();
            }

            float elapsed = 0f;
            bool first = true;
            while (true)
            {
                bool done;
                if (c.seconds >= 0f) done = elapsed >= c.seconds - 0.0001f;
                else if (c.untilGrounded) done = player == null || (!first && player.IsGrounded);
                else done = player == null || ReachedTarget(c);
                if (done || elapsed > 15f) break;

                autoHold -= Time.deltaTime;
                autoCooldown -= Time.deltaTime;
                Horizontal = c.h;
                Vertical = c.v;
                RunHeld = c.run;
                bool press = c.jump && first;
                if (c.auto && ShouldAutoJump(c.h))
                {
                    press = true;
                    autoHold = 0.4f;
                    autoCooldown = 0.25f;
                }
                JumpPressed = press;
                JumpHeld = c.jump || autoHold > 0f;

                LogPosition();
                first = false;
                elapsed += Time.deltaTime;
                yield return null;
            }
            JumpPressed = false;
        }

        bool ReachedTarget(Command c)
        {
            Vector2 p = player.transform.position;
            if (c.h > 0f) return p.x >= c.target;
            if (c.h < 0f) return p.x <= c.target;
            if (c.v > 0f) return p.y >= c.target;
            if (c.v < 0f) return p.y <= c.target;
            return true;
        }

        void ClearInput()
        {
            Horizontal = Vertical = 0f;
            JumpPressed = JumpHeld = RunHeld = false;
        }

        // ------------------------------------------------------------ pulo automático
        bool ShouldAutoJump(float dir)
        {
            if (player == null || playerCollider == null || dir == 0f) return false;
            if (!player.IsGrounded || player.IsClimbing || autoCooldown > 0f) return false;

            Bounds b = playerCollider.bounds;
            float feet = b.min.y;

            // buraco à frente
            if (!HitsSolid(new Vector2(b.center.x + dir * 0.7f, feet + 0.1f), Vector2.down, 1.5f)) return true;
            // parede/degrau à frente
            if (HitsSolid(new Vector2(b.center.x, feet + 0.3f), Vector2.right * dir, b.extents.x + 0.45f)) return true;
            // inimigo ou espinhos à frente
            Physics2D.OverlapBox(new Vector2(b.center.x + dir * 1.3f, feet + 0.5f), new Vector2(1.3f, 1.1f), 0f, anyFilter, overlaps);
            foreach (var o in overlaps)
            {
                var enemy = o.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead) return true;
                if (o.GetComponent<Hazard>() != null && o.GetComponent<TilemapCollider2D>() == null) return true;
            }
            return false;
        }

        bool HitsSolid(Vector2 origin, Vector2 dir, float distance)
        {
            Physics2D.Raycast(origin, dir, solidFilter, rayHits, distance);
            foreach (var h in rayHits)
            {
                if (h.collider == playerCollider) continue;
                if (h.collider.GetComponent<Enemy>() != null) continue;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ registro
        void LogPosition()
        {
            if (positions == null) return;
            int frame = Time.frameCount - startFrame;
            if (player == null)
            {
                positions.WriteLine($"{frame},{currentShot},{currentCommand},,,,,,,,,");
                return;
            }
            Vector2 p = player.transform.position;
            Vector2 v = player.Velocity;
            var gm = GameManager.Instance;
            positions.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3:0.00},{4:0.00},{5:0.0},{6:0.0},{7},{8},{9},{10},{11}",
                frame, currentShot, currentCommand, p.x, p.y, v.x, v.y,
                player.IsGrounded ? 1 : 0, player.IsClimbing ? 1 : 0,
                health != null ? health.Energy : 0, gm != null ? gm.Lives : 0,
                health != null && health.IsDead ? 1 : 0));
        }

        void OnApplicationQuit()
        {
            positions?.Close();
            positions = null;
        }
    }
}
