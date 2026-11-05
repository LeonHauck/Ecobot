using UnityEngine;

namespace EcoBot
{
    /// <summary>Voa em vai-e-vem entre dois pontos, balançando para cima e para baixo.</summary>
    public class FlyingMover : EnemyMover
    {
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;
        [SerializeField] float speed = 2f;
        [SerializeField] float bobAmplitude = 0.3f;
        [SerializeField] float bobFrequency = 2.5f;

        float travelled;
        float lastX;

        protected override void Awake()
        {
            base.Awake();
            if (pointA == pointB) pointA = pointB = transform.position;
            lastX = transform.position.x;
        }

        void FixedUpdate()
        {
            float length = Vector2.Distance(pointA, pointB);
            travelled += speed * Time.fixedDeltaTime;
            float t = length > 0.01f ? Mathf.PingPong(travelled, length) / length : 0f;
            Vector2 pos = Vector2.Lerp(pointA, pointB, t);
            pos.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            rb.MovePosition(pos);
            Face(pos.x - lastX);
            lastX = pos.x;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
