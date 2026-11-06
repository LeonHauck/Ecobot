using System;
using System.Collections;
using UnityEngine;

namespace EcoBot
{
    /// <summary>Resumo de uma fase concluída (mostrado no painel de fim de fase).</summary>
    public struct LevelResult
    {
        public int levelIndex;
        public int seeds;
        public int seedsTotal;
        public float time;
        public int timeBonus;
        public int seedBonus;
        public bool isLastLevel;
    }

    /// <summary>
    /// Guarda o estado da partida (vidas, pontos, sementes, fase atual) e controla
    /// o fluxo entre cenas. É criado automaticamente (ver Bootstrap) e sobrevive
    /// à troca de cenas com DontDestroyOnLoad.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Desligado pelo piloto automático do trailer, para a fase não avançar sozinha.</summary>
        public static bool AutoContinueEnabled = true;

        public const string MenuScene = "MainMenu";
        public const string VictoryScene = "Victory";
        public static readonly string[] LevelScenes = { "Level1", "Level2", "Level3", "Level4" };
        const string BestScoreKey = "EcoBot_BestScore";

        [SerializeField] int startingLives = 3;
        [SerializeField] int maxLives = 9;
        [SerializeField] int allSeedsBonus = 1000;
        [SerializeField] int pointsPerSecondLeft = 10;
        [SerializeField] float autoContinueDelay = 7f;

        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int SeedsCollected { get; private set; }
        public int SeedsAvailable { get; private set; }
        public int SeedsInLevel { get; private set; }
        public int SeedsInLevelTotal { get; private set; }
        public int CurrentLevel { get; private set; } = -1;
        public float LevelTime { get; private set; }
        public bool IsPaused { get; private set; }
        public bool LevelRunning { get; private set; }
        public bool IsGameOver { get; private set; }
        public LevelInfo CurrentLevelInfo { get; private set; }
        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public bool NewRecord { get; private set; }

        public event Action StatsChanged;
        public event Action<LevelInfo> LevelStarted;
        public event Action<LevelResult> LevelCompleted;
        public event Action<bool> PauseChanged;
        public event Action GameOver;

        int scoreAtLevelStart, seedsAtLevelStart, availableAtLevelStart;
        Coroutine autoContinue;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetRun();
        }

        void Update()
        {
            if (LevelRunning && !IsPaused) LevelTime += Time.deltaTime;
            if (GameInput.PausePressed && LevelRunning && !IsGameOver) TogglePause();
        }

        // ------------------------------------------------------------ fluxo de cenas
        void ResetRun()
        {
            Lives = startingLives;
            Score = 0;
            SeedsCollected = 0;
            SeedsAvailable = 0;
            NewRecord = false;
        }

        public void NewGame()
        {
            ResetRun();
            LoadLevel(0);
        }

        public void LoadLevel(int index)
        {
            index = Mathf.Clamp(index, 0, LevelScenes.Length - 1);
            LoadScene(LevelScenes[index]);
        }

        /// <summary>Recomeça a fase atual. Pontos e sementes voltam ao valor do início da fase.</summary>
        public void RestartLevel(bool refillLives)
        {
            if (refillLives) Lives = startingLives;
            Score = scoreAtLevelStart;
            SeedsCollected = seedsAtLevelStart;
            SeedsAvailable = availableAtLevelStart;
            LoadLevel(Mathf.Max(0, CurrentLevel));
        }

        public void GoToMenu() => LoadScene(MenuScene);

        public void GoToNextLevel()
        {
            if (autoContinue != null) StopCoroutine(autoContinue);
            autoContinue = null;
            int next = CurrentLevel + 1;
            if (next >= LevelScenes.Length) LoadScene(VictoryScene);
            else LoadLevel(next);
        }

        public void LoadScene(string sceneName)
        {
            SetPaused(false);
            LevelRunning = false;
            IsGameOver = false;
            SceneFader.FadeToScene(sceneName);
        }

        // ------------------------------------------------------------ eventos da fase
        /// <summary>Chamado pelo LevelInfo quando uma fase começa.</summary>
        public void BeginLevel(LevelInfo info)
        {
            CurrentLevelInfo = info;
            CurrentLevel = info.LevelIndex;
            SeedsInLevel = 0;
            SeedsInLevelTotal = info.CountSeeds();
            LevelTime = 0f;
            LevelRunning = true;
            IsGameOver = false;
            scoreAtLevelStart = Score;
            seedsAtLevelStart = SeedsCollected;
            availableAtLevelStart = SeedsAvailable;
            LevelStarted?.Invoke(info);
            StatsChanged?.Invoke();
        }

        public void AddScore(int amount)
        {
            Score += amount;
            StatsChanged?.Invoke();
        }

        public void CollectSeed(int points)
        {
            SeedsInLevel++;
            SeedsCollected++;
            AddScore(points);
        }

        public void AddLife()
        {
            Lives = Mathf.Min(maxLives, Lives + 1);
            StatsChanged?.Invoke();
        }

        /// <summary>Remove uma vida. Retorna false se acabaram as vidas (game over).</summary>
        public bool LoseLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            StatsChanged?.Invoke();
            if (Lives > 0) return true;

            LevelRunning = false;
            IsGameOver = true;
            SaveBestScore();
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.GameOver);
            GameOver?.Invoke();
            return false;
        }

        public void CompleteLevel()
        {
            if (!LevelRunning) return;
            LevelRunning = false;

            float par = CurrentLevelInfo != null ? CurrentLevelInfo.ParTime : 120f;
            var result = new LevelResult
            {
                levelIndex = CurrentLevel,
                seeds = SeedsInLevel,
                seedsTotal = SeedsInLevelTotal,
                time = LevelTime,
                timeBonus = Mathf.Max(0, Mathf.CeilToInt(par - LevelTime)) * pointsPerSecondLeft,
                seedBonus = (SeedsInLevelTotal > 0 && SeedsInLevel >= SeedsInLevelTotal) ? allSeedsBonus : 0,
                isLastLevel = CurrentLevel >= LevelScenes.Length - 1
            };
            Score += result.timeBonus + result.seedBonus;
            SeedsAvailable += SeedsInLevelTotal;
            SaveBestScore();
            StatsChanged?.Invoke();
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.LevelComplete);
            LevelCompleted?.Invoke(result);
            autoContinue = StartCoroutine(AutoContinue());
        }

        IEnumerator AutoContinue()
        {
            if (!AutoContinueEnabled) yield break;
            yield return new WaitForSecondsRealtime(autoContinueDelay);
            autoContinue = null;
            GoToNextLevel();
        }

        // ------------------------------------------------------------ pausa e recorde
        public void TogglePause()
        {
            SetPaused(!IsPaused);
            AudioManager.Play(Sfx.Pause);
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(paused);
        }

        void SaveBestScore()
        {
            if (Score <= BestScore) return;
            PlayerPrefs.SetInt(BestScoreKey, Score);
            PlayerPrefs.Save();
            NewRecord = true;
        }
    }
}
