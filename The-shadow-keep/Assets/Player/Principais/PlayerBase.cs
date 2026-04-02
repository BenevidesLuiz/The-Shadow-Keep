using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerBase — Classe base abstrata para todos os personagens jogáveis.
///
/// CONTÉM (lógica comum a TODOS os personagens):
///   - Vida, Stamina e regeneração
///   - Movimento (andar, correr, pulo, roll)
///   - Bloqueio
///   - Sistema de Estus (cura)
///   - Hurt e morte + respawn pela Bonfire
///   - Setters/getters de dano (usados pelo PlayerStats)
///   - Eventos OnHealthChanged, OnStaminaChanged, OnEstusChanged
///
/// NÃO CONTÉM (cada subclasse define):
///   - Input de ataque e habilidades especiais  → TryAttackLight / TryAttackHeavy / TrySpecialAbility
///   - Constantes de animação de ataque         → GetAttackAnimName / GetSpecialAnimName
///
/// SUBCLASSES:
///   - SoulslikeKnight  : guerreiro, combo de espada
///   - PaladinKnight    : paladino, golpe sagrado + bênção
///
/// SETUP NO UNITY:
///   1. Coloque a subclasse no GameObject do Player (não este script diretamente)
///   2. Adicione Rigidbody2D, Animator, Collider2D(s) e tag "Player"
///   3. Crie um filho "GroundCheck" e arraste em groundCheckTransform
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class PlayerBase : MonoBehaviour {

    // ------------------------------------------------------------------ //
    //  Estado interno
    // ------------------------------------------------------------------ //
    protected enum State { Idle, Running, Jumping, Rolling, Blocking, Healing, Hurt, Dead }
    protected State currentState = State.Idle;

    protected Rigidbody2D rb2D;
    protected Animator animator;

    // Constantes de animação compartilhadas
    protected const string ANIM_IDLE = "Idle";
    protected const string ANIM_RUN = "Run";
    protected const string ANIM_JUMP = "Jump";
    protected const string ANIM_ROLL = "Roll";
    protected const string ANIM_HURT = "Hurt";
    protected const string ANIM_BLOCK = "Block";
    protected const string ANIM_DEATH = "Death";
    protected const string ANIM_HEAL = "Heal";

    // ------------------------------------------------------------------ //
    //  Inspector — Vida
    // ------------------------------------------------------------------ //
    [Header("Vida")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;

    // ------------------------------------------------------------------ //
    //  Inspector — Stamina
    // ------------------------------------------------------------------ //
    [Header("Stamina")]
    [SerializeField] protected float maxStamina = 100f;
    [SerializeField] protected float staminaRegenRate = 15f;
    [SerializeField] protected float staminaRegenDelay = 1.2f;
    [SerializeField] protected float staminaCostJump = 10f;
    [SerializeField] protected float staminaCostRoll = 25f;
    [SerializeField] protected float staminaCostRun = 6f;
    protected float currentStamina;
    protected float staminaRegenTimer;

    // ------------------------------------------------------------------ //
    //  Inspector — Estus
    // ------------------------------------------------------------------ //
    [Header("Estus / Poção (tecla O)")]
    [SerializeField] protected int maxEstusCharges = 5;
    [SerializeField] protected float estusHealAmount = 40f;
    [SerializeField] protected float estusDuration = 1.2f;
    protected int currentEstusCharges;

    // ------------------------------------------------------------------ //
    //  Inspector — Movimento
    // ------------------------------------------------------------------ //
    [Header("Movimento")]
    [SerializeField] protected float walkSpeed = 4f;
    [SerializeField] protected float runSpeed = 8f;
    [SerializeField] protected float airControl = 0.7f;
    protected float moveInput;

    // ------------------------------------------------------------------ //
    //  Inspector — Pulo
    // ------------------------------------------------------------------ //
    [Header("Pulo")]
    [SerializeField] protected float jumpForce = 16f;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected Transform groundCheckTransform;
    [SerializeField] protected float groundCheckRadius = 0.2f;
    protected bool isGrounded;

    // ------------------------------------------------------------------ //
    //  Inspector — Roll
    // ------------------------------------------------------------------ //
    [Header("Roll")]
    [SerializeField] protected float rollForce = 11f;
    [SerializeField] protected float rollDuration = 0.45f;

    // ------------------------------------------------------------------ //
    //  Inspector — Custo de stamina nos ataques
    // ------------------------------------------------------------------ //
    [Header("Custo de Stamina nos Ataques")]
    [SerializeField] protected float staminaCostLight = 12f;
    [SerializeField] protected float staminaCostHeavy = 30f;

    // ------------------------------------------------------------------ //
    //  Inspector — Dano base
    // ------------------------------------------------------------------ //
    [Header("Dano")]
    [SerializeField] protected float lightDamage = 10f;
    [SerializeField] protected float heavyDamage = 16f;

    // ------------------------------------------------------------------ //
    //  Inspector — Hurt
    // ------------------------------------------------------------------ //
    [Header("Hurt")]
    [SerializeField] protected float hurtDuration = 0.6f;

    // ------------------------------------------------------------------ //
    //  Ref ao PlayerStats (opcional)
    // ------------------------------------------------------------------ //
    protected PlayerStats playerStats;

    // ================================================================== //
    //  PROPRIEDADES PÚBLICAS
    // ================================================================== //

    public float MaxHealth => maxHealth;
    public int CurrentEstusCharges => currentEstusCharges;
    public int MaxEstusCharges => maxEstusCharges;

    /// <summary>True enquanto a animação de cura (Estus) está tocando.</summary>
    public bool IsHealing => currentState == State.Healing;

    /// <summary>True se o personagem é um SoulslikeKnight (Guerreiro).</summary>
    public bool IsWarrior => this is SoulslikeKnight;

    /// <summary>
    /// True enquanto uma animação de ataque está tocando.
    /// Setado pela subclasse (ou pelo PlayerCombat via SendMessage).
    /// </summary>
    public bool IsAttacking { get; protected set; }

    // Eventos
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnStaminaChanged;
    public event System.Action<int, int> OnEstusChanged;

    // ================================================================== //
    //  UNITY CALLBACKS
    // ================================================================== //

    protected virtual void Awake() {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
    }

    protected virtual void Start() {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentEstusCharges = maxEstusCharges;
        currentState = State.Idle;
    }

    protected virtual void Update() {
        if (currentState == State.Dead) return;
        CheckIfGrounded();
        ReadBaseInput();
        ReadCombatInput();
        HandleStaminaRegen();
        FlipSprite();
        UpdateAnimation();
    }

    protected virtual void FixedUpdate() {
        if (currentState == State.Dead) return;
        ApplyMovement();
    }

    // ================================================================== //
    //  INPUT
    // ================================================================== //

    /// <summary>Lê movimento, pulo, roll, bloqueio e cura — comum a todos.</summary>
    private void ReadBaseInput() {
        moveInput = 0f;
        if (Input.GetKey(KeyCode.A)) moveInput = -1f;
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.E)) TryRoll();
        if (Input.GetKeyDown(KeyCode.Q)) TryBlock();
        if (Input.GetKeyUp(KeyCode.Q) && currentState == State.Blocking) ExitBlocking();
        if (Input.GetKeyDown(KeyCode.O)) TryHeal();
    }

    /// <summary>
    /// Leia input de ataque e habilidades da subclasse.
    /// Implemente este método em SoulslikeKnight, PaladinKnight, etc.
    /// </summary>
    protected abstract void ReadCombatInput();

    // ================================================================== //
    //  MOVIMENTO
    // ================================================================== //

    protected virtual void ApplyMovement() {
        switch (currentState) {
            case State.Rolling:
                return;
            case State.Blocking:
            case State.Hurt:
                rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
                return;
            case State.Dead:
                rb2D.linearVelocity = Vector2.zero;
                return;
            case State.Jumping:
                if (moveInput != 0f) {
                    bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    float airTarget = moveInput * (shiftHeld ? runSpeed : walkSpeed);
                    float newX = Mathf.Lerp(rb2D.linearVelocity.x, airTarget, airControl * 6f * Time.fixedDeltaTime);
                    rb2D.linearVelocity = new Vector2(newX, rb2D.linearVelocity.y);
                }
                return;
        }

        if (moveInput != 0f) {
            bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                             && currentStamina > 0;
            float speed = sprinting ? runSpeed : walkSpeed;
            if (IsAttacking) speed = walkSpeed * 0.3f;

            rb2D.linearVelocity = new Vector2(moveInput * speed, rb2D.linearVelocity.y);

            if (sprinting && !IsAttacking)
                ConsumeStamina(staminaCostRun * Time.fixedDeltaTime);
        }
        else {
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        }
    }

    // ================================================================== //
    //  PULO
    // ================================================================== //

    protected void TryJump() {
        if (!isGrounded) return;
        if (IsAttacking) return;
        if (currentState != State.Idle && currentState != State.Running) return;
        if (currentStamina < staminaCostJump) return;

        StopAllCoroutines();
        ConsumeStamina(staminaCostJump);

        bool isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                           && currentStamina > 0;
        float horizSpeed = 0f;
        if (moveInput != 0f && isSprinting) horizSpeed = moveInput * runSpeed;
        else if (moveInput != 0f) horizSpeed = moveInput * walkSpeed;

        rb2D.linearVelocity = new Vector2(horizSpeed, jumpForce);
        ChangeState(State.Jumping);
        animator.Play(ANIM_JUMP);
        StartCoroutine(LandingWatcher());
    }

    private IEnumerator LandingWatcher() {
        yield return new WaitForSeconds(0.15f);
        float elapsed = 0f;
        while (!isGrounded && elapsed < 4f) { elapsed += Time.deltaTime; yield return null; }
        if (currentState == State.Jumping || currentState == State.Rolling) ChangeState(State.Idle);
    }

    protected void CheckIfGrounded() {
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, groundCheckRadius, groundLayer);
    }

    // ================================================================== //
    //  ROLL
    // ================================================================== //

    protected void TryRoll() {
        if (currentState == State.Dead || currentState == State.Blocking ||
            currentState == State.Hurt) return;
        if (currentStamina < staminaCostRoll) return;

        StopAllCoroutines();
        ChangeState(State.Rolling);
        ConsumeStamina(staminaCostRoll);

        float dir = moveInput != 0f ? moveInput : GetFacingDirection();
        rb2D.linearVelocity = new Vector2(rollForce * dir, rb2D.linearVelocity.y);
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine() {
        animator.Play(ANIM_ROLL);
        yield return new WaitForSeconds(rollDuration);
        if (isGrounded) rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        if (currentState == State.Rolling)
            ChangeState(isGrounded ? State.Idle : State.Jumping);
    }

    // ================================================================== //
    //  BLOQUEIO
    // ================================================================== //

    protected void TryBlock() {
        if (IsAttacking) return;
        if (currentState != State.Idle && currentState != State.Running) return;
        ChangeState(State.Blocking);
    }

    protected void ExitBlocking() => ChangeState(State.Idle);

    // ================================================================== //
    //  CURA (Estus)
    // ================================================================== //

    protected void TryHeal() {
        if (currentState != State.Idle && currentState != State.Running) return;
        if (currentEstusCharges <= 0) return;
        if (currentHealth >= maxHealth) return;

        currentEstusCharges--;
        OnEstusChanged?.Invoke(currentEstusCharges, maxEstusCharges);
        ChangeState(State.Healing);
        StartCoroutine(HealRoutine());
    }

    private IEnumerator HealRoutine() {
        animator.Play(ANIM_HEAL);
        yield return new WaitForSeconds(estusDuration);
        RestoreHealth(estusHealAmount);
        if (currentState == State.Healing) ChangeState(State.Idle);
    }

    public void RefillEstus() {
        currentEstusCharges = maxEstusCharges;
        OnEstusChanged?.Invoke(currentEstusCharges, maxEstusCharges);
    }

    // ================================================================== //
    //  DANO E MORTE
    // ================================================================== //

    public virtual void TakeDamage(float amount) {
        if (currentState == State.Dead) return;
        if (currentState == State.Blocking) amount *= 0.2f;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) { Die(); return; }

        StopAllCoroutines();
        ChangeState(State.Hurt);
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine() {
        animator.Play(ANIM_HURT);
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        yield return new WaitForSeconds(hurtDuration);
        if (currentState == State.Hurt) ChangeState(State.Idle);
    }

    protected virtual void Die() {
        StopAllCoroutines();
        ChangeState(State.Dead);
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(ANIM_DEATH);

        if (SoulManager.Instance != null)
            SoulManager.Instance.OnPlayerDied(transform.position);

        StartCoroutine(DeathAndRespawn());
    }

    private IEnumerator DeathAndRespawn() {
        yield return new WaitForSeconds(2f);
        Bonfire.RespawnPlayerAtCheckpoint(this);
    }

    public void RestoreHealth(float amount) {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentState == State.Dead && currentHealth > 0) {
            ChangeState(State.Idle);
            animator.Play(ANIM_IDLE);
        }
    }

    public void RestoreStamina(float amount) {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void ReduceMaxHealth(float amount) {
        maxHealth = Mathf.Max(10f, maxHealth - amount);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ================================================================== //
    //  STAMINA
    // ================================================================== //

    protected void ConsumeStamina(float amount) {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (currentStamina <= 0f)
            playerStats?.TriggerFatigue();
    }

    private void HandleStaminaRegen() {
        if (currentStamina >= maxStamina) return;
        if (staminaRegenTimer > 0f) { staminaRegenTimer -= Time.deltaTime; return; }
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ================================================================== //
    //  SETTERS / GETTERS DE DANO  (usados pelo PlayerStats)
    // ================================================================== //

    public void SetLightDamage(float v) => lightDamage = v;
    public void SetHeavyDamage(float v) => heavyDamage = v;
    public float GetLightDamage() => lightDamage;
    public float GetHeavyDamage() => heavyDamage;

    // ================================================================== //
    //  STATE MACHINE / ANIMAÇÃO
    // ================================================================== //

    protected void ChangeState(State newState) => currentState = newState;

    protected virtual void UpdateAnimation() {
        if (currentState == State.Rolling || IsAttacking) return;

        if (currentState == State.Idle || currentState == State.Running) {
            if (moveInput != 0f && isGrounded) ChangeState(State.Running);
            else if (moveInput == 0f && isGrounded) ChangeState(State.Idle);
        }

        bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                         && currentStamina > 0;

        switch (currentState) {
            case State.Idle: animator.speed = 1f; animator.Play(ANIM_IDLE); break;
            case State.Running: animator.speed = sprinting ? 1.5f : 1f; animator.Play(ANIM_RUN); break;
            case State.Blocking: animator.speed = 1f; animator.Play(ANIM_BLOCK); break;
            default: animator.speed = 1f; break;
        }
    }

    // ================================================================== //
    //  AUXILIARES
    // ================================================================== //

    protected float GetFacingDirection() => transform.localScale.x >= 0 ? 1f : -1f;

    protected virtual void FlipSprite() {
        if (IsAttacking || currentState == State.Blocking || currentState == State.Dead) return;
        float reference = (currentState == State.Jumping && moveInput != 0f)
            ? moveInput
            : rb2D.linearVelocity.x;
        if (reference < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (reference > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // ================================================================== //
    //  MENSAGENS DO PlayerCombat (via SendMessage)
    // ================================================================== //

    public void SetAttacking(bool value) => IsAttacking = value;
    public void EndAttack() { IsAttacking = false; ChangeState(State.Idle); }
    public void ReEnableInput() { if (currentState != State.Dead && currentState != State.Blocking) ChangeState(State.Idle); }
    public void EndHeal() => ChangeState(State.Idle);

    // Stubs de combo — mantidos para compatibilidade com Animation Events
    public void EnableCanContinueAttackCombo() { }
    public void DisableCanContinueAttackCombo() { }
    public void EnableComboWindow() { }
    public void DisableComboWindow() { }

    // ================================================================== //
    //  GIZMOS
    // ================================================================== //

    protected virtual void OnDrawGizmosSelected() {
        if (groundCheckTransform == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}