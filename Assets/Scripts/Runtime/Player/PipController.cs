using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LittleEmber.Audio;
using LittleEmber.Combat;
using LittleEmber.Core;
using LittleEmber.UI;

namespace LittleEmber.Player
{
    /// <summary>
    /// M0 core loop: 8-dir movement with 4-dir facing, 3-hit sword combo,
    /// dodge roll with iframes, shield block. Keyboard (WASD/arrows + Space/Shift)
    /// and on-screen touch controls both feed in (Active Input Handling = Both).
    ///
    /// Animator contract (generated controller):
    ///   float Speed (0..1), int Orientation (0=up, 2=left, 4=down, 6=right),
    ///   trigger Attack / Roll / Hit, bool Dead / Block / Carrying.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PipController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.5f;
        public float runSpeed = 6f;
        [Range(0.5f, 1f)] public float runThreshold = 0.85f;
        public float blockSpeedMultiplier = 0.45f;

        [Header("Attack")]
        public float attackLock = 0.34f;
        public float attackHitStart = 0.10f;
        public float attackHitEnd = 0.26f;
        public float attackReach = 1.1f;
        public Vector2 attackBoxSize = new Vector2(1.7f, 1.5f);
        public float comboChainWindow = 0.15f;

        [Header("Roll")]
        public float rollSpeed = 9.5f;
        public float rollTime = 0.32f;
        public float rollCooldown = 0.18f;

        [Header("Hurt")]
        public float hitStun = 0.28f;
        public float hurtInvulnerable = 0.6f;

        [Header("Wired by scene (touch UI)")]
        public VirtualJoystick joystick;
        public TouchButton attackButton;
        public TouchButton rollButton;
        public TouchButton blockButton;

        [Header("Save")]
        public bool persistState = true;

        /// <summary>Dialogue/cinematic lock: input is ignored while true.</summary>
        public bool controlsLocked;

        public int Orientation { get; private set; } = 4; // 0=up 2=left 4=down 6=right (pack convention)
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool IsBlocking => blocking;

        /// <summary>Fired when a swing actually starts (onboarding hints, juice hooks).</summary>
        public event System.Action AttackPressed;
        /// <summary>Fired when a roll actually starts (onboarding hints, juice hooks).</summary>
        public event System.Action RollPressed;

        enum State { Free, Attack, Roll, Hit, Dead }

        Rigidbody2D rb;
        Animator anim;
        Health health;

        State state = State.Free;
        Vector2 moveInput;
        Vector2 knockVelocity;
        Vector2 rollDir;
        float stateTimer;
        float rollCdTimer;
        float invulnTimer;
        bool attackDidHit;
        bool comboQueued;
        bool blocking;

        static readonly int P_Speed = Animator.StringToHash("Speed");
        static readonly int P_Orientation = Animator.StringToHash("Orientation");
        static readonly int P_Attack = Animator.StringToHash("Attack");
        static readonly int P_Roll = Animator.StringToHash("Roll");
        static readonly int P_Hit = Animator.StringToHash("Hit");
        static readonly int P_Dead = Animator.StringToHash("Dead");
        static readonly int P_Block = Animator.StringToHash("Block");

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            health = GetComponent<Health>();
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        float saveTimer;
        const float SaveInterval = 20f;

        void Start()
        {
            // door transitions override any saved position
            if (Core.SceneFlow.PendingSpawn.HasValue)
            {
                var p = Core.SceneFlow.PendingSpawn.Value;
                Core.SceneFlow.PendingSpawn = null;
                rb.position = p;
                transform.position = new Vector3(p.x, p.y, 0f);
                if (health != null && Core.SaveSystem.Current != null && Core.SaveSystem.Current.hearts > 0)
                    health.SetCurrent(Core.SaveSystem.Current.hearts);
                return;
            }
            // restore from active save slot (set by MainMenu continue/new-game flow)
            if (!persistState) return;
            var save = Core.SaveSystem.Current;
            if (save == null) return;
            if (save.posX != 0f || save.posY != 0f)
            {
                rb.position = new Vector2(save.posX, save.posY);
                transform.position = new Vector3(rb.position.x, rb.position.y, 0f);
            }
            if (health != null && save.hearts > 0)
            {
                health.maxHearts = Mathf.Max(1, save.maxHearts);
                health.SetCurrent(save.hearts);
            }
        }

        void TickAutosave()
        {
            if (!persistState) return;
            var save = Core.SaveSystem.Current;
            if (save == null) return;
            saveTimer += Time.deltaTime;
            if (saveTimer < SaveInterval) return;
            saveTimer = 0f;
            save.posX = rb.position.x;
            save.posY = rb.position.y;
            if (health != null)
            {
                save.hearts = health.current;
                save.maxHearts = health.maxHearts;
            }
            Core.SaveSystem.Save();
        }

        void Update()
        {
            if (state == State.Dead) { PushParams(0f); return; }

            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
            if (rollCdTimer > 0f) rollCdTimer -= Time.deltaTime;
            if (health != null) health.invulnerable = invulnTimer > 0f;

            ReadInput();
            UpdateFacing();

            switch (state)
            {
                case State.Free: TickFree(); break;
                case State.Attack: TickAttack(); break;
                case State.Roll: TickRoll(); break;
                case State.Hit: TickHit(); break;
            }

            PushParams(CurrentPlanarSpeed01());
            TickAutosave();
        }

        void FixedUpdate()
        {
            Vector2 vel = Vector2.zero;
            switch (state)
            {
                case State.Free:
                    float mag = moveInput.magnitude;
                    if (mag > 0.1f)
                    {
                        float speed = mag >= runThreshold ? runSpeed : walkSpeed;
                        if (blocking) speed *= blockSpeedMultiplier;
                        vel = moveInput.normalized * speed;
                    }
                    break;
                case State.Roll:
                    vel = rollDir * rollSpeed * RollCurve();
                    break;
            }
            vel += knockVelocity;
            knockVelocity = Vector2.Lerp(knockVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
            Vector2 before = rb.position;
            rb.MovePosition(before + vel * Time.fixedDeltaTime);
            TickFootsteps(before, before + vel * Time.fixedDeltaTime);
        }

        // ------------------------------------------------------------------ input

        void ReadInput()
        {
            if (controlsLocked)
            {
                moveInput = Vector2.zero;
                blocking = false;
                anim.SetBool(P_Block, false);
                // eat buffered presses so a tap made during dialogue doesn't fire on unlock
                if (attackButton != null) attackButton.ConsumePress();
                if (rollButton != null) rollButton.ConsumePress();
                return;
            }
            Vector2 keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 pad = joystick != null ? joystick.Value : Vector2.zero;
            moveInput = pad.sqrMagnitude > keys.sqrMagnitude ? pad : keys;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            bool attackPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)
                                 || (attackButton != null && attackButton.ConsumePress());
            bool rollPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K)
                               || (rollButton != null && rollButton.ConsumePress());
            blocking = Input.GetKey(KeyCode.L) || Input.GetMouseButton(1)
                       || (blockButton != null && blockButton.IsHeld);

            if (attackPressed) TryAttack();
            if (rollPressed) TryRoll();
        }

        void UpdateFacing()
        {
            if (moveInput.sqrMagnitude < 0.04f) return;
            if (Mathf.Abs(moveInput.y) >= Mathf.Abs(moveInput.x))
                SetFacing(moveInput.y > 0 ? 0 : 4, moveInput.y > 0 ? Vector2.up : Vector2.down);
            else
                SetFacing(moveInput.x < 0 ? 2 : 6, moveInput.x < 0 ? Vector2.left : Vector2.right);
        }

        void SetFacing(int orientation, Vector2 dir)
        {
            Orientation = orientation;
            Facing = dir;
        }

        // ------------------------------------------------------------------ states

        void TickFree()
        {
            anim.SetBool(P_Block, blocking);
        }

        void TryAttack()
        {
            if (state == State.Attack)
            {
                if (stateTimer >= attackLock * 0.5f) comboQueued = true; // buffer second half
                return;
            }
            if (state != State.Free) return;
            BeginSwing();
        }

        void BeginSwing()
        {
            state = State.Attack;
            stateTimer = 0f;
            attackDidHit = false;
            blocking = false;
            anim.SetBool(P_Block, false);
            anim.SetTrigger(P_Attack);
            AudioManager.PlaySfxName("swing", 0.85f);
            AttackPressed?.Invoke();
        }

        void TickAttack()
        {
            stateTimer += Time.deltaTime;
            if (!attackDidHit && stateTimer >= attackHitStart && stateTimer <= attackHitEnd)
            {
                attackDidHit = true;
                DoAttackHit();
            }
            if (stateTimer >= attackLock)
            {
                if (comboQueued) { comboQueued = false; BeginSwing(); }
                else state = State.Free;
            }
        }

        void DoAttackHit()
        {
            bool hitSomething = false;
            Vector2 center = rb.position + Facing * attackReach;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f);
            foreach (var h in hits)
            {
                if (h.attachedRigidbody != null && h.attachedRigidbody.gameObject == gameObject) continue;
                if (h.gameObject == gameObject) continue;
                var target = h.GetComponentInParent<IDamageable>();
                if (target == null) continue;
                target.ApplyDamage(1, Facing * 3f);
                hitSomething = true;
            }
            if (hitSomething)
            {
                AudioManager.PlaySfxName("hit");
                // micro hit-stop: reads as weight on a phone screen
                if (hitStop != null) StopCoroutine(hitStop);
                hitStop = StartCoroutine(HitStop(0.055f));
            }
        }

        Coroutine hitStop;

        IEnumerator HitStop(float t)
        {
            Time.timeScale = 0.08f;
            yield return new WaitForSecondsRealtime(t);
            Time.timeScale = 1f;
            hitStop = null;
        }

        void OnDisable()
        {
            // scene unload mid-hit-stop must never leave the game frozen
            if (Time.timeScale != 1f) Time.timeScale = 1f;
        }

        void TryRoll()
        {
            if (state != State.Free || rollCdTimer > 0f) return;
            state = State.Roll;
            stateTimer = 0f;
            rollDir = moveInput.sqrMagnitude > 0.04f ? moveInput.normalized : Facing;
            rollCdTimer = rollTime + rollCooldown;
            invulnTimer = Mathf.Max(invulnTimer, rollTime); // iframes
            blocking = false;
            anim.SetBool(P_Block, false);
            anim.SetTrigger(P_Roll);
            AudioManager.PlaySfxName("roll", 0.7f);
            RollPressed?.Invoke();
        }

        float RollCurve()
        {
            float t = Mathf.Clamp01(stateTimer / rollTime);
            return 1f - 0.5f * t; // fast start, soft end
        }

        void TickRoll()
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= rollTime) state = State.Free;
        }

        void TickHit()
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= hitStun) state = State.Free;
        }

        void OnDamaged(int amount, Vector2 knockback)
        {
            if (state == State.Dead) return;
            invulnTimer = hurtInvulnerable;
            state = State.Hit;
            stateTimer = 0f;
            comboQueued = false;
            blocking = false;
            knockVelocity = knockback * 2.2f;
            anim.SetBool(P_Block, false);
            anim.SetTrigger(P_Hit);
            AudioManager.PlaySfxName("hurt");
            SettingsData.Vibrate();
            CameraWork.CameraFollow2D.Shake(0.12f, 0.18f);
            if (hurtFlash != null) StopCoroutine(hurtFlash);
            hurtFlash = StartCoroutine(FlashSprite());
        }

        SpriteRenderer _sr;
        Coroutine hurtFlash;

        IEnumerator FlashSprite()
        {
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr == null) yield break;
            var c = _sr.color;
            _sr.color = new Color(3.2f, 0.9f, 0.9f, c.a); // hot red flash on the sprite material
            yield return new WaitForSeconds(0.09f);
            _sr.color = c;
            hurtFlash = null;
        }

        void OnDied()
        {
            state = State.Dead;
            moveInput = Vector2.zero;
            anim.SetBool(P_Block, false);
            anim.SetBool(P_Dead, true);
            AudioManager.PlaySfxName("die");
        }

        // ------------------------------------------------------------------ anim

        float CurrentPlanarSpeed01()
        {
            if (state == State.Roll) return 1f;
            if (state != State.Free) return 0f;
            return moveInput.magnitude;
        }

        void PushParams(float speed01)
        {
            anim.SetFloat(P_Speed, speed01);
            anim.SetInteger(P_Orientation, Orientation);
        }

        // ------------------------------------------------------------------ footsteps

        float _stepDist;
        const float StepStride = 0.85f; // world units per footstep

        void TickFootsteps(Vector2 from, Vector2 to)
        {
            if (state != State.Free) return;
            _stepDist += (to - from).magnitude;
            if (_stepDist < StepStride) return;
            _stepDist = 0f;
            bool indoors = SceneManager.GetActiveScene().name.StartsWith("Interior");
            AudioManager.PlaySfxName(indoors ? "step_wood" : "step_grass", 0.55f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var p = GetComponent<Rigidbody2D>() != null ? (Vector2)GetComponent<Rigidbody2D>().position : (Vector2)transform.position;
            Gizmos.DrawWireCube(p + Facing * attackReach, attackBoxSize);
        }
    }
}
