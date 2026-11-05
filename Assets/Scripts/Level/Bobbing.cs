using UnityEngine;

namespace EcoBot
{
    /// <summary>Faz um objeto flutuar suavemente para cima e para baixo (itens coletáveis).</summary>
    public class Bobbing : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.12f;
        [SerializeField] float frequency = 2.5f;

        Vector3 basePosition;
        float phase;

        void Start()
        {
            basePosition = transform.localPosition;
            phase = transform.position.x * 0.7f; // itens vizinhos não sobem juntos
        }

        void Update()
        {
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * frequency + phase) * amplitude);
        }
    }
}
