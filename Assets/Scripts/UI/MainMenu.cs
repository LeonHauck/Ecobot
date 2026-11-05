using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>Menu principal: Jogar (mostra a história antes), Como Jogar, Créditos e Sair.</summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject storyPanel;
        [SerializeField] GameObject howToPanel;
        [SerializeField] GameObject creditsPanel;

        [SerializeField] Button playButton;
        [SerializeField] Button howToButton;
        [SerializeField] Button creditsButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button startGameButton;
        [SerializeField] Button[] backButtons;
        [SerializeField] Text bestScoreText;

        void Start()
        {
            Time.timeScale = 1f;
            AudioManager.PlayMenuMusic();

            playButton.onClick.AddListener(() => Open(storyPanel, startGameButton));
            howToButton.onClick.AddListener(() => Open(howToPanel, backButtons[0]));
            creditsButton.onClick.AddListener(() => Open(creditsPanel, backButtons[1]));
            quitButton.onClick.AddListener(Quit);
            startGameButton.onClick.AddListener(() => GameManager.Instance.NewGame());
            foreach (var b in backButtons) b.onClick.AddListener(Back);

            int best = GameManager.Instance != null ? GameManager.Instance.BestScore : 0;
            bestScoreText.text = best > 0 ? $"Recorde: {best:000000}" : "";

            Open(mainPanel, playButton);
        }

        void Update()
        {
            if (!mainPanel.activeSelf && GameInput.CancelPressed) Back();
        }

        void Back()
        {
            Open(mainPanel, playButton);
        }

        void Open(GameObject panel, Button selected)
        {
            mainPanel.SetActive(panel == mainPanel);
            storyPanel.SetActive(panel == storyPanel);
            howToPanel.SetActive(panel == howToPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
