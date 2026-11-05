using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>Tela final: encerra a história e mostra o resultado da partida.</summary>
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] Text resultText;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button menuButton;
        [SerializeField] Animator ecoAnimator;

        void Start()
        {
            Time.timeScale = 1f;
            AudioManager.PlayVictoryMusic();
            if (ecoAnimator != null) ecoAnimator.SetTrigger("Win");
            Effects.Spawn(FxType.Confetti, Vector3.up * 2f);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                resultText.text =
                    $"Pontuação final: {gm.Score:000000}\n" +
                    $"Sementes recuperadas: {gm.SeedsCollected}/{gm.SeedsAvailable}\n" +
                    (gm.NewRecord ? "NOVO RECORDE!" : $"Recorde: {gm.BestScore:000000}");
            }

            playAgainButton.onClick.AddListener(() => GameManager.Instance.NewGame());
            menuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playAgainButton.gameObject);
        }
    }
}
