using UnityEngine;

namespace EcoBot
{
    /// <summary>Texto que sobe e desaparece ("+100", "+1 VIDA"...).</summary>
    [RequireComponent(typeof(TextMesh))]
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.9f;
        [SerializeField] float riseSpeed = 1.6f;

        TextMesh textMesh;
        Color color;
        float age;

        void Awake() => textMesh = GetComponent<TextMesh>();

        public void Setup(string text, Color c)
        {
            textMesh.text = text;
            color = c;
            textMesh.color = c;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            float k = age / lifetime;
            textMesh.color = new Color(color.r, color.g, color.b, 1f - k * k);
            transform.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(Mathf.Min(k * 4f, 1f) * Mathf.PI));
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
