using UnityEngine;
using UnityEngine.Tilemaps;

namespace EcoBot
{
    /// <summary>
    /// Marca um collider (gatilho) como escalável: cipós e escadas.
    /// O PlayerController procura este componente para entrar no modo escalada.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Climbable : MonoBehaviour
    {
        [Tooltip("Opcional: se o escalável é um Tilemap, centraliza o jogador na coluna do tile.")]
        [SerializeField] Tilemap tilemap;

        public float GetCenterX(Vector2 worldPosition)
        {
            if (tilemap != null)
                return tilemap.GetCellCenterWorld(tilemap.WorldToCell(worldPosition)).x;
            return GetComponent<Collider2D>().bounds.center.x;
        }
    }
}
