using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Área perigosa (gatilho). Espinhos tiram energia; lodo tóxico e o fundo
    /// do abismo (instantKill) tiram uma vida inteira.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField] bool instantKill;
        [SerializeField] int damage = 1;

        void OnTriggerEnter2D(Collider2D other) => Hit(other);
        void OnTriggerStay2D(Collider2D other) => Hit(other);

        void Hit(Collider2D other)
        {
            var player = other.GetComponent<PlayerHealth>();
            if (player == null) return;
            if (instantKill) player.Kill();
            else player.TakeDamage(damage, transform.position);
        }
    }
}
