using UnityEngine;

namespace EcoBot
{
    /// <summary>Placa de dica: mostra uma mensagem no HUD enquanto o jogador está perto.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class TutorialSign : MonoBehaviour
    {
        [SerializeField, TextArea(2, 4)] string message = "";

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null && HUD.Instance != null)
                HUD.Instance.ShowHint(message);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null && HUD.Instance != null)
                HUD.Instance.HideHint(message);
        }
    }
}
