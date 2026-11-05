using System.Collections.Generic;
using UnityEngine;

namespace EcoBot
{
    /// <summary>
    /// Movimento do Eco: andar, correr, pular (com coyote time, buffer e altura variável),
    /// escalar cipós/escadas, descer de plataformas e pegar carona em plataformas móveis.
    /// A física roda no FixedUpdate; a leitura de controles e a animação, no Update.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Andar e correr")]
        [SerializeField] float walkSpeed = 5.5f;
        [SerializeField] float runSpeed = 9f;
        [SerializeField] float groundAcceleration = 60f;
        [SerializeField] float groundDeceleration = 70f;
        [SerializeField] float airAcceleration = 40f;

        [Header("Pulo")]
        [SerializeField] float jumpHeight = 3.4f;
        [SerializeField] float gravityScale = 3.5f;
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [SerializeField] float maxFallSpeed = 20f;
        [Tooltip("Tempo extra para pular depois de sair da borda.")]
        [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Pulo apertado um pouco antes de tocar o chão ainda vale.")]
        [SerializeField] float jumpBufferTime = 0.12f;
        [Tooltip("Soltar o botão de pulo cedo corta a subida (pulo variável).")]
        [SerializeField, Range(0f, 1f)] float jumpCutMultiplier = 0.5f;
        [SerializeField] float stompBounceHeight = 2.2f;

        [Header("Escalada")]
        [SerializeField] float climbSpeed = 4f;
        [SerializeField] float climbSideSpeed = 2.5f;

        [Header("Dano")]
        [SerializeField] Vector2 knockback = new Vector2(6f, 9f);
        [SerializeField] float hurtControlLock = 0.35f;

        [Header("Visual")]
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Objeto pai do sprite, com pivô nos pés (usado no squash & stretch).")]
        [SerializeField] Transform visual;

        enum State { Normal, Climbing, Hurt, Dead, Frozen }

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int GroundedHash = Animator.StringToHash("Grounded");
        static readonly int VelYHash = Animator.StringToHash("VelY");
        static readonly int ClimbingHash = Animator.StringToHash("Climbing");
        static readonly int ClimbMoveHash = Animator.StringToHash("ClimbMove");
        static readonly int HurtHash = Animator.StringToHash("Hurt");
        static readonly int WinHash = Animator.StringToHash("Win");

        Rigidbody2D rb;
        BoxCollider2D box;
        State state = State.Normal;

        Vector2 input;
        bool runHeld;
        bool grounded;
        Collider2D groundCollider;
        bool isJumping;
        float coyoteTimer, jumpBufferTimer, groundLockTimer, climbCooldown, hurtTimer;
        float lowestFallSpeed;
        bool facingRight = true;
        Vector2 platformCarry;

        ContactFilter2D solidFilter, triggerFilter;
        readonly List<Collider2D> hits = new List<Collider2D>();
        readonly List<Collider2D> oneWayPlatforms = new List<Collider2D>();
        readonly HashSet<Collider2D> ignoredPlatforms = new HashSet<Collider2D>();
        float ignoreMinTimer;

        public bool FacingRight => facingRight;
        public bool IsFrozen => state == State.Frozen;
        public float VerticalVelocity => rb.linearVelocity.y;
        float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * jumpHeight);

        // ================================================================ ciclo de vida
        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            solidFilter = new ContactFilter2D { useTriggers = false };
            triggerFilter = new ContactFilter2D { useTriggers = true };
        }

        void Start()
        {
            // Plataformas de mão única (com PlatformEffector2D) — usadas para descer/escalar através delas.
            foreach (var effector in FindObjectsByType<PlatformEffector2D>())
                foreach (var c in effector.GetComponents<Collider2D>())
                    if (c.usedByEffector) oneWayPlatforms.Add(c);
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            bool canControl = state == State.Normal || state == State.Climbing;
            input = canControl ? new Vector2(GameInput.Horizontal, GameInput.Vertical) : Vector2.zero;
            runHeld = canControl && GameInput.RunHeld;

            if (canControl && GameInput.JumpPressed) jumpBufferTimer = jumpBufferTime;
            else jumpBufferTimer -= Time.deltaTime;

            coyoteTimer = grounded ? coyoteTime : coyoteTimer - Time.deltaTime;
            climbCooldown -= Time.deltaTime;

            // Pulo variável: soltar o botão durante a subida corta o pulo.
            if (isJumping && !GameInput.JumpHeld && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                isJumping = false;
            }

            if (state == State.Normal) TryStartClimb();

            if (input.x > 0.1f) facingRight = true;
            else if (input.x < -0.1f) facingRight = false;

            UpdateVisuals();
        }

        void FixedUpdate()
        {
            CheckGround();
            RestoreIgnoredPlatforms();

            switch (state)
            {
                case State.Normal: MoveNormal(); break;
                case State.Climbing: MoveClimbing(); break;
                case State.Hurt:
                    hurtTimer -= Time.fixedDeltaTime;
                    ApplyGravity();
                    if (hurtTimer <= 0f) state = State.Normal;
                    break;
                case State.Dead:
                    ApplyGravity();
                    break;
                case State.Frozen:
                    var v = rb.linearVelocity;
                    v.x = Mathf.MoveTowards(v.x, 0f, groundDeceleration * Time.fixedDeltaTime);
                    rb.linearVelocity = v;
                    ApplyGravity();
                    break;
            }
        }

        // ================================================================ chão
        void CheckGround()
        {
            bool wasGrounded = grounded;
            grounded = false;
            groundCollider = null;

            if (groundLockTimer > 0f)
            {
                groundLockTimer -= Time.fixedDeltaTime;
                return;
            }
            if (state == State.Dead) return;

            Bounds b = box.bounds;
            var point = new Vector2(b.center.x, b.min.y - 0.04f);
            var size = new Vector2(b.size.x * 0.9f, 0.08f);
            Physics2D.OverlapBox(point, size, 0f, solidFilter, hits);

            foreach (var h in hits)
            {
                if (h == box || h.attachedRigidbody == rb) continue;
                if (ignoredPlatforms.Contains(h)) continue;
                if (h.GetComponentInParent<Enemy>() != null) continue;
                bool isMoving = h.GetComponent<MovingPlatform>() != null;
                // Subindo por dentro de uma plataforma de mão única não conta como chão.
                if (!isMoving && rb.linearVelocity.y > 0.1f) continue;
                grounded = true;
                groundCollider = h;
                break;
            }

            if (grounded && !wasGrounded) OnLanded();
            if (!grounded) lowestFallSpeed = Mathf.Min(lowestFallSpeed, rb.linearVelocity.y);
        }

        void OnLanded()
        {
            isJumping = false;
            if (lowestFallSpeed < -8f)
            {
                Squash(new Vector3(1.3f, 0.75f, 1f));
                Effects.Spawn(FxType.Dust, Feet);
                AudioManager.Play(Sfx.Land);
            }
            lowestFallSpeed = 0f;
        }

        Vector3 Feet => new Vector3(box.bounds.center.x, box.bounds.min.y, 0f);

        // ================================================================ movimento normal
        void MoveNormal()
        {
            // Descer de plataforma de mão única: ↓ + pulo
            if (jumpBufferTimer > 0f && grounded && input.y < -0.5f && IsOneWay(groundCollider))
            {
                jumpBufferTimer = 0f;
                IgnorePlatform(groundCollider, 0.3f);
                grounded = false;
                groundLockTimer = 0.15f;
            }
            else if (jumpBufferTimer > 0f && (grounded || coyoteTimer > 0f))
            {
                Jump();
            }

            Vector2 carry = Vector2.zero;
            if (grounded && groundCollider != null)
            {
                var platform = groundCollider.GetComponent<MovingPlatform>();
                if (platform != null) carry = platform.Velocity;
            }

            float target = input.x * (runHeld ? runSpeed : walkSpeed);
            float accel = grounded
                ? (Mathf.Abs(target) > 0.01f ? groundAcceleration : groundDeceleration)
                : airAcceleration;

            Vector2 v = rb.linearVelocity;
            float relativeX = v.x - platformCarry.x;
            relativeX = Mathf.MoveTowards(relativeX, target, accel * Time.fixedDeltaTime);
            v.x = relativeX + carry.x;
            if (grounded && !isJumping && carry != Vector2.zero) v.y = carry.y;
            platformCarry = carry;

            rb.linearVelocity = v;
            ApplyGravity();
        }

        void Jump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            grounded = false;
            groundLockTimer = 0.1f;
            isJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, JumpVelocity);
            AudioManager.Play(Sfx.Jump);
            Effects.Spawn(FxType.Dust, Feet);
            Squash(new Vector3(0.75f, 1.25f, 1f));
        }

        void ApplyGravity()
        {
            float vy = rb.linearVelocity.y;
            rb.gravityScale = vy < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
            if (vy < -maxFallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }

        // ================================================================ escalada
        Climbable FindClimbable(Vector2 center, Vector2 size)
        {
            Physics2D.OverlapBox(center, size, 0f, triggerFilter, hits);
            foreach (var h in hits)
            {
                var c = h.GetComponent<Climbable>();
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>Procura cipó/escada na metade de baixo do corpo.</summary>
        Climbable ClimbableAtBody()
        {
            Bounds b = box.bounds;
            return FindClimbable(new Vector2(b.center.x, b.min.y + 0.25f), new Vector2(0.3f, 0.5f));
        }

        Climbable ClimbableBelowFeet()
        {
            Bounds b = box.bounds;
            return FindClimbable(new Vector2(b.center.x, b.min.y - 0.3f), new Vector2(0.3f, 0.2f));
        }

        void TryStartClimb()
        {
            if (climbCooldown > 0f || Mathf.Abs(input.y) < 0.5f) return;

            var c = ClimbableAtBody();
            if (c != null && (input.y > 0f || !grounded))
            {
                StartClimb();
                return;
            }
            if (input.y < 0f && grounded && ClimbableBelowFeet() != null) StartClimb();
        }

        void StartClimb()
        {
            state = State.Climbing;
            isJumping = false;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            platformCarry = Vector2.zero;
            foreach (var p in oneWayPlatforms) IgnorePlatform(p, 0.1f);
        }

        void StopClimb()
        {
            state = State.Normal;
            rb.gravityScale = gravityScale;
        }

        void MoveClimbing()
        {
            Climbable c = ClimbableAtBody();
            if (c == null && input.y < 0f) c = ClimbableBelowFeet();
            if (c == null)
            {
                StopClimb();
                return;
            }

            if (jumpBufferTimer > 0f)
            {
                StopClimb();
                climbCooldown = 0.25f;
                Jump();
                return;
            }

            if (grounded && input.y < 0f && !IsOneWay(groundCollider))
            {
                StopClimb();
                return;
            }

            Vector2 v = new Vector2(input.x * climbSideSpeed, input.y * climbSpeed);
            if (Mathf.Abs(input.x) < 0.1f)
                v.x = (c.GetCenterX(rb.position) - rb.position.x) * 10f; // centraliza no cipó
            rb.linearVelocity = v;
        }

        // ================================================================ plataformas de mão única
        bool IsOneWay(Collider2D c) => c != null && c.usedByEffector;

        void IgnorePlatform(Collider2D platform, float minTime)
        {
            if (platform == null) return;
            Physics2D.IgnoreCollision(box, platform, true);
            ignoredPlatforms.Add(platform);
            ignoreMinTimer = Mathf.Max(ignoreMinTimer, minTime);
        }

        /// <summary>Volta a colidir com as plataformas assim que o corpo sair de dentro delas.</summary>
        void RestoreIgnoredPlatforms()
        {
            if (ignoredPlatforms.Count == 0) return;
            ignoreMinTimer -= Time.fixedDeltaTime;
            if (ignoreMinTimer > 0f || state == State.Climbing) return;

            ignoredPlatforms.RemoveWhere(p =>
            {
                if (p == null) return true;
                if (box.Distance(p).isOverlapped) return false;
                Physics2D.IgnoreCollision(box, p, false);
                return true;
            });
        }

        // ================================================================ chamadas externas
        /// <summary>Quica depois de pisar num inimigo (segurar pulo = quique mais alto).</summary>
        public void Bounce()
        {
            float h = GameInput.JumpHeld ? jumpHeight : stompBounceHeight;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * h));
            isJumping = GameInput.JumpHeld;
            grounded = false;
            groundLockTimer = 0.05f;
            Squash(new Vector3(0.8f, 1.2f, 1f));
        }

        public void Hurt(Vector2 sourcePosition)
        {
            if (state == State.Dead || state == State.Frozen) return;
            if (state == State.Climbing) StopClimb();
            state = State.Hurt;
            hurtTimer = hurtControlLock;
            isJumping = false;
            float dir = Mathf.Sign(rb.position.x - sourcePosition.x);
            if (dir == 0f) dir = facingRight ? -1f : 1f;
            rb.linearVelocity = new Vector2(dir * knockback.x, knockback.y);
            animator.SetTrigger(HurtHash);
        }

        public void Die()
        {
            if (state == State.Climbing) StopClimb();
            state = State.Dead;
            box.enabled = false;            // atravessa o chão e cai para fora da tela
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(0f, 12f);
            animator.SetTrigger(HurtHash);
        }

        public void Respawn(Vector3 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = gravityScale;
            box.enabled = true;
            state = State.Normal;
            platformCarry = Vector2.zero;
            animator.ResetTrigger(HurtHash);
            animator.Play("Idle", 0, 0f);
            CameraFollow.SnapToTarget();
        }

        /// <summary>Trava o jogador (fim de fase) e toca a animação de comemoração.</summary>
        public void Freeze(bool celebrate)
        {
            if (state == State.Climbing) StopClimb();
            state = State.Frozen;
            if (celebrate) animator.SetTrigger(WinHash);
        }

        // ================================================================ visual
        void UpdateVisuals()
        {
            if (spriteRenderer != null) spriteRenderer.flipX = !facingRight;

            bool alive = state != State.Dead && state != State.Frozen;
            float speed = alive ? Mathf.Abs(rb.linearVelocity.x - platformCarry.x) : 0f;
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(GroundedHash, grounded || state == State.Frozen);
            animator.SetFloat(VelYHash, rb.linearVelocity.y);
            animator.SetBool(ClimbingHash, state == State.Climbing);
            animator.SetFloat(ClimbMoveHash, input.sqrMagnitude > 0.01f ? 1f : 0f);

            if (visual != null)
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, 12f * Time.deltaTime);
        }

        void Squash(Vector3 scale)
        {
            if (visual != null) visual.localScale = scale;
        }

        void OnDrawGizmosSelected()
        {
            var b = GetComponent<BoxCollider2D>();
            if (b == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.bounds.center.x, b.bounds.min.y - 0.04f), new Vector3(b.bounds.size.x * 0.9f, 0.08f));
        }
    }
}
