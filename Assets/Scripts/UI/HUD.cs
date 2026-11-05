using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>
    /// Interface da fase: vidas, energia, sementes, pontos, tempo, dicas,
    /// título da fase e os painéis de pausa, fim de fase e game over.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Barra superior")]
        [SerializeField] Text livesText;
        [SerializeField] Text scoreText;
        [SerializeField] Text seedsText;
        [SerializeField] Text timeText;
        [SerializeField] Text levelNameText;
        [SerializeField] Image[] energyIcons;
        [SerializeField] Sprite energyFull;
        [SerializeField] Sprite energyEmpty;
        [SerializeField] RectTransform seedIcon;

        [Header("Mensagens")]
        [SerializeField] CanvasGroup titleGroup;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField] Text hintText;

        [Header("Painéis")]
        [SerializeField] GameObject levelCompletePanel;
        [SerializeField] Text levelCompleteText;
        [SerializeField] Button continueButton;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] Text gameOverText;
        [SerializeField] Button retryButton;
        [SerializeField] Button gameOverMenuButton;
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button pauseMenuButton;

        int lastSeeds = -1, lastLives = -1, lastScore = -1;
        string currentHint;
        float titleTimer;

        void Awake()
        {
            Instance = this;
            levelCompletePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            pausePanel.SetActive(false);
            titleGroup.alpha = 0f;
            hintGroup.alpha = 0f;

            continueButton.onClick.AddListener(() => GameManager.Instance.GoToNextLevel());
            retryButton.onClick.AddListener(() => GameManager.Instance.RestartLevel(true));
            gameOverMenuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
            resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel(false));
            pauseMenuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
        }

        void OnEnable()
        {
            PlayerHealth.EnergyChanged += OnEnergyChanged;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.StatsChanged += Refresh;
            gm.LevelStarted += OnLevelStarted;
            gm.LevelCompleted += OnLevelCompleted;
            gm.GameOver += OnGameOver;
            gm.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            PlayerHealth.EnergyChanged -= OnEnergyChanged;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.StatsChanged -= Refresh;
            gm.LevelStarted -= OnLevelStarted;
            gm.LevelCompleted -= OnLevelCompleted;
            gm.GameOver -= OnGameOver;
            gm.PauseChanged -= OnPauseChanged;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                int t = Mathf.FloorToInt(gm.LevelTime);
                timeText.text = $"{t / 60}:{t % 60:00}";
            }

            // Título da fase aparece e some sozinho.
            if (titleTimer > 0f)
            {
                titleTimer -= Time.deltaTime;
                titleGroup.alpha = Mathf.Clamp01(titleTimer / 0.6f);
            }

            float targetHint = currentHint != null ? 1f : 0f;
            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, targetHint, 4f * Time.unscaledDeltaTime);
        }

        // ------------------------------------------------------------ atualização dos números
        void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            livesText.text = "x " + gm.Lives;
            scoreText.text = gm.Score.ToString("000000");
            seedsText.text = $"{gm.SeedsInLevel}/{gm.SeedsInLevelTotal}";

            if (lastSeeds >= 0 && gm.SeedsInLevel > lastSeeds) StartCoroutine(Punch(seedIcon, 1.5f));
            if (lastLives >= 0 && gm.Lives != lastLives) StartCoroutine(Punch(livesText.rectTransform, 1.4f));
            if (lastScore >= 0 && gm.Score > lastScore) StartCoroutine(Punch(scoreText.rectTransform, 1.15f));
            lastSeeds = gm.SeedsInLevel;
            lastLives = gm.Lives;
            lastScore = gm.Score;
        }

        void OnEnergyChanged(int energy, int max)
        {
            for (int i = 0; i < energyIcons.Length; i++)
            {
                energyIcons[i].gameObject.SetActive(i < max);
                energyIcons[i].sprite = i < energy ? energyFull : energyEmpty;
            }
        }

        IEnumerator Punch(RectTransform target, float scale)
        {
            const float duration = 0.25f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f + (scale - 1f) * Mathf.Sin(t / duration * Mathf.PI);
                target.localScale = new Vector3(k, k, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // ------------------------------------------------------------ eventos do jogo
        void OnLevelStarted(LevelInfo info)
        {
            levelNameText.text = info.DisplayName;
            titleText.text = $"FASE {info.LevelIndex + 1}";
            subtitleText.text = info.DisplayName + (string.IsNullOrEmpty(info.IntroText) ? "" : "\n<size=22>" + info.IntroText + "</size>");
            titleGroup.alpha = 1f;
            titleTimer = 4f;
        }

        void OnLevelCompleted(LevelResult r)
        {
            HideHintImmediate();
            int t = Mathf.FloorToInt(r.time);
            levelCompleteText.text =
                $"Sementes: {r.seeds}/{r.seedsTotal}\n" +
                $"Tempo: {t / 60}:{t % 60:00}\n" +
                $"Bônus de tempo: +{r.timeBonus}\n" +
                (r.seedBonus > 0 ? $"Todas as sementes! +{r.seedBonus}\n" : "") +
                $"\nPontuação: {GameManager.Instance.Score}";
            StartCoroutine(ShowPanelDelayed(levelCompletePanel, continueButton, 1.2f));
        }

        void OnGameOver()
        {
            HideHintImmediate();
            gameOverText.text = $"As baterias do Eco acabaram...\nPontuação: {GameManager.Instance.Score}";
            StartCoroutine(ShowPanelDelayed(gameOverPanel, retryButton, 1f));
        }

        void OnPauseChanged(bool paused)
        {
            pausePanel.SetActive(paused);
            if (paused) Select(resumeButton);
        }

        IEnumerator ShowPanelDelayed(GameObject panel, Button first, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            panel.SetActive(true);
            Select(first);
        }

        static void Select(Button b)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        // ------------------------------------------------------------ dicas (placas)
        public void ShowHint(string message)
        {
            currentHint = message;
            hintText.text = message;
        }

        public void HideHint(string message)
        {
            if (currentHint == message) currentHint = null;
        }

        void HideHintImmediate()
        {
            currentHint = null;
            hintGroup.alpha = 0f;
        }
    }
}
