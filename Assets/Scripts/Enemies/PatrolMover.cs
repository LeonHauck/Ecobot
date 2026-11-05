using System.Collections.Generic;
using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Anda para um lado até encontrar uma parede, outro inimigo ou a borda
    /// de uma plataforma; então vira. Usa raycasts para "enxergar" o terreno.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PatrolMover : EnemyMover
    {
        [SerializeField] float speed = 1.6f;
        [SerializeField] bool startMovingLeft = true;

        Collider2D col;
        float direction;
        float turnCooldown;
        ContactFilter2D filter;
        readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

        protected override void Awake()
        {
            base.Awake();
            col = GetComponent<Collider2D>();
            direction = startMovingLeft ? -1f : 1f;
            filter = new ContactFilter2D { useTriggers = false };
        }

        void FixedUpdate()
        {
            Bounds b = col.bounds;
            turnCooldown -= Time.fixedDeltaTime;

            if (turnCooldown <= 0f)
            {
                bool wallAhead = HitsSolid(b.center, Vector2.right * direction, b.extents.x + 0.1f);
                var ledgeOrigin = new Vector2(b.center.x + direction * (b.extents.x + 0.1f), b.center.y);
                bool groundAhead = HitsSolid(ledgeOrigin, Vector2.down, b.extents.y + 0.35f);
                if (wallAhead || !groundAhead)
                {
                    direction = -direction;
                    turnCooldown = 0.2f;
                }
            }

            rb.MovePosition(rb.position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
            Face(direction);
        }

        bool HitsSolid(Vector2 origin, Vector2 dir, float distance)
        {
            Physics2D.Raycast(origin, dir, filter, hits, distance);
            foreach (var h in hits)
            {
                if (h.collider == col) continue;
                if (h.collider.GetComponent<PlayerHealth>() != null) continue; // o jogador não é parede
                return true;
            }
            return false;
        }
    }
}
