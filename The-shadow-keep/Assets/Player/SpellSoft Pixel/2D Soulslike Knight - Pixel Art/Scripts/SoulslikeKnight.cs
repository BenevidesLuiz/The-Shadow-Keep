using System.Collections;
using UnityEngine;

/// <summary>
/// SoulslikeKnight v5 — Integrado com SoulManager e PlayerStats.
///
/// MUDANÇAS v5:
///   - Morte notifica SoulManager (perde almas no chão)
///   - Dano de ataque é controlado externamente pelo PlayerStats
///   - MaxHealth pode ser reduzido pelo sacrifício de vida
///   - Setter público para lightDamage/heavyDamage (usado pelo PlayerStats)
///   - Propriedade MaxHealth exposta publicamente
/// </summary>
public class SoulslikeKnight : MonoBehaviour
{
    private enum State { Idle, Running, Jumping, Rolling, Attacking, Blocking, Healing, Hurt, Dead }
    private State currentState = State.Idle;

    private Rigidbody2D rb2D;
    private Animator animator;

    private const string IDLE = "Idle";
    private const string RUN = "Run";
    private const string JUMP = "Jump";
    private const string ROLL = "Roll";
    private const string HURT = "Hurt";
    private const string BLOCK = "Block";
    private const string DEATH = "Death";
    private const string ATTACK1 = "Attack1";
    private const string ATTACK2 = "Attack2";
    private const string ATTACK3 = "Attack3";
    private const string HEAL = "Heal";

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 1.2f;
    [SerializeField] private float staminaCostJump = 10f;
    [SerializeField] private float staminaCostRoll = 25f;
    [SerializeField] private float staminaCostRun = 6f;
    private float currentStamina;
    private float staminaRegenTimer;

    [Header("Estus / Pocao (tecla O)")]
    [SerializeField] private int maxEstusCharges = 5;
    [SerializeField] private float estusHealAmount = 40f;
    [SerializeField] private float estusDuration = 1.2f;
    private int currentEstusCharges;

    public event System.Action<int, int> OnEstusChanged;

    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float airControl = 0.7f;
    private float moveInput;

    [Header("Pulo")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private float groundCheckRadius = 0.2f;
    private bool isGrounded;

    [Header("Roll")]
    [SerializeField] private float rollForce = 11f;
    [SerializeField] private float rollDuration = 0.45f;

    [Header("Ataque Leve (P) — combo Attack1 > Attack2")]
    [SerializeField] private float attack1Duration = 0.5f;
    [SerializeField] private float attack2Duration = 0.5f;
    [SerializeField] private float comboWindowTime = 0.25f;
    [SerializeField] private float staminaCostLight = 12f;

    [Header("Ataque Forte (U) — golpe unico Attack3")]
    [SerializeField] private float attack3Duration = 0.7f;
    [SerializeField] private float staminaCostHeavy = 30f;
    [SerializeField] private float lightDamage = 10f;
    [SerializeField] private float heavyDamage = 16f;

    private int lightComboStep;
    private bool comboWindowOpen;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.6f;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnStaminaChanged;

    // ── Ref ao PlayerStats (opcional, no mesmo GameObject) ───────────────
    private PlayerStats playerStats;

    // ==================================================================== //
    //  PROPRIEDADES PÚBLICAS
    // ==================================================================== //

    public float MaxHealth => maxHealth;
    public int CurrentEstusCharges => currentEstusCharges;
    public int MaxEstusCharges => maxEstusCharges;
    public bool IsAttacking => currentState == State.Attacking;

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>(); // pode ser null, tudo bem

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentEstusCharges = maxEstusCharges;
        currentState = State.Idle;
    }

    private void Update()
    {
        if (currentState == State.Dead) return;
        CheckIfGrounded();
        ReadInput();
        HandleStaminaRegen();
        FlipSprite();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (currentState == State.Dead) return;
        ApplyMovement();
    }

    // ==================================================================== //
    //  INPUT
    // ==================================================================== //

    private void ReadInput()
    {
        // Fadiga bloqueia ações de ataque
        bool fatigued = playerStats != null && playerStats.isFatigued;

        moveInput = 0f;
        if (Input.GetKey(KeyCode.A)) moveInput = -1f;
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.E)) TryRoll();

        if (Input.GetKeyDown(KeyCode.Q)) TryBlock();
        if (Input.GetKeyUp(KeyCode.Q) && currentState == State.Blocking) ExitBlocking();

        if (!fatigued)
        {
            if (Input.GetKeyDown(KeyCode.P)) TryLightAttack();
            if (Input.GetKeyDown(KeyCode.U)) TryHeavyAttack();
        }

        if (Input.GetKeyDown(KeyCode.O)) TryHeal();
    }

    // ==================================================================== //
    //  MOVIMENTO
    // ==================================================================== //

    private void ApplyMovement()
    {
        switch (currentState)
        {
            case State.Rolling: return;
            case State.Blocking:
            case State.Hurt:
                rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
                return;
            case State.Dead:
                rb2D.linearVelocity = Vector2.zero;
                return;
            case State.Jumping:
                if (moveInput != 0f)
                {
                    bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    float airTarget = moveInput * (shiftHeld ? runSpeed : walkSpeed);
                    float newX = Mathf.Lerp(rb2D.linearVelocity.x, airTarget, airControl * 6f * Time.fixedDeltaTime);
                    rb2D.linearVelocity = new Vector2(newX, rb2D.linearVelocity.y);
                }
                return;
        }

        if (moveInput != 0f)
        {
            bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                             && currentStamina > 0;

            float speed = sprinting ? runSpeed : walkSpeed;
            if (currentState == State.Attacking) speed = walkSpeed * 0.3f;

            rb2D.linearVelocity = new Vector2(moveInput * speed, rb2D.linearVelocity.y);

            if (sprinting && currentState != State.Attacking)
                ConsumeStamina(staminaCostRun * Time.fixedDeltaTime);
        }
        else
        {
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        }
    }

    // ==================================================================== //
    //  PULO
    // ==================================================================== //

    private void TryJump()
    {
        if (!isGrounded) return;
        if (currentState != State.Idle && currentState != State.Running) return;
        if (currentStamina < staminaCostJump) return;

        StopAllCoroutines();
        ConsumeStamina(staminaCostJump);

        bool isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentStamina > 0;
        float horizSpeed = 0f;
        if (moveInput != 0f && isSprinting) horizSpeed = moveInput * runSpeed;
        else if (moveInput != 0f) horizSpeed = moveInput * walkSpeed;

        rb2D.linearVelocity = new Vector2(horizSpeed, jumpForce);
        animator.Play(JUMP);
        StartCoroutine(LandingWatcher());
    }

    private IEnumerator LandingWatcher()
    {
        yield return new WaitForSeconds(0.15f);
        float elapsed = 0f;
        while (!isGrounded && elapsed < 4f) { elapsed += Time.deltaTime; yield return null; }
        if (currentState == State.Jumping || currentState == State.Rolling) ChangeState(State.Idle);
    }

    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, groundCheckRadius, groundLayer);
    }

    // ==================================================================== //
    //  ROLL
    // ==================================================================== //

    private void TryRoll()
    {
        bool blocked = currentState == State.Dead || currentState == State.Blocking || currentState == State.Hurt;
        if (blocked) return;
        if (currentStamina < staminaCostRoll) return;

        StopAllCoroutines();
        ChangeState(State.Rolling);
        ConsumeStamina(staminaCostRoll);

        float dir = moveInput != 0f ? moveInput : GetFacingDirection();
        rb2D.linearVelocity = new Vector2(rollForce * dir, rb2D.linearVelocity.y);
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        animator.Play(ROLL);
        yield return new WaitForSeconds(rollDuration);
        if (isGrounded) rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        if (currentState == State.Rolling) ChangeState(isGrounded ? State.Idle : State.Jumping);
    }

    // ==================================================================== //
    //  BLOQUEIO
    // ==================================================================== //

    private void TryBlock()
    {
        if (currentState != State.Idle && currentState != State.Running) return;
        ChangeState(State.Blocking);
    }

    private void ExitBlocking() => ChangeState(State.Idle);

    // ==================================================================== //
    //  ATAQUE
    // ==================================================================== //

    private void TryLightAttack()
    {
        bool canAttack = currentState == State.Idle || currentState == State.Running ||
                         (currentState == State.Attacking && comboWindowOpen);
        if (!canAttack) return;
        if (currentStamina < staminaCostLight) return;

        StopAllCoroutines();
        comboWindowOpen = false;
        ChangeState(State.Attacking);
        ConsumeStamina(staminaCostLight);

        // Notifica PlayerStats (desgaste da lâmina)
        playerStats?.OnAttackPerformed();

        StartCoroutine(LightAttackRoutine());
    }

    private IEnumerator LightAttackRoutine()
    {
        string anim = (lightComboStep == 0) ? ATTACK1 : ATTACK2;
        float dur = (lightComboStep == 0) ? attack1Duration : attack2Duration;
        lightComboStep = (lightComboStep == 0) ? 1 : 0;

        animator.Play(anim);
        yield return new WaitForSeconds(dur * 0.55f);
        comboWindowOpen = true;
        yield return new WaitForSeconds(dur * 0.45f + comboWindowTime);

        if (currentState == State.Attacking)
        {
            lightComboStep = 0; comboWindowOpen = false;
            ChangeState(State.Idle);
        }
    }

    private void TryHeavyAttack()
    {
        bool canAttack = currentState == State.Idle || currentState == State.Running ||
                         (currentState == State.Attacking && comboWindowOpen);
        if (!canAttack) return;
        if (currentStamina < staminaCostHeavy) return;

        StopAllCoroutines();
        comboWindowOpen = false; lightComboStep = 0;
        ChangeState(State.Attacking);
        ConsumeStamina(staminaCostHeavy);

        // Notifica PlayerStats (desgaste da lâmina)
        playerStats?.OnAttackPerformed();

        StartCoroutine(HeavyAttackRoutine());
    }

    private IEnumerator HeavyAttackRoutine()
    {
        animator.Play(ATTACK3);
        yield return new WaitForSeconds(attack3Duration);
        if (currentState == State.Attacking) { comboWindowOpen = false; ChangeState(State.Idle); }
    }

    // ==================================================================== //
    //  HEAL
    // ==================================================================== //

    private void TryHeal()
    {
        if (currentState != State.Idle && currentState != State.Running) return;
        if (currentEstusCharges <= 0) return;
        if (currentHealth >= maxHealth) return;

        currentEstusCharges--;
        OnEstusChanged?.Invoke(currentEstusCharges, maxEstusCharges);

        ChangeState(State.Healing);
        StartCoroutine(HealRoutine());
    }

    private IEnumerator HealRoutine()
    {
        animator.Play(HEAL);
        yield return new WaitForSeconds(estusDuration);
        RestoreHealth(estusHealAmount);
        if (currentState == State.Healing) ChangeState(State.Idle);
    }

    public void RefillEstus()
    {
        currentEstusCharges = maxEstusCharges;
        OnEstusChanged?.Invoke(currentEstusCharges, maxEstusCharges);
    }

    // ==================================================================== //
    //  DANO E MORTE
    // ==================================================================== //

    public void TakeDamage(float amount)
    {
        if (currentState == State.Dead) return;
        if (currentState == State.Blocking) amount *= 0.2f;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) { Die(); return; }

        StopAllCoroutines();
        ChangeState(State.Hurt);
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        animator.Play(HURT);
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        yield return new WaitForSeconds(hurtDuration);
        if (currentState == State.Hurt) ChangeState(State.Idle);
    }

    private void Die()
    {
        StopAllCoroutines();
        ChangeState(State.Dead);
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(DEATH);

        // ── NOVO: notifica SoulManager das almas perdidas ──
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnPlayerDied(transform.position);

        StartCoroutine(DeathAndRespawn());
    }

    private IEnumerator DeathAndRespawn()
    {
        yield return new WaitForSeconds(2f);
        Bonfire.RespawnPlayerAtCheckpoint(this);
    }

    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentState == State.Dead && currentHealth > 0)
        {
            currentState = State.Idle;
            animator.Play(IDLE);
        }
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    /// <summary>
    /// Reduz a vida MÁXIMA permanentemente (usado pelo Sacrifício de Vida).
    /// </summary>
    public void ReduceMaxHealth(float amount)
    {
        maxHealth = Mathf.Max(10f, maxHealth - amount);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ==================================================================== //
    //  STAMINA
    // ==================================================================== //

    private void ConsumeStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        // Fadiga se stamina zerar durante sprint/combate intenso
        if (currentStamina <= 0f)
            playerStats?.TriggerFatigue();
    }

    private void HandleStaminaRegen()
    {
        if (currentStamina >= maxStamina) return;
        if (staminaRegenTimer > 0f) { staminaRegenTimer -= Time.deltaTime; return; }
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ==================================================================== //
    //  SETTERS DE DANO (usados pelo PlayerStats para aplicar modificadores)
    // ==================================================================== //

    public void SetLightDamage(float value) => lightDamage = value;
    public void SetHeavyDamage(float value) => heavyDamage = value;

    // ==================================================================== //
    //  STATE MACHINE / ANIMACAO
    // ==================================================================== //

    private void ChangeState(State newState) => currentState = newState;

    private void UpdateAnimation()
    {
        if (currentState == State.Rolling || currentState == State.Attacking) return;

        if (currentState == State.Idle || currentState == State.Running)
        {
            if (moveInput != 0f && isGrounded) ChangeState(State.Running);
            else if (moveInput == 0f && isGrounded) ChangeState(State.Idle);
        }

        bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentStamina > 0;

        switch (currentState)
        {
            case State.Idle: animator.speed = 1f; animator.Play(IDLE); break;
            case State.Running: animator.speed = sprinting ? 1.5f : 1f; animator.Play(RUN); break;
            case State.Blocking: animator.speed = 1f; animator.Play(BLOCK); break;
            default: animator.speed = 1f; break;
        }
    }

    // ==================================================================== //
    //  AUXILIARES
    // ==================================================================== //

    private float GetFacingDirection() => transform.localScale.x >= 0 ? 1f : -1f;

    public float GetLightDamage() => lightDamage;
    public float GetHeavyDamage() => heavyDamage;

    private void FlipSprite()
    {
        if (currentState == State.Attacking || currentState == State.Blocking || currentState == State.Dead) return;
        float reference = (currentState == State.Jumping && moveInput != 0f) ? moveInput : rb2D.linearVelocity.x;
        if (reference < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (reference > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // ==================================================================== //
    //  ANIMATION EVENTS
    // ==================================================================== //

    public void ReEnableInput()
    {
        if (currentState != State.Dead && currentState != State.Blocking) ChangeState(State.Idle);
    }

    public void EnableCanContinueAttackCombo() => comboWindowOpen = true;
    public void DisableCanContinueAttackCombo() { comboWindowOpen = false; lightComboStep = 0; }
    public void EnableComboWindow() => comboWindowOpen = true;
    public void EndAttack() { comboWindowOpen = false; lightComboStep = 0; ChangeState(State.Idle); }
    public void EndHeal() => ChangeState(State.Idle);

    // ==================================================================== //
    //  GIZMOS
    // ==================================================================== //

    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}