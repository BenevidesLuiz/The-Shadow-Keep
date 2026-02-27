using System.Collections;
using UnityEngine;

/// <summary>
/// SoulslikeKnight v3 — Reescrita com State Machine.
///
/// PROBLEMAS RESOLVIDOS:
///   - canReceiveInput nunca mais trava permanentemente
///   - Pulo funcional com timeout de seguranca (3s)
///   - Sem deslizamento: atrito manual ao soltar teclas
///   - Controle horizontal no ar
///   - Cada estado tem duracao propria; Animation Events sao opcionais
///   - Bloqueio libera corretamente ao soltar Q
///   - Gizmo muda de verde (chao) para vermelho (ar) em tempo real
/// </summary>
public class SoulslikeKnight : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  STATE MACHINE
    // ------------------------------------------------------------------ //
    private enum State { Idle, Running, Jumping, Rolling, Attacking, Blocking, Healing, Hurt, Dead }
    private State currentState = State.Idle;

    // ------------------------------------------------------------------ //
    //  Componentes
    // ------------------------------------------------------------------ //
    private Rigidbody2D rb2D;
    private Animator animator;

    // ------------------------------------------------------------------ //
    //  Nomes das animacoes
    // ------------------------------------------------------------------ //
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

    // ------------------------------------------------------------------ //
    //  Vida
    // ------------------------------------------------------------------ //
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    // ------------------------------------------------------------------ //
    //  Stamina
    // ------------------------------------------------------------------ //
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 1.2f;
    [SerializeField] private float staminaCostJump = 10f;
    [SerializeField] private float staminaCostRoll = 25f;
    [SerializeField] private float staminaCostRun = 6f;
    // staminaCost dividido em Light/Heavy (ver secao Ataque acima)
    private float currentStamina;
    private float staminaRegenTimer;

    // ------------------------------------------------------------------ //
    //  Movimento
    // ------------------------------------------------------------------ //
    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float airControl = 0.7f;
    private float moveInput;

    // ------------------------------------------------------------------ //
    //  Pulo
    // ------------------------------------------------------------------ //
    [Header("Pulo")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private float groundCheckRadius = 0.2f;
    private bool isGrounded;

    // ------------------------------------------------------------------ //
    //  Roll
    // ------------------------------------------------------------------ //
    [Header("Roll")]
    [SerializeField] private float rollForce = 11f;
    [SerializeField] private float rollDuration = 0.45f;

    // ------------------------------------------------------------------ //
    //  Ataque
    // ------------------------------------------------------------------ //
    [Header("Ataque Leve (P) - combo Attack1 > Attack2")]
    [SerializeField] private float attack1Duration = 0.5f;
    [SerializeField] private float attack2Duration = 0.5f;
    [SerializeField] private float comboWindowTime = 0.25f;
    [SerializeField] private float staminaCostLight = 12f;

    [Header("Ataque Forte (U) - golpe unico Attack3")]
    [SerializeField] private float attack3Duration = 0.7f;
    [SerializeField] private float staminaCostHeavy = 30f;
    [SerializeField] private float lightDamage = 10f;
    [SerializeField] private float heavyDamage = 16f;

    private int lightComboStep;
    private bool comboWindowOpen;

    // ------------------------------------------------------------------ //
    //  Heal
    // ------------------------------------------------------------------ //
    [Header("Heal")]
    [SerializeField] private float healDuration = 1.0f;
    [SerializeField] private float healAmount = 30f;

    // ------------------------------------------------------------------ //
    //  Hurt
    // ------------------------------------------------------------------ //
    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.6f;

    // ------------------------------------------------------------------ //
    //  Eventos para a UI
    // ------------------------------------------------------------------ //
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnStaminaChanged;

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
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
        moveInput = 0f;
        if (Input.GetKey(KeyCode.A)) moveInput = -1f;
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.E)) TryRoll();

        if (Input.GetKeyDown(KeyCode.Q)) TryBlock();
        if (Input.GetKeyUp(KeyCode.Q) && currentState == State.Blocking) ExitBlocking();

        if (Input.GetKeyDown(KeyCode.P)) TryLightAttack();
        if (Input.GetKeyDown(KeyCode.U)) TryHeavyAttack();
        if (Input.GetKeyDown(KeyCode.O)) TryHeal();
    }

    // ==================================================================== //
    //  MOVIMENTO
    // ==================================================================== //

    private void ApplyMovement()
    {
        switch (currentState)
        {
            case State.Rolling:
                return; // coroutine de roll controla o X

            case State.Blocking:
            case State.Hurt:
                rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
                return;

            case State.Dead:
                rb2D.linearVelocity = Vector2.zero;
                return;

            case State.Jumping:
                // NO AR: conserva momentum do pulo; ajuste leve de direcao
                if (moveInput != 0f)
                {
                    // Velocidade alvo = velocidade de corrida na direcao pressionada
                    bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    float airTarget = moveInput * (shiftHeld ? runSpeed : walkSpeed);
                    // Lerp suave — airControl define o quanto pode redirecionar no ar
                    float newX = Mathf.Lerp(rb2D.linearVelocity.x, airTarget, airControl * 6f * Time.fixedDeltaTime);
                    rb2D.linearVelocity = new Vector2(newX, rb2D.linearVelocity.y);
                }
                // Sem input no ar: momentum continua intacto (nao freia)
                return;
        }

        // Estados no chao: Idle, Running, Attacking, Healing
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
            // Para imediatamente no chao — sem deslizamento
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

        // Calcula impulso horizontal no momento do pulo (Dark Souls style)
        bool isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                           && currentStamina > 0;
        float horizSpeed = 0f;
        if (moveInput != 0f && isSprinting) horizSpeed = moveInput * runSpeed;
        else if (moveInput != 0f) horizSpeed = moveInput * walkSpeed;
        // Parado: pulo vertical puro (sem impulso horizontal)

        rb2D.linearVelocity = new Vector2(horizSpeed, jumpForce);

        // IMPORTANTE: nao bloqueia input — o estado continua Idle/Running
        // O personagem esta "no ar" mas pode se mover, rolar, atacar normalmente
        // A animacao de pulo é tocada mas o estado so muda se for rolar/pousar
        animator.Play(JUMP);

        // Inicia monitoramento de pouso em background — nao trava nenhum estado
        StartCoroutine(LandingWatcher());
    }

    private IEnumerator LandingWatcher()
    {
        // Buffer: espera sair do chao antes de monitorar o pouso
        yield return new WaitForSeconds(0.15f);

        // Aguarda pousar — timeout de 4s como failsafe
        float elapsed = 0f;
        while (!isGrounded && elapsed < 4f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ao pousar: se estava pulando/rolando no ar, volta ao estado correto
        if (currentState == State.Jumping || currentState == State.Rolling)
            ChangeState(State.Idle);

        // Toca Idle/Run dependendo do input (UpdateAnimation cuida disso)
    }

    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheckTransform.position,
            groundCheckRadius,
            groundLayer);
    }

    // ==================================================================== //
    //  ROLL
    // ==================================================================== //

    private void TryRoll()
    {
        // Pode rolar em qualquer estado exceto Morto, Bloqueando ou sofrendo dano
        bool blocked = currentState == State.Dead ||
                       currentState == State.Blocking ||
                       currentState == State.Hurt;
        if (blocked) return;
        if (currentStamina < staminaCostRoll) return;

        StopAllCoroutines();
        ChangeState(State.Rolling);
        ConsumeStamina(staminaCostRoll);

        // Direcao: para onde esta se movendo, ou para onde esta virado se parado
        float dir = moveInput != 0f ? moveInput : GetFacingDirection();

        // Roll no ar: mantem velocidade vertical para nao cortar o arco do pulo
        // Roll no chao: velocidade Y zerada para nao voar
        float yVel = isGrounded ? rb2D.linearVelocity.y : rb2D.linearVelocity.y;
        rb2D.linearVelocity = new Vector2(rollForce * dir, yVel);

        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        animator.Play(ROLL);

        // Aguarda o roll terminar
        yield return new WaitForSeconds(rollDuration);

        // So freia se estiver no chao ao terminar; no ar deixa a gravidade agir
        if (isGrounded)
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

        if (currentState == State.Rolling)
            ChangeState(isGrounded ? State.Idle : State.Jumping);
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

    // ---------- LEVE (P): Attack1 -> Attack2 ----------

    private void TryLightAttack()
    {
        bool canAttack = currentState == State.Idle ||
                         currentState == State.Running ||
                         (currentState == State.Attacking && comboWindowOpen);
        if (!canAttack) return;
        if (currentStamina < staminaCostLight) return;

        StopAllCoroutines();
        comboWindowOpen = false;
        ChangeState(State.Attacking);
        ConsumeStamina(staminaCostLight);
        StartCoroutine(LightAttackRoutine());
    }

    private IEnumerator LightAttackRoutine()
    {
        // Alterna entre Attack1 e Attack2 no combo
        string anim = (lightComboStep == 0) ? ATTACK1 : ATTACK2;
        float dur = (lightComboStep == 0) ? attack1Duration : attack2Duration;
        lightComboStep = (lightComboStep == 0) ? 1 : 0; // alterna para proximo

        animator.Play(anim);

        // Abre janela de combo na metade da animacao
        yield return new WaitForSeconds(dur * 0.55f);
        comboWindowOpen = true;

        // Aguarda fim da animacao + janela extra
        yield return new WaitForSeconds(dur * 0.45f + comboWindowTime);

        if (currentState == State.Attacking)
        {
            lightComboStep = 0;
            comboWindowOpen = false;
            ChangeState(State.Idle);
        }
    }

    // ---------- FORTE (U): Attack3 golpe unico ----------

    private void TryHeavyAttack()
    {
        bool canAttack = currentState == State.Idle ||
                         currentState == State.Running ||
                         (currentState == State.Attacking && comboWindowOpen);
        if (!canAttack) return;
        if (currentStamina < staminaCostHeavy) return;

        StopAllCoroutines();
        comboWindowOpen = false;
        lightComboStep = 0; // reset do combo leve ao usar forte
        ChangeState(State.Attacking);
        ConsumeStamina(staminaCostHeavy);
        StartCoroutine(HeavyAttackRoutine());
    }

    private IEnumerator HeavyAttackRoutine()
    {
        animator.Play(ATTACK3);

        yield return new WaitForSeconds(attack3Duration);

        if (currentState == State.Attacking)
        {
            comboWindowOpen = false;
            ChangeState(State.Idle);
        }
    }

    // ==================================================================== //
    //  HEAL
    // ==================================================================== //

    private void TryHeal()
    {
        if (currentState != State.Idle && currentState != State.Running) return;
        ChangeState(State.Healing);
        StartCoroutine(HealRoutine());
    }

    private IEnumerator HealRoutine()
    {
        animator.Play(HEAL);
        yield return new WaitForSeconds(healDuration);
        RestoreHealth(healAmount);
        if (currentState == State.Healing) ChangeState(State.Idle);
    }

    // ==================================================================== //
    //  DANO E MORTE — chamados por inimigos/projéteis
    // ==================================================================== //

    public void TakeDamage(float amount)
    {
        if (currentState == State.Dead) return;

        if (currentState == State.Blocking) amount *= 0.2f; // bloqueia 80%

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

        // Aguarda animacao de morte e respawna no checkpoint
        StartCoroutine(DeathAndRespawn());
    }

    private IEnumerator DeathAndRespawn()
    {
        // Aguarda animacao de morte terminar
        yield return new WaitForSeconds(2f);

        // Respawna no ultimo checkpoint (fogueira)
        Bonfire.RespawnPlayerAtCheckpoint(this);
    }

    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Se estava morto e recebeu cura (respawn da fogueira), revive
        if (currentState == State.Dead && currentHealth > 0)
        {
            currentState = State.Idle;
            animator.Play("Idle");
        }
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ==================================================================== //
    //  STAMINA
    // ==================================================================== //

    private void ConsumeStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void HandleStaminaRegen()
    {
        if (currentStamina >= maxStamina) return;
        if (staminaRegenTimer > 0f) { staminaRegenTimer -= Time.deltaTime; return; }
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ==================================================================== //
    //  STATE MACHINE
    // ==================================================================== //

    private void ChangeState(State newState) => currentState = newState;

    // ==================================================================== //
    //  ANIMACAO — controlada pelo estado
    // ==================================================================== //

    private void UpdateAnimation()
    {
        // Roll e Ataque tocam a propria animacao nas coroutines
        if (currentState == State.Rolling || currentState == State.Attacking) return;

        // Atualiza Idle <-> Running automaticamente
        if (currentState == State.Idle || currentState == State.Running)
        {
            if (moveInput != 0f && isGrounded) ChangeState(State.Running);
            else if (moveInput == 0f && isGrounded) ChangeState(State.Idle);
        }

        bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                         && currentStamina > 0;

        switch (currentState)
        {
            case State.Idle:
                animator.speed = 1f;
                animator.Play(IDLE);
                break;
            case State.Running:
                animator.speed = sprinting ? 1.5f : 1f;
                animator.Play(RUN);
                break;
            case State.Jumping:
                animator.speed = 1f;
                // Animacao ja disparada na coroutine
                break;
            case State.Blocking:
                animator.speed = 1f;
                animator.Play(BLOCK);
                break;
            case State.Healing:
                animator.speed = 1f;
                break;
            case State.Hurt:
                animator.speed = 1f;
                break;
            case State.Dead:
                animator.speed = 1f;
                break;
        }
    }

    // ==================================================================== //
    //  AUXILIARES
    // ==================================================================== //

    private float GetFacingDirection() => transform.localScale.x >= 0 ? 1f : -1f;

    /// <summary> Retorna o dano do ataque leve. Usar no script de Hitbox. </summary>
    public float GetLightDamage() => lightDamage;

    /// <summary> Retorna o dano do ataque forte. Usar no script de Hitbox. </summary>
    public float GetHeavyDamage() => heavyDamage;

    /// <summary> True enquanto o knight esta no estado de ataque. Usado pelo PlayerHitbox. </summary>
    public bool IsAttacking => currentState == State.Attacking;

    private void FlipSprite()
    {
        // Nao vira durante ataque, bloqueio ou morte
        if (currentState == State.Attacking ||
            currentState == State.Blocking ||
            currentState == State.Dead) return;

        // Usa moveInput no ar para virar antes de mudar velocidade (mais responsivo)
        float reference = (currentState == State.Jumping && moveInput != 0f)
                          ? moveInput
                          : rb2D.linearVelocity.x;

        if (reference < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (reference > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // ==================================================================== //
    //  ANIMATION EVENTS — aliases para compatibilidade com os clips do asset
    // ==================================================================== //

    public void ReEnableInput()
    {
        if (currentState != State.Dead && currentState != State.Blocking)
            ChangeState(State.Idle);
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