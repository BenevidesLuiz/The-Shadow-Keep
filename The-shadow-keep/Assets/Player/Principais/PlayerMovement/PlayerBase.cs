using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static PlayerStats;

/// <summary>
/// PlayerBase — Classe base abstrata para todos os personagens jogáveis.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class PlayerBase : MonoBehaviour {

    // ------------------------------------------------------------------ //
    //  Estado interno
    // ------------------------------------------------------------------ //
    public enum State { Idle, Running, Jumping, Rolling, Blocking, Healing, Hurt, Dead }
    protected State currentState = State.Idle;

    public State CurrentPlayerState => currentState;

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
    protected const string ANIM_ATTACK_LIGHT = "Attack1";
    protected const string ANIM_ATTACK_SPECIAL1 = "Attack2";
    protected const string ANIM_ATTACK_SPECIAL2 = "Attack3";


    //  Inspector — Vida
    [Header("Vida")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;

    [HideInInspector] public string playerName;


    //  Inspector — Stamina
    [Header("Stamina")]
    [SerializeField] protected float maxStamina = 100f;
    [SerializeField] protected float staminaRegenRate = 15f;
    [SerializeField] protected float staminaRegenDelay = 1.2f;
    [SerializeField] protected float staminaCostJump = 10f;
    [SerializeField] protected float staminaCostRoll = 25f;
    [SerializeField] protected float staminaCostRun = 6f;
    protected float currentStamina;
    protected float staminaRegenTimer;

    //  Inspector — Estus
    [Header("Estus / Poção (tecla O)")]
    [SerializeField] protected int maxEstusCharges = 5;
    [SerializeField] protected float estusHealAmount = 40f;
    [SerializeField] protected float estusDuration = 1.2f;
    protected int currentEstusCharges;

    //  Inspector — Movimento
    [Header("Movimento")]
    [SerializeField] protected float walkSpeed = 4f;
    [SerializeField] protected float runSpeed = 8f;
    [SerializeField] protected float airControl = 0.7f;
    protected float moveInput;

    //  Inspector — Pulo
    [Header("Pulo")]
    [SerializeField] protected float jumpForce = 16f;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected Transform groundCheckTransform;
    [SerializeField] protected float groundCheckRadius = 0.4f;
    protected bool isGrounded;

    //  Inspector — Roll
    [Header("Roll")]
    [SerializeField] protected float rollForce = 11f;
    [SerializeField] protected float rollDuration = 0.45f;

    //  Inspector — Custo de stamina nos ataques
    [Header("Custo de Stamina nos Ataques")]
    [SerializeField] protected float staminaCostLight = 12f;
    [SerializeField] protected float staminaCostHeavy = 30f;

    //  Inspector — Dano base
    [Header("Dano")]
    [SerializeField] protected float lightDamage = 10f;
    [SerializeField] protected float heavyDamage = 16f;

    //  Inspector — Dano de Queda
    [Header("Dano de Queda")]
    [SerializeField] protected float maxFallSpeedSafe = 18f; 
    [SerializeField] protected float fallDamageMultiplier = 2.5f; 

    //  Inspector — Hurt
    [Header("Hurt")]
    [SerializeField] protected float hurtDuration = 0.6f;

    //  Inspector — Objetos de Combate
    [Header("Hitbox de Combate do Jogador")]
    public GameObject hitboxArmaObjeto;

    [SerializeField] protected int maxJumps = 2; 
    protected int jumpCount = 0;

    protected virtual void OnCollisionEnter2D(Collision2D collision) {
        if (currentState == State.Dead) return;

        // Verifica se o objeto em que batemos faz parte da sua groundLayer
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) {
        
            // Pega a força do impacto vertical
            float impactForce = Mathf.Abs(collision.relativeVelocity.y);

            // Se a força for maior que o limite seguro, calcula e aplica o dano
            if (impactForce > maxFallSpeedSafe) {
                float extraForce = impactForce - maxFallSpeedSafe;
                float damage = extraForce * fallDamageMultiplier;

                Debug.Log($"[Dano de Queda] Força: {impactForce} | Dano recebido: {damage}");

                TakeDamage(damage);
            }
        }
    }

    protected bool TryAttackLight() {
        if (currentState == State.Dead || currentState == State.Blocking || currentState == State.Rolling) return false;
        if (IsAttacking) return false;
        if (currentStamina < staminaCostLight) return false;

        ConsumeStamina(staminaCostLight);
        playerStats?.OnAttackPerformed();

        IsAttacking = true;
        animator.Play(ANIM_ATTACK_LIGHT);
        StartCoroutine(ResetAttackRoutine());
        return true; // Retorna VERDADEIRO se o golpe saiu
    }

    protected bool TryAttackSpecial1() {
        if (currentState == State.Dead || currentState == State.Blocking || currentState == State.Rolling) return false;
        if (IsAttacking) return false;
        if (currentStamina < staminaCostHeavy) return false;

        ConsumeStamina(staminaCostHeavy);
        playerStats?.OnAttackPerformed();

        IsAttacking = true;
        animator.Play(ANIM_ATTACK_SPECIAL1);
        StartCoroutine(ResetAttackRoutine());
        return true;
    }

    protected bool TryAttackSpecial2() {
        if (currentState == State.Dead || currentState == State.Blocking || currentState == State.Rolling) return false;
        if (IsAttacking) return false;
        if (currentStamina < staminaCostHeavy) return false;

        ConsumeStamina(staminaCostHeavy);
        playerStats?.OnAttackPerformed();

        IsAttacking = true;
        animator.Play(ANIM_ATTACK_SPECIAL2);
        StartCoroutine(ResetAttackRoutine());
        return true;
    }


    private IEnumerator ResetAttackRoutine() {
        yield return null;

        float duration = 0.5f;
        if (animator.GetCurrentAnimatorClipInfo(0).Length > 0) {
            duration = animator.GetCurrentAnimatorStateInfo(0).length;
        }

        yield return new WaitForSeconds(duration);

        IsAttacking = false;

        // Garante que se o ataque acabar sem eventos de animação, a hitbox seja desligada por segurança
        DesligarHitboxJogador();

        if (currentState != State.Dead && currentState != State.Blocking && currentState != State.Rolling) {
            ChangeState(State.Idle);
            animator.Play(ANIM_IDLE);
        }
    }

    //  Ref ao PlayerStats
    protected PlayerStats playerStats;


    //  PROPRIEDADES PÚBLICAS
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public int CurrentEstusCharges => currentEstusCharges;
    public int MaxEstusCharges => maxEstusCharges;

    public bool IsHealing => currentState == State.Healing;
    public bool IsWarrior => this is SoulslikeKnight;
    public bool IsAttacking { get; protected set; }

    public float heightMax = -10;

    // Eventos
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnStaminaChanged;
    public event System.Action<int, int> OnEstusChanged;

    
    protected virtual void Awake() {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
    }

    protected virtual void Start() {
        DesligarHitboxJogador();

        if (GameManager.Instance != null && GameManager.Instance.pendingLoad != null && !GameManager.Instance.shouldLoad) {
            this.playerName = GameManager.Instance.pendingLoad.playerName;
            maxHealth = GameManager.Instance.pendingLoad.currentHealth;
            maxStamina = GameManager.Instance.pendingLoad.currentStamina;

            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentEstusCharges = maxEstusCharges;
            currentState = State.Idle;

            if (playerStats != null) {
                playerStats.level = GameManager.Instance.pendingLoad.level;
                playerStats.strength = GameManager.Instance.pendingLoad.strength;
                playerStats.bladeSharpness = GameManager.Instance.pendingLoad.bladeSharpness;
                playerStats.faith = GameManager.Instance.pendingLoad.faith;
                playerStats.currentClass = GameManager.Instance.pendingLoad.characterClass;
                playerStats.ApplyStatsToKnight();
            }

            if (playerStats != null && playerStats.currentClass == PlayerStats.CharacterClass.Paladin) {
                maxHealth += 20f;
                currentHealth = maxHealth;
                lightDamage -= 2f;
                heavyDamage += 4f;
                staminaCostLight += 5f;
                Debug.Log($"[PlayerBase] {this.playerName} inicializado com modificadores matemáticos de Paladino!");
            }

            SavePlayer();
            return;
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentEstusCharges = maxEstusCharges;
        currentState = State.Idle;
    }

    protected virtual void Update() {
        if (currentState == State.Dead) return;
        VerificationHeight();
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

    public void SavePlayer() {
        PlayerStats stats = GetComponent<PlayerStats>();
        SaveSystem.SavePlayer(this, stats);
    }

    public void LoadPlayer() {
        PlayerData data = SaveSystem.LoadPlayer();
        if (data == null) return;

        this.playerName = data.playerName;
        maxHealth = data.maxHealth;
        maxStamina = data.maxStamina;
        currentHealth = data.currentHealth;
        currentStamina = data.currentStamina;
        currentEstusCharges = data.currentEstusCharges;
        maxEstusCharges = data.maxEstusCharges;

        SetLightDamage(data.lightDamage);
        SetHeavyDamage(data.heavyDamage);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnEstusChanged?.Invoke(currentEstusCharges, maxEstusCharges);

        transform.position = new Vector3(data.position[0], data.position[1], 0f);

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null) {
            stats.level = data.level;
            stats.strength = data.strength;
            stats.bladeSharpness = data.bladeSharpness;
            stats.currentClass = data.characterClass;
            stats.faith = data.faith;
            stats.ApplyStatsToKnight();
        }

        if (SoulManager.Instance != null) {
            SoulManager.Instance.OnPlayerDied(Vector3.zero);
            SoulManager.Instance.AddSouls(data.souls);
        }

        Debug.Log($"[PlayerBase] Save carregado com sucesso para: {this.playerName} (Nível {data.level})");
    }

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

    protected abstract void ReadCombatInput();

    protected virtual void ApplyMovement() {
       
        if (currentState == State.Dead || currentState == State.Hurt) {
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
            return;
        }

        if (currentState == State.Rolling) {
            return;
        }

        if (currentState == State.Blocking) {
            float blockSpeed = walkSpeed * 0.5f; // Corta a velocidade pela metade
            rb2D.linearVelocity = new Vector2(moveInput * blockSpeed, rb2D.linearVelocity.y);
            return; 
        }

        if (currentState == State.Jumping) {
            if (moveInput != 0f) {
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float airTarget = moveInput * (shiftHeld ? runSpeed : walkSpeed);
                float newX = Mathf.Lerp(rb2D.linearVelocity.x, airTarget, airControl * 6f * Time.fixedDeltaTime);
                rb2D.linearVelocity = new Vector2(newX, rb2D.linearVelocity.y);
            }
            return;
        }

        // 5. Movimento Livre no Chão (Idle / Run)
        if (moveInput != 0f) {
            bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentStamina > 0;
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

    protected void TryJump() {
     
        if (jumpCount >= maxJumps) return;

        if (IsAttacking) return;

        if (currentState != State.Idle && currentState != State.Running && currentState != State.Jumping) return;

        if (currentStamina < staminaCostJump) return;

        StopAllCoroutines();
        ConsumeStamina(staminaCostJump);

        jumpCount++;

        bool isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentStamina > 0;
        float horizSpeed = 0f;
        if (moveInput != 0f && isSprinting) horizSpeed = moveInput * runSpeed;
        else if (moveInput != 0f) horizSpeed = moveInput * walkSpeed;

        rb2D.linearVelocity = new Vector2(horizSpeed, jumpForce);
        ChangeState(State.Jumping);

        animator.Play(ANIM_JUMP, -1, 0f);

        StartCoroutine(LandingWatcher());
    }

    public void VerificationHeight() {
        if (currentState == State.Dead) return;

        float height = transform.position.y;
        if (height < heightMax) {
            Debug.Log("Caiu no abismo!");
            Die();
        }
    }

    private IEnumerator LandingWatcher() {
        yield return new WaitForSeconds(0.15f);
        float elapsed = 0f;
        while (!isGrounded && elapsed < 4f) { elapsed += Time.deltaTime; yield return null; }
        if (currentState == State.Jumping || currentState == State.Rolling) ChangeState(State.Idle);
    }

    protected void CheckIfGrounded() {
        if (groundCheckTransform == null) {
            Debug.LogError($"Falta arrastar o Ground Check Transform no script de {gameObject.name}!");
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, groundCheckRadius, groundLayer);

        if (isGrounded) {
            jumpCount = 0;
        }
    }

    protected void TryRoll() {
        if (currentState == State.Dead || currentState == State.Blocking ||
            currentState == State.Hurt || currentState == State.Rolling) return;
        if (currentStamina < staminaCostRoll) return;

        StopAllCoroutines();
        IsAttacking = false;
        ChangeState(State.Rolling);
        ConsumeStamina(staminaCostRoll);

        float dir = moveInput != 0f ? moveInput : GetFacingDirection();
        rb2D.linearVelocity = new Vector2(rollForce * dir, rb2D.linearVelocity.y);
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine() {
        animator.Play(ANIM_ROLL);
        yield return new WaitForSeconds(rollDuration);

        if (currentState == State.Rolling) {
            if (isGrounded) {
                rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

                if (moveInput != 0f) {
                    ChangeState(State.Running);
                    animator.Play(ANIM_RUN);
                }
                else {
                    ChangeState(State.Idle);
                    animator.Play(ANIM_IDLE);
                }
            }
            else {
                ChangeState(State.Jumping);
                animator.Play(ANIM_JUMP);
            }
        }
    }

    protected void TryBlock() {
        if (IsAttacking) return;
        if (currentState != State.Idle && currentState != State.Running) return;

        ChangeState(State.Blocking);
        animator.Play(ANIM_BLOCK);
    }

    protected void ExitBlocking() {
        ChangeState(State.Idle);
        animator.Play(ANIM_IDLE); 
    }

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

    public virtual void TakeDamage(float amount) {
        if (currentState == State.Dead) return;

        if (currentState == State.Blocking) {
            amount *= 0.2f; 

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f) { Die(); }

            return; 
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) { Die(); return; }

        IsAttacking = false;
        DesligarHitboxJogador();

        StopAllCoroutines();
        ChangeState(State.Hurt);
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine() {
        animator.Play(ANIM_HURT);
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

        yield return new WaitForSeconds(hurtDuration);

        if (currentState == State.Hurt) {
            ChangeState(State.Idle);
            animator.Play(ANIM_IDLE); 
        }
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

        SavePlayer();

        string cenaAtual = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetString("LastScene", cenaAtual);
        PlayerPrefs.Save();

        if (GameManager.Instance != null) {
            GameManager.Instance.currentScene = cenaAtual;
            GameManager.Instance.GoToSceneInstant("Morte");
        }
        else {
            Debug.LogWarning("[PlayerBase] Modo de teste: GameManager ausente. Carregando a cena 'Morte' direto!");
            SceneManager.LoadScene("Morte");
        }
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

    public void SetLightDamage(float v) => lightDamage = v;
    public void SetHeavyDamage(float v) => heavyDamage = v;
    public float GetLightDamage() => lightDamage;
    public float GetHeavyDamage() => heavyDamage;

    protected void ChangeState(State newState) => currentState = newState;

    protected virtual void UpdateAnimation() {
        if (animator == null) return;

        if (currentState == State.Rolling || currentState == State.Blocking ||
            currentState == State.Healing || currentState == State.Hurt ||
            currentState == State.Dead || IsAttacking) {
            return;
        }

        if (isGrounded) {
            if (moveInput != 0f) {
                if (currentState != State.Running) {
                    ChangeState(State.Running);
                    animator.Play(ANIM_RUN);
                }
                bool sprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentStamina > 0;
                animator.speed = sprinting ? 1.2f : 1f;
            }
            else {
                if (currentState != State.Idle) {
                    ChangeState(State.Idle);
                    animator.Play(ANIM_IDLE);
                    animator.speed = 1f;
                }
            }
        }
        else {
            if (currentState != State.Jumping) {
                ChangeState(State.Jumping);
                animator.Play(ANIM_JUMP);
                animator.speed = 1f;
            }
        }
    }
    public void SetCoreStatsFromMenu(float newMaxHealth, float newMaxStamina) {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        maxStamina = newMaxStamina;
        currentStamina = maxStamina;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    protected float GetFacingDirection() => transform.localScale.x >= 0 ? 1f : -1f;

    protected virtual void FlipSprite() {
        if (IsAttacking || currentState == State.Blocking || currentState == State.Dead) return;
        float reference = (currentState == State.Jumping && moveInput != 0f) ? moveInput : rb2D.linearVelocity.x;
        if (reference < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (reference > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
    }

    //  MENSAGENS E EVENTOS DE ANIMAÇÃO (Lógica da Lâmina/Hitbox)
  
    public void SetAttacking(bool value) => IsAttacking = value;
    public void EndAttack() { IsAttacking = false; ChangeState(State.Idle); }
    public void ReEnableInput() { if (currentState != State.Dead && currentState != State.Blocking) ChangeState(State.Idle); }
    public void EndHeal() => ChangeState(State.Idle);

    public void LigarHitboxJogador() {
        if (hitboxArmaObjeto != null) hitboxArmaObjeto.SetActive(true);
    }

    public void DesligarHitboxJogador() {
        if (hitboxArmaObjeto != null) hitboxArmaObjeto.SetActive(false);
    }

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