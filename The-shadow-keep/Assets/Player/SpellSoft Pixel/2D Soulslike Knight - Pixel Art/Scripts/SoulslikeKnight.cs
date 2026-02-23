using System.Collections;
using UnityEngine;

public class SoulslikeKnight : MonoBehaviour
{
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
    //  Atributos: Vida
    // ------------------------------------------------------------------ //
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead;

    // ------------------------------------------------------------------ //
    //  Atributos: Stamina
    // ------------------------------------------------------------------ //
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 12f;   // por segundo
    [SerializeField] private float staminaRegenDelay = 1.2f;  // segundos antes de comecar a regen
    [SerializeField] private float staminaCostRoll = 25f;
    [SerializeField] private float staminaCostRun = 8f;    // por segundo
    [SerializeField] private float staminaCostAttack = 15f;
    [SerializeField] private float staminaCostJump = 10f;
    private float currentStamina;
    private float staminaRegenTimer;

    // ------------------------------------------------------------------ //
    //  Movimento
    // ------------------------------------------------------------------ //
    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    private bool isMovingLeft;
    private bool isMovingRight;
    private bool isRunning;     // Shift = correr

    // ------------------------------------------------------------------ //
    //  Pulo
    // ------------------------------------------------------------------ //
    [Header("Pulo")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private float groundCheckRadius = 0.2f;
    private bool isGrounded;
    private bool isJumping;

    // ------------------------------------------------------------------ //
    //  Roll
    // ------------------------------------------------------------------ //
    [Header("Roll")]
    [SerializeField] private float rollForce = 12f;  // era 25, agora mais lento
    [SerializeField] private float rollDuration = 0.4f; // segundos que o roll dura
    private bool isRolling;

    // ------------------------------------------------------------------ //
    //  Bloqueio
    // ------------------------------------------------------------------ //
    private bool isHoldingBlock;

    // ------------------------------------------------------------------ //
    //  Ataque
    // ------------------------------------------------------------------ //
    private bool canContinueCombo;
    private int comboStep; // 0, 1, 2

    // ------------------------------------------------------------------ //
    //  Controle geral de input
    // ------------------------------------------------------------------ //
    private bool canReceiveInput;

    // ------------------------------------------------------------------ //
    //  Eventos (a UI assina esses eventos)
    // ------------------------------------------------------------------ //
    public event System.Action<float, float> OnHealthChanged;   // (atual, max)
    public event System.Action<float, float> OnStaminaChanged;  // (atual, max)

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        canReceiveInput = true;
        isDead = false;
    }

    private void Update()
    {
        if (isDead) return;

        ReadMoveInput();
        ReadJumpInput();
        ReadRollInput();
        ReadBlockInput();
        ReadAttackInput();
        ReadHealInput();

        HandleStaminaRegen();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        CheckIfGrounded();
        ApplyMovement();
    }

    // ==================================================================== //
    //  INPUT
    // ==================================================================== //

    private void ReadMoveInput()
    {
        if (!canReceiveInput) return;

        isMovingLeft = Input.GetKey(KeyCode.A);
        isMovingRight = Input.GetKey(KeyCode.D);

        // Shift (esquerdo ou direito) = correr
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void ReadJumpInput()
    {
        if (!canReceiveInput) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (!isGrounded) return;

        if (currentStamina < staminaCostJump)
        {
            // Sem stamina: nao pula
            return;
        }

        Jump();
    }

    private void ReadRollInput()
    {
        if (!canReceiveInput) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (currentStamina < staminaCostRoll)
        {
            return; // Sem stamina
        }

        Roll();
    }

    private void ReadBlockInput()
    {
        // Bloquear nao consome stamina mas impede movimento
        if (canReceiveInput && Input.GetKeyDown(KeyCode.Q))
        {
            StartBlock();
        }

        if (isHoldingBlock && Input.GetKeyUp(KeyCode.Q))
        {
            EndBlock();
        }
    }

    private void ReadAttackInput()
    {
        bool pressed = Input.GetKeyDown(KeyCode.P);
        if (!pressed) return;

        // Pode atacar quando: tem input livre OU esta em janela de combo
        bool canAttack = canReceiveInput || (!canReceiveInput && canContinueCombo);
        if (!canAttack) return;

        if (currentStamina < staminaCostAttack) return;

        Attack();
    }

    private void ReadHealInput()
    {
        if (!canReceiveInput) return;
        if (!Input.GetKeyDown(KeyCode.O)) return;

        Heal();
    }

    // ==================================================================== //
    //  MOVIMENTO
    // ==================================================================== //

    private void ApplyMovement()
    {
        if (isRolling || isJumping) return; // fisica do roll/jump controla o x
        if (!canReceiveInput) return;

        if (isMovingLeft || isMovingRight)
        {
            bool sprinting = isRunning && currentStamina > 0;
            float speed = sprinting ? runSpeed : walkSpeed;
            float dir = isMovingLeft ? -1f : 1f;

            rb2D.linearVelocity = new Vector2(dir * speed, rb2D.linearVelocity.y);

            // Consome stamina apenas ao correr (sprint)
            if (sprinting)
            {
                ConsumeStamina(staminaCostRun * Time.fixedDeltaTime);
            }

            // Ajusta a velocidade da animacao: mais rapida ao correr
            animator.speed = sprinting ? 1.5f : 1f;
            animator.Play(RUN);
        }
        else
        {
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
            animator.speed = 1f;
            animator.Play(IDLE);
        }
    }

    // ==================================================================== //
    //  PULO
    // ==================================================================== //

    private void Jump()
    {
        canReceiveInput = false;
        isJumping = true;

        ConsumeStamina(staminaCostJump);

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpForce);
        animator.Play(JUMP);

        StartCoroutine(WaitForLanding());
    }

    private IEnumerator WaitForLanding()
    {
        // Espera sair do chao antes de checar o retorno
        yield return new WaitForSeconds(0.15f);

        while (!isGrounded)
            yield return null;

        isJumping = false;
        canReceiveInput = true;
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

    private void Roll()
    {
        canReceiveInput = false;
        isRolling = true;

        ConsumeStamina(staminaCostRoll);

        float dir = GetFacingDirection();
        rb2D.linearVelocity = new Vector2(rollForce * dir, rb2D.linearVelocity.y);
        animator.Play(ROLL);

        StartCoroutine(EndRollAfter(rollDuration));
    }

    private IEnumerator EndRollAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Freia o roll gradualmente (para nao parar bruscamente)
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

        isRolling = false;
        canReceiveInput = true;
    }

    // ==================================================================== //
    //  BLOQUEAR
    // ==================================================================== //

    private void StartBlock()
    {
        isHoldingBlock = true;
        canReceiveInput = false;
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(BLOCK);
    }

    private void EndBlock()
    {
        isHoldingBlock = false;
        canReceiveInput = true;
    }

    // ==================================================================== //
    //  ATAQUE (combo de 3)
    // ==================================================================== //

    private void Attack()
    {
        canReceiveInput = false;
        canContinueCombo = false;

        ConsumeStamina(staminaCostAttack);

        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

        switch (comboStep)
        {
            case 0: animator.Play(ATTACK1); comboStep = 1; break;
            case 1: animator.Play(ATTACK2); comboStep = 2; break;
            case 2: animator.Play(ATTACK3); comboStep = 0; break;
        }
    }

    // Chamado por Animation Event no meio de cada animacao de ataque
    // para abrir a janela do combo.
    public void EnableComboWindow()
    {
        canContinueCombo = true;
    }

    // Chamado por Animation Event ao FINAL de cada animacao de ataque.
    public void EndAttack()
    {
        canContinueCombo = false;
        comboStep = 0;
        canReceiveInput = true;
    }

    // ==================================================================== //
    //  CURAR
    // ==================================================================== //

    private void Heal()
    {
        canReceiveInput = false;
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(HEAL);
        // Animation Event "EndHeal" deve ser adicionado no ultimo frame da animacao
    }

    // Chamado por Animation Event no final de Heal
    public void EndHeal()
    {
        canReceiveInput = true;
    }

    // ==================================================================== //
    //  DANO E MORTE (chamados externamente pelo sistema de combate inimigo)
    // ==================================================================== //

    /// <summary>
    /// Recebe dano. Chamar por scripts de projétil/inimigo.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        bool wasReceiving = canReceiveInput;
        canReceiveInput = false;
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(HURT);

        // Aguarda a animacao de Hurt terminar (ajuste o tempo conforme seu clip)
        yield return new WaitForSeconds(0.6f);

        if (!isDead)
            canReceiveInput = wasReceiving;
    }

    private void Die()
    {
        isDead = true;
        canReceiveInput = false;
        rb2D.linearVelocity = Vector2.zero;
        animator.Play(DEATH);
    }

    /// <summary>
    /// Cura o personagem. Pode ser chamado externamente tambem.
    /// </summary>
    public void RestoreHealth(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ==================================================================== //
    //  STAMINA
    // ==================================================================== //

    private void ConsumeStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay; // reseta o delay de regen
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void HandleStaminaRegen()
    {
        if (currentStamina >= maxStamina) return;

        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ==================================================================== //
    //  AUXILIARES
    // ==================================================================== //

    private float GetFacingDirection()
    {
        return transform.localScale.x >= 0 ? 1f : -1f;
    }

    private void FlipSprite()
    {
        float vx = rb2D.linearVelocity.x;
        if (vx < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (vx > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);
    }

  
    /// <summary> Animation Event: final de Heal, Attack1, Attack2, Attack3, Roll, Hurt. </summary>
    public void ReEnableInput()
    {
        canReceiveInput = true;
        canContinueCombo = false;
        isRolling = false;
        isJumping = false;
    }

    /// <summary> Animation Event: janela de combo (meio do clip de ataque). </summary>
    public void EnableCanContinueAttackCombo()
    {
        EnableComboWindow();
    }

    /// <summary> Animation Event: fecha a janela de combo (fim do clip de ataque). </summary>
    public void DisableCanContinueAttackCombo()
    {
        EndAttack();
    }

    // ==================================================================== //
    //  GIZMOS (visualiza o sensor de chao no editor)
    // ==================================================================== //
    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}