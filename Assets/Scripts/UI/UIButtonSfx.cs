using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoBot
{
    /// <summary>
    /// Feedback sonoro dos botões: "bip" ao navegar e som de confirmação ao clicar.
    /// Passar o mouse por cima também seleciona o botão (teclado e mouse ficam iguais).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSfx : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        static float lastMoveSound;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => AudioManager.Play(Sfx.UiSelect));
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Evita som duplicado quando um painel abre e já seleciona um botão.
            if (Time.unscaledTime - lastMoveSound < 0.08f) return;
            lastMoveSound = Time.unscaledTime;
            AudioManager.Play(Sfx.UiMove);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
